using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

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
    public class Offset_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Offset class.
        /// </summary>
        public Offset_Component()
          : base("Offset", "O",
            "Offset description",
            "Pondskater", "Skeleton")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Polyline", "P", "A planar polyline", GH_ParamAccess.item);
            pManager.AddNumberParameter("Weights", "W", "A list of weights per edge, by default a constant weight is assumed for all sides", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Plane", "Pl", "The plane on which the curve lies", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Type", "T", "True for Multiplicative weights and false for Additive weights", GH_ParamAccess.item, true);
            pManager.AddIntegerParameter("Direction", "D", "Offset only inside - 0; offset only outside - 1; offset both sides - 2", GH_ParamAccess.item, 2);
            pManager.AddTextParameter("Distances", "Di",
                "offset-spec = <one-block> [ ',' <one-block> [ ',' ... ] ] /n " +
                "one - block = < one - offset > ['+' < one - offset > ['+'... ]] /n" +
                "one - offset = [< cnt > '*'] < time > /n" +
                "examples: '0.01 + 3 * 0.025, 0.15' or '10 * 0.025'" +
                " <cnt> an integer representing the amount of time an offset is repeated" +
                " <time> a double or integer representing the distance between offsets", GH_ParamAccess.item);
            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Skeleton", "Sk", "The 2d skeleton of the provided polyline", GH_ParamAccess.list);
            pManager.AddCurveParameter("Offsets", "O", "The offsets", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve curve = new PolylineCurve();
            Polyline poly = new Polyline();
            int direction = 2;
            Plane curvePlane = Plane.Unset;
            List<Line> skeleton = new List<Line>();
            List<Polyline> offsetList = new List<Polyline>();
            List<double> weights = new List<double>();
            bool weight_type = true;
            string distances = String.Empty;
            List<int> offsetSpecPattern = new List<int>();

            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!SurferNative.TryLoad(directory, out string loadError))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, loadError);
                return;
            }

            if (!DA.GetData(0, ref curve)) return;
            DA.GetDataList(1, weights);
            if (!DA.GetData(2, ref curvePlane)) return;
            if (!DA.GetData(3, ref weight_type)) return;
            DA.GetData(4, ref direction);
            if (!DA.GetData(5, ref distances)) return;

            try
            {
                offsetSpecPattern = OffsetParser.ParseOffsetSpec(distances);
                distances = RemoveSpaces(distances);
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "a valid string was found: " + distances);
            }
            catch (OffsetParserException ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
                return;
            }

            if (!curve.IsPlanar())
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The polygon must be a planar polyline");
                return;
            }

            // Ensure the curve is CCW
            Transform transform = Transform.PlaneToPlane(curvePlane, Plane.WorldXY);
            Transform reverse_transform = Transform.PlaneToPlane(Plane.WorldXY, curvePlane);
            curve.Transform(transform);

            if (curve.IsClosed && curve.ClosedCurveOrientation() == CurveOrientation.Clockwise)
            {
                curve.Reverse();
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

            // We prevent the user from providing wrong numbers
            if (direction < 0 || direction > 2 )
            {
                direction = 2;
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "direction values must be between 0 and 2");
            }

            // We prevent the user from requesting left sided offsets in open polylines
            if (!poly.IsClosed) direction = 2;

            /// TODO - See why polylines with one segment fail...

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
            int component = (direction < 2) ? direction : -1;
            if (!SurferNative.TryRunGraphml(graphmlXml, distances, component, true, out string result, out string nativeError))
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

            // Get the skeleton as a List of lines
            // if no offsets are requested then the skeleton is the first group -> Group[0]
            if (ipe2?.Page?.Group == null || ipe2.Page.Group.Length < 2 || ipe2.Page.Group[1]?.Path == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unexpected Ipe structure (missing skeleton group).");
                return;
            }
            ipePageGroupPath[] sk = ipe2.Page.Group[1].Path;
            foreach (ipePageGroupPath p in sk)
            {
                Line l = p.PathToRHLine();
                l.Transform(reverse_transform);
                skeleton.Add(l);
            }

            // Get the offsets as a list of polylines
            if (ipe2.Page.Group[0]?.Group == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unexpected Ipe structure (missing offsets group).");
                return;
            }
            ipePageGroupGroup[] offsets = ipe2.Page.Group[0].Group;
            foreach (ipePageGroupGroup offset in offsets)
            {
                if(offset.Path != null) {
                    ipePageGroupGroupPath[] lines = offset.Path;
                    List<Curve> offsetCurve = new List<Curve>();
                    foreach (ipePageGroupGroupPath line in lines)
                    {
                        Curve l = line.PathToRHLine().ToNurbsCurve();
                        l.Transform(reverse_transform);
                        offsetCurve.Add(l);
                    }
                    var polies = Curve.JoinCurves(offsetCurve, 0.001, true);
                    foreach (Curve p in polies)
                    {
                        Polyline polie = new Polyline();
                        p.TryGetPolyline(out polie);
                        offsetList.Add(polie);
                    }
                }
            }

            DA.SetDataList(0, skeleton);
            DA.SetDataList(1, offsetList);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => IconLoader.Offset;


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("65D1E3DD-7E33-4787-A5FE-CDDEA6B0FADF"); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filestream"></param>
        /// <returns></returns>
        private Ipe DeserializeIpe(string filestream)
        {
            // Create an instance of the XmlSerializer specifying type and namespace.
            XmlSerializer serializer = new XmlSerializer(typeof(Ipe));

            // A FileStream is needed to read the XML document.
            //FileStream fs = new FileStream(file, FileMode.Open);
            //XmlReader reader = XmlReader.Create(filestream);
            Ipe ipe;
            using (TextReader reader = new StringReader(filestream))
            {
                ipe = (Ipe)serializer.Deserialize(reader);
            }

            // Declare an object variable of the type to be deserialized.


            // Use the Deserialize method to restore the object's state.
            //ipe = (Ipe)serializer.Deserialize(reader);
            //fs.Close();

            return ipe;
        }
        private static string RemoveSpaces(string input)
        {
            return input.Replace(" ", "");
        }

    }
}
