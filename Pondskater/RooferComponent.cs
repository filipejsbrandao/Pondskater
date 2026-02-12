using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;

using System.Xml;
using System.Xml.Serialization;
using System.IO;

using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Globalization;

using Pondskater.IO;
using Pondskater.Native;

namespace Pondskater
{
  public class RooferComponent : GH_Component
  {
    private string _tempObjPath;
    /// <summary>
    /// Each implementation of GH_Component must provide a public 
    /// constructor without any arguments.
    /// Category represents the Tab in which the component will appear, 
    /// Subcategory the panel. If you use non-existing tab or panel names, 
    /// new tabs/panels will automatically be created.
    /// </summary>
    public RooferComponent()
      : base("Roofer", "Rf",
        "Build a roof from a closed planar polygon",
        "Pondskater", "Skeleton")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddCurveParameter("Polyline", "P", "A planar polyline", GH_ParamAccess.item);
      pManager.AddNumberParameter("Slopes", "S", "A list of slope values. Slope is the cotangent of the angle. Note that 0 slope is a vertical wall", GH_ParamAccess.list);
      pManager.AddPlaneParameter("Plane", "Pl", "The plane where the curve lies", GH_ParamAccess.item);
      pManager.AddBooleanParameter("Type", "T", "True for Multiplicative weights (edge speed = w) and false for Additive weights (edge speed = 1 + wa)", GH_ParamAccess.item, true);
      pManager[1].Optional = true;
    }


    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddMeshParameter("Roof", "R", "A mesh with the roof shape", GH_ParamAccess.list);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
    /// to store data in output parameters.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      Curve curve = new PolylineCurve();
      Polyline poly = new Polyline();
      Plane plane = new Plane();
      List<double> weights = new List<double>();
      bool weight_type = true;

