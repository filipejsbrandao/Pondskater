using System;
using System.Collections.Generic;

using Rhino.Geometry;

namespace Pondskater
{
    internal sealed class ConvexPolygon2D
    {
        private ConvexPolygon2D(List<Point3d> localPoints, Polyline localPolyline, List<Point3d> hullPoints, Polyline hullPolyline, Plane sourcePlane)
        {
            LocalPoints = localPoints;
            LocalPolyline = localPolyline;
            HullPoints = hullPoints;
            HullPolyline = hullPolyline;
            SourcePlane = sourcePlane;
            ToWorld = Transform.PlaneToPlane(Plane.WorldXY, sourcePlane);
        }

        internal List<Point3d> LocalPoints { get; }
        internal Polyline LocalPolyline { get; }
        internal List<Point3d> HullPoints { get; }
        internal Polyline HullPolyline { get; }
        internal Plane SourcePlane { get; }
        internal Transform ToWorld { get; }

        internal static bool TryCreate(Polyline source, Plane plane, double tolerance, out ConvexPolygon2D polygon, out string error)
        {
            polygon = null;
            error = string.Empty;

            if (!source.IsValid)
            {
                error = "Polyline is invalid.";
                return false;
            }

            if (!source.IsClosed)
            {
                error = "Polyline must be closed.";
                return false;
            }

            if (!plane.IsValid)
            {
                error = "Plane is invalid.";
                return false;
            }

            var unique = new List<Point3d>();
            for (int i = 0; i < source.Count - 1; i++)
            {
                Point3d point = source[i];
                if (plane.DistanceTo(point) > tolerance)
                {
                    error = "Polyline is not on the supplied plane.";
                    return false;
                }

                if (unique.Count == 0 || unique[unique.Count - 1].DistanceTo(point) > tolerance)
                    unique.Add(point);
            }

            if (unique.Count < 3)
            {
                error = "Polyline needs at least three distinct vertices.";
                return false;
            }

            Transform toLocal = Transform.PlaneToPlane(plane, Plane.WorldXY);
            for (int i = 0; i < unique.Count; i++)
                unique[i].Transform(toLocal);

            List<Point3d> filtered = RemoveCollinear(unique, tolerance);
            if (filtered.Count < 3)
            {
                error = "Polyline degenerates after removing collinear vertices.";
                return false;
            }

            if (SignedArea(filtered) < 0.0)
                filtered.Reverse();

            var localPolyline = new Polyline(filtered);
            localPolyline.Add(filtered[0]);

            List<Point3d> hull = ComputeConvexHull(filtered, tolerance);
            if (hull.Count < 3)
            {
                error = "Convex hull degenerates after cleaning the polyline.";
                return false;
            }

            var hullPolyline = new Polyline(hull);
            hullPolyline.Add(hull[0]);
            polygon = new ConvexPolygon2D(filtered, localPolyline, hull, hullPolyline, plane);
            return true;
        }

        internal Polyline ToWorldPolyline(Polyline localPolyline)
        {
            Polyline world = new Polyline(localPolyline);
            world.Transform(ToWorld);
            return world;
        }

        internal Line ToWorldLine(Line localLine)
        {
            Line world = localLine;
            world.Transform(ToWorld);
            return world;
        }

        internal Point3d ToWorldPoint(Point3d localPoint)
        {
            Point3d world = localPoint;
            world.Transform(ToWorld);
            return world;
        }

        private static List<Point3d> RemoveCollinear(List<Point3d> points, double tolerance)
        {
            var result = new List<Point3d>();
            int count = points.Count;

            for (int i = 0; i < count; i++)
            {
                Point3d prev = points[(i - 1 + count) % count];
                Point3d current = points[i];
                Point3d next = points[(i + 1) % count];

                Vector3d a = current - prev;
                Vector3d b = next - current;

                if (a.Length <= tolerance || b.Length <= tolerance)
                    continue;

                double cross = a.X * b.Y - a.Y * b.X;
                if (Math.Abs(cross) <= tolerance * Math.Max(a.Length, b.Length))
                    continue;

                result.Add(current);
            }

            return result;
        }

        private static double SignedArea(List<Point3d> points)
        {
            double area = 0.0;
            for (int i = 0; i < points.Count; i++)
            {
                Point3d a = points[i];
                Point3d b = points[(i + 1) % points.Count];
                area += a.X * b.Y - b.X * a.Y;
            }
            return area * 0.5;
        }

        private static List<Point3d> ComputeConvexHull(List<Point3d> points, double tolerance)
        {
            var sorted = new List<Point3d>(points);
            sorted.Sort(ComparePoints);

            if (sorted.Count <= 1)
                return sorted;

            var lower = new List<Point3d>();
            for (int i = 0; i < sorted.Count; i++)
            {
                Point3d point = sorted[i];
                while (lower.Count >= 2 && Cross(lower[lower.Count - 2], lower[lower.Count - 1], point) <= tolerance)
                    lower.RemoveAt(lower.Count - 1);
                lower.Add(point);
            }

            var upper = new List<Point3d>();
            for (int i = sorted.Count - 1; i >= 0; i--)
            {
                Point3d point = sorted[i];
                while (upper.Count >= 2 && Cross(upper[upper.Count - 2], upper[upper.Count - 1], point) <= tolerance)
                    upper.RemoveAt(upper.Count - 1);
                upper.Add(point);
            }

            lower.RemoveAt(lower.Count - 1);
            upper.RemoveAt(upper.Count - 1);
            lower.AddRange(upper);
            return lower;
        }

        private static int ComparePoints(Point3d a, Point3d b)
        {
            int x = a.X.CompareTo(b.X);
            return x != 0 ? x : a.Y.CompareTo(b.Y);
        }

        private static double Cross(Point3d a, Point3d b, Point3d c)
        {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }
    }
}
