using System;

using Grasshopper.Kernel;

namespace Pondskater._3DCPUtils
{
    public class _3DCPSpeed : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the _3DCPSpeed class.
        /// </summary>
        public _3DCPSpeed()
          : base("3DCP Velocity", "3DCPSpeed",
              "Compute the nozzle velocity for 3D concrete printing based on the material flow velocity, nozzle diameter, layer width and nozzle height. The analytical expressions were derived from (Alhussain et al., 2024) and tunned based on experimental data and regression analysis collected at the ARENA Lab of the School of Architecture Art and Design of the University of Minho.",
              "Pondskater", "Paths")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Material Extrusion Velocity", "Vm", "The material extrusion velocity in millimeters per second (mm/s)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Nozzle Diameter", "D", "The nozzle diameter in millimeters (mm)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Filament Width", "W", "The filament width in millimeters (mm)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Nozzle Height", "Hn", "The nozzle height in millimeters (mm). Nozzle height can be considered equal to filament height if the extrusion is kept with adequate bounds, roughly between 1.7 and 2.3 width ratios.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Nozzle Velocity", "Vn", "The nozzle velocity in meters per second (m/s)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Contact Width", "Wc", "The contact width in millimeters (mm)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height Ratio", "H*", "The ratio between the Nozzle Heigth and the Nozzle Diameter", GH_ParamAccess.item);
            pManager.AddNumberParameter("Width Ratio", "W*", "The ratio between the Filament Width and Nozzle Diameter", GH_ParamAccess.item);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double materialFlowVelocity = 0;
            double nozzleDiameter = 0;
            double layerWidth = 0;
            double nozzleHeight = 0;

            if (!DA.GetData(0, ref materialFlowVelocity)) return;
            if (!DA.GetData(1, ref nozzleDiameter)) return;
            if (!DA.GetData(2, ref layerWidth)) return;
            if (!DA.GetData(3, ref nozzleHeight)) return;

            if (materialFlowVelocity <= 0 || nozzleDiameter <= 0 || layerWidth <= 0 || nozzleHeight <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "All inputs must be greater than zero.");
                return;
            }

            double HeightRatio = nozzleHeight / nozzleDiameter;
            double WidthRatio = layerWidth / nozzleDiameter;
            double denominator = HeightRatio * (WidthRatio - 0.0139 - 0.2784 * HeightRatio);
            if (Math.Abs(denominator) < 1e-9)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input ratios produce an unstable denominator.");
                return;
            }
            double velocityRatio = 0.7188 / denominator;

            // Calculate the nozzle velocity
            double nozzleVelocity = materialFlowVelocity * velocityRatio / 1000; // Convert from mm/s to m/s
            if (Math.Abs(velocityRatio * HeightRatio) < 1e-9)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input ratios produce an unstable contact width expression.");
                return;
            }
            double contactWidth = 0.7059 * (1 / (velocityRatio * HeightRatio)) - 0.0783 * HeightRatio - 0.0935;

            DA.SetData(0, nozzleVelocity);
            DA.SetData(1, contactWidth);
            DA.SetData(2, HeightRatio);
            DA.SetData(3, WidthRatio);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return IconLoader.ThreeDcpSpeed;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6B430929-A4B4-4086-9230-6E0813D7AC67"); }
        }
    }
}
