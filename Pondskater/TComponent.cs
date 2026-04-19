using System;
using System.Collections.Generic;
using System.Drawing;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Pondskater
{
    public class TComponent : GH_Component
    {
        public TComponent()
          : base("T Component", "TComp",
              "Creates the closed polyline of a T corner connection from a construction plane, three arm directions, per-arm widths and per-arm minimum member lengths measured beyond the joint geometry.",
              "Pondskater", "Subdivision")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "P", "Construction plane. Its origin defines the component center.", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddVectorParameter("Vector A", "vA", "First arm direction.", GH_ParamAccess.item);
            pManager.AddVectorParameter("Vector B", "vB", "Second arm direction.", GH_ParamAccess.item);
            pManager.AddVectorParameter("Vector C", "vC", "Third arm direction.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Widths", "W", "Per-arm widths. If fewer values are provided than arms, the last value is repeated.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Min Member Lengths", "B", "Per-arm minimum member lengths measured beyond the resolved joint. If fewer values are provided than arms, the last value is repeated.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Polyline", "P", "Closed T-component polyline.", GH_ParamAccess.item);
            pManager.AddPointParameter("Vertices", "V", "Polyline vertices in order.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Plane plane = Plane.Unset;
            Vector3d vectorA = Vector3d.Unset;
            Vector3d vectorB = Vector3d.Unset;
            Vector3d vectorC = Vector3d.Unset;
            var widths = new List<double>();
            var lengths = new List<double>();

            if (!DA.GetData(0, ref plane)) return;
            if (!DA.GetData(1, ref vectorA)) return;
            if (!DA.GetData(2, ref vectorB)) return;
            if (!DA.GetData(3, ref vectorC)) return;
            if (!DA.GetDataList(4, widths)) return;
            if (!DA.GetDataList(5, lengths)) return;

            if (!plane.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Plane is invalid.");
                return;
            }

            if (!vectorA.IsValid || !vectorB.IsValid || !vectorC.IsValid)
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

            List<double> expandedWidths = ComponentGeometry2D.ExpandValues(widths, 3);
            List<double> expandedLengths = ComponentGeometry2D.ExpandValues(lengths, 3);

            Point3d origin = plane.Origin;
            Vector3d normal = plane.ZAxis;

            Vector3d projectedA = ComponentGeometry2D.ProjectToPlane(vectorA, normal);
            Vector3d projectedB = ComponentGeometry2D.ProjectToPlane(vectorB, normal);
            Vector3d projectedC = ComponentGeometry2D.ProjectToPlane(vectorC, normal);

            if (ComponentGeometry2D.HasNormalComponent(vectorA, normal) ||
                ComponentGeometry2D.HasNormalComponent(vectorB, normal) ||
                ComponentGeometry2D.HasNormalComponent(vectorC, normal))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input vectors were projected onto the construction plane.");
            }

            if (!projectedA.Unitize() || !projectedB.Unitize() || !projectedC.Unitize())
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input vectors must have non-zero length after projection onto the plane.");
                return;
            }

            if (ComponentGeometry2D.AreNearlyParallel(projectedA, projectedB) ||
                ComponentGeometry2D.AreNearlyParallel(projectedA, projectedC) ||
                ComponentGeometry2D.AreNearlyParallel(projectedB, projectedC))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Arm directions must not be parallel or nearly parallel.");
                return;
            }

            double distanceA = Math.Max(
                ComponentGeometry2D.ProjectIntersectionPoint(expandedWidths[0], expandedWidths[1], projectedA, projectedB, normal),
                ComponentGeometry2D.ProjectIntersectionPoint(expandedWidths[0], expandedWidths[2], projectedA, projectedC, normal)) + expandedLengths[0];

            double distanceB = Math.Max(
                ComponentGeometry2D.ProjectIntersectionPoint(expandedWidths[1], expandedWidths[0], projectedB, projectedA, normal),
                ComponentGeometry2D.ProjectIntersectionPoint(expandedWidths[1], expandedWidths[2], projectedB, projectedC, normal)) + expandedLengths[1];

            double distanceC = Math.Max(
                ComponentGeometry2D.ProjectIntersectionPoint(expandedWidths[2], expandedWidths[0], projectedC, projectedA, normal),
                ComponentGeometry2D.ProjectIntersectionPoint(expandedWidths[2], expandedWidths[1], projectedC, projectedB, normal)) + expandedLengths[2];

            if (distanceA <= expandedLengths[0] || distanceB <= expandedLengths[1] || distanceC <= expandedLengths[2])
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The resolved joint flips behind the origin for the supplied arm directions and widths.");
                return;
            }

            var outline = new Polyline();
            outline.Add(ComponentGeometry2D.EndPoint(origin, expandedWidths[1], distanceB, projectedB, normal, false));
            outline.Add(ComponentGeometry2D.IntersectionPoint(origin, expandedWidths[1], expandedWidths[0], projectedB, projectedA, normal, true));
            outline.Add(ComponentGeometry2D.EndPoint(origin, expandedWidths[0], distanceA, projectedA, normal, true));
            outline.Add(ComponentGeometry2D.EndPoint(origin, expandedWidths[0], distanceA, projectedA, normal, false));
            outline.Add(ComponentGeometry2D.IntersectionPoint(origin, expandedWidths[0], expandedWidths[2], projectedA, projectedC, normal, true));
            outline.Add(ComponentGeometry2D.EndPoint(origin, expandedWidths[2], distanceC, projectedC, normal, true));
            outline.Add(ComponentGeometry2D.EndPoint(origin, expandedWidths[2], distanceC, projectedC, normal, false));
            outline.Add(ComponentGeometry2D.IntersectionPoint(origin, expandedWidths[2], expandedWidths[1], projectedC, projectedB, normal, true));
            outline.Add(ComponentGeometry2D.EndPoint(origin, expandedWidths[1], distanceB, projectedB, normal, true));
            outline.Add(ComponentGeometry2D.EndPoint(origin, expandedWidths[1], distanceB, projectedB, normal, false));

            var vertices = new List<Point3d>();
            for (int i = 0; i < outline.Count - 1; i++)
                vertices.Add(outline[i]);

            DA.SetData(0, new PolylineCurve(outline));
            DA.SetDataList(1, vertices);
        }

        protected override Bitmap Icon => IconLoader.TComponent;

        public override Guid ComponentGuid
        {
            get { return new Guid("BFE311AE-6D23-48B7-89BD-B2900E6B6D36"); }
        }
    }
}
