using System;
using System.Collections.Generic;

using Rhino.Geometry;

namespace Pondskater
{
    internal static class BoundingRectangle2D
    {
        internal static RectangleResult FromDirection(List<Point3d> points, Vector3d direction)
        {
            Vector3d xAxis = direction;
            if (!xAxis.Unitize())
                throw new InvalidOperationException("Direction must be non-zero.");

            Vector3d yAxis = new Vector3d(-xAxis.Y, xAxis.X, 0.0);

            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;

            for (int i = 0; i < points.Count; i++)
            {
                Point3d point = points[i];
                double x = point.X * xAxis.X + point.Y * xAxis.Y;
                double y = point.X * yAxis.X + point.Y * yAxis.Y;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            Point3d p0 = Point3d.Origin + xAxis * minX + yAxis * minY;
            Point3d p1 = Point3d.Origin + xAxis * maxX + yAxis * minY;
            Point3d p2 = Point3d.Origin + xAxis * maxX + yAxis * maxY;
            Point3d p3 = Point3d.Origin + xAxis * minX + yAxis * maxY;

            Polyline rectangle = new Polyline { p0, p1, p2, p3, p0 };
            return new RectangleResult(rectangle, maxX - minX, maxY - minY, (maxX - minX) * (maxY - minY), xAxis, yAxis);
        }

        internal readonly struct RectangleResult
        {
            internal RectangleResult(Polyline rectangle, double length, double width, double area, Vector3d xAxis, Vector3d yAxis)
            {
                Rectangle = rectangle;
                Length = length;
                Width = width;
                Area = area;
                XAxis = xAxis;
                YAxis = yAxis;
            }

            internal Polyline Rectangle { get; }
            internal double Length { get; }
            internal double Width { get; }
            internal double Area { get; }
            internal Vector3d XAxis { get; }
            internal Vector3d YAxis { get; }
        }
    }
}
