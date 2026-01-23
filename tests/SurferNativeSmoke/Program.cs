using System.Reflection;
using System.Runtime.InteropServices;

static class SurferNative
{
    private const string LibraryName = "surfer";

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
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

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void surf_free(IntPtr p);

    private static bool _resolverSet;

    internal static void LoadNative(string baseDir)
    {
        if (!_resolverSet)
        {
            NativeLibrary.SetDllImportResolver(typeof(SurferNative).Assembly, Resolve);
            _resolverSet = true;
        }

        string libName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "surfer.dll"
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? "libsurfer.dylib"
                : "libsurfer.so";

        string libPath = Path.Combine(baseDir, libName);
        if (!File.Exists(libPath))
        {
            throw new FileNotFoundException("Native library not found", libPath);
        }

        NativeLibrary.Load(libPath);
    }

    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, LibraryName, StringComparison.Ordinal))
        {
            return IntPtr.Zero;
        }

        string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string nativeBase = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(repoRoot, "native", "bin", "windows")
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? Path.Combine(repoRoot, "native", "bin", "macos")
                : Path.Combine(repoRoot, "native", "bin", "linux");

        string libName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "surfer.dll"
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? "libsurfer.dylib"
                : "libsurfer.so";

        string libPath = Path.Combine(nativeBase, libName);
        return File.Exists(libPath) ? NativeLibrary.Load(libPath) : IntPtr.Zero;
    }

    internal static string RunGraphml(string graphml, string skoffset, int component, bool writeIpe)
    {
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
            string output = outPtr != IntPtr.Zero && outLen > 0
                ? Marshal.PtrToStringUTF8(outPtr, outLen) ?? string.Empty
                : string.Empty;

            string error = errPtr != IntPtr.Zero && errLen > 0
                ? Marshal.PtrToStringUTF8(errPtr, errLen) ?? string.Empty
                : string.Empty;

            if (result != 0)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? $"surfer failed with code {result}" : error);
            }

            return output;
        }
        finally
        {
            if (outPtr != IntPtr.Zero) surf_free(outPtr);
            if (errPtr != IntPtr.Zero) surf_free(errPtr);
        }
    }
}

static class Program
{
    static int Main(string[] args)
    {
        try
        {
            string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
            string graphmlPath = Path.Combine(repoRoot, "tests", "graphml", "test_files", "test.graphml");
            if (!File.Exists(graphmlPath))
            {
                Console.Error.WriteLine($"Missing test graphml: {graphmlPath}");
                return 1;
            }

            string nativeBase = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(repoRoot, "native", "bin", "windows")
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? Path.Combine(repoRoot, "native", "bin", "macos")
                    : Path.Combine(repoRoot, "native", "bin", "linux");

            SurferNative.LoadNative(nativeBase);

            string graphml = File.ReadAllText(graphmlPath);
            string output = SurferNative.RunGraphml(graphml, string.Empty, -1, writeIpe: false);

            if (string.IsNullOrWhiteSpace(output))
            {
                Console.Error.WriteLine("surfer returned empty output");
                return 1;
            }

            Console.WriteLine("OK");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }
}
