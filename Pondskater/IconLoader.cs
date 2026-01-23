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

    static IconLoader()
    {
      Offset = LoadIcon("Pondskater.Resources.offset.png");
      Skeleton = LoadIcon("Pondskater.Resources.skeleton2d.png");
      Roofer = LoadIcon("Pondskater.Resources.roofer.png");
    }

    private static Bitmap LoadIcon(string resourceName)
    {
      string[] resNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
      foreach (string resName in resNames)
      Console.WriteLine(resName);
      Console.WriteLine(resourceName);
      var assembly = Assembly.GetExecutingAssembly();
      var imageStream = assembly.GetManifestResourceStream(resourceName);

      return new Bitmap(imageStream);
    }

  }

}