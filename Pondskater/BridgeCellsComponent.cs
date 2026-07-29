using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace Pondskater
{
    public class BridgeCellsComponent : GH_Component
    {
        public BridgeCellsComponent()
          : base("Bridge Cells", "CellBridge",
              "Joins planar cell boundaries with a corridor around an open polyline path to create a continuous closed toolpath.",
              "Pondskater", "Paths")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Cells", "C", "Closed coplanar cell boundaries to join.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Path", "P", "Open polyline path crossing the cells in the order they are to be joined.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Width", "W", "Total width of the connecting corridor, or filament layer width.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Inset Cells", "I", "Inset every cell by half the corridor width (or half of the filament layer width) before joining.", GH_ParamAccess.item, false);
            pManager.AddPlaneParameter("Plane", "Pl", "Optional common working plane. When omitted, the plane is inferred from the path.", GH_ParamAccess.item);
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Boundaries", "B", "Closed boundaries produced by uniting the cells and connecting corridor.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var sourceCells = new List<Curve>();
            Curve sourcePath = null;
            double width = 0.0;
            bool insetCells = false;
            Plane workingPlane = Plane.Unset;

            if (!DA.GetDataList(0, sourceCells)) return;
            if (!DA.GetData(1, ref sourcePath)) return;
            if (!DA.GetData(2, ref width)) return;
            if (!DA.GetData(3, ref insetCells)) return;
            bool planeWasSupplied = DA.GetData(4, ref workingPlane);

            double tolerance = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-6;
            if (sourceCells.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "At least one cell boundary must be supplied.");
                return;
            }

            if (sourcePath == null || !sourcePath.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Path is invalid.");
                return;
            }

            if (!sourcePath.TryGetPolyline(out Polyline pathPolyline) || pathPolyline.Count < 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Path must be a polyline containing at least two points.");
                return;
            }

            if (sourcePath.IsClosed)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Path must be open.");
                return;
            }

            double pathLength = sourcePath.GetLength();
            if (!RhinoMath.IsValidDouble(pathLength) || pathLength <= tolerance)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Path must contain at least one non-degenerate segment.");
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
            else if (!sourcePath.TryGetPlane(out workingPlane, tolerance))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Path must be planar so a working plane can be inferred.");
                return;
            }

            Transform toLocal = Transform.PlaneToPlane(workingPlane, Plane.WorldXY);
            Transform toWorld = Transform.PlaneToPlane(Plane.WorldXY, workingPlane);
            if (!TryMakeLocal(sourcePath, toLocal, tolerance, out Curve localPath, out string pathError))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Path " + pathError);
                return;
            }

            if (HasSelfIntersections(localPath, tolerance))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Path is self-intersecting. A single unambiguous corridor cannot be constructed.");
                return;
            }

            if (!TryOffsetSingle(localPath, width * 0.5, tolerance, out Curve left, out string leftError))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Left path offset failed: " + leftError);
                return;
            }

            if (!TryOffsetSingle(localPath, -width * 0.5, tolerance, out Curve right, out string rightError))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Right path offset failed: " + rightError);
                return;
            }

            right.Reverse();
            Curve[] corridorParts =
            {
                left,
                new LineCurve(left.PointAtEnd, right.PointAtStart),
                right,
                new LineCurve(right.PointAtEnd, left.PointAtStart)
            };

            Curve[] joinedCorridor = Curve.JoinCurves(corridorParts, tolerance, true);
            if (joinedCorridor == null || joinedCorridor.Length != 1 || !joinedCorridor[0].IsClosed)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The path offsets could not be joined into one closed corridor.");
                return;
            }

            Curve corridor = joinedCorridor[0];
            if (HasSelfIntersections(corridor, tolerance))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The corridor is self-intersecting. Reduce Width or simplify the path.");
                return;
            }

            var regions = new List<Curve>();
            for (int i = 0; i < sourceCells.Count; i++)
            {
                Curve sourceCell = sourceCells[i];
                string label = "Cell " + i + ": ";
                if (sourceCell == null || !sourceCell.IsValid)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, label + "invalid and was skipped.");
                    continue;
                }

                if (!sourceCell.IsClosed)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, label + "open and was skipped.");
                    continue;
                }

                if (!TryMakeLocal(sourceCell, toLocal, tolerance, out Curve cell, out string cellError))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, label + cellError);
                    return;
                }

                if (HasSelfIntersections(cell, tolerance))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, label + "self-intersecting and was skipped.");
                    continue;
                }

                if (insetCells)
                {
                    if (!TryOffsetInward(cell, width * 0.5, tolerance, out Curve inset, out int discardedRegions, out string insetError))
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, label + insetError + " It was skipped.");
                        continue;
                    }

                    if (discardedRegions > 0)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                            label + "its inward offset split into multiple regions; only the largest region was retained.");
                    }

                    cell = inset;
                }

                regions.Add(cell);
            }

            if (regions.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid closed cells remain.");
                return;
            }

            regions.Add(corridor);
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

            if (localResults == null || localResults.Length == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The Boolean union produced no result.");
                return;
            }

            var results = localResults
                .Where(curve => curve != null && curve.IsValid && curve.IsClosed)
                .OrderByDescending(GetCurveArea)
                .ToList();

            if (results.Count == 0)
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

            if (results.Count > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "The result contains " + results.Count + " disconnected boundaries. Check that the path corridor intersects every retained cell.");
            }

            DA.SetDataList(0, results);
        }

        private static bool TryMakeLocal(
            Curve source,
            Transform toLocal,
            double tolerance,
            out Curve local,
            out string error)
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

        private static bool TryOffsetSingle(
            Curve curve,
            double distance,
            double tolerance,
            out Curve offset,
            out string error)
        {
            offset = null;
            error = string.Empty;
            Curve[] offsets = curve.Offset(Plane.WorldXY, distance, tolerance, CurveOffsetCornerStyle.Sharp);
            var valid = offsets?.Where(candidate => candidate != null && candidate.IsValid).ToList() ?? new List<Curve>();
            if (valid.Count == 0)
            {
                error = "Rhino produced no valid offset.";
                return false;
            }

            if (valid.Count != 1)
            {
                error = "Rhino produced " + valid.Count + " branches, so the corridor would be ambiguous.";
                return false;
            }

            offset = valid[0];
            return true;
        }

        private static bool TryOffsetInward(
            Curve cell,
            double distance,
            double tolerance,
            out Curve inward,
            out int discardedRegions,
            out string error)
        {
            inward = null;
            discardedRegions = 0;
            error = string.Empty;

            CurveOrientation orientation = cell.ClosedCurveOrientation();
            if (orientation != CurveOrientation.Clockwise && orientation != CurveOrientation.CounterClockwise)
            {
                error = "orientation could not be determined.";
                return false;
            }

            double signedDistance = orientation == CurveOrientation.CounterClockwise ? distance : -distance;
            Curve[] offsets = cell.Offset(Plane.WorldXY, signedDistance, tolerance, CurveOffsetCornerStyle.Sharp);
            double originalArea = GetCurveArea(cell);
            double areaTolerance = tolerance * tolerance;
            var inwardCandidates = offsets?
                .Where(candidate => candidate != null && candidate.IsValid && candidate.IsClosed)
                .Select(candidate => new { Curve = candidate, Area = GetCurveArea(candidate) })
                .Where(candidate => candidate.Area > areaTolerance && candidate.Area < originalArea - areaTolerance)
                .OrderByDescending(candidate => candidate.Area)
                .ToList();

            if (inwardCandidates == null || inwardCandidates.Count == 0)
            {
                error = "collapsed or failed to produce a verified inward offset.";
                return false;
            }

            inward = inwardCandidates[0].Curve;
            discardedRegions = inwardCandidates.Count - 1;
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
            {
                return properties == null ? 0.0 : Math.Abs(properties.Area);
            }
        }

        protected override Bitmap Icon => IconLoader.PondskaterIcon;

        public override Guid ComponentGuid => new Guid("3D6156BA-38BC-470C-81F3-23E59B0540A0");
    }
}
