using System;
using System.Collections.Generic;
using System.Drawing;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace Pondskater._3DCPUtils
{
    /// <summary>
    /// Creates a closed mesh representation of a 3D concrete-printing bead along a toolpath.
    /// </summary>
    public class ExtrusionProfileComponent : GH_Component
    {
        public ExtrusionProfileComponent()
          : base("3DCP Extrusion Profile", "3DCPProfile",
              "Creates a closed extrusion mesh with a rounded-rectangle bead profile along a toolpath.",
              "Pondskater", "Paths")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Toolpath", "C", "Toolpath curve along which the extrusion profile is generated.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Layer Width", "W", "Extruded layer width in model units.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Layer Height", "H", "Extruded layer height in model units.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Sampling Distance", "S", "Approximate distance between mesh profile rings.", GH_ParamAccess.item, 10.0);
            pManager.AddNumberParameter("Profile Tolerance", "Tol", "Maximum chord deviation used to discretize the profile and end caps.", GH_ParamAccess.item, 0.1);
            pManager.AddNumberParameter("Fillet Radius", "R", "Toolpath corner fillet radius. Use zero for half the layer width.", GH_ParamAccess.item, 0.0);
            pManager.AddPlaneParameter("Printing Plane", "P", "Plane whose normal defines the printing vertical direction.", GH_ParamAccess.item, Plane.WorldXY);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Closed extrusion-profile mesh.", GH_ParamAccess.item);
            pManager.AddCurveParameter("Filleted Curve", "FC", "Toolpath curve after corner filleting.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve source = null;
            double width = 0.0;
            double height = 0.0;
            double samplingDistance = 10.0;
            double profileTolerance = 0.1;
            double radius = 0.0;
            Plane printingPlane = Plane.WorldXY;

            if (!DA.GetData(0, ref source)) return;
            if (!DA.GetData(1, ref width)) return;
            if (!DA.GetData(2, ref height)) return;
            if (!DA.GetData(3, ref samplingDistance)) return;
            if (!DA.GetData(4, ref profileTolerance)) return;
            if (!DA.GetData(5, ref radius)) return;
            if (!DA.GetData(6, ref printingPlane)) return;

            if (source == null || !source.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The toolpath curve is null or invalid.");
                return;
            }
            if (width <= 0.0 || height <= 0.0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Layer width and height must be greater than zero.");
                return;
            }
            if (width < height)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Layer width must be greater than or equal to layer height.");
                return;
            }
            if (samplingDistance <= 0.0 || profileTolerance <= 0.0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Sampling distance and profile tolerance must be greater than zero.");
                return;
            }
            if (radius < 0.0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Fillet radius cannot be negative. Use zero for the default radius.");
                return;
            }
            if (!printingPlane.IsValid)
                printingPlane = Plane.WorldXY;

            RhinoDoc document = RhinoDoc.ActiveDoc;
            double modelTolerance = document?.ModelAbsoluteTolerance ?? RhinoMath.SqrtEpsilon;
            double angleTolerance = document?.ModelAngleToleranceRadians ?? RhinoMath.ToRadians(1.0);
            double filletRadius = radius > 0.0 ? radius : width * 0.5;

            Curve path = source.DuplicateCurve();
            Curve candidate = Curve.CreateFilletCornersCurve(path, filletRadius, modelTolerance, angleTolerance);
            if (candidate != null)
                path = candidate;
            else
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Toolpath filleting failed. The original curve will be used.");

            double pathLength = path.GetLength();
            if (pathLength <= RhinoMath.ZeroTolerance)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The toolpath has zero length.");
                return;
            }

            int intervalCount = Math.Max(1, (int)Math.Ceiling(pathLength / samplingDistance));
            double[] parameters = path.DivideByCount(intervalCount, true, out Point3d[] sampledPoints);
            if (parameters == null || sampledPoints == null || parameters.Length < 2 || sampledPoints.Length < 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The toolpath could not be divided.");
                return;
            }

            int sampleCount = Math.Min(parameters.Length, sampledPoints.Length);
            if (path.IsClosed && sampleCount > 2 &&
                sampledPoints[0].DistanceToSquared(sampledPoints[sampleCount - 1]) <= modelTolerance * modelTolerance)
                sampleCount--;

            if (sampleCount < 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Not enough distinct toolpath samples were generated.");
                return;
            }

            Vector3d printingVertical = printingPlane.Normal;
            if (!printingVertical.Unitize())
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The printing plane has an invalid normal.");
                return;
            }

            var centers = new List<Point3d>(sampleCount);
            var tangents = new List<Vector3d>(sampleCount);
            var laterals = new List<Vector3d>(sampleCount);
            var verticals = new List<Vector3d>(sampleCount);

            for (int i = 0; i < sampleCount; i++)
            {
                Vector3d tangent = path.TangentAt(parameters[i]);
                tangent -= printingVertical * (tangent * printingVertical);
                if (!tangent.Unitize())
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "A toolpath tangent is parallel to the printing-plane normal.");
                    return;
                }

                Vector3d lateral = Vector3d.CrossProduct(printingVertical, tangent);
                Vector3d vertical = Vector3d.CrossProduct(tangent, lateral);
                if (!lateral.Unitize() || !vertical.Unitize())
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not calculate an orthogonal profile frame.");
                    return;
                }

                centers.Add(sampledPoints[i]);
                tangents.Add(tangent);
                laterals.Add(lateral);
                verticals.Add(vertical);
            }

            List<Point2d> rightHalf = CreateRightHalfProfile(width, height, profileTolerance);
            List<Point2d> profile = CreateCompleteProfile(rightHalf);
            if (rightHalf.Count < 3 || profile.Count < 4)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The generated bead profile is invalid.");
                return;
            }

            int capSegments = Math.Min(GetArcSegmentCount(Math.PI, width * 0.5, profileTolerance, 4), 32);
            var mesh = new Mesh();
            AddLoftVertices(mesh, centers, laterals, verticals, profile);
            AddLoftFaces(mesh, centers.Count, profile.Count, path.IsClosed);

            if (!path.IsClosed)
            {
                AddRevolvedCap(mesh, 0, profile.Count, centers[0], tangents[0], laterals[0], verticals[0], rightHalf, capSegments, true);
                int last = centers.Count - 1;
                AddRevolvedCap(mesh, last * profile.Count, profile.Count, centers[last], tangents[last], laterals[last], verticals[last], rightHalf, capSegments, false);
            }

            mesh.Faces.CullDegenerateFaces();
            mesh.Normals.ComputeNormals();
            mesh.Compact();

            DA.SetData(0, mesh);
            DA.SetData(1, path);
        }

        private static List<Point2d> CreateRightHalfProfile(double width, double height, double tolerance)
        {
            var points = new List<Point2d>();
            double radius = height * 0.5;
            double centerY = width * 0.5 - radius;
            double centerZ = -height * 0.5;
            int arcSegments = GetArcSegmentCount(Math.PI, radius, tolerance, 4);

            points.Add(new Point2d(0.0, 0.0));
            if (centerY > RhinoMath.ZeroTolerance)
                points.Add(new Point2d(centerY, 0.0));

            for (int i = 1; i <= arcSegments; i++)
            {
                double angle = Math.PI * 0.5 - Math.PI * i / arcSegments;
                AddPointIfDifferent(points, new Point2d(centerY + radius * Math.Cos(angle), centerZ + radius * Math.Sin(angle)));
            }
            AddPointIfDifferent(points, new Point2d(0.0, -height));
            return points;
        }

        private static List<Point2d> CreateCompleteProfile(List<Point2d> rightHalf)
        {
            var profile = new List<Point2d>();
            foreach (Point2d point in rightHalf)
                profile.Add(new Point2d(-point.X, point.Y));
            for (int i = rightHalf.Count - 2; i >= 1; i--)
                profile.Add(rightHalf[i]);
            return profile;
        }

        private static void AddLoftVertices(Mesh mesh, IList<Point3d> centers, IList<Vector3d> laterals,
            IList<Vector3d> verticals, IList<Point2d> profile)
        {
            for (int i = 0; i < centers.Count; i++)
                foreach (Point2d point in profile)
                    mesh.Vertices.Add(centers[i] + laterals[i] * point.X + verticals[i] * point.Y);
        }

        private static void AddLoftFaces(Mesh mesh, int ringCount, int profileCount, bool closedPath)
        {
            int spanCount = closedPath ? ringCount : ringCount - 1;
            for (int i = 0; i < spanCount; i++)
            {
                int ring0 = i * profileCount;
                int ring1 = ((i + 1) % ringCount) * profileCount;
                for (int j = 0; j < profileCount; j++)
                {
                    int next = (j + 1) % profileCount;
                    mesh.Faces.AddFace(ring0 + j, ring0 + next, ring1 + next, ring1 + j);
                }
            }
        }

        private static void AddRevolvedCap(Mesh mesh, int ringBaseIndex, int completeProfileCount,
            Point3d endpoint, Vector3d tangent, Vector3d lateral, Vector3d vertical,
            IList<Point2d> rightHalf, int angularSegments, bool startCap)
        {
            int halfCount = rightHalf.Count;
            var indices = new int[angularSegments + 1, halfCount];

            for (int p = 0; p < halfCount; p++)
            {
                int profileIndex = p == 0 ? 0 : p == halfCount - 1 ? halfCount - 1 : completeProfileCount - p;
                indices[0, p] = ringBaseIndex + profileIndex;
                indices[angularSegments, p] = ringBaseIndex + p;
            }

            int topIndex = ringBaseIndex;
            int bottomIndex = ringBaseIndex + halfCount - 1;
            double axialDirection = startCap ? -1.0 : 1.0;

            for (int q = 1; q < angularSegments; q++)
            {
                double angle = Math.PI * q / angularSegments;
                indices[q, 0] = topIndex;
                indices[q, halfCount - 1] = bottomIndex;
                for (int p = 1; p < halfCount - 1; p++)
                {
                    Point2d point = rightHalf[p];
                    Point3d vertex = endpoint + lateral * (point.X * Math.Cos(angle)) +
                        tangent * (axialDirection * point.X * Math.Sin(angle)) + vertical * point.Y;
                    indices[q, p] = mesh.Vertices.Add(vertex);
                }
            }

            for (int q = 0; q < angularSegments; q++)
            {
                for (int p = 0; p < halfCount - 1; p++)
                {
                    int a = indices[q, p];
                    int b = indices[q + 1, p];
                    int c = indices[q + 1, p + 1];
                    int d = indices[q, p + 1];
                    if (p == 0)
                        mesh.Faces.AddFace(a, startCap ? d : c, startCap ? c : d);
                    else if (p == halfCount - 2)
                        mesh.Faces.AddFace(a, startCap ? c : b, startCap ? b : c);
                    else if (startCap)
                        mesh.Faces.AddFace(a, d, c, b);
                    else
                        mesh.Faces.AddFace(a, b, c, d);
                }
            }
        }

        private static int GetArcSegmentCount(double totalAngle, double radius, double tolerance, int minimum)
        {
            if (radius <= RhinoMath.ZeroTolerance)
                return minimum;
            double ratio = Math.Max(-1.0, Math.Min(1.0, 1.0 - tolerance / radius));
            double maximumStep = 2.0 * Math.Acos(ratio);
            return maximumStep <= RhinoMath.ZeroTolerance
                ? minimum
                : Math.Max(minimum, (int)Math.Ceiling(totalAngle / maximumStep));
        }

        private static void AddPointIfDifferent(ICollection<Point2d> points, Point2d candidate)
        {
            var list = (List<Point2d>)points;
            if (list.Count == 0)
            {
                list.Add(candidate);
                return;
            }

            Point2d previous = list[list.Count - 1];
            double dx = candidate.X - previous.X;
            double dy = candidate.Y - previous.Y;
            if (dx * dx + dy * dy > RhinoMath.ZeroTolerance * RhinoMath.ZeroTolerance)
                list.Add(candidate);
        }

        protected override Bitmap Icon => IconLoader.ThreeDcpSpeed;

        public override Guid ComponentGuid => new Guid("9A2A95F1-7AD7-4EA0-A2EA-9AA37E7E9871");
    }
}
