﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides APIs to enumerate and look up assemblies stored in the Global Assembly Cache.
    /// </summary>
    internal static class GlobalAssemblyCache
    {
        /// <summary>
        /// Represents the current Processor architecture
        /// </summary>
        public static readonly ImmutableArray<ProcessorArchitecture> CurrentArchitectures = (IntPtr.Size == 4)
            ? ImmutableArray.Create(ProcessorArchitecture.None, ProcessorArchitecture.MSIL, ProcessorArchitecture.X86)
            : ImmutableArray.Create(ProcessorArchitecture.None, ProcessorArchitecture.MSIL, ProcessorArchitecture.Amd64);

        #region Interop

        private const int MAX_PATH = 260;

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("21b8916c-f28e-11d2-a473-00c04f8ef448")]
        private interface IAssemblyEnum
        {
            [PreserveSig]
            int GetNextAssembly(out FusionAssemblyIdentity.IApplicationContext ppAppCtx, out FusionAssemblyIdentity.IAssemblyName ppName, uint dwFlags);

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
            public readonly uint dwAssemblyFlags;
            public readonly ulong uliAssemblySizeInKB;
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

        [DllImport("clr", PreserveSig = true)]
        private static extern int CreateAssemblyEnum(out IAssemblyEnum ppEnum, FusionAssemblyIdentity.IApplicationContext pAppCtx, FusionAssemblyIdentity.IAssemblyName pName, ASM_CACHE dwFlags, IntPtr pvReserved);

        [DllImport("clr", PreserveSig = true)]
        private static unsafe extern int GetCachePath(ASM_CACHE id, byte* path, ref int length);

        [DllImport("clr", PreserveSig = false)]
        private static extern void CreateAssemblyCache(out IAssemblyCache ppAsmCache, uint dwReserved);

        private const int ERROR_INSUFFICIENT_BUFFER = unchecked((int)0x8007007A);

        public static readonly ImmutableArray<string> RootLocations;

        static GlobalAssemblyCache()
        {
            RootLocations = ImmutableArray.Create<string>(
                GetLocation(ASM_CACHE.ROOT),
                GetLocation(ASM_CACHE.ROOT_EX));
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
        public static IEnumerable<AssemblyIdentity> GetAssemblyIdentities(AssemblyName partialName, ImmutableArray<ProcessorArchitecture> architectureFilter = default(ImmutableArray<ProcessorArchitecture>))
        {
            return GetAssemblyIdentities(FusionAssemblyIdentity.ToAssemblyNameObject(partialName), architectureFilter);
        }

        /// <summary>
        /// Enumerates assemblies in the GAC returning those that match given partial name and
        /// architecture.
        /// </summary>
        /// <param name="partialName">The optional partial name.</param>
        /// <param name="architectureFilter">The optional architecture filter.</param>
        public static IEnumerable<AssemblyIdentity> GetAssemblyIdentities(string partialName = null, ImmutableArray<ProcessorArchitecture> architectureFilter = default(ImmutableArray<ProcessorArchitecture>))
        {
            FusionAssemblyIdentity.IAssemblyName nameObj;
            if (partialName != null)
            {
                nameObj = FusionAssemblyIdentity.ToAssemblyNameObject(partialName);
                if (nameObj == null)
                {
                    return SpecializedCollections.EmptyEnumerable<AssemblyIdentity>();
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
        public static IEnumerable<string> GetAssemblySimpleNames(ImmutableArray<ProcessorArchitecture> architectureFilter = default(ImmutableArray<ProcessorArchitecture>))
        {
            var q = from nameObject in GetAssemblyObjects(partialNameFilter: null, architectureFilter: architectureFilter)
                    select FusionAssemblyIdentity.GetName(nameObject);
            return q.Distinct();
        }

        private static IEnumerable<AssemblyIdentity> GetAssemblyIdentities(
            FusionAssemblyIdentity.IAssemblyName partialName,
            ImmutableArray<ProcessorArchitecture> architectureFilter)
        {
            return from nameObject in GetAssemblyObjects(partialName, architectureFilter)
                   select FusionAssemblyIdentity.ToAssemblyIdentity(nameObject);
        }

        private const int S_OK = 0;
        private const int S_FALSE = 1;

        // Internal for testing.
        internal static IEnumerable<FusionAssemblyIdentity.IAssemblyName> GetAssemblyObjects(
            FusionAssemblyIdentity.IAssemblyName partialNameFilter,
            ImmutableArray<ProcessorArchitecture> architectureFilter)
        {
            IAssemblyEnum enumerator;
            FusionAssemblyIdentity.IApplicationContext applicationContext = null;

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
#if SCRIPTING
                    throw new ArgumentException(Microsoft.CodeAnalysis.Scripting.ScriptingResources.InvalidAssemblyName);
#else
                    throw new ArgumentException(Microsoft.CodeAnalysis.WorkspaceDesktopResources.InvalidAssemblyName);
#endif
                }
            }

            while (true)
            {
                FusionAssemblyIdentity.IAssemblyName nameObject;

                hr = enumerator.GetNextAssembly(out applicationContext, out nameObject, 0);
                if (hr != 0)
                {
                    if (hr < 0)
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }

                    break;
                }

                if (!architectureFilter.IsDefault)
                {
                    var assemblyArchitecture = FusionAssemblyIdentity.GetProcessorArchitecture(nameObject);
                    if (!architectureFilter.Contains(assemblyArchitecture))
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
        /// <returns>An assembly identity or null, if <paramref name="displayName"/> can't be resolved.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="displayName"/> is null.</exception>
        public static AssemblyIdentity ResolvePartialName(
            string displayName,
            ImmutableArray<ProcessorArchitecture> architectureFilter = default(ImmutableArray<ProcessorArchitecture>),
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
        /// <returns>An assembly identity or null, if <paramref name="displayName"/> can't be resolved.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="displayName"/> is null.</exception>
        public static AssemblyIdentity ResolvePartialName(
            string displayName,
            out string location,
            ImmutableArray<ProcessorArchitecture> architectureFilter = default(ImmutableArray<ProcessorArchitecture>),
            CultureInfo preferredCulture = null)
        {
            return ResolvePartialName(displayName, architectureFilter, preferredCulture, out location, resolveLocation: true);
        }

        private static AssemblyIdentity ResolvePartialName(
            string displayName,
            ImmutableArray<ProcessorArchitecture> architectureFilter,
            CultureInfo preferredCulture,
            out string location,
            bool resolveLocation)
        {
            if (displayName == null)
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            location = null;
            FusionAssemblyIdentity.IAssemblyName nameObject = FusionAssemblyIdentity.ToAssemblyNameObject(displayName);
            if (nameObject == null)
            {
                return null;
            }

            var candidates = GetAssemblyObjects(nameObject, architectureFilter);
            string cultureName = (preferredCulture != null && !preferredCulture.IsNeutralCulture) ? preferredCulture.Name : null;

            var bestMatch = FusionAssemblyIdentity.GetBestMatch(candidates, cultureName);
            if (bestMatch == null)
            {
                return null;
            }

            if (resolveLocation)
            {
                location = GetAssemblyLocation(bestMatch);
            }

            return FusionAssemblyIdentity.ToAssemblyIdentity(bestMatch);
        }

        internal static unsafe string GetAssemblyLocation(FusionAssemblyIdentity.IAssemblyName nameObject)
        {
            // NAME | VERSION | CULTURE | PUBLIC_KEY_TOKEN | RETARGET | PROCESSORARCHITECTURE
            string fullName = FusionAssemblyIdentity.GetDisplayName(nameObject, FusionAssemblyIdentity.ASM_DISPLAYF.FULL);

            fixed (char* p = new char[MAX_PATH])
            {
                ASSEMBLY_INFO info = new ASSEMBLY_INFO
                {
                    cbAssemblyInfo = (uint)Marshal.SizeOf<ASSEMBLY_INFO>(),
                    pszCurrentAssemblyPathBuf = p,
                    cchBuf = MAX_PATH
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
