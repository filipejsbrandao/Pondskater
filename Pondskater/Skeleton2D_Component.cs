using System;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;

using System.Xml;
using System.Xml.Serialization;
using System.IO;

using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;

using Pondskater.IO;
using Pondskater.Native;

namespace Pondskater
{
    public class Skeleton2D_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the _2DSkeleton_Component class.
        /// </summary>
        public Skeleton2D_Component()
          : base("Skeleton2d", "Sk2d",
            "Get the 2d skeleton of a planar polygon",
            "Pondskater", "Skeleton")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Polyline", "P", "A planar polyline", GH_ParamAccess.item);
            pManager.AddNumberParameter("Weights", "W", " list of weights per edge, by default a constant weight of 1 is assumed for all sides. Weights can be understood as the cotangent of the roof angle.", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Type", "T", "True for Multiplicative weights (edge speed = w) and false for Additive weights (edge speed = 1 + wa)", GH_ParamAccess.item, true);
            pManager.AddIntegerParameter("Direction", "D", "Skeleton side selection: inside = 0, outside = 1, both = 2", GH_ParamAccess.item, 0);
            pManager[1].Optional = true;
            pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Skeleton", "Sk", "The 2d skeleton of the provided polyline", GH_ParamAccess.list);
            pManager.AddTextParameter("Ipe output", "IO", "The command result in ipe format", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve curve = new PolylineCurve();
            Polyline poly = new Polyline();
            List<Line> skeleton = new List<Line>();
            List<double> weights = new List<double>();
            bool weight_type = true;
            int direction = 2;
            const double offsetDistance = 1.0;
            
            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!SurferNative.TryLoad(directory, out string loadError))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, loadError);
                return;
            }

            if (!DA.GetData(0, ref curve)) return;
            DA.GetDataList(1, weights);
            if (!DA.GetData(2, ref weight_type)) return;
            DA.GetData(3, ref direction);

            if (!curve.IsPlanar())
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The polygon must be a planar polyline");
                return;
            }

            //To ensure this always works we must move the polygon to the plane XY and then return it to it original position
            curve.TryGetPlane(out Plane uPlane);

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

            Transform transform = Transform.PlaneToPlane(uPlane, Plane.WorldXY);
            Transform reverse_transform = Transform.PlaneToPlane(Plane.WorldXY, uPlane);
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
            if (direction < 0 || direction > 2)
            {
                direction = 2;
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "direction values must be between 0 and 2");
            }

            int component = (direction < 2) ? direction : -1;
            string skoffset = offsetDistance > 0
                ? offsetDistance.ToString("g17", CultureInfo.InvariantCulture)
                : string.Empty;

            if (!SurferNative.TryRunGraphml(graphmlXml, skoffset, component, true, out string result, out string nativeError))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, nativeError);
                return;
            }

            Ipe ipe2 = new Ipe();
            // convert the string to an ipe
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Ipe));

                using (TextReader reader = new StringReader(result))
                {
                    ipe2 = (Ipe)serializer.Deserialize(reader);
                }
                //ipe = DeserializeIpe(result);
            }
            catch (ArgumentException ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
                return;
            }
            
            // Collect skeleton paths. With offsets enabled, skeleton is typically in Group[1].
            if (ipe2?.Page?.Group == null || ipe2.Page.Group.Length == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unexpected Ipe structure (missing groups).");
                return;
            }
            if (!string.IsNullOrEmpty(skoffset))
            {
                if (ipe2.Page.Group.Length > 1 && ipe2.Page.Group[1]?.Path != null)
                {
                    foreach (ipePageGroupPath p in ipe2.Page.Group[1].Path)
                    {
                        Line l = p.PathToRHLine();
                        l.Transform(reverse_transform);
                        skeleton.Add(l);
                    }
                }
            }
            else
            {
                foreach (ipePageGroup group in ipe2.Page.Group)
                {
                    if (group?.Path != null)
                    {
                        foreach (ipePageGroupPath p in group.Path)
                        {
                            Line l = p.PathToRHLine();
                            l.Transform(reverse_transform);
                            skeleton.Add(l);
                        }
                    }
                    if (group?.Group != null)
                    {
                        foreach (ipePageGroupGroup sub in group.Group)
                        {
                            if (sub?.Path == null)
                            {
                                continue;
                            }
                            foreach (ipePageGroupGroupPath p in sub.Path)
                            {
                                Line l = p.PathToRHLine();
                                l.Transform(reverse_transform);
                                skeleton.Add(l);
                            }
                        }
                    }
                }
            }
            if (skeleton.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unexpected Ipe structure (no paths found).");
                return;
            }

            DA.SetDataList(0, skeleton);
            DA.SetData(1, result);

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => IconLoader.Skeleton;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("840088D4-2C0B-45B1-BA21-C8DAF63B4161"); }
        }

        /// <summary>
        /// Deserialize an Ipe file
        /// </summary>
        /// <param name="filestream">An ipe file</param>
        /// <returns>An object contain</returns>
        private Pondskater.IO.Ipe DeserializeIpe(string filestream)
        {
            // Create an instance of the XmlSerializer specifying type and namespace.
            XmlSerializer serializer = new XmlSerializer(typeof(Ipe));

            // A FileStream is needed to read the XML document.

            Ipe ipe;
            using (TextReader reader = new StringReader(filestream))
            {
                ipe = (Ipe) serializer.Deserialize(reader);
            }

            // Declare an object variable of the type to be deserialized.
            

            // Use the Deserialize method to restore the object's state.
            //ipe = (Ipe)serializer.Deserialize(reader);
            //fs.Close();

            return ipe;
        }

    }
}
