using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace Pondskater
{
    public class HamiltonianPath_Component : GH_Component
    {
        private const int MaxDpVertices = 23;
        private const int BeamWidth = 64;
        private const int BeamMilliseconds = 200;

        public HamiltonianPath_Component()
          : base("Hamiltonian Path", "H_Path",
              "Finds a Hamiltonian vertex path in a connected curve network. Uses exact dynamic programming for small graphs and beam search for larger graphs.",
              "Pondskater", "Paths")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "Connected curve network. Curve endpoints define graph vertices.", GH_ParamAccess.list);
            pManager.AddPointParameter("Start", "S", "Start point. The closest graph vertex is used.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Use Exact", "E", "If true, use exact dynamic programming up to 23 vertices, then fall back to beam search. If false, use beam search directly.", GH_ParamAccess.item, true);
            pManager.AddNumberParameter("Tolerance", "T", "Endpoint snap tolerance.", GH_ParamAccess.item, RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-6);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "Hamiltonian vertex order from start to end.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Path", "C", "Polyline through the Hamiltonian vertex order.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Full", "F", "True if the returned path visits every vertex in the start component.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var curves = new List<Curve>();
            Point3d start = Point3d.Unset;
            bool useExact = true;
            double tolerance = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-6;

            if (!DA.GetDataList(0, curves)) return;
            if (!DA.GetData(1, ref start)) return;
            if (!DA.GetData(2, ref useExact)) return;
            if (!DA.GetData(3, ref tolerance)) return;

            if (curves.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Provide at least one curve.");
                DA.SetData(2, false);
                return;
            }

            if (!start.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Start point is invalid.");
                DA.SetData(2, false);
                return;
            }

            if (tolerance <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Tolerance must be greater than zero.");
                DA.SetData(2, false);
                return;
            }

            GraphData graph = BuildGraph(curves, tolerance);
            if (graph.Points.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No usable graph vertices were found.");
                DA.SetData(2, false);
                return;
            }

            int startIndex = FindStartIndex(graph, start, tolerance);
            if (startIndex < 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not match the start point to any graph vertex.");
                DA.SetData(2, false);
                return;
            }

            List<int> component = GetComponent(graph.AdjacencySets, startIndex);
            if (component.Count < graph.Points.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"Graph is disconnected. Using only the start component: {component.Count}/{graph.Points.Count} vertices.");
            }

            CompactGraph compact = CompactToComponent(graph, component, startIndex);
            if (compact.Points.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The start component is empty.");
                DA.SetData(2, false);
                return;
            }

            bool usedDp = false;
            bool solved = false;
            List<int> indexPath;

            if (useExact && compact.Points.Count <= MaxDpVertices)
            {
                usedDp = true;
                indexPath = HamiltonianDpBitset(compact.Adjacency, compact.StartIndex, out solved);
                if (!solved)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        "Exact DP found no Hamiltonian path from the selected start. Falling back to beam search.");
                    indexPath = HamiltonianPathBeam(compact.Adjacency, compact.StartIndex, BeamWidth, BeamMilliseconds);
                }
            }
            else
            {
                if (useExact && compact.Points.Count > MaxDpVertices)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                        $"N={compact.Points.Count} exceeds the exact DP cap ({MaxDpVertices}). Using beam search.");
                }
                indexPath = HamiltonianPathBeam(compact.Adjacency, compact.StartIndex, BeamWidth, BeamMilliseconds);
            }

            if (indexPath == null || indexPath.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Solver returned an empty path.");
                DA.SetData(2, false);
                return;
            }

            bool full = indexPath.Count == compact.Points.Count;
            if (!full)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"Returned longest simple path found: {indexPath.Count}/{compact.Points.Count} vertices.");
            }
            else if (!usedDp)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Beam search found a full Hamiltonian path.");
            }

            var points = new List<Point3d>(indexPath.Count);
            foreach (int index in indexPath)
                points.Add(compact.Points[index]);

            DA.SetDataList(0, points);
            DA.SetData(1, new PolylineCurve(new Polyline(points)));
            DA.SetData(2, full);
        }

        protected override Bitmap Icon => IconLoader.Hamiltonian;

        public override Guid ComponentGuid
        {
            get { return new Guid("033B9CE7-87D5-4AF9-A679-F6144F7C65B6"); }
        }

        private static GraphData BuildGraph(List<Curve> curves, double tolerance)
        {
            var graph = new GraphData();

            int EnsureVertex(Point3d point)
            {
                string key = Key(point, tolerance);
                if (!graph.KeyToIndex.TryGetValue(key, out int index))
                {
                    index = graph.Points.Count;
                    graph.KeyToIndex[key] = index;
                    graph.Points.Add(point);
                    graph.AdjacencySets.Add(new HashSet<int>());
                }
                return index;
            }

            foreach (Curve curve in curves)
            {
                if (curve == null || !curve.IsValid)
                    continue;

                int start = EnsureVertex(curve.PointAtStart);
                int end = EnsureVertex(curve.PointAtEnd);
                if (start == end)
                    continue;

                graph.AdjacencySets[start].Add(end);
                graph.AdjacencySets[end].Add(start);
            }

            return graph;
        }

        private static int FindStartIndex(GraphData graph, Point3d start, double tolerance)
        {
            string startKey = Key(start, tolerance);
            if (graph.KeyToIndex.TryGetValue(startKey, out int index))
                return index;

            int bestIndex = -1;
            double bestDistance = double.MaxValue;
            for (int i = 0; i < graph.Points.Count; i++)
            {
                double distance = graph.Points[i].DistanceToSquared(start);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static CompactGraph CompactToComponent(GraphData graph, List<int> component, int startIndex)
        {
            var oldToNew = new Dictionary<int, int>(component.Count);
            var points = new List<Point3d>(component.Count);

            for (int i = 0; i < component.Count; i++)
            {
                oldToNew[component[i]] = i;
                points.Add(graph.Points[component[i]]);
            }

            var adjacency = new List<int>[points.Count];
            for (int i = 0; i < points.Count; i++)
                adjacency[i] = new List<int>();

            for (int old = 0; old < graph.Points.Count; old++)
            {
                if (!oldToNew.TryGetValue(old, out int newIndex))
                    continue;

                foreach (int oldNeighbor in graph.AdjacencySets[old])
                {
                    if (!oldToNew.TryGetValue(oldNeighbor, out int newNeighbor) || newIndex == newNeighbor)
                        continue;

                    adjacency[newIndex].Add(newNeighbor);
                }
            }

            for (int i = 0; i < adjacency.Length; i++)
                adjacency[i] = adjacency[i].Distinct().ToList();

            return new CompactGraph(points, adjacency, oldToNew[startIndex]);
        }

        private static List<int> HamiltonianDpBitset(List<int>[] adjacency, int startIndex, out bool solved)
        {
            solved = false;
            int vertexCount = adjacency.Length;
            int full = (1 << vertexCount) - 1;

            var dpEnds = new ulong[full + 1];
            var parent = new sbyte[vertexCount, full + 1];

            for (int vertex = 0; vertex < vertexCount; vertex++)
            {
                for (int mask = 0; mask <= full; mask++)
                    parent[vertex, mask] = -1;
            }

            dpEnds[1 << startIndex] = 1UL << startIndex;

            for (int mask = 1; mask <= full; mask++)
            {
                if ((mask & (1 << startIndex)) == 0)
                    continue;

                for (int end = 0; end < vertexCount; end++)
                {
                    if ((mask & (1 << end)) == 0)
                        continue;

                    if (mask == (1 << end))
                        continue;

                    int previousMask = mask ^ (1 << end);
                    ulong previousEnds = dpEnds[previousMask];
                    if (previousEnds == 0UL)
                        continue;

                    foreach (int previous in adjacency[end])
                    {
                        if ((previousMask & (1 << previous)) == 0)
                            continue;

                        if ((previousEnds & (1UL << previous)) != 0UL)
                        {
                            dpEnds[mask] |= 1UL << end;
                            if (parent[end, mask] == -1)
                                parent[end, mask] = (sbyte)previous;
                            break;
                        }
                    }
                }
            }

            int finalEnd = -1;
            ulong fullEnds = dpEnds[full];
            if (fullEnds != 0UL)
            {
                for (int vertex = 0; vertex < vertexCount; vertex++)
                {
                    if ((fullEnds & (1UL << vertex)) != 0UL)
                    {
                        finalEnd = vertex;
                        break;
                    }
                }
            }

            if (finalEnd == -1)
                return new List<int>();

            var indexPath = new List<int>(vertexCount);
            int current = finalEnd;
            int currentMask = full;

            while (current != -1)
            {
                indexPath.Add(current);
                int previous = parent[current, currentMask];
                currentMask ^= 1 << current;
                current = previous;
            }

            indexPath.Reverse();
            solved = indexPath.Count == vertexCount;
            return indexPath;
        }

        private static List<int> HamiltonianPathBeam(List<int>[] adjacency, int startIndex, int beamWidth, int maxMilliseconds)
        {
            var stopwatch = Stopwatch.StartNew();
            int vertexCount = adjacency.Length;
            int timeBudget = Math.Max(10, maxMilliseconds);
            var random = new Random(1234567);

            var initialUsed = new HashSet<int> { startIndex };
            var candidates = new List<BeamCandidate>
            {
                new BeamCandidate(new List<int> { startIndex }, initialUsed)
            };

            var globalBest = new List<int> { startIndex };

            while (stopwatch.ElapsedMilliseconds < timeBudget)
            {
                foreach (BeamCandidate candidate in candidates)
                {
                    if (candidate.Path.Count == vertexCount)
                        return candidate.Path;

                    if (candidate.Path.Count > globalBest.Count)
                        globalBest = new List<int>(candidate.Path);
                }

                var next = new List<BeamCandidate>(beamWidth * 4);

                foreach (BeamCandidate candidate in candidates)
                {
                    List<int> path = candidate.Path;
                    HashSet<int> used = candidate.Used;
                    int head = path[0];
                    int tail = path[path.Count - 1];

                    List<int> headOptions = adjacency[head].Where(vertex => !used.Contains(vertex)).ToList();
                    List<int> tailOptions = adjacency[tail].Where(vertex => !used.Contains(vertex)).ToList();

                    headOptions.Sort((x, y) => CountOnward(adjacency[x], used).CompareTo(CountOnward(adjacency[y], used)));
                    tailOptions.Sort((x, y) => CountOnward(adjacency[x], used).CompareTo(CountOnward(adjacency[y], used)));

                    if (headOptions.Count > 1 && random.NextDouble() < 0.2)
                        SwapFirstTwo(headOptions);
                    if (tailOptions.Count > 1 && random.NextDouble() < 0.2)
                        SwapFirstTwo(tailOptions);

                    bool headFirst = headOptions.Count <= tailOptions.Count;
                    if (headFirst)
                    {
                        ExtendHead(next, path, used, headOptions);
                        ExtendTail(next, path, used, tailOptions);
                    }
                    else
                    {
                        ExtendTail(next, path, used, tailOptions);
                        ExtendHead(next, path, used, headOptions);
                    }
                }

                if (next.Count == 0)
                    break;

                next.Sort((a, b) => Score(adjacency, a).CompareTo(Score(adjacency, b)));
                if (next.Count > beamWidth)
                    next.RemoveRange(beamWidth, next.Count - beamWidth);

                candidates = next;
            }

            return globalBest;
        }

        private static void ExtendHead(List<BeamCandidate> next, List<int> path, HashSet<int> used, List<int> options)
        {
            foreach (int vertex in options)
            {
                var nextPath = new List<int>(path.Count + 1) { vertex };
                nextPath.AddRange(path);
                var nextUsed = new HashSet<int>(used) { vertex };
                next.Add(new BeamCandidate(nextPath, nextUsed));
            }
        }

        private static void ExtendTail(List<BeamCandidate> next, List<int> path, HashSet<int> used, List<int> options)
        {
            foreach (int vertex in options)
            {
                var nextPath = new List<int>(path) { vertex };
                var nextUsed = new HashSet<int>(used) { vertex };
                next.Add(new BeamCandidate(nextPath, nextUsed));
            }
        }

        private static int Score(List<int>[] adjacency, BeamCandidate candidate)
        {
            int head = candidate.Path[0];
            int tail = candidate.Path[candidate.Path.Count - 1];
            return CountOnward(adjacency[head], candidate.Used) + CountOnward(adjacency[tail], candidate.Used);
        }

        private static int CountOnward(List<int> neighbors, HashSet<int> used)
        {
            int count = 0;
            for (int i = 0; i < neighbors.Count; i++)
            {
                if (!used.Contains(neighbors[i]))
                    count++;
            }
            return count;
        }

        private static void SwapFirstTwo(List<int> values)
        {
            int tmp = values[0];
            values[0] = values[1];
            values[1] = tmp;
        }

        private static string Key(Point3d point, double tolerance)
        {
            double x = Math.Round(point.X / tolerance) * tolerance;
            double y = Math.Round(point.Y / tolerance) * tolerance;
            double z = Math.Round(point.Z / tolerance) * tolerance;

            return x.ToString("R", CultureInfo.InvariantCulture) + "," +
                   y.ToString("R", CultureInfo.InvariantCulture) + "," +
                   z.ToString("R", CultureInfo.InvariantCulture);
        }

        private static List<int> GetComponent(List<HashSet<int>> adjacencySets, int startIndex)
        {
            var queue = new Queue<int>();
            var seen = new HashSet<int>();
            var component = new List<int>();

            queue.Enqueue(startIndex);
            seen.Add(startIndex);

            while (queue.Count > 0)
            {
                int vertex = queue.Dequeue();
                component.Add(vertex);

                foreach (int neighbor in adjacencySets[vertex])
                {
                    if (seen.Add(neighbor))
                        queue.Enqueue(neighbor);
                }
            }

            return component;
        }

        private sealed class GraphData
        {
            public Dictionary<string, int> KeyToIndex { get; } = new Dictionary<string, int>();
            public List<Point3d> Points { get; } = new List<Point3d>();
            public List<HashSet<int>> AdjacencySets { get; } = new List<HashSet<int>>();
        }

        private sealed class CompactGraph
        {
            public CompactGraph(List<Point3d> points, List<int>[] adjacency, int startIndex)
            {
                Points = points;
                Adjacency = adjacency;
                StartIndex = startIndex;
            }

            public List<Point3d> Points { get; }
            public List<int>[] Adjacency { get; }
            public int StartIndex { get; }
        }

        private sealed class BeamCandidate
        {
            public BeamCandidate(List<int> path, HashSet<int> used)
            {
                Path = path;
                Used = used;
            }

            public List<int> Path { get; }
            public HashSet<int> Used { get; }
        }
    }
}
