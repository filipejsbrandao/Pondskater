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

    static IconLoader()
    {
      Offset = LoadIcon("Pondskater.Resources.offset.png");
      Skeleton = LoadIcon("Pondskater.Resources.skeleton2d.png");
      Roofer = LoadIcon("Pondskater.Resources.roofer.png");
      ReadGraphml = LoadIcon("Pondskater.Resources.ReadGML.png");
      WriteGraphml = LoadIcon("Pondskater.Resources.WriteGML.png");
      RandomHamilton = LoadIcon("Pondskater.Resources.RandomHamilton.png");
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
