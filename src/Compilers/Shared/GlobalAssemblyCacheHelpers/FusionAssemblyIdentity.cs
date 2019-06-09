// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class FusionAssemblyIdentity
    {
        [Flags]
        internal enum ASM_DISPLAYF
        {
            VERSION = 0x01,
            CULTURE = 0x02,
            PUBLIC_KEY_TOKEN = 0x04,
            PUBLIC_KEY = 0x08,
            CUSTOM = 0x10,
            PROCESSORARCHITECTURE = 0x20,
            LANGUAGEID = 0x40,
            RETARGET = 0x80,
            CONFIG_MASK = 0x100,
            MVID = 0x200,
            CONTENT_TYPE = 0x400,
            FULL = VERSION | CULTURE | PUBLIC_KEY_TOKEN | RETARGET | PROCESSORARCHITECTURE | CONTENT_TYPE
        }

        internal enum PropertyId
        {
            PUBLIC_KEY = 0,        // 0
            PUBLIC_KEY_TOKEN,      // 1
            HASH_VALUE,            // 2
            NAME,                  // 3
            MAJOR_VERSION,         // 4
            MINOR_VERSION,         // 5
            BUILD_NUMBER,          // 6
            REVISION_NUMBER,       // 7
            CULTURE,               // 8
            PROCESSOR_ID_ARRAY,    // 9
            OSINFO_ARRAY,          // 10
            HASH_ALGID,            // 11
            ALIAS,                 // 12
            CODEBASE_URL,          // 13
            CODEBASE_LASTMOD,      // 14
            NULL_PUBLIC_KEY,       // 15
            NULL_PUBLIC_KEY_TOKEN, // 16
            CUSTOM,                // 17
            NULL_CUSTOM,           // 18
            MVID,                  // 19
            FILE_MAJOR_VERSION,    // 20
            FILE_MINOR_VERSION,    // 21
            FILE_BUILD_NUMBER,     // 22
            FILE_REVISION_NUMBER,  // 23
            RETARGET,              // 24
            SIGNATURE_BLOB,        // 25
            CONFIG_MASK,           // 26
            ARCHITECTURE,          // 27
            CONTENT_TYPE,          // 28
            MAX_PARAMS             // 29
        }

        private static class CANOF
        {
            public const uint PARSE_DISPLAY_NAME = 0x1;
            public const uint SET_DEFAULT_VALUES = 0x2;
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("CD193BC0-B4BC-11d2-9833-00C04FC31D2E")]
        internal unsafe interface IAssemblyName
        {
            void SetProperty(PropertyId id, void* data, uint size);

            [PreserveSig]
            int GetProperty(PropertyId id, void* data, ref uint size);

            [PreserveSig]
            int Finalize();

            [PreserveSig]
            int GetDisplayName(byte* buffer, ref uint characterCount, ASM_DISPLAYF dwDisplayFlags);

            [PreserveSig]
            int __BindToObject(/*...*/);

            [PreserveSig]
            int __GetName(/*...*/);

            [PreserveSig]
            int GetVersion(out uint versionHi, out uint versionLow);

            [PreserveSig]
            int IsEqual(IAssemblyName pName, uint dwCmpFlags);

            [PreserveSig]
            int Clone(out IAssemblyName pName);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("7c23ff90-33af-11d3-95da-00a024a85b51")]
        internal interface IApplicationContext
        {
        }

        // NOTE: The CLR caches assembly identities, but doesn't do so in a threadsafe manner.
        // Wrap all calls to this with a lock.
        private static readonly object s_assemblyIdentityGate = new object();
        private static int CreateAssemblyNameObject(out IAssemblyName ppEnum, string szAssemblyName, uint dwFlags, IntPtr pvReserved)
        {
            lock (s_assemblyIdentityGate)
            {
                return RealCreateAssemblyNameObject(out ppEnum, szAssemblyName, dwFlags, pvReserved);
            }
        }

        [DllImport("clr", EntryPoint = "CreateAssemblyNameObject", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int RealCreateAssemblyNameObject(out IAssemblyName ppEnum, [MarshalAs(UnmanagedType.LPWStr)]string szAssemblyName, uint dwFlags, IntPtr pvReserved);

        private const int ERROR_INSUFFICIENT_BUFFER = unchecked((int)0x8007007A);
        private const int FUSION_E_INVALID_NAME = unchecked((int)0x80131047);

        internal static unsafe string GetDisplayName(IAssemblyName nameObject, ASM_DISPLAYF displayFlags)
        {
            int hr;
            uint characterCountIncludingTerminator = 0;

            hr = nameObject.GetDisplayName(null, ref characterCountIncludingTerminator, displayFlags);
            if (hr == 0)
            {
                return String.Empty;
            }

            if (hr != ERROR_INSUFFICIENT_BUFFER)
            {
                throw Marshal.GetExceptionForHR(hr);
            }

            byte[] data = new byte[(int)characterCountIncludingTerminator * 2];
            fixed (byte* p = data)
            {
                hr = nameObject.GetDisplayName(p, ref characterCountIncludingTerminator, displayFlags);
                if (hr != 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }

                return Marshal.PtrToStringUni((IntPtr)p, (int)characterCountIncludingTerminator - 1);
            }
        }

        internal static unsafe byte[] GetPropertyBytes(IAssemblyName nameObject, PropertyId propertyId)
        {
            int hr;
            uint size = 0;

            hr = nameObject.GetProperty(propertyId, null, ref size);
            if (hr == 0)
            {
                return null;
            }

            if (hr != ERROR_INSUFFICIENT_BUFFER)
            {
                throw Marshal.GetExceptionForHR(hr);
            }

            byte[] data = new byte[(int)size];
            fixed (byte* p = data)
            {
                hr = nameObject.GetProperty(propertyId, p, ref size);
                if (hr != 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }

            return data;
        }

        internal static unsafe string GetPropertyString(IAssemblyName nameObject, PropertyId propertyId)
        {
            byte[] data = GetPropertyBytes(nameObject, propertyId);
            if (data == null)
            {
                return null;
            }

            fixed (byte* p = data)
            {
                return Marshal.PtrToStringUni((IntPtr)p, (data.Length / 2) - 1);
            }
        }

        internal static unsafe bool IsKeyOrTokenEmpty(IAssemblyName nameObject, PropertyId propertyId)
        {
            Debug.Assert(propertyId == PropertyId.NULL_PUBLIC_KEY_TOKEN || propertyId == PropertyId.NULL_PUBLIC_KEY);
            uint size = 0;
            int hr = nameObject.GetProperty(propertyId, null, ref size);
            return hr == 0;
        }

        internal static unsafe Version GetVersion(IAssemblyName nameObject)
        {
            uint hi, lo;
            int hr = nameObject.GetVersion(out hi, out lo);
            if (hr != 0)
            {
                Debug.Assert(hr == FUSION_E_INVALID_NAME);
                return null;
            }

            return new Version((int)(hi >> 16), (int)(hi & 0xffff), (int)(lo >> 16), (int)(lo & 0xffff));
        }

        internal static Version GetVersion(IAssemblyName name, out AssemblyIdentityParts parts)
        {
            uint? major = GetPropertyWord(name, PropertyId.MAJOR_VERSION);
            uint? minor = GetPropertyWord(name, PropertyId.MINOR_VERSION);
            uint? build = GetPropertyWord(name, PropertyId.BUILD_NUMBER);
            uint? revision = GetPropertyWord(name, PropertyId.REVISION_NUMBER);

            parts = 0;

            if (major != null)
            {
                parts |= AssemblyIdentityParts.VersionMajor;
            }

            if (minor != null)
            {
                parts |= AssemblyIdentityParts.VersionMinor;
            }

            if (build != null)
            {
                parts |= AssemblyIdentityParts.VersionBuild;
            }

            if (revision != null)
            {
                parts |= AssemblyIdentityParts.VersionRevision;
            }

            return new Version((int)(major ?? 0), (int)(minor ?? 0), (int)(build ?? 0), (int)(revision ?? 0));
        }

        internal static byte[] GetPublicKeyToken(IAssemblyName nameObject)
        {
            byte[] result = GetPropertyBytes(nameObject, PropertyId.PUBLIC_KEY_TOKEN);
            if (result != null)
            {
                return result;
            }

            if (IsKeyOrTokenEmpty(nameObject, PropertyId.NULL_PUBLIC_KEY_TOKEN))
            {
                return Array.Empty<byte>();
            }

            return null;
        }

        internal static byte[] GetPublicKey(IAssemblyName nameObject)
        {
            byte[] result = GetPropertyBytes(nameObject, PropertyId.PUBLIC_KEY);
            if (result != null)
            {
                return result;
            }

            if (IsKeyOrTokenEmpty(nameObject, PropertyId.NULL_PUBLIC_KEY))
            {
                return Array.Empty<byte>();
            }

            return null;
        }

        internal static unsafe uint? GetPropertyWord(IAssemblyName nameObject, PropertyId propertyId)
        {
            uint result;
            uint size = sizeof(uint);
            int hr = nameObject.GetProperty(propertyId, &result, ref size);
            if (hr != 0)
            {
                throw Marshal.GetExceptionForHR(hr);
            }

            if (size == 0)
            {
                return null;
            }

            return result;
        }

        internal static string GetName(IAssemblyName nameObject)
        {
            return GetPropertyString(nameObject, PropertyId.NAME);
        }

        internal static string GetCulture(IAssemblyName nameObject)
        {
            return GetPropertyString(nameObject, PropertyId.CULTURE);
        }

        internal static AssemblyContentType GetContentType(IAssemblyName nameObject)
        {
            return (AssemblyContentType)(GetPropertyWord(nameObject, PropertyId.CONTENT_TYPE) ?? 0);
        }

        internal static ProcessorArchitecture GetProcessorArchitecture(IAssemblyName nameObject)
        {
            return (ProcessorArchitecture)(GetPropertyWord(nameObject, PropertyId.ARCHITECTURE) ?? 0);
        }

        internal static unsafe AssemblyNameFlags GetFlags(IAssemblyName nameObject)
        {
            AssemblyNameFlags result = 0;

            uint retarget = GetPropertyWord(nameObject, PropertyId.RETARGET) ?? 0;
            if (retarget != 0)
            {
                result |= AssemblyNameFlags.Retargetable;
            }

            return result;
        }

        private static unsafe void SetProperty(IAssemblyName nameObject, PropertyId propertyId, string data)
        {
            if (data == null)
            {
                nameObject.SetProperty(propertyId, null, 0);
            }
            else
            {
                Debug.Assert(data.IndexOf('\0') == -1);

                fixed (char* p = data)
                {
                    Debug.Assert(p[data.Length] == '\0');

                    // size is in bytes, include trailing \0 character:
                    nameObject.SetProperty(propertyId, p, (uint)(data.Length + 1) * 2);
                }
            }
        }

        private static unsafe void SetProperty(IAssemblyName nameObject, PropertyId propertyId, byte[] data)
        {
            if (data == null)
            {
                nameObject.SetProperty(propertyId, null, 0);
            }
            else
            {
                fixed (byte* p = data)
                {
                    nameObject.SetProperty(propertyId, p, (uint)data.Length);
                }
            }
        }

        private static unsafe void SetProperty(IAssemblyName nameObject, PropertyId propertyId, ushort data)
        {
            nameObject.SetProperty(propertyId, &data, sizeof(ushort));
        }

        private static unsafe void SetProperty(IAssemblyName nameObject, PropertyId propertyId, uint data)
        {
            nameObject.SetProperty(propertyId, &data, sizeof(uint));
        }

        private static unsafe void SetPublicKeyToken(IAssemblyName nameObject, byte[] value)
        {
            // An empty public key token is set via NULL_PUBLIC_KEY_TOKEN property.
            if (value != null && value.Length == 0)
            {
                nameObject.SetProperty(PropertyId.NULL_PUBLIC_KEY_TOKEN, null, 0);
            }
            else
            {
                SetProperty(nameObject, PropertyId.PUBLIC_KEY_TOKEN, value);
            }
        }

        /// <summary>
        /// Converts <see cref="IAssemblyName"/> to <see cref="AssemblyName"/> with all metadata fields filled.
        /// </summary>
        /// <returns>
        /// Assembly name with Version, Culture and PublicKeyToken components filled in:
        /// "SimpleName, Version=#.#.#.#, Culture=XXX, PublicKeyToken=XXXXXXXXXXXXXXXX".
        /// In addition Retargetable flag and ContentType are set.
        /// </returns>
        internal static AssemblyIdentity ToAssemblyIdentity(IAssemblyName nameObject)
        {
            if (nameObject == null)
            {
                return null;
            }

            AssemblyNameFlags flags = GetFlags(nameObject);

            byte[] publicKey = GetPublicKey(nameObject);
            bool hasPublicKey = publicKey != null && publicKey.Length != 0;

            AssemblyIdentityParts versionParts;
            return new AssemblyIdentity(
                GetName(nameObject),
                GetVersion(nameObject, out versionParts),
                GetCulture(nameObject) ?? "",
                (hasPublicKey ? publicKey : GetPublicKeyToken(nameObject)).AsImmutableOrNull(),
                hasPublicKey: hasPublicKey,
                isRetargetable: (flags & AssemblyNameFlags.Retargetable) != 0,
                contentType: GetContentType(nameObject));
        }

        /// <summary>
        /// Converts <see cref="AssemblyName"/> to an equivalent <see cref="IAssemblyName"/>.
        /// </summary>
        internal static IAssemblyName ToAssemblyNameObject(AssemblyName name)
        {
            if (name == null)
            {
                return null;
            }

            IAssemblyName result;
            Marshal.ThrowExceptionForHR(CreateAssemblyNameObject(out result, null, 0, IntPtr.Zero));

            string assemblyName = name.Name;
            if (assemblyName != null)
            {
                if (assemblyName.IndexOf('\0') >= 0)
                {
#if SCRIPTING
                    throw new ArgumentException(Scripting.ScriptingResources.InvalidCharactersInAssemblyName, nameof(name));
#elif EDITOR_FEATURES
                    throw new ArgumentException(Microsoft.CodeAnalysis.Editor.EditorFeaturesResources.Invalid_characters_in_assembly_name, nameof(name));
#else
                    throw new ArgumentException(Microsoft.CodeAnalysis.CodeAnalysisResources.InvalidCharactersInAssemblyName, nameof(name));
#endif
                }

                SetProperty(result, PropertyId.NAME, assemblyName);
            }

            if (name.Version != null)
            {
                SetProperty(result, PropertyId.MAJOR_VERSION, unchecked((ushort)name.Version.Major));
                SetProperty(result, PropertyId.MINOR_VERSION, unchecked((ushort)name.Version.Minor));
                SetProperty(result, PropertyId.BUILD_NUMBER, unchecked((ushort)name.Version.Build));
                SetProperty(result, PropertyId.REVISION_NUMBER, unchecked((ushort)name.Version.Revision));
            }

            string cultureName = name.CultureName;
            if (cultureName != null)
            {
                if (cultureName.IndexOf('\0') >= 0)
                {
#if SCRIPTING
                    throw new ArgumentException(Microsoft.CodeAnalysis.Scripting.ScriptingResources.InvalidCharactersInAssemblyName, nameof(name));
#elif EDITOR_FEATURES
                    throw new ArgumentException(Microsoft.CodeAnalysis.Editor.EditorFeaturesResources.Invalid_characters_in_assembly_name, nameof(name));
#else
                    throw new ArgumentException(Microsoft.CodeAnalysis.CodeAnalysisResources.InvalidCharactersInAssemblyName, nameof(name));
#endif
                }

                SetProperty(result, PropertyId.CULTURE, cultureName);
            }

            if (name.Flags == AssemblyNameFlags.Retargetable)
            {
                SetProperty(result, PropertyId.RETARGET, 1U);
            }

            if (name.ContentType != AssemblyContentType.Default)
            {
                SetProperty(result, PropertyId.CONTENT_TYPE, (uint)name.ContentType);
            }

            byte[] token = name.GetPublicKeyToken();
            SetPublicKeyToken(result, token);
            return result;
        }

        /// <summary>
        /// Creates <see cref="IAssemblyName"/> object by parsing given display name.
        /// </summary>
        internal static IAssemblyName ToAssemblyNameObject(string displayName)
        {
            // CLR doesn't handle \0 in the display name well:
            if (displayName.IndexOf('\0') >= 0)
            {
                return null;
            }

            Debug.Assert(displayName != null);
            IAssemblyName result;
            int hr = CreateAssemblyNameObject(out result, displayName, CANOF.PARSE_DISPLAY_NAME, IntPtr.Zero);
            if (hr != 0)
            {
                return null;
            }

            Debug.Assert(result != null);
            return result;
        }

        /// <summary>
        /// Selects the candidate assembly with the largest version number.  Uses culture as a tie-breaker if it is provided.
        /// All candidates are assumed to have the same name and must include versions and cultures.  
        /// </summary>
        internal static IAssemblyName GetBestMatch(IEnumerable<IAssemblyName> candidates, string preferredCultureOpt)
        {
            IAssemblyName bestCandidate = null;
            Version bestVersion = null;
            string bestCulture = null;
            foreach (var candidate in candidates)
            {
                if (bestCandidate != null)
                {
                    Version candidateVersion = GetVersion(candidate);
                    Debug.Assert(candidateVersion != null);

                    if (bestVersion == null)
                    {
                        bestVersion = GetVersion(bestCandidate);
                        Debug.Assert(bestVersion != null);
                    }

                    int cmp = bestVersion.CompareTo(candidateVersion);
                    if (cmp == 0)
                    {
                        if (preferredCultureOpt != null)
                        {
                            string candidateCulture = GetCulture(candidate);
                            Debug.Assert(candidateCulture != null);

                            if (bestCulture == null)
                            {
                                bestCulture = GetCulture(candidate);
                                Debug.Assert(bestCulture != null);
                            }

                            // we have exactly the preferred culture or 
                            // we have neutral culture and the best candidate's culture isn't the preferred one:
                            if (StringComparer.OrdinalIgnoreCase.Equals(candidateCulture, preferredCultureOpt) ||
                                candidateCulture.Length == 0 && !StringComparer.OrdinalIgnoreCase.Equals(bestCulture, preferredCultureOpt))
                            {
                                bestCandidate = candidate;
                                bestVersion = candidateVersion;
                                bestCulture = candidateCulture;
                            }
                        }
                    }
                    else if (cmp < 0)
                    {
                        bestCandidate = candidate;
                        bestVersion = candidateVersion;
                    }
                }
                else
                {
                    bestCandidate = candidate;
                }
            }

            return bestCandidate;
        }
    }
}
