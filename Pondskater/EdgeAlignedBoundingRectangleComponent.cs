using System;
using System.Drawing;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace Pondskater
{
    public class EdgeAlignedBoundingRectangleComponent : GH_Component
    {
        public EdgeAlignedBoundingRectangleComponent()
          : base("Edge Aligned Bounding Rectangle", "EABR",
              "Constructs the bounding rectangle of a planar polygon aligned to the direction of a selected input polyline edge. Bounding extents are computed from the polygon convex hull.",
              "Pondskater", "Components")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Polyline", "P", "Closed planar polyline.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Plane", "Pl", "Reference plane of the polygon.", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddIntegerParameter("Edge Index", "E", "Zero-based index of the input polyline edge whose direction is used to align the rectangle. The index refers to the cleaned input polyline order, not the convex hull edge order.", GH_ParamAccess.item, 0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Rectangle", "R", "Edge-aligned bounding rectangle.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Area", "A", "Rectangle area.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Length", "L", "Rectangle length along the selected edge direction.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Width", "W", "Rectangle width perpendicular to the selected edge direction.", GH_ParamAccess.item);
            pManager.AddCurveParameter("Convex Hull", "C", "Convex hull used to compute the rectangle extents.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve curve = null;
            Plane plane = Plane.Unset;
            int edgeIndex = 0;

            if (!DA.GetData(0, ref curve)) return;
            if (!DA.GetData(1, ref plane)) return;
            if (!DA.GetData(2, ref edgeIndex)) return;

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

            int count = polygon.LocalPoints.Count;
            if (count < 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Polygon needs at least two edges.");
                return;
            }

            int edge = ((edgeIndex % count) + count) % count;
            int next = (edge + 1) % count;
            Vector3d direction = polygon.LocalPoints[next] - polygon.LocalPoints[edge];
            if (direction.Length <= ComponentGeometry2D.Epsilon)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Selected edge is degenerate after cleaning the input polyline.");
                return;
            }

            BoundingRectangle2D.RectangleResult rectangle = BoundingRectangle2D.FromDirection(polygon.HullPoints, direction);
            Polyline worldRectangle = polygon.ToWorldPolyline(rectangle.Rectangle);
            Polyline worldHull = polygon.ToWorldPolyline(polygon.HullPolyline);

            DA.SetData(0, new PolylineCurve(worldRectangle));
            DA.SetData(1, rectangle.Area);
            DA.SetData(2, rectangle.Length);
            DA.SetData(3, rectangle.Width);
            DA.SetData(4, new PolylineCurve(worldHull));
        }

        protected override Bitmap Icon => IconLoader.EdgeAlignedBoundingRectangle;

        public override Guid ComponentGuid
        {
            get { return new Guid("F704AE8E-B04B-41BD-A562-2C112430D804"); }
        }
    }
}
