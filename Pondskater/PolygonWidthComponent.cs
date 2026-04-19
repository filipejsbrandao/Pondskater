using System;
using System.Collections.Generic;
using System.Drawing;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace Pondskater
{
    public class PolygonWidthComponent : GH_Component
    {
        public PolygonWidthComponent()
          : base("Polygon Width", "PWidth",
              "Computes the minimum width of a planar polygon from its convex hull and returns the two support lines for the best antipodal configuration.",
              "Pondskater", "Components")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Polyline", "P", "Closed planar polyline.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Plane", "Pl", "Reference plane of the polygon.", GH_ParamAccess.item, Plane.WorldXY);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Width", "W", "Minimum polygon width.", GH_ParamAccess.item);
            pManager.AddLineParameter("Support A", "A", "Support line on one side of the minimum-width direction.", GH_ParamAccess.item);
            pManager.AddLineParameter("Support B", "B", "Support line on the opposite side of the minimum-width direction.", GH_ParamAccess.item);
            pManager.AddCurveParameter("Convex Hull", "C", "Convex hull used in the computation.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve curve = null;
            Plane plane = Plane.Unset;

            if (!DA.GetData(0, ref curve)) return;
            if (!DA.GetData(1, ref plane)) return;

            if (curve == null || !curve.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Polyline input is invalid.");
                return;
            }

            if (!plane.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Plane is invalid.");
                return;
            }

            if (!curve.TryGetPolyline(out Polyline polyline))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input curve could not be converted to a polyline.");
                return;
            }

            double tolerance = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-6;
            if (!ConvexPolygon2D.TryCreate(polyline, plane, tolerance, out ConvexPolygon2D polygon, out string error))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, error);
                return;
            }

            MinWidthResult result = ComputeMinimumWidth(polygon.HullPoints);
            Polyline cleanedWorld = polygon.ToWorldPolyline(polygon.HullPolyline);
            Line supportA = polygon.ToWorldLine(result.SupportA);
            Line supportB = polygon.ToWorldLine(result.SupportB);

            DA.SetData(0, result.Width);
            DA.SetData(1, supportA);
            DA.SetData(2, supportB);
            DA.SetData(3, new PolylineCurve(cleanedWorld));
        }

        protected override Bitmap Icon => IconLoader.PolygonWidth;

        public override Guid ComponentGuid
        {
            get { return new Guid("DE8F47C0-8DAA-41C5-A405-230C7F6D5B18"); }
        }

        private static MinWidthResult ComputeMinimumWidth(List<Point3d> points)
        {
            int count = points.Count;
            int antipodal = 1;
            double bestWidth = double.MaxValue;
            Line bestA = Line.Unset;
            Line bestB = Line.Unset;

            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                Point3d edgeStart = points[i];
                Point3d edgeEnd = points[next];

                while (DistanceToLine(edgeStart, edgeEnd, points[(antipodal + 1) % count]) >=
                       DistanceToLine(edgeStart, edgeEnd, points[antipodal]) - ComponentGeometry2D.Epsilon)
                {
                    antipodal = (antipodal + 1) % count;
                    if (antipodal == i)
                        break;
                }

                double width = DistanceToLine(edgeStart, edgeEnd, points[antipodal]);
                if (width < bestWidth)
                {
                    Vector3d direction = edgeEnd - edgeStart;
                    direction.Unitize();

                    bestWidth = width;
                    bestA = new Line(edgeStart, edgeStart + direction * width);
                    bestB = new Line(points[antipodal], points[antipodal] - direction * width);
                }
            }

            return new MinWidthResult(bestWidth, bestA, bestB);
        }

        private static double DistanceToLine(Point3d from, Point3d to, Point3d point)
        {
            double dx = to.X - from.X;
            double dy = to.Y - from.Y;
            return Math.Abs((from.Y - to.Y) * point.X + (to.X - from.X) * point.Y + from.X * to.Y - to.X * from.Y)
                   / Math.Sqrt(dx * dx + dy * dy);
        }

        private readonly struct MinWidthResult
        {
            public MinWidthResult(double width, Line supportA, Line supportB)
            {
                Width = width;
                SupportA = supportA;
                SupportB = supportB;
            }

            public double Width { get; }
            public Line SupportA { get; }
            public Line SupportB { get; }
        }
    }
}
