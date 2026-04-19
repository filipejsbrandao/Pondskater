using System;
using System.Drawing;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace Pondskater
{
    public class MinimumBoundingRectangleComponent : GH_Component
    {
        public MinimumBoundingRectangleComponent()
          : base("Minimum Bounding Rectangle", "MBR",
              "Constructs a robust minimum-area bounding rectangle for a planar polygon by evaluating all convex-hull edge-aligned candidates.",
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
            pManager.AddCurveParameter("Rectangle", "R", "Minimum-area bounding rectangle.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Area", "A", "Rectangle area.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Length", "L", "Rectangle length along its local x-axis.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Width", "W", "Rectangle width along its local y-axis.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Frame", "F", "Rectangle local frame.", GH_ParamAccess.item);
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

            BoundingRectangle2D.RectangleResult best = default;
            bool hasBest = false;

            for (int i = 0; i < polygon.HullPoints.Count; i++)
            {
                int next = (i + 1) % polygon.HullPoints.Count;
                Vector3d direction = polygon.HullPoints[next] - polygon.HullPoints[i];
                BoundingRectangle2D.RectangleResult candidate = BoundingRectangle2D.FromDirection(polygon.HullPoints, direction);

                if (!hasBest || candidate.Area < best.Area)
                {
                    best = candidate;
                    hasBest = true;
                }
            }

            if (!hasBest)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to compute a bounding rectangle.");
                return;
            }

            Polyline worldRectangle = polygon.ToWorldPolyline(best.Rectangle);
            Polyline worldHull = polygon.ToWorldPolyline(polygon.HullPolyline);

            Plane frame = new Plane(Point3d.Origin, best.XAxis, best.YAxis);
            frame.Transform(polygon.ToWorld);

            DA.SetData(0, new PolylineCurve(worldRectangle));
            DA.SetData(1, best.Area);
            DA.SetData(2, best.Length);
            DA.SetData(3, best.Width);
            DA.SetData(4, frame);
            DA.SetData(5, new PolylineCurve(worldHull));
        }

        protected override Bitmap Icon => IconLoader.MinimumBoundingRectangle;

        public override Guid ComponentGuid
        {
            get { return new Guid("0F6B52FB-0DFC-416B-A70A-E10550B88448"); }
        }
    }
}
