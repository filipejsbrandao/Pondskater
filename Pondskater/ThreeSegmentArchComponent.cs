using System;
using System.Collections.Generic;
using System.Drawing;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Pondskater
{
    public class ThreeSegmentArchComponent : GH_Component
    {
        private static readonly double MaxRiseRatio = 0.5 * Math.Sqrt(3.0);
        private static readonly double MinRiseRatio = Math.Tan(Math.PI / 12.0) * 0.5;
        private const double RiseClampTolerance = 1e-9;

        public ThreeSegmentArchComponent()
          : base("Three-Segment Arch", "3SegArch",
              "Constructs a three-segment arch from a span, a rise, and a construction plane.",
              "Pondskater", "Curves")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Span", "S", "Overall width of the arch.", GH_ParamAccess.item, 10.0);
            pManager.AddNumberParameter("Rise", "R", "Vertical height from the spring line to the apex.", GH_ParamAccess.item, 3.0);
            pManager.AddPlaneParameter("Plane", "P", "Construction plane. Span follows the X axis and rise follows the Y axis.", GH_ParamAccess.item, Plane.WorldXY);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Arch", "A", "Three-segment polycurve arch.", GH_ParamAccess.item);
            pManager.AddArcParameter("Arc Segments", "AS", "The three individual arc segments from left to right.", GH_ParamAccess.list);
            pManager.AddPointParameter("Centers", "C", "Centers of the three arc segments.", GH_ParamAccess.list);
            pManager.AddPointParameter("Joints", "J", "Spring points and junction points between consecutive arc segments.", GH_ParamAccess.list);
            pManager.AddPointParameter("Apex", "AP", "Apex point of the arch.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double span = 0.0;
            double rise = 0.0;
            Plane plane = Plane.WorldXY;

            if (!DA.GetData(0, ref span)) return;
            if (!DA.GetData(1, ref rise)) return;
            if (!DA.GetData(2, ref plane)) return;

            if (span <= 0.0 || rise <= 0.0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Span and rise must be greater than zero.");
                return;
            }

            //double riseSpanRatio = rise / span;
            double clampedRise = rise;
            double minRise = MinRiseRatio * span;
            double maxRise = MaxRiseRatio * span;

            if (clampedRise <= minRise)
            {
                clampedRise = minRise + RiseClampTolerance;
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"Rise is too low for the given span. It has been clamped to {clampedRise:F3}.");
            }
            else if (clampedRise >= maxRise)
            {
                clampedRise = maxRise - RiseClampTolerance;
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"Rise is too high for the given span. It has been clamped to {clampedRise:F3}.");
            }
            else
            {
                clampedRise = rise;
            }

            if (!plane.IsValid || !plane.XAxis.IsValid || !plane.YAxis.IsValid || !plane.ZAxis.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The supplied plane is invalid. World XY will be used.");
                plane = Plane.WorldXY;
            }

            Transform toTarget = Transform.PlaneToPlane(Plane.WorldXY, plane);
            double halfSpan = 0.5 * span;

            Point3d leftSpring = new Point3d(-halfSpan, 0.0, 0.0);
            Point3d rightSpring = new Point3d(halfSpan, 0.0, 0.0);

            double x = (clampedRise - Math.Sqrt(3.0) * span / 2.0) / (Math.Sqrt(3.0) - Math.Tan(Math.PI / 12.0));
            double y = Math.Sqrt(3.0) * x + (Math.Sqrt(3.0) * span / 2.0);

            Arc baseArc = new Arc(Plane.WorldXY, halfSpan, Math.PI);
            //Point3d a = PointAtArcFraction(baseArc, 1.0 / 3.0);
            //Point3d b = PointAtArcFraction(baseArc, 2.0 / 3.0);
            ArcCurve baseArcCurve = new ArcCurve(baseArc, 0.0, 1.0);
            Point3d a = baseArcCurve.PointAt(1.0 / 3.0);
            Point3d b = baseArcCurve.PointAt(2.0 / 3.0);
            Point3d f = Plane.WorldXY.Origin + -x * Plane.WorldXY.XAxis + y * Plane.WorldXY.YAxis;
            Point3d e = Plane.WorldXY.Origin + x * Plane.WorldXY.XAxis + y * Plane.WorldXY.YAxis;
            Vector3d arc2StartTangent = Rotate90Degrees(baseArc.Center - b);
            Vector3d arc3StartTangent = Rotate90Degrees(baseArc.Center - a);

            Arc arc1 = new Arc(leftSpring, Plane.WorldXY.YAxis, e);
            Arc arc2 = new Arc(e, arc2StartTangent, f);
            Arc arc3 = new Arc(f, arc3StartTangent, rightSpring);

            List<Point3d> centers = new List<Point3d> { arc1.Center, arc2.Center, arc3.Center };
            List<Point3d> joints = new List<Point3d> { leftSpring, e, f, rightSpring };
            Point3d apex = arc2.PointAt(0.5);

            if (!arc1.IsValid || !arc2.IsValid || !arc3.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The construction produced invalid arc segments. Check the input values.");
                return;
            }

            var transformedArcs = new List<Arc>(3);
            var arch = new PolyCurve();
            foreach (Arc localArc in new[] { arc1, arc2, arc3 })
            {
                Arc transformedArc = localArc;
                transformedArc.Transform(toTarget);
                transformedArcs.Add(transformedArc);
                arch.Append(new ArcCurve(transformedArc));
            }
            arch.Reverse();

            TransformPoints(centers, toTarget);
            TransformPoints(joints, toTarget);
            apex.Transform(toTarget);

            DA.SetData(0, arch);
            DA.SetDataList(1, transformedArcs);
            DA.SetDataList(2, centers);
            DA.SetDataList(3, joints);
            DA.SetData(4, apex);
        }

        /*private static Point3d PointAtArcFraction(Arc arc, double fraction)
        {
            if (fraction <= 0.0) return arc.StartPoint;
            if (fraction >= 1.0) return arc.EndPoint;

            Vector3d startVector = arc.StartPoint - arc.Center;
            Vector3d endVector = arc.EndPoint - arc.Center;
            double startAngle = Math.Atan2(startVector.Y, startVector.X);
            double endAngle = Math.Atan2(endVector.Y, endVector.X);
            double sweep = endAngle - startAngle;

            while (sweep < 0.0) sweep += 2.0 * Math.PI;
            while (sweep > 2.0 * Math.PI) sweep -= 2.0 * Math.PI;

            double angle = startAngle + sweep * fraction;
            return arc.Center + arc.Radius * new Vector3d(Math.Cos(angle), Math.Sin(angle), 0.0);
        }*/

        private static Vector3d Rotate90Degrees(Vector3d vector)
        {
            Transform rotate90 = Transform.Rotation(Math.PI / 2, Vector3d.ZAxis, Point3d.Origin);
            Vector3d rotatedVector = vector;
            rotatedVector.Transform(rotate90);
            return rotatedVector;
        }

        private static void TransformPoints(IList<Point3d> points, Transform transform)
        {
            for (int i = 0; i < points.Count; i++)
            {
                Point3d point = points[i];
                point.Transform(transform);
                points[i] = point;
            }
        }

        protected override Bitmap Icon => IconLoader.ThreeSegmentArch;

        public override Guid ComponentGuid => new Guid("A1B2C3D4-E5F6-7890-1234-56789ABCDEF0");
    }
}