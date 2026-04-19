using System.Reflection;
using System.Drawing;
using System;

namespace Pondskater
{
  public static class IconLoader
  {

    public static Bitmap Offset { get; private set; }
    public static Bitmap Skeleton { get; private set; }
    public static Bitmap Roofer { get; private set; }
    public static Bitmap ReadGraphml { get; private set; }
    public static Bitmap WriteGraphml { get; private set; }
    public static Bitmap RandomHamilton { get; private set; }
    public static Bitmap ThreeDcpSpeed { get; private set; }
    public static Bitmap EdgeAlignedBoundingRectangle { get; private set; }
    public static Bitmap MinimumBoundingRectangle { get; private set; }
    public static Bitmap Eulerian { get; private set; }
    public static Bitmap Hamiltonian { get; private set; }
    public static Bitmap LComponent { get; private set; }
    public static Bitmap TComponent { get; private set; }
    public static Bitmap NComponent { get; private set; }
    public static Bitmap MiterComponent { get; private set; }
    public static Bitmap PolygonWidth { get; private set; }
    public static Bitmap MetricSubdivision { get; private set; }
    public static Bitmap SymmetricSubdivision { get; private set; }
    public static Bitmap PondskaterIcon { get; private set; }


    static IconLoader()
    {
      Offset = LoadIcon("Pondskater.Resources.offset.png");
      Skeleton = LoadIcon("Pondskater.Resources.skeleton2d.png");
      Roofer = LoadIcon("Pondskater.Resources.roofer.png");
      ReadGraphml = LoadIcon("Pondskater.Resources.ReadGML.png");
      WriteGraphml = LoadIcon("Pondskater.Resources.WriteGML.png");
      RandomHamilton = LoadIcon("Pondskater.Resources.RandomHamilton.png");
      ThreeDcpSpeed = LoadIcon("Pondskater.Resources.3dcp.png");
      EdgeAlignedBoundingRectangle = LoadIcon("Pondskater.Resources.EdgeAligned.png");
      MinimumBoundingRectangle = LoadIcon("Pondskater.Resources.MBR.png");
      Eulerian = LoadIcon("Pondskater.Resources.Eulerian.png");
      Hamiltonian = LoadIcon("Pondskater.Resources.Hamiltonian.png");
      LComponent = LoadIcon("Pondskater.Resources.L_Component.png");
      TComponent = LoadIcon("Pondskater.Resources.T_Component.png");
      NComponent = LoadIcon("Pondskater.Resources.N_Component.png");
      MiterComponent = LoadIcon("Pondskater.Resources.Mitter_Component.png");
      PolygonWidth = LoadIcon("Pondskater.Resources.PolygonWidth.png");
      MetricSubdivision = LoadIcon("Pondskater.Resources.Metric.png");
      SymmetricSubdivision = LoadIcon("Pondskater.Resources.Symmetrical.png");
      PondskaterIcon = LoadIcon("Pondskater.Resources.Pondskater_Icon.png");
    }

    private static Bitmap LoadIcon(string resourceName)
    {
      var assembly = Assembly.GetExecutingAssembly();
      var imageStream = assembly.GetManifestResourceStream(resourceName);
      if (imageStream == null)
      {
        throw new InvalidOperationException($"Missing embedded resource: {resourceName}");
      }
      return new Bitmap(imageStream);
    }

  }

}
