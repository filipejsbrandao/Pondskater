using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Pondskater
{
    public class NComponent : GH_Component
    {
        public NComponent()
          : base("N Component", "NComp",
              "Creates the closed polyline of a multi-wall intersection component from a construction plane, wall directions, per-arm widths and per-arm minimum member lengths measured beyond the joint geometry.",
              "Pondskater", "Components")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "P", "Construction plane. Its origin defines the component center.", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddVectorParameter("Vectors", "V", "Wall directions meeting at the intersection.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Min Member Lengths", "B", "Per-arm minimum member lengths measured beyond the resolved joint. If fewer values are provided than directions, the last value is repeated.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Widths", "W", "Per-arm widths. If fewer values are provided than directions, the last value is repeated.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Min Angle", "A", "Minimum angular separation in degrees for distinct wall directions.", GH_ParamAccess.item, 5.0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Polyline", "P", "Closed N-component polyline.", GH_ParamAccess.item);
            pManager.AddPointParameter("Leg Points", "C", "Stub center points for each wall direction.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Plane plane = Plane.Unset;
            var inputVectors = new List<Vector3d>();
            var minLegLengths = new List<double>();
            var widths = new List<double>();
            double minAngleDegrees = 5.0;

            if (!DA.GetData(0, ref plane)) return;
            if (!DA.GetDataList(1, inputVectors)) return;
            if (!DA.GetDataList(2, minLegLengths)) return;
            if (!DA.GetDataList(3, widths)) return;
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

            if (minLegLengths.Count == 0 || widths.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Provide at least one minimum member length and one width.");
                return;
            }

            if (minLegLengths.Exists(x => x <= 0.0) || widths.Exists(x => x <= 0.0))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "All minimum member lengths and widths must be greater than zero.");
                return;
            }

            if (minAngleDegrees < 5.0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The minimum angle cannot be smaller than 5 degrees.");
                return;
            }

            double minAngle = minAngleDegrees * Math.PI / 180.0;
            Vector3d normal = plane.ZAxis;
            Point3d origin = plane.Origin;

            var projected = new List<Vector3d>();
            bool projectedWarning = false;
            foreach (Vector3d vector in inputVectors)
            {
                if (!vector.IsValid)
                    continue;

                if (ComponentGeometry2D.HasNormalComponent(vector, normal))
                    projectedWarning = true;

                if (!ComponentGeometry2D.TryProjectAndUnitize(vector, normal, out Vector3d projectedVector))
                    continue;

                projected.Add(projectedVector);
            }

            if (projectedWarning)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input vectors were projected onto the construction plane.");
            }

            if (projected.Count < 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "At least two non-zero vectors are required after projection onto the plane.");
                return;
            }

            projected.Sort((a, b) => ComponentGeometry2D.AngleOrder(ComponentGeometry2D.AngleInPlane(plane, a))
                .CompareTo(ComponentGeometry2D.AngleOrder(ComponentGeometry2D.AngleInPlane(plane, b))));

            List<Vector3d> vectors = ComponentGeometry2D.MergeDuplicateDirections(projected, minAngle, normal);
            if (vectors.Count < 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "There are fewer than two distinct wall directions after merging nearly parallel duplicates.");
                return;
            }

            if (vectors.Count < projected.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Merged nearly parallel duplicate wall directions.");
            }

            List<double> expandedLengths = ComponentGeometry2D.ExpandValues(minLegLengths, vectors.Count);
            List<double> expandedWidths = ComponentGeometry2D.ExpandValues(widths, vectors.Count);

            var legPoints = new List<Point3d>();
            var outline = new Polyline();

            for (int i = 0; i < vectors.Count; i++)
            {
                int prev = (i == 0) ? vectors.Count - 1 : i - 1;
                int next = (i == vectors.Count - 1) ? 0 : i + 1;

                double prevAngle = ComponentGeometry2D.CcwAngle(vectors[prev], vectors[i], normal);
                double nextAngle = ComponentGeometry2D.CcwAngle(vectors[i], vectors[next], normal);

                double currentWidth = expandedWidths[i];
                double prevWidth = expandedWidths[prev];
                double nextWidth = expandedWidths[next];
                double distance = prevAngle < nextAngle
                    ? ComponentGeometry2D.ProjectIntersectionPoint(prevWidth, currentWidth, vectors[prev], vectors[i], normal) + expandedLengths[i]
                    : ComponentGeometry2D.ProjectIntersectionPoint(currentWidth, nextWidth, vectors[i], vectors[next], normal) + expandedLengths[i];

                legPoints.Add(origin + vectors[i] * distance);
                outline.Add(ComponentGeometry2D.EndPoint(origin, currentWidth, distance, vectors[i], normal, true));
                outline.Add(ComponentGeometry2D.EndPoint(origin, currentWidth, distance, vectors[i], normal, false));

                if (nextAngle >= minAngle)
                    outline.Add(ComponentGeometry2D.IntersectionPoint(origin, currentWidth, nextWidth, vectors[i], vectors[next], normal));
            }

            if (outline.Count < 3)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to create a valid polyline.");
                return;
            }

            outline.Add(outline[0]);

            DA.SetData(0, new PolylineCurve(outline));
            DA.SetDataList(1, legPoints);
        }

        protected override Bitmap Icon => IconLoader.NComponent;

        public override Guid ComponentGuid
        {
            get { return new Guid("206DA7BE-AB51-4C19-B90B-DE13333B1FFF"); }
        }
    }
}
