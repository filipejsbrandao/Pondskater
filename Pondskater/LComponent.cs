using System;
using System.Collections.Generic;
using System.Drawing;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Pondskater
{
    public class LComponent : GH_Component
    {
        public LComponent()
          : base("L Component", "LComp",
              "Creates the closed polyline of an L corner connection from a construction plane, two arm directions, per-arm widths and per-arm minimum member lengths measured beyond the joint geometry.",
              "Pondskater", "Components")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "P", "Construction plane. Its origin defines the corner origin.", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddVectorParameter("Vector A", "vA", "First arm direction.", GH_ParamAccess.item);
            pManager.AddVectorParameter("Vector B", "vB", "Second arm direction.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Widths", "W", "Per-arm widths. If fewer values are provided than arms, the last value is repeated.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Min Member Lengths", "B", "Per-arm minimum member lengths measured beyond the resolved joint. If fewer values are provided than arms, the last value is repeated.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Polyline", "P", "Closed L-component polyline.", GH_ParamAccess.item);
            pManager.AddPointParameter("Vertices", "V", "Polyline vertices in order.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Plane plane = Plane.Unset;
            Vector3d vectorA = Vector3d.Unset;
            Vector3d vectorB = Vector3d.Unset;
            var widths = new List<double>();
            var lengths = new List<double>();

            if (!DA.GetData(0, ref plane)) return;
            if (!DA.GetData(1, ref vectorA)) return;
            if (!DA.GetData(2, ref vectorB)) return;
            if (!DA.GetDataList(3, widths)) return;
            if (!DA.GetDataList(4, lengths)) return;

            if (!plane.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Plane is invalid.");
                return;
            }

            if (!vectorA.IsValid || !vectorB.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input vectors are invalid.");
                return;
            }

            if (widths.Count == 0 || lengths.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Provide at least one width and one minimum member length.");
                return;
            }

            if (widths.Exists(x => x <= 0.0) || lengths.Exists(x => x <= 0.0))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "All widths and minimum member lengths must be greater than zero.");
                return;
            }

            List<double> expandedWidths = ComponentGeometry2D.ExpandValues(widths, 2);
            List<double> expandedLengths = ComponentGeometry2D.ExpandValues(lengths, 2);

            Point3d origin = plane.Origin;
            Vector3d normal = plane.ZAxis;
            double normalA = Math.Abs(Vector3d.Multiply(vectorA, normal));
            double normalB = Math.Abs(Vector3d.Multiply(vectorB, normal));

            Vector3d projectedA = ComponentGeometry2D.ProjectToPlane(vectorA, normal);
            Vector3d projectedB = ComponentGeometry2D.ProjectToPlane(vectorB, normal);

            if (normalA > 1e-9 || normalB > 1e-9)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input vectors were projected onto the construction plane.");
            }

            if (!projectedA.Unitize() || !projectedB.Unitize())
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input vectors must have non-zero length after projection onto the plane.");
                return;
            }

            Vector3d vaLeft = ComponentGeometry2D.LeftPerp(projectedA, normal);
            Vector3d vaRight = -vaLeft;
            Vector3d vbLeft = ComponentGeometry2D.LeftPerp(projectedB, normal);
            Vector3d vbRight = -vbLeft;

            double denomC = Vector3d.Multiply(projectedA, vbLeft);
            double denomD = Vector3d.Multiply(projectedB, vaRight);
            if (Math.Abs(denomC) < 1e-9 || Math.Abs(denomD) < 1e-9)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The arm directions are parallel or nearly parallel.");
                return;
            }

            double widthA = expandedWidths[0];
            double widthB = expandedWidths[1];
            double memberLengthA = expandedLengths[0];
            double memberLengthB = expandedLengths[1];

            double jointProjectionA = ComponentGeometry2D.ProjectIntersectionPoint(widthA, widthB, projectedA, projectedB, normal);
            double jointProjectionB = ComponentGeometry2D.ProjectIntersectionPoint(widthB, widthA, projectedB, projectedA, normal);

            if (jointProjectionA <= ComponentGeometry2D.Epsilon || jointProjectionB <= ComponentGeometry2D.Epsilon)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The resolved joint flips behind the origin for the supplied arm directions and widths.");
                return;
            }

            double lengthA = jointProjectionA + memberLengthA;
            double lengthB = jointProjectionB + memberLengthB;

            Point3d o1 = origin + lengthA * projectedA;
            Point3d o5 = origin + lengthB * projectedB;

            Vector3d vc = ((widthB / 2.0) / denomC) * projectedA;
            Vector3d vd = ((widthA / 2.0) / denomD) * projectedB;

            Point3d o2 = o1 + (widthA / 2.0) * vaLeft;
            Point3d o8 = o1 + (widthA / 2.0) * vaRight;
            Point3d o6 = o5 + (widthB / 2.0) * vbLeft;
            Point3d o4 = o5 + (widthB / 2.0) * vbRight;
            Point3d o7 = origin + vc + vd;
            Point3d o3 = origin - vc - vd;

            var outline = new Polyline
            {
                o2,
                o3,
                o4,
                o6,
                o7,
                o8,
                o2
            };

            var vertices = new List<Point3d> { o2, o3, o4, o6, o7, o8 };

            DA.SetData(0, new PolylineCurve(outline));
            DA.SetDataList(1, vertices);
        }

        protected override Bitmap Icon => IconLoader.LComponent;

        public override Guid ComponentGuid
        {
            get { return new Guid("75A7B325-529F-4DC6-B3C7-0BA8A0ED45CD"); }
        }
    }
}
