using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace Pondskater
{
    public class BridgeCellTreeComponent : GH_Component
    {
        public BridgeCellTreeComponent()
          : base("Bridge Cell Tree", "CellTreeBridge",
              "Joins planar cells with width corridors between their centroids using an indexed tree of cell neighbors.",
              "Pondskater", "Paths")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Cells", "C", "Closed coplanar cell boundaries. Cell indices follow the input list order.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Neighbors", "N", "Neighbor indices for each cell. Branch {i} contains the indices connected to cell i.", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Width", "W", "Total width of each centroid connection corridor.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Inset Cells", "I", "Inset every cell by half the corridor width before joining.", GH_ParamAccess.item, false);
            pManager.AddPlaneParameter("Plane", "Pl", "Optional common working plane. When omitted, the plane is inferred from the first cell.", GH_ParamAccess.item);
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Boundaries", "B", "Closed boundaries produced by uniting the cells and centroid corridors.", GH_ParamAccess.list);
            pManager.AddLineParameter("Connections", "L", "Centroid-to-centroid connections represented by the validated tree.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var sourceCells = new List<Curve>();
            GH_Structure<GH_Integer> neighborTree;
            double width = 0.0;
            bool insetCells = false;
            Plane workingPlane = Plane.Unset;

            if (!DA.GetDataList(0, sourceCells)) return;
            if (!DA.GetDataTree(1, out neighborTree)) return;
            if (!DA.GetData(2, ref width)) return;
            if (!DA.GetData(3, ref insetCells)) return;
            bool planeWasSupplied = DA.GetData(4, ref workingPlane);

            double tolerance = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-6;
            if (sourceCells.Count <= 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "At least two cells boundaries must be supplied.");
                return;
            }

            if (!RhinoMath.IsValidDouble(width) || width <= tolerance)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Width must be greater than the document tolerance.");
                return;
            }

            if (planeWasSupplied)
            {
                if (!workingPlane.IsValid)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Plane is invalid.");
                    return;
                }
            }
            else
            {
                // this is fine but what if the cells are not coplanar?
                Curve firstCell = sourceCells.FirstOrDefault(cell => cell != null && cell.IsValid);
                if (firstCell == null || !firstCell.TryGetPlane(out workingPlane, tolerance))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Plane must be supplied when a valid cell plane cannot be inferred.");
                    return;
                }
            }

            Transform toLocal = Transform.PlaneToPlane(workingPlane, Plane.WorldXY);
            Transform toWorld = Transform.PlaneToPlane(Plane.WorldXY, workingPlane);
            var cells = new List<Curve>(sourceCells.Count);
            var centroids = new List<Point3d>(sourceCells.Count);

            for (int i = 0; i < sourceCells.Count; i++)
            {
                Curve sourceCell = sourceCells[i];
                string label = "Cell " + i + ": ";
                if (sourceCell == null || !sourceCell.IsValid || !sourceCell.IsClosed)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, label + "a valid closed curve is required.");
                    return;
                }

                if (!TryMakeLocal(sourceCell, toLocal, tolerance, out Curve cell, out string cellError))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, label + cellError);
                    return;
                }

                if (HasSelfIntersections(cell, tolerance))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, label + "is self-intersecting.");
                    return;
                }

                if (!TryGetCentroid(cell, out Point3d centroid))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, label + "has no valid area centroid.");
                    return;
                }

                if (insetCells)
                {
                    if (!TryOffsetInward(cell, width * 0.5, tolerance, out Curve inset, out string insetError))
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, label + insetError);
                        return;
                    }

                    cell = inset;
                }

                cells.Add(cell);
                centroids.Add(centroid);
            }

            if (!TryBuildTreeEdges(neighborTree, sourceCells.Count, out List<Tuple<int, int>> edges, out string treeError))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, treeError);
                return;
            }

            if (!IsConnectedTree(edges, sourceCells.Count))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Neighbors must describe one connected tree with exactly cell count minus one unique edges.");
                return;
            }

            var corridors = new List<Curve>(edges.Count);
            var connections = new List<Line>(edges.Count);
            foreach (Tuple<int, int> edge in edges)
            {
                Point3d start = centroids[edge.Item1];
                Point3d end = centroids[edge.Item2];
                if (!TryMakeCorridor(start, end, width, tolerance, out Curve corridor))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                        "The centroid connection between cells " + edge.Item1 + " and " + edge.Item2 + " is degenerate.");
                    return;
                }

                corridors.Add(corridor);
                connections.Add(new Line(start, end));
            }

            var regions = new List<Curve>(cells.Count + corridors.Count);
            regions.AddRange(cells);
            regions.AddRange(corridors);

            Curve[] localResults;
            try
            {
                localResults = Curve.CreateBooleanUnion(regions, tolerance);
            }
            catch (Exception exception)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Boolean union failed: " + exception.Message);
                return;
            }

            var results = localResults?
                .Where(curve => curve != null && curve.IsValid && curve.IsClosed)
                .OrderByDescending(GetCurveArea)
                .ToList();
            if (results == null || results.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The Boolean union produced no valid closed boundaries.");
                return;
            }

            foreach (Curve result in results)
            {
                if (!result.Transform(toWorld))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "A result could not be transformed back to the working plane.");
                    return;
                }
            }

            for (int i = 0; i < connections.Count; i++)
            {
                Line connection = connections[i];
                connection.Transform(toWorld);
                connections[i] = connection;
            }

            if (results.Count > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "The result contains " + results.Count + " disconnected boundaries. Check that centroid corridors intersect every retained cell.");
            }

            DA.SetDataList(0, results);
            DA.SetDataList(1, connections);
        }

        private static bool TryBuildTreeEdges(
            GH_Structure<GH_Integer> neighborTree,
            int cellCount,
            out List<Tuple<int, int>> edges,
            out string error)
        {
            edges = new List<Tuple<int, int>>();
            error = string.Empty;
            var directedEdges = new HashSet<string>();
            var uniqueEdges = new HashSet<string>();

            foreach (GH_Path path in neighborTree.Paths)
            {
                if (path.Indices.Length != 1)
                {
                    error = "Neighbors must use one-level paths such as {0}, {1}, and {2}.";
                    return false;
                }

                int source = path.Indices[0];
                if (source < 0 || source >= cellCount)
                {
                    error = "Neighbor branch index " + source + " does not match a cell index.";
                    return false;
                }

                System.Collections.IList branch = neighborTree.get_Branch(path);
                if (branch == null) continue;
                foreach (object item in branch)
                {
                    GH_Integer value = item as GH_Integer;
                    if (value == null)
                    {
                        error = "Neighbors contains a null index.";
                        return false;
                    }

                    int target = value.Value;
                    if (target < 0 || target >= cellCount)
                    {
                        error = "Neighbor index " + target + " does not match a cell index.";
                        return false;
                    }

                    if (source == target)
                    {
                        error = "A cell cannot be its own neighbor: " + source + ".";
                        return false;
                    }

                    int first = Math.Min(source, target);
                    int second = Math.Max(source, target);
                    if (!directedEdges.Add(source + ":" + target))
                    {
                        error = "The connection from cell " + source + " to cell " + target + " is listed more than once.";
                        return false;
                    }

                    if (uniqueEdges.Add(first + ":" + second))
                        edges.Add(Tuple.Create(first, second));
                }
            }

            return true;
        }

        private static bool IsConnectedTree(List<Tuple<int, int>> edges, int cellCount)
        {
            if (edges.Count != cellCount - 1) return false;
            var visited = new HashSet<int> { 0 };
            var pending = new Queue<int>();
            pending.Enqueue(0);
            var adjacency = Enumerable.Range(0, cellCount).ToDictionary(index => index, index => new List<int>());

            foreach (Tuple<int, int> edge in edges)
            {
                adjacency[edge.Item1].Add(edge.Item2);
                adjacency[edge.Item2].Add(edge.Item1);
            }

            while (pending.Count > 0)
            {
                int current = pending.Dequeue();
                foreach (int neighbor in adjacency[current])
                {
                    if (visited.Add(neighbor)) pending.Enqueue(neighbor);
                }
            }

            return visited.Count == cellCount;
        }

        private static bool TryMakeCorridor(Point3d start, Point3d end, double width, double tolerance, out Curve corridor)
        {
            corridor = null;
            Vector3d direction = end - start;
            double length = direction.Length;
            if (!RhinoMath.IsValidDouble(length) || length <= tolerance) return false;

            direction /= length;
            Vector3d perpendicular = new Vector3d(-direction.Y, direction.X, 0.0);
            Point3d firstLeft = start + perpendicular * (width * 0.5);
            Point3d firstRight = start - perpendicular * (width * 0.5);
            Point3d secondLeft = end + perpendicular * (width * 0.5);
            Point3d secondRight = end - perpendicular * (width * 0.5);
            corridor = new PolylineCurve(new Polyline
            {
                firstLeft,
                secondLeft,
                secondRight,
                firstRight,
                firstLeft
            });
            return corridor.IsValid;
        }

        private static bool TryMakeLocal(Curve source, Transform toLocal, double tolerance, out Curve local, out string error)
        {
            local = null;
            error = string.Empty;
            if (!source.IsPlanar(tolerance))
            {
                error = "is not planar within the document tolerance.";
                return false;
            }

            local = source.DuplicateCurve();
            if (local == null || !local.Transform(toLocal))
            {
                error = "could not be transformed to the working plane.";
                local = null;
                return false;
            }

            BoundingBox bounds = local.GetBoundingBox(true);
            if (!bounds.IsValid || Math.Max(Math.Abs(bounds.Min.Z), Math.Abs(bounds.Max.Z)) > tolerance)
            {
                error = "does not lie on the working plane within the document tolerance.";
                local.Dispose();
                local = null;
                return false;
            }

            return true;
        }

        private static bool TryGetCentroid(Curve curve, out Point3d centroid)
        {
            centroid = Point3d.Unset;
            using (AreaMassProperties properties = AreaMassProperties.Compute(curve))
            {
                if (properties == null || !properties.Centroid.IsValid) return false;
                centroid = properties.Centroid;
                return true;
            }
        }

        private static bool TryOffsetInward(Curve cell, double distance, double tolerance, out Curve inward, out string error)
        {
            inward = null;
            error = string.Empty;
            CurveOrientation orientation = cell.ClosedCurveOrientation();
            if (orientation != CurveOrientation.Clockwise && orientation != CurveOrientation.CounterClockwise)
            {
                error = "orientation could not be determined.";
                return false;
            }

            double signedDistance = orientation == CurveOrientation.CounterClockwise ? -distance : distance;
            Curve[] offsets = cell.Offset(Plane.WorldXY, signedDistance, tolerance, CurveOffsetCornerStyle.Sharp);
            double originalArea = GetCurveArea(cell);
            var candidates = offsets?
                .Where(candidate => candidate != null && candidate.IsValid && candidate.IsClosed)
                .Select(candidate => new { Curve = candidate, Area = GetCurveArea(candidate) })
                .Where(candidate => candidate.Area > tolerance * tolerance && candidate.Area < originalArea)
                .OrderByDescending(candidate => candidate.Area)
                .ToList();

            if (candidates == null || candidates.Count == 0)
            {
                error = "inward offset collapsed or failed.";
                return false;
            }

            inward = candidates[0].Curve;
            return true;
        }

        private static bool HasSelfIntersections(Curve curve, double tolerance)
        {
            var intersections = Intersection.CurveSelf(curve, tolerance);
            return intersections != null && intersections.Count > 0;
        }

        private static double GetCurveArea(Curve curve)
        {
            if (curve == null || !curve.IsClosed) return 0.0;
            using (AreaMassProperties properties = AreaMassProperties.Compute(curve))
                return properties == null ? 0.0 : Math.Abs(properties.Area);
        }

        protected override Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("F7D07A78-B302-4E65-BC81-7B30B792B7C9");
    }
}