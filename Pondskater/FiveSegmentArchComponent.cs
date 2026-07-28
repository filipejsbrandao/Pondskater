using System;
using System.Collections.Generic;
using System.Drawing;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Pondskater
{
    public class FiveSegmentArchComponent : GH_Component
    {
        private const double MichalSlope = 2.67142857142857;
        private const double MichalIntercept = -0.405;
        private const double MinimumRiseSpanRatio = 0.405 / MichalSlope;
        private const double MaximumRiseSpanRatio = 0.6804717203641484;

        public FiveSegmentArchComponent()
          : base("Five-Segment Arch", "5SegArch",
              "Constructs a five-segment arch using Michal's radius-ratio method.",
              "Pondskater", "Curves")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Span", "S", "Overall width of the arch.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Rise", "R", "Vertical height from the spring line to the apex.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Plane", "P", "Construction plane. Span follows its X axis and rise follows its Y axis.", GH_ParamAccess.item, Plane.WorldXY);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Arch", "A", "Five-segment polycurve arch.", GH_ParamAccess.item);
            pManager.AddArcParameter("Arc Segments", "AS", "The five individual arc segments from left to right.", GH_ParamAccess.list);
            pManager.AddPointParameter("Centers", "C", "Centers of the five arc segments.", GH_ParamAccess.list);
            pManager.AddPointParameter("Joints", "J", "Junction points between consecutive arc segments.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Michal Ratio", "MR", "First-radius factor calculated by Michal's linear fit.", GH_ParamAccess.item);
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

            double riseSpanRatio = rise / span;
            if (riseSpanRatio <= MinimumRiseSpanRatio)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"The rise-to-span ratio must be greater than {MinimumRiseSpanRatio:F6}.");
                return;
            }

            if (riseSpanRatio >= MaximumRiseSpanRatio)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"The rise-to-span ratio must be less than {MaximumRiseSpanRatio:F6}.");
                return;
            }

            if (!plane.IsValid || !plane.XAxis.IsValid || !plane.YAxis.IsValid || !plane.ZAxis.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The supplied plane is invalid. World XY will be used.");
                plane = Plane.WorldXY;
            }

            double firstRadiusRatio = MichalFirstRadiusRatio(riseSpanRatio);
            if (riseSpanRatio < 0.30 || riseSpanRatio > 0.36)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    "Michal's radius ratio is being extrapolated outside its fitted rise-to-span range of 0.30 to 0.36.");
            }

            // We build everything in local WorldXY, then map onto 'plane'
            Transform toTarget = Transform.PlaneToPlane(Plane.WorldXY, plane);
            double halfSpan = 0.5 * span;

            // --------- Frame in local XY ----------
            Point3d leftSpring = new Point3d(-halfSpan, 0.0, 0.0);
            Point3d rightSpring = new Point3d(halfSpan, 0.0, 0.0);
            Point3d origin = Point3d.Origin;
            Point3d apex = new Point3d(0.0, rise, 0.0);
            Point3d semicircleTop = new Point3d(0.0, halfSpan, 0.0);

            // --------- 1) Semicircle division -> P1..P4 ----------
            var divisionPoints = new List<Point3d>(4);
            for (int i = 1; i <= 4; i++)
            {
                double angle = Math.PI - i * (Math.PI / 5.0); // from left to right
                divisionPoints.Add(new Point3d(
                    halfSpan * Math.Cos(angle),
                    halfSpan * Math.Sin(angle),
                    0.0));
            }

            // --------- 2) Radial vectors (O -> Pk) ----------
            Point3d p1 = divisionPoints[0];
            Point3d p2 = divisionPoints[1];
            Vector3d radial1 = p1 - origin;
            Vector3d radial2 = p2 - origin;

            double firstRadius = halfSpan * firstRadiusRatio;
            Point3d center1 = new Point3d(leftSpring.X + firstRadius, 0.0, 0.0);

            Vector3d leftSecant = p1 - leftSpring;
            Vector3d radial1Direction = radial1;
            if (!leftSecant.Unitize() || !radial1Direction.Unitize() ||
                !LineLineXY(leftSpring, leftSecant, center1, radial1Direction, out double firstJointParameter, out _))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "The first joint could not be found. Check the span and rise values.");
                DA.SetData(4, firstRadiusRatio);
                return;
            }

            Point3d joint1 = leftSpring + firstJointParameter * leftSecant;

            Vector3d secondSecant = p2 - p1;
            Vector3d apexSecant = p2 - semicircleTop;
            if (!secondSecant.Unitize() || !apexSecant.Unitize() ||
                !LineLineXY(joint1, secondSecant, apex, apexSecant, out double secondJointParameter, out _))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "The second joint could not be found because the construction lines are parallel.");
                return;
            }

            Point3d joint2 = joint1 + secondJointParameter * secondSecant;
            Vector3d inverseRadial1 = -radial1;
            Vector3d inverseRadial2 = -radial2;

            if (!inverseRadial1.Unitize() || !inverseRadial2.Unitize() ||
                !LineLineXY(joint1, inverseRadial1, joint2, inverseRadial2, out double center2Parameter, out _))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "The second arc center could not be found because the radial lines are parallel.");
                return;
            }

            Point3d center2 = joint1 + center2Parameter * inverseRadial1;
            if (!LineWithYAxis(joint2, inverseRadial2, out Point3d center3))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "The crown arc center could not be found because its radial line is parallel to the Y axis.");
                return;
            }

            Point3d joint4 = MirrorAcrossYAxis(joint1);
            Point3d joint3 = MirrorAcrossYAxis(joint2);
            Point3d center5 = MirrorAcrossYAxis(center1);
            Point3d center4 = MirrorAcrossYAxis(center2);

            if (IsDegenerate(center1, leftSpring, joint1) ||
                IsDegenerate(center2, joint1, joint2) ||
                IsDegenerate(center3, joint2, joint3) ||
                IsDegenerate(center4, joint3, joint4) ||
                IsDegenerate(center5, joint4, rightSpring))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "The construction produced degenerate geometry. Check the input parameters.");
                return;
            }

            var localArcs = new List<Arc>
            {
                ArcFromCenter(leftSpring, joint1, center1),
                ArcFromCenter(joint1, joint2, center2),
                ArcFromCenter(joint2, joint3, center3),
                ArcFromCenter(joint3, joint4, center4),
                ArcFromCenter(joint4, rightSpring, center5)
            };

            var transformedArcs = new List<Arc>(localArcs.Count);
            var arch = new PolyCurve();
            foreach (Arc localArc in localArcs)
            {
                Arc transformedArc = localArc;
                if (!transformedArc.Transform(toTarget) || !transformedArc.IsValid)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "An arc segment could not be transformed to the target plane.");
                    return;
                }

                transformedArcs.Add(transformedArc);
                arch.Append(new ArcCurve(transformedArc));
            }

            var centers = new List<Point3d> { center1, center2, center3, center4, center5 };
            var joints = new List<Point3d> { joint1, joint2, joint3, joint4 };
            TransformPoints(centers, toTarget);
            TransformPoints(joints, toTarget);
            apex.Transform(toTarget);

            DA.SetData(0, arch);
            DA.SetDataList(1, transformedArcs);
            DA.SetDataList(2, centers);
            DA.SetDataList(3, joints);
            DA.SetData(4, firstRadiusRatio);
            DA.SetData(5, apex);
        }

        private static double MichalFirstRadiusRatio(double riseSpanRatio)
        {
            return MichalSlope * riseSpanRatio + MichalIntercept;
        }

        private static Point3d MirrorAcrossYAxis(Point3d point)
        {
            return new Point3d(-point.X, point.Y, point.Z);
        }

        private static bool LineLineXY(
            Point3d firstOrigin,
            Vector3d firstDirection,
            Point3d secondOrigin,
            Vector3d secondDirection,
            out double firstParameter,
            out double secondParameter)
        {
            firstParameter = 0.0;
            secondParameter = 0.0;
            double determinant = firstDirection.X * secondDirection.Y - firstDirection.Y * secondDirection.X;
            if (Math.Abs(determinant) < 1e-12) return false;

            Vector3d delta = secondOrigin - firstOrigin;
            firstParameter = (delta.X * secondDirection.Y - delta.Y * secondDirection.X) / determinant;
            secondParameter = (delta.X * firstDirection.Y - delta.Y * firstDirection.X) / determinant;
            return true;
        }

        private static bool LineWithYAxis(Point3d origin, Vector3d direction, out Point3d intersection)
        {
            intersection = Point3d.Unset;
            if (Math.Abs(direction.X) < 1e-12) return false;

            double parameter = -origin.X / direction.X;
            intersection = origin + parameter * direction;
            return true;
        }

        private static Arc ArcFromCenter(Point3d start, Point3d end, Point3d center)
        {
            if (!start.IsValid || !end.IsValid || !center.IsValid) return Arc.Unset;

            Vector3d centerToStart = start - center;
            Vector3d centerToEnd = end - center;
            if (centerToStart.SquareLength < 1e-16 || centerToEnd.SquareLength < 1e-16) return Arc.Unset;

            double radius = 0.5 * (centerToStart.Length + centerToEnd.Length);
            var circle = new Circle(new Plane(center, Vector3d.ZAxis), radius);
            double startAngle = Math.Atan2(centerToStart.Y, centerToStart.X);
            double endAngle = Math.Atan2(centerToEnd.Y, centerToEnd.X);
            double sweep = endAngle - startAngle;

            while (sweep <= -Math.PI) sweep += 2.0 * Math.PI;
            while (sweep > Math.PI) sweep -= 2.0 * Math.PI;

            return new Arc(circle, new Interval(startAngle, startAngle + sweep));
        }

        private static bool IsDegenerate(Point3d center, Point3d start, Point3d end)
        {
            return !center.IsValid || !start.IsValid || !end.IsValid ||
                   center.DistanceTo(start) < 1e-9 || center.DistanceTo(end) < 1e-9;
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

        protected override Bitmap Icon => IconLoader.PondskaterIcon;

        public override Guid ComponentGuid => new Guid("D80563B7-75B8-414F-B4DA-780A3EBE0105");
    }
}
