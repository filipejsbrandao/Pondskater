using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Pondskater.Native
{
    internal static class SurferNative
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

#if NET7_0_OR_GREATER
        private static bool _loaded;
#endif

        internal static bool TryLoad(string baseDir, out string error)
        {
            error = null;
            string libPath = Path.Combine(baseDir, GetLibraryFileName());
            if (!File.Exists(libPath))
            {
                error = "Native library not found: " + libPath;
                return false;
            }

#if NET7_0_OR_GREATER
            if (_loaded) return true;
            try
            {
                NativeLibrary.Load(libPath);
                _loaded = true;
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

        internal static bool TryRunGraphml(string graphml, string skoffset, int component, bool writeIpe, out string output, out string error)
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
#if NET7_0_OR_GREATER
                    output = Marshal.PtrToStringUTF8(outPtr, outLen) ?? string.Empty;
#else
                    output = Marshal.PtrToStringAnsi(outPtr, outLen) ?? string.Empty;
#endif
                }
                if (errPtr != IntPtr.Zero && errLen > 0)
                {
#if NET7_0_OR_GREATER
                    error = Marshal.PtrToStringUTF8(errPtr, errLen) ?? string.Empty;
#else
                    error = Marshal.PtrToStringAnsi(errPtr, errLen) ?? string.Empty;
#endif
                }
            }
            finally
            {
                if (outPtr != IntPtr.Zero) surf_free(outPtr);
                if (errPtr != IntPtr.Zero) surf_free(errPtr);
            }

            return result == 0;
        }

        private static string GetLibraryFileName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "surfer.dll";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "libsurfer.dylib";
            return "libsurfer.so";
        }
    }
}
