using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Pondskater
{
    public class RandomHamilton__Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the RandomHamilton__Component class.
        /// </summary>
        public RandomHamilton__Component()
          : base("Random Hamilton Path", "RH_Path",
              "Generates a random hamiltonian path from a seed zig-zag horizontal or vertical path by cutting and rotation parallel links",
              "Pondskater", "Paths")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Columns","N","Number of columns",GH_ParamAccess.item, 100);
            pManager.AddIntegerParameter("Rows","M","Number of rows",GH_ParamAccess.item, 10);
            pManager.AddIntegerParameter("Steps","S","Number of randomization steps",GH_ParamAccess.item, 4);
            pManager.AddBooleanParameter("Increment X", "iX", "Increment or decrement from the start point in the X direction. True for increment.", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Increment Y", "iY", "Increment or decrement from the start point in the Y direction. True for increment.", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Horizontal", "H", "Create the seed zig-zag path in the horizontal or vertical direction. True for horizontal.", GH_ParamAccess.item, true);
            pManager.AddIntegerParameter("Seed", "S", "Seed for random value.", GH_ParamAccess.item, 2);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Path", "P", "The randomized hamiltonian path", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int width = 10;
            int height = 10;
            int steps = 10;
            bool horiz = true;
            bool incrX = true;
            bool incrY = true;
            int seed = 2;

            if (!DA.GetData(0, ref width)) return;
            if (!DA.GetData(1, ref height)) return;
            if (!DA.GetData(2, ref steps)) return;
            if (!DA.GetData(3, ref incrX)) return;
            if (!DA.GetData(4, ref incrY)) return;
            if (!DA.GetData(5, ref horiz)) return;
            if (!DA.GetData(6, ref seed)) return;

            if(width <= 2 || height <= 2){
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "there must be at least 2 rows and 2 columns");
                return;
            }

            HPath hPath = new HPath(width, height, horiz, incrX, incrY);

            Random rnd = new Random(seed);
            for(int i = 0; i < steps; i++){
                hPath.RandomCut(rnd);
            }

            DA.SetData (0, hPath.PathToPolyline());
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return IconLoader.RandomHamilton;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("23A4F6F7-1181-4978-9A1B-E02F72FD8072"); }
        }
    }
}