      string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      if (!SurferNative.TryLoad(directory, out string loadError))
      {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, loadError);
          return;
      }

      if (!DA.GetData(0, ref curve)) return;
      DA.GetDataList(1, weights);
      if (!DA.GetData(2, ref plane)) return;
      if (!DA.GetData(3, ref weight_type)) return;

      if (!curve.IsPlanar())
      {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The polygon must be a planar polyline");
          return;
      }

      //To ensure this always works we must move the polygon to the plane XY and then return it to it original position
      //curve.TryGetPlane(out Plane uPlane);

      if (curve.TryGetPolyline(out poly)) { }
      else
      {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "P input expects a polygon to be provided");
          return;
      }

      if (poly.IsValid == false)
      {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Closed polylines must have at least 2 segments");
          return;
      }

      Transform transform = Transform.PlaneToPlane(plane, Plane.WorldXY);
      Transform reverse_transform = Transform.PlaneToPlane(Plane.WorldXY, plane);
      poly.Transform(transform);


      // Convert the polyline into a graphml file
      Graphml graph = new Graphml();
      if (weights != null && weights.Count > 0)
      {
          if (weights.Count < poly.SegmentCount)
          {
              int n = poly.SegmentCount - weights.Count;
              double last = weights[weights.Count - 1];
              for (int i = 0; i < n; i++)
              {
                  weights.Add(last);
              }
          }
          graph = new Graphml(poly, weights, weight_type);
      }
      else
      {
          graph = new Graphml(poly);
      }

      string graphmlXml = GraphmlSerializer.ToXml(graph);
      if (!SurferNative.TryRunGraphml(graphmlXml, string.Empty, -1, false, out string objData, out string nativeError))
      {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, nativeError);
          return;
      }
      var meshes = ParseObjMeshes(objData);
      if (meshes.Count == 0)
      {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "OBJ output did not contain any mesh faces.");
          return;
      }
      foreach (var mesh in meshes)
      {
          if (mesh == null) continue;

          //Remove the external points that appear in non-convex polygons
          var removals = new List<int>();
          Curve c = poly.ToNurbsCurve();
          for (var i = 0; i < mesh.Vertices.Count; i++)
          {
              if (c.Contains(mesh.Vertices[i], Plane.WorldXY, 0.001) == PointContainment.Outside)
                  removals.Add(i);
          }
          var ordered = removals.OrderByDescending(x => x);
          mesh.Vertices.Remove(ordered, false);

          mesh.Normals.ComputeNormals();
          mesh.Compact();

          //return the mesh to its rightful place
          mesh.Transform(reverse_transform);
      }
      var resultMeshes = new List<Mesh>(meshes.Count);
      foreach (var mesh in meshes)
      {
          if (mesh == null) continue;
          resultMeshes.Add(mesh.DuplicateMesh());
      }

      //Math.
      //Math.Tanh()

      DA.SetDataList(0, resultMeshes);

    }

    /// <summary>
    /// Provides an Icon for every component that will be visible in the User Interface.
    /// Icons need to be 24x24 pixels.
    /// You can add image files to your project resources and access them like this:
    /// return Resources.IconForThisComponent;
    /// </summary>
    protected override System.Drawing.Bitmap Icon => IconLoader.Roofer;

    /// <summary>
    /// Each component must have a unique Guid to identify it. 
    /// It is vital this Guid doesn't change otherwise old ghx files 
    /// that use the old ID will partially fail during loading.
    /// </summary>
    public override Guid ComponentGuid => new Guid("fa59155e-95b8-4195-8145-6be07ccdd863");

    private static List<Mesh> ParseObjMeshes(string objData)
    {
      var meshes = new List<Mesh>();
      if (string.IsNullOrWhiteSpace(objData)) return meshes;

      var vertices = new List<Point3d>();
      Mesh current = new Mesh();
      var remap = new Dictionary<int, int>();

      using (var reader = new StringReader(objData))
      {
        string line;
        while ((line = reader.ReadLine()) != null)
        {
          line = line.Trim();
          if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;

          if (line.StartsWith("o ", StringComparison.Ordinal) || line.StartsWith("g ", StringComparison.Ordinal))
          {
            if (current.Faces.Count > 0)
            {
              current.Normals.ComputeNormals();
              current.Compact();
              meshes.Add(current);
            }
            current = new Mesh();
            remap = new Dictionary<int, int>();
            continue;
          }

          if (line.StartsWith("v ", StringComparison.Ordinal))
          {
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) continue;
            double x = double.Parse(parts[1], CultureInfo.InvariantCulture);
            double y = double.Parse(parts[2], CultureInfo.InvariantCulture);
            double z = double.Parse(parts[3], CultureInfo.InvariantCulture);
            vertices.Add(new Point3d(x, y, z));
            continue;
          }

          if (line.StartsWith("f ", StringComparison.Ordinal))
          {
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) continue;

            int[] face = new int[parts.Length - 1];
            for (int i = 1; i < parts.Length; i++)
            {
              string token = parts[i];
              int slash = token.IndexOf('/');
              if (slash >= 0) token = token.Substring(0, slash);
              if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx)) return meshes;
              if (idx < 0) idx = vertices.Count + idx + 1;
              int globalIndex = idx - 1;
              if (!remap.TryGetValue(globalIndex, out int localIndex))
              {
                if (globalIndex < 0 || globalIndex >= vertices.Count) return meshes;
                localIndex = current.Vertices.Add(vertices[globalIndex]);
                remap[globalIndex] = localIndex;
              }
              face[i - 1] = localIndex;
            }

            if (face.Length == 3)
            {
              current.Faces.AddFace(face[0], face[1], face[2]);
            }
            else if (face.Length == 4)
            {
              current.Faces.AddFace(face[0], face[1], face[2], face[3]);
            }
            else if (face.Length > 4)
            {
              for (int i = 2; i < face.Length; i++)
              {
                current.Faces.AddFace(face[0], face[i - 1], face[i]);
              }
            }
          }
        }
      }

      if (current.Faces.Count > 0)
      {
        current.Normals.ComputeNormals();
        current.Compact();
        meshes.Add(current);
      }

      return meshes;
    }
  }
}
