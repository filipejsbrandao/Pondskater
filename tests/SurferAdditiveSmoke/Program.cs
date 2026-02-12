using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

static class Program
{
    private static int Main()
    {
        string repoRoot = Directory.GetCurrentDirectory();
        string testsDir = Path.Combine(repoRoot, "tests", "SurferAdditiveSmoke");
        Directory.CreateDirectory(testsDir);

        string multPath = Path.Combine(repoRoot, "tests", "graphml", "weights", "multiplicative.graphml");
        string addPath = Path.Combine(repoRoot, "tests", "graphml", "weights", "additive.graphml");

        if (!File.Exists(multPath) || !File.Exists(addPath))
        {
            Console.Error.WriteLine("Missing graphml test inputs.");
            return 1;
        }

        if (!TryLoadNative(repoRoot, out string loadError, out string libPath))
        {
            Console.Error.WriteLine(loadError);
            return 2;
        }

        RegisterResolver(libPath);

        string multXml = File.ReadAllText(multPath);
        string addXml = File.ReadAllText(addPath);

        if (!TryRunGraphml(multXml, string.Empty, -1, true, out string multOut, out string multErr))
        {
            Console.Error.WriteLine("Multiplicative run failed: " + multErr);
            return 3;
        }
        if (!TryRunGraphml(addXml, string.Empty, -1, true, out string addOut, out string addErr))
        {
            Console.Error.WriteLine("Additive run failed: " + addErr);
            return 4;
        }

        File.WriteAllText(Path.Combine(testsDir, "output_multiplicative.ipe"), multOut, Encoding.UTF8);
        File.WriteAllText(Path.Combine(testsDir, "output_additive.ipe"), addOut, Encoding.UTF8);
        File.WriteAllText(Path.Combine(testsDir, "errors.txt"), $"mult: {multErr}\nadd: {addErr}", Encoding.UTF8);

        if (string.Equals(multOut, addOut, StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Outputs are identical; additive weights may not be applied.");
            return 5;
        }

        Console.WriteLine("Additive smoke test passed.");
        return 0;
    }

    private static bool TryLoadNative(string repoRoot, out string error, out string libPath)
    {
        error = string.Empty;
        libPath = string.Empty;
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

    private static void RegisterResolver(string libPath)
    {
#if NET7_0_OR_GREATER
        NativeLibrary.SetDllImportResolver(typeof(Program).Assembly, (name, assembly, path) =>
        {
            if (name == "surfer")
            {
                return NativeLibrary.Load(libPath);
            }
            return IntPtr.Zero;
        });
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
