using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Roslyn.Services.Internal.Log
{
    [ExcludeFromCodeCoverage]
    internal sealed class CodeMarkers
    {
        // Singleton access
        public static readonly CodeMarkers Instance = new CodeMarkers();

        public static class NativeMethods
        {
            // Code markers' functions (imported from the code markers dll)
#if Codemarkers_IncludeAppEnum
            [DllImport(DllName, EntryPoint = "InitPerf")]
            public static extern void DllInitPerf(System.Int32 iApp);

            [DllImport(DllName, EntryPoint = "UnInitPerf")]
            public static extern void DllUnInitPerf(System.Int32 iApp);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments")]
            public static extern System.UInt16 AddAtom(string lpString);

            [DllImport("kernel32.dll")]
            public static extern System.UInt16 DeleteAtom(System.UInt16 atom);
#endif //Codemarkers_IncludeAppEnum

            [DllImport(DllName, EntryPoint = "PerfCodeMarker")]
            public static extern void DllPerfCodeMarker(int nTimerID, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] aUserParams, int cbParams);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments")]
            public static extern short FindAtom(string lpString);
        }

        // Atom name. This ATOM will be set by the host application when code markers are enabled
        // in the registry.
        private const string AtomName = "VSCodeMarkersEnabled";

        // CodeMarkers DLL name
        private const string DllName = "Microsoft.Internal.Performance.CodeMarkers.dll";

        // Do we want to use code markers?
        private bool useCodeMarkers;

        // Constructor. Do not call directly. Use CodeMarkers.Instance to access the singleton
        // Checks to see if code markers are enabled by looking for a named ATOM
        private CodeMarkers()
        {
            // This ATOM will be set by the native Code Markers host
            useCodeMarkers = NativeMethods.FindAtom(AtomName) != 0;
        }

        // Implements sending the code marker value nTimerID.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void CodeMarker(CodeMarkerId timerID)
        {
            if (!useCodeMarkers)
            {
                return;
            }

            try
            {
                NativeMethods.DllPerfCodeMarker((int)timerID, null, 0);
            }
            catch (DllNotFoundException)
            {
                // If the DLL doesn't load or the entry point doesn't exist, then abandon all
                // further attempts to send code markers.
                useCodeMarkers = false;
            }
        }

        // Implements sending the code marker value nTimerID with additional user data
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void CodeMarkerEx(CodeMarkerId timerID, byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("aBuff");
            }

            if (!useCodeMarkers)
            {
                return;
            }

            try
            {
                NativeMethods.DllPerfCodeMarker((int)timerID, buffer, buffer.Length);
            }
            catch (DllNotFoundException)
            {
                // If the DLL doesn't load or the entry point doesn't exist, then abandon all
                // further attempts to send code markers.
                useCodeMarkers = false;
            }
        }

        // Implements sending the code marker value nTimerID with additional Guid user data
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void CodeMarkerEx(CodeMarkerId timerID, Guid guidData)
        {
            CodeMarkerEx(timerID, guidData.ToByteArray());
        }

#if Codemarkers_IncludeAppEnum
        // Check the registry and, if appropriate, loads and initializes the code markers dll.
        // Must be used only if your code is called from outside of VS.
        public void InitPerformanceDll(CodeMarkerApp iApp, string strRegRoot)
        {
            fUseCodeMarkers = false;

            if (!UseCodeMarkers(strRegRoot))
            {
                return;
            }

            try
            {
                // Add an ATOM so that other CodeMarker enabled code in this process
                // knows that CodeMarkers are enabled 
                NativeMethods.AddAtom(AtomName);

                NativeMethods.DllInitPerf((int)iApp);
                fUseCodeMarkers = true;
            }
            catch (DllNotFoundException)
            {
                ; // Ignore, but note that fUseCodeMarkers is false
            }
        }

        // Checks the registry to see if code markers are enabled
        static bool UseCodeMarkers(string strRegRoot)
        {
            // SECURITY: We no longer check HKCU because that might lead to a DLL spoofing attack via
            // the code markers DLL. Check only HKLM since that has a strong ACL. You therefore need
            // admin rights to enable/disable code markers.

            // It doesn't matter what the value is, if it's present and not empty, code markers are enabled
            return !String.IsNullOrEmpty(GetPerformanceSubKey(Registry.LocalMachine, strRegRoot));
        }

        // Reads the Performance subkey from the appropriate registry key
        // Returns: the Default value from the subkey (null if not found)
        static string GetPerformanceSubKey(RegistryKey hKey, string strRegRoot)
        {
            if (hKey == null)
                return null;

            // does the subkey exist
            string str = null;
            using (RegistryKey key = hKey.OpenSubKey(strRegRoot + "\\Performance"))
            {
                if (key != null)
                {
                    // reads the default value
                    str = key.GetValue("").ToString();
                }
            }
            return str;
        }

        // Opposite of InitPerformanceDLL. Call it when your app does not need the code markers dll.
        public void UninitializePerformanceDLL(CodeMarkerApp iApp)
        {
            if (!fUseCodeMarkers)
            {
                return;
            }

            fUseCodeMarkers = false;

            // Delete the atom created during the initialization if it exists
            System.UInt16 atom = NativeMethods.FindAtom(AtomName);
            if (atom != 0)
            {
                NativeMethods.DeleteAtom(atom);
            }

            try
            {
                NativeMethods.DllUnInitPerf((int)iApp);
            }
            catch (DllNotFoundException)
            {
                // Swallow exception
            }
        }
#endif //Codemarkers_IncludeAppEnum

#if !Codemarkers_NoCodeMarkerStartEnd
        /// <summary>
        /// Use CodeMarkerStartEnd in a using clause when you need to bracket an
        /// operation with a start/end CodeMarker event pair.
        /// </summary>
        internal sealed class CodeMarkerStartEnd : IDisposable
        {
            private CodeMarkerId end;

            public CodeMarkerStartEnd(CodeMarkerId begin, CodeMarkerId end)
            {
                Debug.Assert(end != (CodeMarkerId)0);
                Instance.CodeMarker(begin);
                this.end = end;
            }

            public void Dispose()
            {
                if (this.end != (CodeMarkerId)0)
                {
                    // Protect against multiple Dispose calls
                    Instance.CodeMarker(this.end);
                    this.end = (CodeMarkerId)0;
                }
            }
        }

        /// <summary>
        /// Use CodeMarkerExStartEnd in a using clause when you need to bracket an
        /// operation with a start/end CodeMarker event pair.
        /// </summary>
        internal sealed class CodeMarkerExStartEnd : IDisposable
        {
            private CodeMarkerId end;
            private byte[] buffer;

            public CodeMarkerExStartEnd(CodeMarkerId begin, CodeMarkerId end, byte[] buffer)
            {
                Debug.Assert(end != (CodeMarkerId)0);
                Instance.CodeMarkerEx(begin, buffer);
                this.end = end;
                this.buffer = buffer;
            }

            // Specialization to use Guids for the code marker data
            public CodeMarkerExStartEnd(CodeMarkerId begin, CodeMarkerId end, Guid guidData)
                : this(begin, end, guidData.ToByteArray())
            {
            }

            public void Dispose()
            {
                if (this.end != (CodeMarkerId)0)
                {
                    // Protect against multiple Dispose calls
                    Instance.CodeMarkerEx(this.end, this.buffer);
                    this.end = (CodeMarkerId)0;
                    this.buffer = null;
                }
            }
        }
#endif
    }
}
