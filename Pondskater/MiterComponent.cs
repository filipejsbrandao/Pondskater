using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Pondskater
{
    public class MiterComponent : GH_Component
    {
        public MiterComponent()
          : base("Miter Component", "Miter",
              "Creates one closed polyline miter-joint component per wall direction from a construction plane, wall directions, arm lengths and widths.",
              "Pondskater", "Subdivision")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "P", "Construction plane. Its origin defines the joint center.", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddVectorParameter("Vectors", "V", "Wall directions meeting at the joint.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Arm Lengths", "B", "Per-arm lengths. If fewer values are provided than vectors, the last value is repeated.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Widths", "H", "Per-arm widths. If fewer values are provided than vectors, the last value is repeated.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Min Angle", "A", "Minimum angular separation in degrees for distinct wall directions.", GH_ParamAccess.item, 5.0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Components", "C", "One closed miter component polyline per wall direction.", GH_ParamAccess.list);
            pManager.AddPointParameter("Leg Points", "L", "Center points at the end of each arm.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Plane plane = Plane.Unset;
            var inputVectors = new List<Vector3d>();
            var inputLengths = new List<double>();
            var inputWidths = new List<double>();
            double minAngleDegrees = 5.0;

            if (!DA.GetData(0, ref plane)) return;
            if (!DA.GetDataList(1, inputVectors)) return;
            if (!DA.GetDataList(2, inputLengths)) return;
            if (!DA.GetDataList(3, inputWidths)) return;
            if (!DA.GetData(4, ref minAngleDegrees)) return;

            if (!plane.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Plane is invalid.");
                return;
            }

            if (inputVectors.Count < 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Provide at least two wall directions.");
                return;
            }

            if (inputLengths.Count == 0 || inputWidths.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Provide at least one arm length and one width.");
                return;
            }

            if (inputLengths.Exists(x => x <= 0.0) || inputWidths.Exists(x => x <= 0.0))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "All arm lengths and widths must be greater than zero.");
                return;
            }

            if (minAngleDegrees < 5.0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The minimum angle cannot be smaller than 5 degrees.");
                return;
            }

            List<double> expandedLengths = ComponentGeometry2D.ExpandValues(inputLengths, inputVectors.Count);
            List<double> expandedWidths = ComponentGeometry2D.ExpandValues(inputWidths, inputVectors.Count);

            var arms = new List<MiterArm>();
            Vector3d normal = plane.ZAxis;
            bool projectedWarning = false;

            for (int i = 0; i < inputVectors.Count; i++)
            {
                Vector3d vector = inputVectors[i];
                if (!vector.IsValid)
                    continue;

                if (ComponentGeometry2D.HasNormalComponent(vector, normal))
                    projectedWarning = true;

                if (!ComponentGeometry2D.TryProjectAndUnitize(vector, normal, out Vector3d direction))
                    continue;

                arms.Add(new MiterArm(
                    direction,
                    expandedLengths[i],
                    expandedWidths[i],
                    ComponentGeometry2D.AngleOrder(ComponentGeometry2D.AngleInPlane(plane, direction))));
            }

            if (projectedWarning)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input vectors were projected onto the construction plane.");
            }

            if (arms.Count < 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "At least two non-zero vectors are required after projection onto the plane.");
                return;
            }

            arms.Sort((a, b) => a.Angle.CompareTo(b.Angle));

            double minAngle = minAngleDegrees * Math.PI / 180.0;
            arms = MergeArms(arms, minAngle, normal);
            if (arms.Count < 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "There are fewer than two distinct wall directions after merging nearly parallel duplicates.");
                return;
            }

            var components = new List<Curve>(arms.Count);
            var legPoints = new List<Point3d>(arms.Count);
            Point3d origin = plane.Origin;
            bool usedPerpendicularFallback = false;

            for (int i = 0; i < arms.Count; i++)
            {
                int prev = (i == 0) ? arms.Count - 1 : i - 1;
                int next = (i == arms.Count - 1) ? 0 : i + 1;

                MiterArm previousArm = arms[prev];
                MiterArm currentArm = arms[i];
                MiterArm nextArm = arms[next];

                bool previousPairParallel = ComponentGeometry2D.AreNearlyParallel(previousArm.Direction, currentArm.Direction);
                bool nextPairParallel = ComponentGeometry2D.AreNearlyParallel(currentArm.Direction, nextArm.Direction);
                bool previousPairAntiparallel = previousPairParallel && Vector3d.Multiply(previousArm.Direction, currentArm.Direction) < 0.0;
                bool nextPairAntiparallel = nextPairParallel && Vector3d.Multiply(currentArm.Direction, nextArm.Direction) < 0.0;

                if (previousPairParallel && !previousPairAntiparallel)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Adjacent wall directions are parallel; they should be merged before constructing miter components.");
                    return;
                }

                if (nextPairParallel && !nextPairAntiparallel)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Adjacent wall directions are parallel; they should be merged before constructing miter components.");
                    return;
                }

                Point3d previousIntersection = previousPairAntiparallel
                    ? ComponentGeometry2D.EndPoint(origin, currentArm.Width, 0.0, currentArm.Direction, normal, true)
                    : ComponentGeometry2D.IntersectionPoint(
                        origin, previousArm.Width, currentArm.Width, previousArm.Direction, currentArm.Direction, normal);

                Point3d nextIntersection = nextPairAntiparallel
                    ? ComponentGeometry2D.EndPoint(origin, currentArm.Width, 0.0, currentArm.Direction, normal, false)
                    : ComponentGeometry2D.IntersectionPoint(
                        origin, currentArm.Width, nextArm.Width, currentArm.Direction, nextArm.Direction, normal);

                usedPerpendicularFallback |= previousPairAntiparallel || nextPairAntiparallel;

                Point3d rightEnd = ComponentGeometry2D.EndPoint(
                    origin, currentArm.Width, currentArm.Length, currentArm.Direction, normal, true);

                Point3d leftEnd = ComponentGeometry2D.EndPoint(
                    origin, currentArm.Width, currentArm.Length, currentArm.Direction, normal, false);

                var polyline = new Polyline();
                AddIfDistinct(polyline, origin);
                AddIfDistinct(polyline, previousIntersection);
                AddIfDistinct(polyline, rightEnd);
                AddIfDistinct(polyline, leftEnd);
                AddIfDistinct(polyline, nextIntersection);
                AddIfDistinct(polyline, origin);

                if (polyline.Count < 4)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Skipped a degenerate miter component.");
                    continue;
                }

                components.Add(new PolylineCurve(polyline));
                legPoints.Add(origin + currentArm.Length * currentArm.Direction);
            }

            if (usedPerpendicularFallback)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Used perpendicular fallback joints where adjacent wall directions were parallel or antiparallel.");
            }

            DA.SetDataList(0, components);
            DA.SetDataList(1, legPoints);
        }

        protected override Bitmap Icon => IconLoader.MiterComponent;

        public override Guid ComponentGuid
        {
            get { return new Guid("BF3633C6-10B4-4C94-B81B-88799AFF95ED"); }
        }

        private static List<MiterArm> MergeArms(List<MiterArm> sortedArms, double minAngle, Vector3d normal)
        {
            var merged = new List<MiterArm>();

            foreach (MiterArm arm in sortedArms)
            {
                if (merged.Count == 0)
                {
                    merged.Add(arm);
                    continue;
                }

                if (ComponentGeometry2D.CcwAngle(merged[merged.Count - 1].Direction, arm.Direction, normal) < minAngle)
                    continue;

                merged.Add(arm);
            }

            if (merged.Count > 1 &&
                ComponentGeometry2D.CcwAngle(merged[merged.Count - 1].Direction, merged[0].Direction, normal) < minAngle)
            {
                merged.RemoveAt(merged.Count - 1);
            }

            return merged;
        }

        private static void AddIfDistinct(Polyline polyline, Point3d point)
        {
            if (polyline.Count == 0 || polyline[polyline.Count - 1].DistanceToSquared(point) > ComponentGeometry2D.Epsilon * ComponentGeometry2D.Epsilon)
                polyline.Add(point);
        }

        private sealed class MiterArm
        {
            public MiterArm(Vector3d direction, double length, double width, double angle)
            {
                Direction = direction;
                Length = length;
                Width = width;
                Angle = angle;
            }

            public Vector3d Direction { get; }
            public double Length { get; }
            public double Width { get; }
            public double Angle { get; }
        }
    }
}
