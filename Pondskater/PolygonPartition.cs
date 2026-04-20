//
//PolygonPartition.cs
//
// Author:
//       Filipe Jorge da Silva Brandao
//
// Copyright (c) 2021 ©2021 Filipe Brandao
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Drawing;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Pondskater
{
    public class PolygonPartition : GH_Component
    {
        public PolygonPartition()
          : base("Polygon Partition", "PP",
            "Divides a simple planar polygon into non-overlapping convex components using Bayazit's convex decomposition algorithm.",
            "Pondskater", "Subdivision")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Polygon", "P", "A simple non-self-intersecting planar polygon to partition.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Convex Polygons", "CP", "A list of non-overlapping convex subdivisions.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve curve = null;

            if (!DA.GetData(0, ref curve)) return;
            if (curve == null || !curve.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Polygon input is invalid.");
                return;
            }

            if (!curve.IsPlanar())
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The polygon must be planar.");
                return;
            }

            var events = Rhino.Geometry.Intersect.Intersection.CurveSelf(curve, 0.001);
            if (events.Count != 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The polygon is self-intersecting or too thin. Self-intersection tolerance is 0.001.");
                return;
            }

            if (!curve.TryGetPolyline(out Polyline poly))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input expects a polyline polygon.");
                return;
            }

            if (!poly.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Closed polylines must have at least 3 segments.");
                return;
            }

            if (!poly.IsClosed)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "A closed polyline must be supplied.");
                return;
            }

            if (!curve.TryGetPlane(out Plane plane))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to determine the polygon plane.");
                return;
            }

            Transform toLocal = Transform.PlaneToPlane(plane, Plane.WorldXY);
            Transform toWorld = Transform.PlaneToPlane(Plane.WorldXY, plane);

            var localPolyline = new Polyline(poly);
            localPolyline.Transform(toLocal);

            List<Vector2d> vertices = new List<Vector2d>(localPolyline.Count - 1);

            for (int i = 0; i < localPolyline.Count - 1; i++)
            {
                vertices.Add(new Vector2d(localPolyline[i].X, localPolyline[i].Y));
            }

            List<List<Vector2d>> convexPolygonPoints = BayazitPartition.ConvexPartition(vertices);
            var polygons = new List<Polyline>(convexPolygonPoints.Count);

            foreach (List<Vector2d> polygonPts in convexPolygonPoints)
            {
                Polyline polygon = new Polyline();

                for (int i = 0; i < polygonPts.Count; i++)
                {
                    polygon.Add(new Point3d(polygonPts[i].X, polygonPts[i].Y, 0));
                    if (i == polygonPts.Count - 1)
                    {
                        polygon.Add(new Point3d(polygonPts[0].X, polygonPts[0].Y, 0));
                    }
                }

                polygon.Transform(toWorld);
                polygons.Add(polygon);
            }

            DA.SetDataList(0, polygons);
        }

        protected override Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("47802b12-490f-4e19-92aa-6f1036da22fd");
    }
}
