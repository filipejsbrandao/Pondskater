using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

namespace Pondskater
{
    public class PondskaterCategoryIcon : Grasshopper.Kernel.GH_AssemblyPriority
  {
      public override Grasshopper.Kernel.GH_LoadingInstruction PriorityLoad()
      {
          Grasshopper.Instances.ComponentServer.AddCategoryIcon("Pondskater", Pondskater.IconLoader.PondskaterIcon);
          Grasshopper.Instances.ComponentServer.AddCategorySymbolName("Pondskater", 'P');
          return Grasshopper.Kernel.GH_LoadingInstruction.Proceed;
      }
  }
  public class PondskaterInfo : GH_AssemblyInfo
  {
    public override string Name => "Pondskater";

    //Return a 24x24 pixel bitmap to represent this GHA library.
    public override Bitmap Icon => IconLoader.PondskaterIcon;

    //Return a short string describing the purpose of this GHA library.
    public override string Description => "Pondskater provides tools for offseting, obtaining the straight skeleton and creating roofs or topographic features.";

    public override Guid Id => new Guid("def025e7-a8e6-468f-a97e-6a7255fe7c59");

    //Return a string identifying you or your company.
    public override string AuthorName => "Filipe Brandão";

    //Return a string representing your preferred contact details.
    public override string AuthorContact => "filipe.brandao@eaad.uminho.pt";
  }
}
