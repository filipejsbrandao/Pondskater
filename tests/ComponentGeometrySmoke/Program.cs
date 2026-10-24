using System;
using Pondskater;
using Rhino.Geometry;

static void Near(double actual, double expected, string label)
{
    if (Math.Abs(actual - expected) > 1e-8)
        throw new Exception($"{label}: expected {expected}, got {actual}");
}

int count = 0;
foreach (var normal in new[] { Vector3d.ZAxis, -Vector3d.ZAxis, Vector3d.YAxis })
foreach (double angle in new[] { Math.PI / 6, Math.PI / 2, 2 * Math.PI / 3 })
foreach (int winding in new[] { -1, 1 })
foreach (double widthA in new[] { 2.0, 4.0 })
{
    var a = Vector3d.XAxis;
    var b = Math.Cos(angle) * a + winding * Math.Sin(angle) * ComponentGeometry2D.LeftPerp(a, normal);
    const double widthB = 2.0;
    // Check both arms, retaining each arm's associated width when swapping.
    foreach (bool swap in new[] { false, true })
    {
        var first = swap ? b : a;
        var second = swap ? a : b;
        double firstWidth = swap ? widthB : widthA;
        double secondWidth = swap ? widthA : widthB;
        double extent = ComponentGeometry2D.LJointExtent(firstWidth, secondWidth, first, second, normal);
        var joint = ComponentGeometry2D.IntersectionPoint(Point3d.Origin, firstWidth, secondWidth, first, second, normal);
        double signed = Vector3d.Multiply(joint - Point3d.Origin, first);
        const double minimumLength = 3;
        double cap = extent + minimumLength;
        Near(Math.Min(cap - signed, cap + signed), minimumLength, "Clearance beyond both joint corners");
        if (Math.Abs(angle - Math.PI / 2) < 1e-9)
            Near(extent, secondWidth / 2, "Perpendicular joint extent");
        if (extent < 0) throw new Exception("Joint extent must be non-negative");
        count++;
    }
}
Console.WriteLine($"PASS: {count} L-joint extent cases (both windings, swapped arms, unequal widths, oblique angles and plane normals).");
