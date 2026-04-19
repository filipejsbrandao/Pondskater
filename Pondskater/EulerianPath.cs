using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace Pondskater
{
    public class EulerianPath : GH_Component
    {
        public EulerianPath()
          : base("Eulerian Path", "E_Path",
              "Finds an Eulerian path through a connected network of curves.",
              "Pondskater", "Paths")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "Connected curve network. Each curve is treated as one graph edge.", GH_ParamAccess.list);
            pManager.AddPointParameter("Start", "S", "Start point. The closest graph endpoint is used.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Tolerance", "T", "Endpoint merge tolerance.", GH_ParamAccess.item, RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-6);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Path", "P", "Eulerian path curves, oriented in traversal order.", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Exists", "E", "True when an Eulerian path exists from the selected start vertex.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var curves = new List<Curve>();
            Point3d start = Point3d.Unset;
            double tolerance = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-6;

            if (!DA.GetDataList(0, curves)) return;
            if (!DA.GetData(1, ref start)) return;
            if (!DA.GetData(2, ref tolerance)) return;

            if (curves.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Provide at least one curve.");
                DA.SetData(1, false);
                return;
            }

            if (!start.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Start point is invalid.");
                DA.SetData(1, false);
                return;
            }

            if (tolerance <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Tolerance must be greater than zero.");
                DA.SetData(1, false);
                return;
            }

            var graph = new CurveGraph(tolerance);
            foreach (Curve curve in curves)
            {
                if (curve == null || !curve.IsValid)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Ignored a null or invalid curve.");
                    continue;
                }

                if (curve.GetLength() <= tolerance)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Ignored a zero-length edge.");
                    continue;
                }

                graph.AddCurve(curve);
            }

            if (graph.EdgeCount == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No usable open curve edges were found.");
                DA.SetData(1, false);
                return;
            }

            graph.SortEdgesClockwise();
            int startVertex = graph.FindClosestVertex(start);

            if (!IsGraphConnected(graph, startVertex))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Graph is not connected from the selected start vertex.");
                DA.SetData(1, false);
                return;
            }

            List<int> oddVertices = graph.GetOddVertices();
            bool hasEulerianPath = oddVertices.Count == 0 || oddVertices.Count == 2;

            if (!hasEulerianPath)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Eulerian path exists. The graph must have 0 or 2 odd-degree vertices.");
                DA.SetData(1, false);
                return;
            }

            if (oddVertices.Count == 2 && !oddVertices.Contains(startVertex))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "For a graph with two odd-degree vertices, the start point must be closest to one of them.");
                DA.SetData(1, false);
                return;
            }

            List<Curve> path = FindEulerianPath(graph, startVertex);
            if (path.Count != graph.EdgeCount)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Path construction failed before using every edge.");
                DA.SetData(1, false);
                return;
            }

            DA.SetDataList(0, path);
            DA.SetData(1, true);
        }

        protected override Bitmap Icon => IconLoader.Eulerian;

        public override Guid ComponentGuid
        {
            get { return new Guid("2C36956E-0B26-4D23-9841-A9F0E9678F3E"); }
        }

        private static List<Curve> FindEulerianPath(CurveGraph graph, int startVertex)
        {
            var stack = new Stack<TraversalState>();
            stack.Push(new TraversalState(startVertex, -1, null));

            var usedEdges = new HashSet<int>();
            var orientedEdges = new List<OrientedEdge>();

            while (stack.Count > 0)
            {
                TraversalState state = stack.Peek();
                List<AdjacentEdge> edges = graph.AdjacencyList[state.Vertex];

                GraphEdge bestEdge = null;
                int bestNeighbor = -1;
                int bestScore = int.MaxValue;

                int lastIndex = -1;
                if (state.Incoming != null)
                    lastIndex = edges.FindIndex(e => e.Edge.Id == state.Incoming.Id);

                for (int i = 0; i < edges.Count; i++)
                {
                    AdjacentEdge candidate = edges[i];
                    if (usedEdges.Contains(candidate.Edge.Id))
                        continue;

                    int score = 0;
                    if (state.Incoming != null && lastIndex >= 0)
                    {
                        int degree = edges.Count;
                        int diff = Math.Abs(i - lastIndex);
                        score = Math.Min(diff, degree - diff);
                    }

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestEdge = candidate.Edge;
                        bestNeighbor = candidate.Neighbor;
                    }
                }

                if (bestEdge != null)
                {
                    usedEdges.Add(bestEdge.Id);
                    stack.Push(new TraversalState(bestNeighbor, state.Vertex, bestEdge));
                }
                else
                {
                    stack.Pop();
                    if (state.Incoming != null && state.Parent >= 0)
                        orientedEdges.Add(new OrientedEdge(state.Parent, state.Vertex, state.Incoming));
                }
            }

            orientedEdges.Reverse();

            var result = new List<Curve>(orientedEdges.Count);
            foreach (OrientedEdge edge in orientedEdges)
            {
                Point3d fromPoint = graph.Vertices[edge.From].Point;
                Curve curve = edge.Edge.Curve.DuplicateCurve();

                double startDistance = curve.PointAtStart.DistanceTo(fromPoint);
                double endDistance = curve.PointAtEnd.DistanceTo(fromPoint);
                if (endDistance + graph.Tolerance < startDistance)
                    curve.Reverse();

                result.Add(curve);
            }

            return result;
        }

        private static bool IsGraphConnected(CurveGraph graph, int startVertex)
        {
            var visited = new HashSet<int>();
            var stack = new Stack<int>();
            stack.Push(startVertex);

            while (stack.Count > 0)
            {
                int vertex = stack.Pop();
                if (!visited.Add(vertex))
                    continue;

                foreach (AdjacentEdge edge in graph.AdjacencyList[vertex])
                {
                    if (!visited.Contains(edge.Neighbor))
                        stack.Push(edge.Neighbor);
                }
            }

            return graph.AdjacencyList
                .Where(kvp => kvp.Value.Count > 0)
                .All(kvp => visited.Contains(kvp.Key));
        }

        private sealed class CurveGraph
        {
            private readonly List<GraphEdge> _edges = new List<GraphEdge>();

            public CurveGraph(double tolerance)
            {
                Tolerance = tolerance;
            }

            public double Tolerance { get; }
            public List<GraphVertex> Vertices { get; } = new List<GraphVertex>();
            public Dictionary<int, List<AdjacentEdge>> AdjacencyList { get; } = new Dictionary<int, List<AdjacentEdge>>();
            public int EdgeCount => _edges.Count;

            public void AddCurve(Curve curve)
            {
                int start = GetOrAddVertex(curve.PointAtStart);
                int end = GetOrAddVertex(curve.PointAtEnd);

                var edge = new GraphEdge(_edges.Count, start, end, curve);
                _edges.Add(edge);

                AdjacencyList[start].Add(new AdjacentEdge(end, edge));
                AdjacencyList[end].Add(new AdjacentEdge(start, edge));
            }

            public void SortEdgesClockwise()
            {
                foreach (int vertexId in AdjacencyList.Keys.ToList())
                {
                    Point3d vertexPoint = Vertices[vertexId].Point;
                    AdjacencyList[vertexId] = AdjacencyList[vertexId]
                        .OrderBy(edge => CalculateAngle(vertexId, vertexPoint, edge))
                        .ToList();
                }
            }

            public int FindClosestVertex(Point3d point)
            {
                int closest = -1;
                double bestDistance = double.MaxValue;

                for (int i = 0; i < Vertices.Count; i++)
                {
                    double distance = Vertices[i].Point.DistanceTo(point);
                    if (distance < bestDistance)
                    {
                        closest = i;
                        bestDistance = distance;
                    }
                }

                return closest;
            }

            public List<int> GetOddVertices()
            {
                return AdjacencyList
                    .Where(kvp => kvp.Value.Count % 2 != 0)
                    .Select(kvp => kvp.Key)
                    .ToList();
            }

            private int GetOrAddVertex(Point3d point)
            {
                for (int i = 0; i < Vertices.Count; i++)
                {
                    if (Vertices[i].Point.DistanceTo(point) <= Tolerance)
                        return i;
                }

                int id = Vertices.Count;
                Vertices.Add(new GraphVertex(id, point));
                AdjacencyList[id] = new List<AdjacentEdge>();
                return id;
            }

            private double CalculateAngle(int vertexId, Point3d origin, AdjacentEdge adjacent)
            {
                Curve curve = adjacent.Edge.Curve;
                bool fromStart = adjacent.Edge.Start == vertexId;
                Vector3d tangent = fromStart ? curve.TangentAtStart : curve.TangentAtEnd;

                if (!fromStart)
                    tangent.Reverse();

                if (!tangent.IsValid || tangent.IsTiny())
                {
                    tangent = Vertices[adjacent.Neighbor].Point - origin;
                }

                double angle = Math.Atan2(tangent.Y, tangent.X);
                return angle >= 0 ? angle : angle + 2.0 * Math.PI;
            }
        }

        private sealed class GraphVertex
        {
            public GraphVertex(int id, Point3d point)
            {
                Id = id;
                Point = point;
            }

            public int Id { get; }
            public Point3d Point { get; }
        }

        private sealed class GraphEdge
        {
            public GraphEdge(int id, int start, int end, Curve curve)
            {
                Id = id;
                Start = start;
                End = end;
                Curve = curve;
            }

            public int Id { get; }
            public int Start { get; }
            public int End { get; }
            public Curve Curve { get; }
        }

        private sealed class AdjacentEdge
        {
            public AdjacentEdge(int neighbor, GraphEdge edge)
            {
                Neighbor = neighbor;
                Edge = edge;
            }

            public int Neighbor { get; }
            public GraphEdge Edge { get; }
        }

        private sealed class TraversalState
        {
            public TraversalState(int vertex, int parent, GraphEdge incoming)
            {
                Vertex = vertex;
                Parent = parent;
                Incoming = incoming;
            }

            public int Vertex { get; }
            public int Parent { get; }
            public GraphEdge Incoming { get; }
        }

        private sealed class OrientedEdge
        {
            public OrientedEdge(int from, int to, GraphEdge edge)
            {
                From = from;
                To = to;
                Edge = edge;
            }

            public int From { get; }
            public int To { get; }
            public GraphEdge Edge { get; }
        }
    }
}
