using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

namespace GraphML_IO
{
    public class GraphML_IOInfo : GH_AssemblyInfo
    {
        public override string Name => "GraphML_IO";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("584776A8-5D24-4824-B67F-FF8F1987C42B");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}
