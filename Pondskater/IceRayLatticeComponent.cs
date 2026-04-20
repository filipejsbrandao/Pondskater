using System;
using System.Collections.Generic;
using System.Drawing;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Pondskater
{
    public class IceRayLatticeComponent : GH_Component
    {
        private const int MaxSplitAttempts = 32;
        private const int MaxIterations = 10000;
        private const int MaxPolygons = 5000;

        public IceRayLatticeComponent()
          : base("Ice Ray Lattice", "IceRay",
              "Subdivides a convex planar polygon into smaller polygons using an iterative Ice Ray splitting strategy.",
              "Pondskater", "Subdivision")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Polygon", "P", "Closed convex planar polyline to subdivide.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Max Area", "A", "Maximum target area for each polygon.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Cut Ratio", "R", "Normalized cut range on each chosen side. Must satisfy 0 < R < 1.", GH_ParamAccess.item, 0.6);
            pManager.AddNumberParameter("Side Ratio", "S", "Minimum side-length factor relative to average side length for a side to be eligible for splitting.", GH_ParamAccess.item, 1.0);
            pManager.AddIntegerParameter("Seed", "Sd", "Random seed.", GH_ParamAccess.item, 1);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Polygons", "P", "Subdivided polygons.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve curve = null;
            double maxArea = 0.0;
            double ratio = 0.0;
            double sideRatio = 0.0;
            int seed = 1;

            if (!DA.GetData(0, ref curve)) return;
            if (!DA.GetData(1, ref maxArea)) return;
            if (!DA.GetData(2, ref ratio)) return;
            if (!DA.GetData(3, ref sideRatio)) return;
            if (!DA.GetData(4, ref seed)) return;

            if (curve == null || !curve.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Polygon input is invalid.");
                return;
            }

            if (maxArea <= 0.0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Max Area must be greater than zero.");
                return;
            }

            if (ratio <= 0.0 || ratio >= 1.0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Cut Ratio must satisfy 0 < R < 1.");
                return;
            }

            if (sideRatio <= 0.0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Side Ratio must be greater than zero.");
                return;
            }

            if (!curve.IsPlanar())
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The polygon must be planar.");
                return;
            }

            if (!curve.TryGetPolyline(out Polyline worldPolyline))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input expects a polyline polygon.");
                return;
            }

            if (!worldPolyline.IsClosed || !worldPolyline.IsValid || worldPolyline.SegmentCount < 3)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "A valid closed polyline with at least three segments must be supplied.");
                return;
            }

            var selfEvents = Rhino.Geometry.Intersect.Intersection.CurveSelf(curve, 0.001);
            if (selfEvents.Count > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The polygon is self-intersecting or too thin. Self-intersection tolerance is 0.001.");
                return;
            }

            if (!curve.TryGetPlane(out Plane plane))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to determine the polygon plane.");
                return;
            }

            Transform toLocal = Transform.PlaneToPlane(plane, Plane.WorldXY);
            Transform toWorld = Transform.PlaneToPlane(Plane.WorldXY, plane);

            var localPolyline = new Polyline(worldPolyline);
            localPolyline.Transform(toLocal);

            if (!IsConvex(localPolyline))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The Ice Ray lattice component currently requires a convex polygon.");
                return;
            }

            var rng = new Random(seed);
            var stack = new List<Polyline> { localPolyline };
            var result = new List<Polyline>();
            int iterations = 0;
            bool warnedUnsplit = false;

            while (stack.Count > 0)
            {
                if (iterations++ > MaxIterations)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Subdivision aborted after reaching the iteration safeguard.");
                    return;
                }

                Polyline current = stack[stack.Count - 1];
                stack.RemoveAt(stack.Count - 1);

                double currentArea = Math.Abs(SignedArea(current));
                if (currentArea <= maxArea)
                {
                    result.Add(current);
                    continue;
                }

                if (!TryIceRaySplit(current, ratio, sideRatio, rng, out Polyline childA, out Polyline childB))
                {
                    if (!warnedUnsplit)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Some polygons could not be subdivided further under the current parameter constraints.");
                        warnedUnsplit = true;
                    }
                    result.Add(current);
                    continue;
                }

                if (Math.Abs(SignedArea(childA)) <= ComponentGeometry2D.Epsilon ||
                    Math.Abs(SignedArea(childB)) <= ComponentGeometry2D.Epsilon)
                {
                    if (!warnedUnsplit)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Some candidate splits were discarded because they produced degenerate polygons.");
                        warnedUnsplit = true;
                    }
                    result.Add(current);
                    continue;
                }

                stack.Add(childA);
                stack.Add(childB);

                if (stack.Count + result.Count > MaxPolygons)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Subdivision aborted after reaching the polygon-count safeguard.");
                    return;
                }
            }

            var worldPolygons = new List<PolylineCurve>(result.Count);
            foreach (Polyline polygon in result)
            {
                var world = new Polyline(polygon);
                world.Transform(toWorld);
                worldPolygons.Add(new PolylineCurve(world));
            }

            DA.SetDataList(0, worldPolygons);
        }

        protected override Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("A7598120-A28E-4A08-A8DB-5B3D37E8E2F7");

        private static bool TryIceRaySplit(Polyline polyline, double ratio, double sideRatio, Random rng, out Polyline first, out Polyline second)
        {
            first = default;
            second = default;

            int sideCount = polyline.SegmentCount;
            if (sideCount < 3)
                return false;

            double averageSideLength = polyline.Length / sideCount;
            double minSideLength = averageSideLength * sideRatio;

            var candidateSides = new List<int>();
            for (int i = 0; i < sideCount; i++)
            {
                if (polyline.SegmentAt(i).Length >= minSideLength)
                    candidateSides.Add(i);
            }

            if (candidateSides.Count < 2)
                return false;

            for (int attempt = 0; attempt < MaxSplitAttempts; attempt++)
            {
                int sideA = candidateSides[rng.Next(candidateSides.Count)];
                int sideB = candidateSides[rng.Next(candidateSides.Count)];

                if (sideA == sideB)
                    continue;

                if (AreAdjacent(sideA, sideB, sideCount))
                    continue;

                double paramA = sideA + Lerp(ratio / 2.0, 1.0 - ratio / 2.0, rng.NextDouble());
                double paramB = sideB + Lerp(ratio / 2.0, 1.0 - ratio / 2.0, rng.NextDouble());

                if (Math.Abs(paramA - paramB) <= ComponentGeometry2D.Epsilon)
                    continue;

                double start = Math.Min(paramA, paramB);
                double end = Math.Max(paramA, paramB);

                if (!TrySplitPolyline(polyline, start, end, out first, out second))
                    continue;

                if (!first.IsValid || !first.IsClosed || first.SegmentCount < 3)
                    continue;

                if (!second.IsValid || !second.IsClosed || second.SegmentCount < 3)
                    continue;

                return true;
            }

            return false;
        }

        private static bool TrySplitPolyline(Polyline polyline, double start, double end, out Polyline first, out Polyline second)
        {
            first = default;
            second = default;

            int count = polyline.Count - 1;
            if (count < 3)
                return false;

            Point3d pointA = polyline.PointAt(start);
            Point3d pointB = polyline.PointAt(end);
            int startNext = (int)Math.Ceiling(start);
            int endNext = (int)Math.Ceiling(end);

            var partA = new List<Point3d> { pointA };
            for (int i = startNext; i <= end; i++)
            {
                int index = i % count;
                AddDistinct(partA, polyline[index]);
                if (index == ((int)Math.Floor(end) % count))
                    break;
            }
            AddDistinct(partA, pointB);
            ClosePolyline(partA);

            var partB = new List<Point3d> { pointB };
            int current = endNext % count;
            while (current != startNext % count)
            {
                AddDistinct(partB, polyline[current]);
                current = (current + 1) % count;
            }
            AddDistinct(partB, pointA);
            ClosePolyline(partB);

            if (partA.Count < 4 || partB.Count < 4)
                return false;

            first = new Polyline(partA);
            second = new Polyline(partB);
            return true;
        }

        private static bool AreAdjacent(int a, int b, int count)
        {
            int diff = Math.Abs(a - b);
            return diff == 1 || diff == count - 1;
        }

        private static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

        private static void AddDistinct(List<Point3d> points, Point3d point)
        {
            if (points.Count == 0 || points[points.Count - 1].DistanceToSquared(point) > ComponentGeometry2D.Epsilon * ComponentGeometry2D.Epsilon)
                points.Add(point);
        }

        private static void ClosePolyline(List<Point3d> points)
        {
            if (points.Count == 0)
                return;

            if (points[0].DistanceToSquared(points[points.Count - 1]) > ComponentGeometry2D.Epsilon * ComponentGeometry2D.Epsilon)
                points.Add(points[0]);
        }

        private static bool IsConvex(Polyline polyline)
        {
            int count = polyline.Count - 1;
            double sign = 0.0;

            for (int i = 0; i < count; i++)
            {
                Point3d a = polyline[i];
                Point3d b = polyline[(i + 1) % count];
                Point3d c = polyline[(i + 2) % count];
                double cross = Cross2D(a, b, c);

                if (Math.Abs(cross) <= ComponentGeometry2D.Epsilon)
                    continue;

                if (sign == 0.0)
                    sign = Math.Sign(cross);
                else if (Math.Sign(cross) != Math.Sign(sign))
                    return false;
            }

            return true;
        }

        private static double SignedArea(Polyline polyline)
        {
            double area = 0.0;
            int count = polyline.Count - 1;

            for (int i = 0; i < count; i++)
            {
                Point3d a = polyline[i];
                Point3d b = polyline[(i + 1) % count];
                area += a.X * b.Y - b.X * a.Y;
            }

            return area * 0.5;
        }

        private static double Cross2D(Point3d a, Point3d b, Point3d c)
        {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }
    }
}
