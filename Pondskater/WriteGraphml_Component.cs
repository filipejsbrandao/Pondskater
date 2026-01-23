using System;
using System.Collections.Generic;

using System.Xml;
using System.Xml.Serialization;
using System.IO;

using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;

using Pondskater.IO;

namespace Pondskater
{
    public class WriteGraphml_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the WriteGraphml_Component class.
        /// </summary>
        public WriteGraphml_Component()
          : base("Write GraphML", "Write GML",
            "Write GraphML files",
            "Pondskater", "IO")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Polyline", "P", "A polyline", GH_ParamAccess.item);
            pManager.AddNumberParameter("Weights", "W", "A list of weights", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Type", "T", "True for Multiplicative weights and false for Additive weights", GH_ParamAccess.item, true);
            pManager.AddTextParameter("Dir", "D", "path to save the file", GH_ParamAccess.item);
            pManager.AddTextParameter("Filename", "F", "the filename", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Save", "S", "Save the file", GH_ParamAccess.item, false);
            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Xml", "X", "a string with the graphml", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve curve = new PolylineCurve();
            Polyline poly = new Polyline();
            List<double> weights = new List<double>();
            bool weight_type = true;
            string dir = String.Empty;
            string filename = String.Empty;
            bool save = false;

            if(!DA.GetData(0, ref curve)) return;
            DA.GetDataList(1, weights);
            if(!DA.GetData(2, ref weight_type)) return;
            if (!DA.GetData(3, ref dir)) return;
            if (!DA.GetData(4, ref filename)) return;
            if (!DA.GetData(5, ref save)) return;

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

            if (!Directory.Exists(dir))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "the folder was not found");
                return;
            }

            try
            {
                string fileext = Path.GetExtension(filename);
                if (fileext.Length == 0)
                {
                    filename += ".graphml";
                }
                else if (fileext != ".graphml")
                {
                    filename = Path.ChangeExtension(filename, "graphml");
                }
            }
            catch (ArgumentException)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "the filename is not valid");
                return;
            }

            string path = dir + "/" + filename;

            Transform transform = Transform.PlaneToPlane(uPlane, Plane.WorldXY);
            poly.Transform(transform);

            // ensure the list of weights is at least as large as the number of segments
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
                //graph.graph = new graphmlGraph(poly);
            }
            else
            {
                graph = new Graphml(poly);
            }

            string s = String.Empty;

            if (save)
            {
                s = GraphmlSerializer.ToXml(graph);
                File.WriteAllText(path, s);
            }
            
            DA.SetData(0, s);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("75E8378C-01A9-4EDD-AB8E-BB652C8E17D7"); }
        }
    }
}
