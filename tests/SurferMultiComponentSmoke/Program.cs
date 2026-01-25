using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

static class Program
{
    private static int Main(string[] args)
    {
        string repoRoot = Directory.GetCurrentDirectory();
        string outputDir = Path.Combine(repoRoot, "tests", "SurferMultiComponentSmoke");
        Directory.CreateDirectory(outputDir);

        string graphmlPath = Path.Combine(outputDir, "input.graphml");
        string errorPath = Path.Combine(outputDir, "errors.txt");

        string graphmlXml = BuildTwoPolygonGraphml();
        File.WriteAllText(graphmlPath, graphmlXml, new UTF8Encoding(false));

        if (!TryLoadNative(repoRoot, out string loadError))
        {
            File.WriteAllText(errorPath, loadError + Environment.NewLine);
            Console.Error.WriteLine(loadError);
            return 1;
        }

        var errors = new List<string>();

        if (!RunComponent(graphmlXml, 0, Path.Combine(outputDir, "output_component_0.ipe"), errors))
        {
            File.WriteAllText(errorPath, string.Join(Environment.NewLine, errors), Encoding.UTF8);
            return 2;
        }

        if (!RunComponent(graphmlXml, 1, Path.Combine(outputDir, "output_component_1.ipe"), errors))
        {
            File.WriteAllText(errorPath, string.Join(Environment.NewLine, errors), Encoding.UTF8);
            return 3;
        }

        File.WriteAllText(errorPath, errors.Count == 0 ? "(no errors)" : string.Join(Environment.NewLine, errors), Encoding.UTF8);

        Console.WriteLine("Wrote:");
        Console.WriteLine(graphmlPath);
        Console.WriteLine(Path.Combine(outputDir, "output_component_0.ipe"));
        Console.WriteLine(Path.Combine(outputDir, "output_component_1.ipe"));
        Console.WriteLine(errorPath);
        return 0;
    }

    private static string BuildTwoPolygonGraphml()
    {
        var nodes = new List<(string id, double x, double y)>();
        var edges = new List<(string source, string target)>();

        AddPolygon(nodes, edges, new[]
        {
            (0.0, 0.0), (10.0, 0.0), (10.0, 10.0), (0.0, 10.0)
        });

        AddPolygon(nodes, edges, new[]
        {
            (20.0, 0.0), (30.0, 0.0), (30.0, 10.0), (20.0, 10.0)
        });

        return BuildGraphmlXml(nodes, edges);
    }

