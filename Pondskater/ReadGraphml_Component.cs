using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;

using Pondskater.IO;

namespace Pondskater
{
    public class ReadGraphml_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ReadGraphml_Component class.
        /// </summary>
        public ReadGraphml_Component()
          : base("GraphML To Polyline", "GML",
            "Convert a GraphML polyline to a Rhino Polyline",
            "Pondskater", "IO")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "P", "the path to the graphml file", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Polyline", "P", "The polyline", GH_ParamAccess.item);
            pManager.AddNumberParameter("Weigths", "W", "weights", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string path = String.Empty;
            Polyline poly = new Polyline();
            List<double> weights = new List<double>();

            if (!DA.GetData(0, ref path)) return;

            string fileext = String.Empty;

            try
            {
                fileext = Path.GetExtension(path);
                if(fileext.Length == 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The file does not have an extention");
                    return;
                }
                else if(fileext != ".graphml")
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The file extention is not .graphml");
                    return;
                }
            }
            catch (ArgumentException)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No extention was found");
                return;
            }

            if (!File.Exists(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "File not found");
                return;
            }

            Graphml graph = DeserializeGraphML(path);

            poly = graph.GraphmlToPolyline();
            weights = graph.GetGraphmlWeights();

            DA.SetData(0, poly);
            DA.SetDataList(1, weights);

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
            get { return new Guid("1D884B1D-E65D-479B-9D09-DB23E0957016"); }
        }

        /// <summary>
        /// Deserialize a GraphML file
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>An object of type Graphml</returns>
        private Pondskater.IO.Graphml DeserializeGraphML(string filename)
        {
            // Create an instance of the XmlSerializer specifying type and namespace.
            XmlSerializer serializer = new
            XmlSerializer(typeof(Graphml));

            // A FileStream is needed to read the XML document.
            FileStream fs = new FileStream(filename, FileMode.Open);
            XmlReader reader = XmlReader.Create(fs);

            // Declare an object variable of the type to be deserialized.
            Graphml gml;

            // Use the Deserialize method to restore the object's state.
            gml = (Graphml)serializer.Deserialize(reader);
            fs.Close();

            return gml;
        }
    }
}
