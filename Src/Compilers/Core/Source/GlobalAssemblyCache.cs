using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides APIs to enumerate and look up assemblies stored in the Global Assembly Cache.
    /// </summary>
    public static class GlobalAssemblyCache
    {
        /// <summary>
        /// Represents the current Processor architecture
        /// </summary>
        public static readonly Func<ProcessorArchitecture, bool> CurrentArchitectureFilter = (IntPtr.Size == 4) ?
            new Func<ProcessorArchitecture, bool>(a => a == ProcessorArchitecture.None || a == ProcessorArchitecture.MSIL || a == ProcessorArchitecture.X86) :
            new Func<ProcessorArchitecture, bool>(a => a == ProcessorArchitecture.None || a == ProcessorArchitecture.MSIL || a == ProcessorArchitecture.Amd64);

        #region Interop

        private const int MAX_PATH = 260;

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("21b8916c-f28e-11d2-a473-00c04f8ef448")]
        private interface IAssemblyEnum
        {
            [PreserveSig]
            int GetNextAssembly(out AssemblyIdentity.IApplicationContext ppAppCtx, out AssemblyIdentity.IAssemblyName ppName, uint dwFlags);

            [PreserveSig]
            int Reset();

            [PreserveSig]
            int Clone(out IAssemblyEnum ppEnum);
        }

        [ComImport, Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAssemblyCache
        {
            void UninstallAssembly();

            void QueryAssemblyInfo(uint dwFlags, [MarshalAs(UnmanagedType.LPWStr)] string pszAssemblyName, ref ASSEMBLY_INFO pAsmInfo);

            void CreateAssemblyCacheItem();
            void CreateAssemblyScavenger();
            void InstallAssembly();
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct ASSEMBLY_INFO
        {
            public uint cbAssemblyInfo;
            public uint dwAssemblyFlags;
            public ulong uliAssemblySizeInKB;
            public char* pszCurrentAssemblyPathBuf;
            public uint cchBuf;
        }

        private enum ASM_CACHE
        {
            ZAP = 0x1,
            GAC = 0x2,                // C:\Windows\Assembly\GAC
            DOWNLOAD = 0x4,
            ROOT = 0x8,               // C:\Windows\Assembly
            GAC_MSIL = 0x10,
            GAC_32 = 0x20,            // C:\Windows\Assembly\GAC_32
            GAC_64 = 0x40,            // C:\Windows\Assembly\GAC_64
            ROOT_EX = 0x80,           // C:\Windows\Microsoft.NET\assembly
        }

        [DllImport("clr", CharSet = CharSet.Auto, PreserveSig = true)]
        private static extern int CreateAssemblyEnum(out IAssemblyEnum ppEnum, AssemblyIdentity.IApplicationContext pAppCtx, AssemblyIdentity.IAssemblyName pName, ASM_CACHE dwFlags, IntPtr pvReserved);

        [DllImport("clr", CharSet = CharSet.Auto, PreserveSig = true)]
        private static unsafe extern int GetCachePath(ASM_CACHE id, byte* path, ref int length);

        [DllImport("clr", CharSet = CharSet.Auto, PreserveSig = false)]
        private static extern void CreateAssemblyCache(out IAssemblyCache ppAsmCache, uint dwReserved);

        private const int ERROR_INSUFFICIENT_BUFFER = unchecked((int)0x8007007A);

        public static readonly ImmutableArray<string> RootLocations;

        static GlobalAssemblyCache()
        {
            RootLocations = ImmutableArray.Create<string>(
                GetLocation(ASM_CACHE.ROOT),
                GetLocation(ASM_CACHE.ROOT_EX)
            );
        }

        private static unsafe string GetLocation(ASM_CACHE gacId)
        {
            int characterCount = 0;
            int hr = GetCachePath(gacId, null, ref characterCount);
            if (hr != ERROR_INSUFFICIENT_BUFFER)
            {
                throw Marshal.GetExceptionForHR(hr);
            }

            byte[] data = new byte[((int)characterCount + 1) * 2];
            fixed (byte* p = data)
            {
                hr = GetCachePath(gacId, p, ref characterCount);
                if (hr != 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }

                return Marshal.PtrToStringUni((IntPtr)p);
            }
        }

        #endregion

        /// <summary>
        /// Enumerates assemblies in the GAC returning those that match given partial name and
        /// architecture.
        /// </summary>
        /// <param name="partialName">Optional partial name.</param>
        /// <param name="architectureFilter">Optional architecture filter.</param>
        public static IEnumerable<AssemblyIdentity> GetAssemblyIdentities(AssemblyName partialName, Func<ProcessorArchitecture, bool> architectureFilter = null)
        {
            return GetAssemblyIdentities(AssemblyIdentity.ToAssemblyNameObject(partialName), architectureFilter);
        }

        /// <summary>
        /// Enumerates assemblies in the GAC returning those that match given partial name and
        /// architecture.
        /// </summary>
        /// <param name="partialName">The optional partial name.</param>
        /// <param name="architectureFilter">The optional architecture filter.</param>
        public static IEnumerable<AssemblyIdentity> GetAssemblyIdentities(string partialName = null, Func<ProcessorArchitecture, bool> architectureFilter = null)
        {
            AssemblyIdentity.IAssemblyName nameObj;
            if (partialName != null)
            {
                nameObj = AssemblyIdentity.ToAssemblyNameObject(partialName);
                if (nameObj == null)
                {
                    return Enumerable.Empty<AssemblyIdentity>();
                }
            }
            else
            {
                nameObj = null;
            }

            return GetAssemblyIdentities(nameObj, architectureFilter);
        }

        /// <summary>
        /// Enumerates assemblies in the GAC returning their simple names.
        /// </summary>
        /// <param name="architectureFilter">Optional architecture filter.</param>
        /// <returns>Unique simple names of GAC assemblies.</returns>
        public static IEnumerable<string> GetAssemblySimpleNames(Func<ProcessorArchitecture, bool> architectureFilter = null)
        {
            var q = from nameObject in GetAssemblyObjects(partialNameFilter: null, architectureFilter: architectureFilter)
                    select AssemblyIdentity.GetName(nameObject);
            return q.Distinct();
        }

        private static IEnumerable<AssemblyIdentity> GetAssemblyIdentities(
            AssemblyIdentity.IAssemblyName partialName,
            Func<ProcessorArchitecture, bool> architectureFilter)
        {
            return from nameObject in GetAssemblyObjects(partialName, architectureFilter)
                   select AssemblyIdentity.ToAssemblyIdentity(nameObject);
        }

        private const int S_OK = 0;
        private const int S_FALSE = 1;

        // Internal for testing.
        internal static IEnumerable<AssemblyIdentity.IAssemblyName> GetAssemblyObjects(
            AssemblyIdentity.IAssemblyName partialNameFilter,
            Func<ProcessorArchitecture, bool> architectureFilter)
        {
            IAssemblyEnum enumerator;
            AssemblyIdentity.IApplicationContext applicationContext = null;

            int hr = CreateAssemblyEnum(out enumerator, applicationContext, partialNameFilter, ASM_CACHE.GAC, IntPtr.Zero);
            if (hr == S_FALSE)
            {
                // no assembly found
                yield break;
            }
            else if (hr != S_OK)
            {
                Exception e = Marshal.GetExceptionForHR(hr);
                if (e is FileNotFoundException)
                {
                    // invalid assembly name:
                    yield break;
                }
                else if (e != null)
                {
                    throw e;
                }
                else
                {
                    // for some reason it might happen that CreateAssemblyEnum returns non-zero HR that doesn't correspond to any exception:
                    throw new ArgumentException(CodeAnalysisResources.InvalidAssemblyName);
                }
            }

            while (true)
            {
                AssemblyIdentity.IAssemblyName nameObject;

                hr = enumerator.GetNextAssembly(out applicationContext, out nameObject, 0);
                if (hr != 0)
                {
                    if (hr < 0)
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }

                    break;
                }

                if (architectureFilter != null)
                {
                    var assemblyArchitecture = AssemblyIdentity.GetProcessorArchitecture(nameObject);
                    if (!architectureFilter(assemblyArchitecture))
                    {
                        continue;
                    }
                }

                yield return nameObject;
            }
        }

        /// <summary>
        /// Looks up specified partial assembly name in the GAC and returns the best matching <see cref="AssemblyIdentity"/>.
        /// </summary>
        /// <param name="displayName">The display name of an assembly</param>
        /// <param name="architectureFilter">The optional processor architecture</param>
        /// <param name="preferredCulture">The optional preferred culture information</param>
        /// <returns>A nassembly identity or null, if <paramref name="displayName"/> can't be resolved.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="displayName"/> is null.</exception>
        public static AssemblyIdentity ResolvePartialName(
            string displayName,
            Func<ProcessorArchitecture, bool> architectureFilter = null,
            CultureInfo preferredCulture = null)
        {
            string location;
            return ResolvePartialName(displayName, architectureFilter, preferredCulture, out location, resolveLocation: false);
        }

        /// <summary>
        /// Looks up specified partial assembly name in the GAC and returns the best matching <see cref="AssemblyIdentity"/>.
        /// </summary>
        /// <param name="displayName">The display name of an assembly</param>
        /// <param name="location">Full path name of the resolved assembly</param>
        /// <param name="architectureFilter">The optional processor architecture</param>
        /// <param name="preferredCulture">The optional preferred culture information</param>
        /// <returns>A nassembly identity or null, if <paramref name="displayName"/> can't be resolved.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="displayName"/> is null.</exception>
        public static AssemblyIdentity ResolvePartialName(
            string displayName,
            out string location,
            Func<ProcessorArchitecture, bool> architectureFilter = null,
            CultureInfo preferredCulture = null)
        {
            return ResolvePartialName(displayName, architectureFilter, preferredCulture, out location, resolveLocation: true);
        }

        private static AssemblyIdentity ResolvePartialName(
            string displayName,
            Func<ProcessorArchitecture, bool> architectureFilter,
            CultureInfo preferredCulture,
            out string location,
            bool resolveLocation)
        {
            if (displayName == null) 
            {
                throw new ArgumentNullException("displayName");
            }

            location = null;
            AssemblyIdentity.IAssemblyName nameObject = AssemblyIdentity.ToAssemblyNameObject(displayName);
            if (nameObject == null)
            {
                return null;
            }

            var candidates = GetAssemblyObjects(nameObject, architectureFilter);
            string cultureName = (preferredCulture != null && !preferredCulture.IsNeutralCulture) ? preferredCulture.Name : null;

            var bestMatch = AssemblyIdentity.GetBestMatch(candidates, cultureName);
            if (bestMatch == null)
            {
                return null;
            }

            if (resolveLocation)
            {
                location = GetAssemblyLocation(bestMatch);
            }

            return AssemblyIdentity.ToAssemblyIdentity(bestMatch);
        }

        internal static unsafe string GetAssemblyLocation(AssemblyIdentity.IAssemblyName nameObject)
        {
            // NAME | VERSION | CULTURE | PUBLIC_KEY_TOKEN | RETARGET | PROCESSORARCHITECTURE
            string fullName = AssemblyIdentity.GetDisplayName(nameObject, AssemblyIdentity.ASM_DISPLAYF.FULL);

            fixed (char* p = new char[MAX_PATH])
            {
                ASSEMBLY_INFO info = new ASSEMBLY_INFO
                {
                    cbAssemblyInfo = (uint)Marshal.SizeOf(typeof(ASSEMBLY_INFO)),
                    pszCurrentAssemblyPathBuf = p,
                    cchBuf = (uint)MAX_PATH
                };

                IAssemblyCache assemblyCacheObject;
                CreateAssemblyCache(out assemblyCacheObject, 0);
                assemblyCacheObject.QueryAssemblyInfo(0, fullName, ref info);
                Debug.Assert(info.pszCurrentAssemblyPathBuf != null);
                Debug.Assert(info.pszCurrentAssemblyPathBuf[info.cchBuf - 1] == '\0');

                var result = Marshal.PtrToStringUni((IntPtr)info.pszCurrentAssemblyPathBuf, (int)info.cchBuf - 1);
                Debug.Assert(result.IndexOf('\0') == -1);
                return result;
            }
        }
    }
}