    private static void AddPolygon(List<(string id, double x, double y)> nodes, List<(string source, string target)> edges, (double x, double y)[] pts)
    {
        int baseIndex = nodes.Count;
        for (int i = 0; i < pts.Length; i++)
        {
            nodes.Add(((baseIndex + i).ToString(CultureInfo.InvariantCulture), pts[i].x, pts[i].y));
        }

        for (int i = 0; i < pts.Length; i++)
        {
            int from = baseIndex + i;
            int to = baseIndex + ((i + 1) % pts.Length);
            edges.Add((from.ToString(CultureInfo.InvariantCulture), to.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private static string BuildGraphmlXml(List<(string id, double x, double y)> nodes, List<(string source, string target)> edges)
    {
        XNamespace ns = "http://graphml.graphdrawing.org/xmlns";
        var graphml = new XElement(ns + "graphml",
            new XElement(ns + "key",
                new XAttribute("id", "x"),
                new XAttribute("for", "node"),
                new XAttribute("attr.name", "vertex-coordinate-x"),
                new XAttribute("attr.type", "string")),
            new XElement(ns + "key",
                new XAttribute("id", "y"),
                new XAttribute("for", "node"),
                new XAttribute("attr.name", "vertex-coordinate-y"),
                new XAttribute("attr.type", "string")),
            new XElement(ns + "key",
                new XAttribute("id", "w"),
                new XAttribute("for", "edge"),
                new XAttribute("attr.name", "edge-weight"),
                new XAttribute("attr.type", "string"),
                new XElement(ns + "default", "1")),
            new XElement(ns + "key",
                new XAttribute("id", "wa"),
                new XAttribute("for", "edge"),
                new XAttribute("attr.name", "edge-weight-additive"),
                new XAttribute("attr.type", "string"),
                new XElement(ns + "default", "0")),
            new XElement(ns + "graph",
                new XAttribute("edgedefault", "undirected"))
        );

        XElement graph = graphml.Element(ns + "graph")!;
        foreach (var node in nodes)
        {
            graph.Add(new XElement(ns + "node",
                new XAttribute("id", node.id),
                new XElement(ns + "data", new XAttribute("key", "x"), node.x.ToString("g17", CultureInfo.InvariantCulture)),
                new XElement(ns + "data", new XAttribute("key", "y"), node.y.ToString("g17", CultureInfo.InvariantCulture))
            ));
        }

        foreach (var edge in edges)
        {
            graph.Add(new XElement(ns + "edge",
                new XAttribute("source", edge.source),
                new XAttribute("target", edge.target),
                new XElement(ns + "data", new XAttribute("key", "w"), "1")
            ));
        }

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), graphml);
        string xml = doc.ToString(SaveOptions.DisableFormatting);
        return "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + xml;
    }

    private static bool TryLoadNative(string repoRoot, out string error)
    {
        error = string.Empty;
        string libPath;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            libPath = Path.Combine(repoRoot, "native", "bin", "macos", "libsurfer.dylib");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            libPath = Path.Combine(repoRoot, "native", "bin", "windows", "surfer.dll");
        }
        else
        {
            libPath = Path.Combine(repoRoot, "native", "bin", "linux", "libsurfer.so");
        }

        if (!File.Exists(libPath))
        {
            error = "Native library not found: " + libPath;
            return false;
        }

#if NET7_0_OR_GREATER
        try
        {
            NativeLibrary.Load(libPath);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
#else
        return true;
#endif
    }

    [DllImport("surfer", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int surf_run_graphml(
        string graphml,
        string skoffset,
        int component,
        int writeIpe,
        out IntPtr outPtr,
        out int outLen,
        out IntPtr errPtr,
        out int errLen
    );

    [DllImport("surfer", CallingConvention = CallingConvention.Cdecl)]
    private static extern void surf_free(IntPtr p);

    private static bool RunComponent(string graphml, int component, string outputPath, List<string> errors)
    {
        if (!TryRunGraphml(graphml, string.Empty, component, true, out string output, out string error))
        {
            errors.Add($"component {component} error: {error}");
            return false;
        }

        File.WriteAllText(outputPath, output, Encoding.UTF8);
        if (!string.IsNullOrWhiteSpace(error))
        {
            errors.Add($"component {component} stderr: {error}");
        }
        return true;
    }

    private static bool TryRunGraphml(string graphml, string skoffset, int component, bool writeIpe, out string output, out string error)
    {
        output = string.Empty;
        error = string.Empty;

        int result = surf_run_graphml(
            graphml,
            skoffset ?? string.Empty,
            component,
            writeIpe ? 1 : 0,
            out IntPtr outPtr,
            out int outLen,
            out IntPtr errPtr,
            out int errLen
        );

        try
        {
            if (outPtr != IntPtr.Zero && outLen > 0)
            {
                output = Marshal.PtrToStringUTF8(outPtr, outLen) ?? string.Empty;
            }
            if (errPtr != IntPtr.Zero && errLen > 0)
            {
                error = Marshal.PtrToStringUTF8(errPtr, errLen) ?? string.Empty;
            }
        }
        finally
        {
            if (outPtr != IntPtr.Zero) surf_free(outPtr);
            if (errPtr != IntPtr.Zero) surf_free(errPtr);
        }

        return result == 0;
    }
}
