using System;
using System.Collections.Generic;

using Rhino.Geometry;

namespace Pondskater
{
    internal static class ComponentGeometry2D
    {
        internal const double Epsilon = 1e-9;

        internal static Vector3d ProjectToPlane(Vector3d vector, Vector3d normal)
        {
            return vector - Vector3d.Multiply(vector, normal) * normal;
        }

        internal static bool HasNormalComponent(Vector3d vector, Vector3d normal)
        {
            return Math.Abs(Vector3d.Multiply(vector, normal)) > Epsilon;
        }

        internal static bool AreNearlyParallel(Vector3d a, Vector3d b)
        {
            return Vector3d.CrossProduct(a, b).Length <= Epsilon;
        }

        internal static Vector3d LeftPerp(Vector3d direction, Vector3d normal)
        {
            return Vector3d.CrossProduct(normal, direction);
        }

        internal static Vector3d RightPerp(Vector3d direction, Vector3d normal)
        {
            return Vector3d.CrossProduct(direction, normal);
        }

        internal static Point3d EndPoint(Point3d center, double width, double length, Vector3d direction, Vector3d normal, bool rightSide)
        {
            Point3d armEnd = center + length * direction;
            Vector3d perpendicular = rightSide ? RightPerp(direction, normal) : LeftPerp(direction, normal);
            return armEnd + (width / 2.0) * perpendicular;
        }

        internal static Point3d IntersectionPoint(Point3d center, double width, Vector3d a, Vector3d b, Vector3d normal)
        {
            return IntersectionPoint(center, width, width, a, b, normal);
        }

        internal static Point3d IntersectionPoint(Point3d center, double widthA, double widthB, Vector3d a, Vector3d b, Vector3d normal)
        {
            Vector3d bRight = RightPerp(b, normal);
            Vector3d aLeft = LeftPerp(a, normal);

            double denom1 = Vector3d.Multiply(a, bRight);
            double denom2 = Vector3d.Multiply(b, aLeft);
            if (Math.Abs(denom1) < Epsilon || Math.Abs(denom2) < Epsilon)
                throw new InvalidOperationException("Invalid vector pair for intersection.");

            Vector3d v1 = ((widthB / 2.0) / denom1) * a;
            Vector3d v2 = ((widthA / 2.0) / denom2) * b;
            return center + v1 + v2;
        }

        internal static Point3d IntersectionPoint(Point3d center, double width, Vector3d a, Vector3d b, Vector3d normal, bool inside)
        {
            return IntersectionPoint(center, width, width, a, b, normal, inside);
        }

        internal static Point3d IntersectionPoint(Point3d center, double widthA, double widthB, Vector3d a, Vector3d b, Vector3d normal, bool inside)
        {
            Point3d point = IntersectionPoint(center, widthA, widthB, a, b, normal);
            if (inside)
                return point;

            Vector3d bRight = RightPerp(b, normal);
            Vector3d aLeft = LeftPerp(a, normal);

            double denom1 = Vector3d.Multiply(a, bRight);
            double denom2 = Vector3d.Multiply(b, aLeft);
            Vector3d v1 = ((widthB / 2.0) / denom1) * a;
            Vector3d v2 = ((widthA / 2.0) / denom2) * b;
            return center - v1 - v2;
        }

        internal static double ProjectIntersectionPoint(double width, Vector3d a, Vector3d b, Vector3d normal)
        {
            return ProjectIntersectionPoint(width, width, a, b, normal);
        }

        internal static double ProjectIntersectionPoint(double widthA, double widthB, Vector3d a, Vector3d b, Vector3d normal)
        {
            Vector3d bRight = RightPerp(b, normal);
            Vector3d aLeft = LeftPerp(a, normal);

            double denom1 = Vector3d.Multiply(a, bRight);
            double denom2 = Vector3d.Multiply(b, aLeft);
            if (Math.Abs(denom1) < Epsilon || Math.Abs(denom2) < Epsilon)
                throw new InvalidOperationException("Invalid vector pair for projection.");

            Vector3d v1 = ((widthB / 2.0) / denom1) * a;
            Vector3d v2 = ((widthA / 2.0) / denom2) * b;
            return Vector3d.Multiply(v1 + v2, a);
        }

        internal static double CcwAngle(Vector3d first, Vector3d second, Vector3d normal)
        {
            double angle = Math.Atan2(Vector3d.Multiply(Vector3d.CrossProduct(first, second), normal), Vector3d.Multiply(first, second));
            if (angle < 0.0)
                angle += 2.0 * Math.PI;
            return angle;
        }

        internal static double AngleInPlane(Plane plane, Vector3d vector)
        {
            double x = Vector3d.Multiply(vector, plane.XAxis);
            double y = Vector3d.Multiply(vector, plane.YAxis);
            return Math.Atan2(y, x);
        }

        internal static double AngleOrder(double angle)
        {
            return angle >= 0.0 ? angle : angle + 2.0 * Math.PI;
        }

        internal static bool TryProjectAndUnitize(Vector3d vector, Vector3d normal, out Vector3d projected)
        {
            projected = ProjectToPlane(vector, normal);
            return projected.Unitize();
        }

        internal static List<Vector3d> MergeDuplicateDirections(List<Vector3d> sortedVectors, double minAngle, Vector3d normal)
        {
            var merged = new List<Vector3d>();

            foreach (Vector3d vector in sortedVectors)
            {
                if (merged.Count == 0)
                {
                    merged.Add(vector);
                    continue;
                }

                if (CcwAngle(merged[merged.Count - 1], vector, normal) < minAngle)
                    continue;

                merged.Add(vector);
            }

            if (merged.Count > 1 && CcwAngle(merged[merged.Count - 1], merged[0], normal) < minAngle)
                merged.RemoveAt(merged.Count - 1);

            return merged;
        }

        internal static List<double> ExpandValues(IList<double> values, int count)
        {
            if (values == null || values.Count == 0)
                throw new InvalidOperationException("At least one numeric value is required.");

            var expanded = new List<double>(count);
            for (int i = 0; i < count; i++)
            {
                expanded.Add(values[Math.Min(i, values.Count - 1)]);
            }
            return expanded;
        }
    }
}
