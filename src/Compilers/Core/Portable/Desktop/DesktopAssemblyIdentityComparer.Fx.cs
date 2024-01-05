// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#define NDP4_AUTO_VERSION_ROLLFORWARD

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis
{
    // TODO: Consider reducing the table memory footprint.

    public partial class DesktopAssemblyIdentityComparer
    {
        private sealed class FrameworkAssemblyDictionary : Dictionary<string, FrameworkAssemblyDictionary.Value>
        {
            public FrameworkAssemblyDictionary()
                : base(SimpleNameComparer)
            {
            }

            public readonly struct Value
            {
                public readonly ImmutableArray<byte> PublicKeyToken;
                public readonly AssemblyVersion Version;

                public Value(ImmutableArray<byte> publicKeyToken, AssemblyVersion version)
                {
                    this.PublicKeyToken = publicKeyToken;
                    this.Version = version;
                }
            }

            public void Add(
                string name,
                ImmutableArray<byte> publicKeyToken,
                AssemblyVersion version)
            {
                Add(name, new Value(publicKeyToken, version));
            }
        }

        private sealed class FrameworkRetargetingDictionary : Dictionary<FrameworkRetargetingDictionary.Key, List<FrameworkRetargetingDictionary.Value>>
        {
            public FrameworkRetargetingDictionary()
            {
            }

            public readonly struct Key : IEquatable<Key>
            {
                public readonly string Name;
                public readonly ImmutableArray<byte> PublicKeyToken;

                public Key(string name, ImmutableArray<byte> publicKeyToken)
                {
                    this.Name = name;
                    this.PublicKeyToken = publicKeyToken;
                }

                public bool Equals(Key other)
                {
                    return SimpleNameComparer.Equals(this.Name, other.Name)
                        && this.PublicKeyToken.SequenceEqual(other.PublicKeyToken);
                }

                public override bool Equals(object? obj)
                {
                    return obj is Key && Equals((Key)obj);
                }

                public override int GetHashCode()
                {
                    return SimpleNameComparer.GetHashCode(Name) ^ PublicKeyToken[0];
                }
            }

            public readonly struct Value
            {
                public readonly AssemblyVersion VersionLow;
                public readonly AssemblyVersion VersionHigh;
                public readonly string NewName;
                public readonly ImmutableArray<byte> NewPublicKeyToken;
                public readonly AssemblyVersion NewVersion;
                public readonly bool IsPortable;

                public Value(
                    AssemblyVersion versionLow,
                    AssemblyVersion versionHigh,
                    string newName,
                    ImmutableArray<byte> newPublicKeyToken,
                    AssemblyVersion newVersion,
                    bool isPortable)
                {
                    VersionLow = versionLow;
                    VersionHigh = versionHigh;
                    NewName = newName;
                    NewPublicKeyToken = newPublicKeyToken;
                    NewVersion = newVersion;
                    IsPortable = isPortable;
                }
            }

            public void Add(
                string name,
                ImmutableArray<byte> publicKeyToken,
                AssemblyVersion versionLow,
#pragma warning disable IDE0060 // Remove unused parameter
                object versionHighNull,
#pragma warning restore IDE0060 // Remove unused parameter
                string newName,
                ImmutableArray<byte> newPublicKeyToken,
                AssemblyVersion newVersion)
            {
                List<Value>? values;
                var key = new Key(name, publicKeyToken);
                if (!TryGetValue(key, out values))
                {
                    Add(key, values = new List<Value>());
                }

                values.Add(new Value(versionLow, versionHigh: default, newName, newPublicKeyToken, newVersion, isPortable: false));
            }

            public void Add(
                string name,
                ImmutableArray<byte> publicKeyToken,
                AssemblyVersion versionLow,
                AssemblyVersion versionHigh,
                string newName,
                ImmutableArray<byte> newPublicKeyToken,
                AssemblyVersion newVersion,
                bool isPortable)
            {
                List<Value>? values;
                var key = new Key(name, publicKeyToken);
                if (!TryGetValue(key, out values))
                {
                    Add(key, values = new List<Value>());
                }

                values.Add(new Value(versionLow, versionHigh, newName, newPublicKeyToken, newVersion, isPortable));
            }

            public bool TryGetValue(AssemblyIdentity identity, out Value value)
            {
                List<Value>? values;
                if (!TryGetValue(new Key(identity.Name, identity.PublicKeyToken), out values))
                {
                    value = default;
                    return false;
                }

                for (int i = 0; i < values.Count; i++)
                {
                    value = values[i];
                    var version = (AssemblyVersion)identity.Version;
                    if (value.VersionHigh.Major == 0)
                    {
                        Debug.Assert(value.VersionHigh == default(AssemblyVersion));
                        if (version == value.VersionLow)
                        {
                            return true;
                        }
                    }
                    else if (version >= value.VersionLow && version <= value.VersionHigh)
                    {
                        return true;
                    }
                }

                value = default;
                return false;
            }
        }

        private static readonly ImmutableArray<byte> s_NETCF_PUBLIC_KEY_TOKEN_1 = ImmutableArray.Create(new byte[] { 0x1c, 0x9e, 0x25, 0x96, 0x86, 0xf9, 0x21, 0xe0 });
        private static readonly ImmutableArray<byte> s_NETCF_PUBLIC_KEY_TOKEN_2 = ImmutableArray.Create(new byte[] { 0x5f, 0xd5, 0x7c, 0x54, 0x3a, 0x9c, 0x02, 0x47 });
        private static readonly ImmutableArray<byte> s_NETCF_PUBLIC_KEY_TOKEN_3 = ImmutableArray.Create(new byte[] { 0x96, 0x9d, 0xb8, 0x05, 0x3d, 0x33, 0x22, 0xac });
        private static readonly ImmutableArray<byte> s_SQL_PUBLIC_KEY_TOKEN = ImmutableArray.Create(new byte[] { 0x89, 0x84, 0x5d, 0xcd, 0x80, 0x80, 0xcc, 0x91 });
        private static readonly ImmutableArray<byte> s_SQL_MOBILE_PUBLIC_KEY_TOKEN = ImmutableArray.Create(new byte[] { 0x3b, 0xe2, 0x35, 0xdf, 0x1c, 0x8d, 0x2a, 0xd3 });
        private static readonly ImmutableArray<byte> s_ECMA_PUBLICKEY_STR_L = ImmutableArray.Create(new byte[] { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 });
        private static readonly ImmutableArray<byte> s_SHAREDLIB_PUBLICKEY_STR_L = ImmutableArray.Create(new byte[] { 0x31, 0xbf, 0x38, 0x56, 0xad, 0x36, 0x4e, 0x35 });
        private static readonly ImmutableArray<byte> s_MICROSOFT_PUBLICKEY_STR_L = ImmutableArray.Create(new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a });
        private static readonly ImmutableArray<byte> s_SILVERLIGHT_PLATFORM_PUBLICKEY_STR_L = ImmutableArray.Create(new byte[] { 0x7c, 0xec, 0x85, 0xd7, 0xbe, 0xa7, 0x79, 0x8e });
        private static readonly ImmutableArray<byte> s_SILVERLIGHT_PUBLICKEY_STR_L = ImmutableArray.Create(new byte[] { 0x31, 0xbf, 0x38, 0x56, 0xad, 0x36, 0x4e, 0x35 });
        private static readonly ImmutableArray<byte> s_RIA_SERVICES_KEY_TOKEN = ImmutableArray.Create(new byte[] { 0xdd, 0xd0, 0xda, 0x4d, 0x3e, 0x67, 0x82, 0x17 });

        private static readonly AssemblyVersion s_VER_VS_COMPATIBILITY_ASSEMBLYVERSION_STR_L = new AssemblyVersion(8, 0, 0, 0);
        private static readonly AssemblyVersion s_VER_VS_ASSEMBLYVERSION_STR_L = new AssemblyVersion(10, 0, 0, 0);
        private static readonly AssemblyVersion s_VER_SQL_ASSEMBLYVERSION_STR_L = new AssemblyVersion(9, 0, 242, 0);
        private static readonly AssemblyVersion s_VER_LINQ_ASSEMBLYVERSION_STR_L = new AssemblyVersion(3, 0, 0, 0);
        private static readonly AssemblyVersion s_VER_LINQ_ASSEMBLYVERSION_STR_2_L = new AssemblyVersion(3, 5, 0, 0);
        private static readonly AssemblyVersion s_VER_SQL_ORCAS_ASSEMBLYVERSION_STR_L = new AssemblyVersion(3, 5, 0, 0);
        private static readonly AssemblyVersion s_VER_ASSEMBLYVERSION_STR_L = new AssemblyVersion(4, 0, 0, 0);
        private static readonly AssemblyVersion s_VER_VC_STLCLR_ASSEMBLYVERSION_STR_L = new AssemblyVersion(2, 0, 0, 0);
        private const string NULL = null;
        private const bool TRUE = true;

        // Replace:
        // "([0-9]+)\.([0-9]+)\.([0-9]+)\.([0-9]+)"  ->   new AssemblyVersion($1, $2, $3, $4)
        // ,[ ]+FxPolicyHelper::AppXBinder_[A-Z]+    -> 
        // " -> "   (whole word, case sensitive)
        // :: -> .

        // copied from ndp\clr\src\fusion\binder\fxretarget.cpp
        private static readonly FrameworkRetargetingDictionary s_arRetargetPolicy = new FrameworkRetargetingDictionary()
        {
            // ECMA v1.0 redirect    
            {"System", s_ECMA_PUBLICKEY_STR_L, new AssemblyVersion(1, 0, 0, 0), NULL, NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml", s_ECMA_PUBLICKEY_STR_L, new AssemblyVersion(1, 0, 0, 0), NULL, NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},

            // Compat framework redirect
            {"System", s_NETCF_PUBLIC_KEY_TOKEN_1, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System", s_NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Drawing", s_NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Services", s_NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms", s_NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml", s_NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Drawing", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Services", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.VisualBasic", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(7, 0, 5000, 0), NULL, NULL, s_MICROSOFT_PUBLICKEY_STR_L, s_VER_VS_ASSEMBLYVERSION_STR_L},
            {"System", s_NETCF_PUBLIC_KEY_TOKEN_1, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System", s_NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Drawing", s_NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Services", s_NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms", s_NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml", s_NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Drawing", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Services", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.VisualBasic", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(7, 0, 5500, 0), NULL, NULL, s_MICROSOFT_PUBLICKEY_STR_L, s_VER_VS_ASSEMBLYVERSION_STR_L},
            {"Microsoft.WindowsCE.Forms", s_NETCF_PUBLIC_KEY_TOKEN_1, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, s_NETCF_PUBLIC_KEY_TOKEN_3, s_VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.WindowsCE.Forms", s_NETCF_PUBLIC_KEY_TOKEN_1, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, s_NETCF_PUBLIC_KEY_TOKEN_3, s_VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.WindowsCE.Forms", s_NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, s_NETCF_PUBLIC_KEY_TOKEN_3, s_VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.WindowsCE.Forms", s_NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, s_NETCF_PUBLIC_KEY_TOKEN_3, s_VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.WindowsCE.Forms", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, s_NETCF_PUBLIC_KEY_TOKEN_3, s_VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.WindowsCE.Forms", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, s_NETCF_PUBLIC_KEY_TOKEN_3, s_VER_ASSEMBLYVERSION_STR_L},

            // Compat framework name redirect
            {"System.Data.SqlClient", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5000, 0), NULL, "System.Data", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.SqlClient", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5500, 0), NULL, "System.Data", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.Common", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5000, 0), NULL, "System.Data", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.Common", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5500, 0), NULL, "System.Data", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms.DataGrid", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5000, 0), NULL, "System.Windows.Forms", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms.DataGrid", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5500, 0), NULL, "System.Windows.Forms", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},

            // v2.0 Compact framework redirect
            {"System", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(2, 0, 0, 0), new AssemblyVersion(2, 0, 10, 0), NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(2, 0, 0, 0), new AssemblyVersion(2, 0, 10, 0), NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Drawing", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(2, 0, 0, 0), new AssemblyVersion(2, 0, 10, 0), NULL, s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Services", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(2, 0, 0, 0), new AssemblyVersion(2, 0, 10, 0), NULL, s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(2, 0, 0, 0), new AssemblyVersion(2, 0, 10, 0), NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(2, 0, 0, 0), new AssemblyVersion(2, 0, 10, 0), NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Messaging", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(2, 0, 0, 0), new AssemblyVersion(2, 0, 10, 0), NULL, s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.SqlClient", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(2, 0, 0, 0), new AssemblyVersion(2, 0, 10, 0), "System.Data", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.Common", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(2, 0, 0, 0), new AssemblyVersion(2, 0, 10, 0), "System.Data", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms.DataGrid", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(2, 0, 0, 0), new AssemblyVersion(2, 0, 10, 0), "System.Windows.Forms", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.VisualBasic", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(8, 0, 0, 0), new AssemblyVersion(8, 0, 10, 0), NULL, s_MICROSOFT_PUBLICKEY_STR_L, s_VER_VS_ASSEMBLYVERSION_STR_L},

            // v3.5 Compact framework redirect
            {"System", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Drawing", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Services", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Messaging", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.SqlClient", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), "System.Data", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms.DataGrid", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), "System.Windows.Forms", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.VisualBasic", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(8, 1, 0, 0), new AssemblyVersion(8, 1, 5, 0), "Microsoft.VisualBasic", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_VS_ASSEMBLYVERSION_STR_L},

            // SQL Everywhere redirect for Orcas
            {"System.Data.SqlClient", s_SQL_MOBILE_PUBLIC_KEY_TOKEN, new AssemblyVersion(3, 5, 0, 0), NULL, "System.Data", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.SqlServerCe", s_SQL_MOBILE_PUBLIC_KEY_TOKEN, new AssemblyVersion(3, 5, 0, 0), NULL, NULL, s_SQL_PUBLIC_KEY_TOKEN, s_VER_SQL_ORCAS_ASSEMBLYVERSION_STR_L},
            {"System.Data.SqlServerCe", s_SQL_MOBILE_PUBLIC_KEY_TOKEN, new AssemblyVersion(3, 5, 1, 0), new AssemblyVersion(3, 5, 200, 999), NULL, s_SQL_PUBLIC_KEY_TOKEN, s_VER_SQL_ORCAS_ASSEMBLYVERSION_STR_L},

            // SQL CE redirect
            {"System.Data.SqlClient", s_SQL_MOBILE_PUBLIC_KEY_TOKEN, new AssemblyVersion(3, 0, 3600, 0), NULL, "System.Data", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.SqlServerCe", s_SQL_MOBILE_PUBLIC_KEY_TOKEN, new AssemblyVersion(3, 0, 3600, 0), NULL, NULL, s_SQL_PUBLIC_KEY_TOKEN, s_VER_SQL_ASSEMBLYVERSION_STR_L},

            // Linq and friends redirect
            {"system.xml.linq", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_LINQ_ASSEMBLYVERSION_STR_2_L},
            {"system.data.DataSetExtensions", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_LINQ_ASSEMBLYVERSION_STR_2_L},
            {"System.Core", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_LINQ_ASSEMBLYVERSION_STR_2_L},
            {"System.ServiceModel", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_LINQ_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.Serialization", s_NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, s_ECMA_PUBLICKEY_STR_L, s_VER_LINQ_ASSEMBLYVERSION_STR_L},

            // Portable Library redirects
            {"mscorlib",                           s_SILVERLIGHT_PLATFORM_PUBLICKEY_STR_L,   new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, s_ECMA_PUBLICKEY_STR_L,      s_VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System",                             s_SILVERLIGHT_PLATFORM_PUBLICKEY_STR_L,   new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, s_ECMA_PUBLICKEY_STR_L,      s_VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.ComponentModel.Composition",  s_SILVERLIGHT_PUBLICKEY_STR_L,            new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, s_ECMA_PUBLICKEY_STR_L,      s_VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.ComponentModel.DataAnnotations",s_RIA_SERVICES_KEY_TOKEN,               new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.Core",                        s_SILVERLIGHT_PLATFORM_PUBLICKEY_STR_L,   new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, s_ECMA_PUBLICKEY_STR_L,      s_VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.Net",                         s_SILVERLIGHT_PLATFORM_PUBLICKEY_STR_L,   new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.Numerics",                    s_SILVERLIGHT_PUBLICKEY_STR_L,            new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, s_ECMA_PUBLICKEY_STR_L,      s_VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"Microsoft.CSharp",                   s_SILVERLIGHT_PUBLICKEY_STR_L,            new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.Runtime.Serialization",       s_SILVERLIGHT_PLATFORM_PUBLICKEY_STR_L,   new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, s_ECMA_PUBLICKEY_STR_L,      s_VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.ServiceModel",                s_SILVERLIGHT_PUBLICKEY_STR_L,            new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, s_ECMA_PUBLICKEY_STR_L,      s_VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.ServiceModel.Web",            s_SILVERLIGHT_PLATFORM_PUBLICKEY_STR_L,   new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.Xml",                         s_SILVERLIGHT_PLATFORM_PUBLICKEY_STR_L,   new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, s_ECMA_PUBLICKEY_STR_L,      s_VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.Xml.Linq",                    s_SILVERLIGHT_PUBLICKEY_STR_L,            new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, s_ECMA_PUBLICKEY_STR_L,      s_VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.Xml.Serialization",           s_SILVERLIGHT_PUBLICKEY_STR_L,            new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, s_ECMA_PUBLICKEY_STR_L,      s_VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.Windows",                     s_SILVERLIGHT_PLATFORM_PUBLICKEY_STR_L,   new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L, TRUE},
        };

        // Copied from ndp\clr\src\inc\fxretarget.h
        private static readonly FrameworkAssemblyDictionary s_arFxPolicy = new FrameworkAssemblyDictionary()
        {
            {"Accessibility", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"CustomMarshalers", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"ISymWrapper", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.JScript", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_VS_ASSEMBLYVERSION_STR_L},
            {"Microsoft.VisualBasic", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_VS_ASSEMBLYVERSION_STR_L},
            {"Microsoft.VisualBasic.Compatibility", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_VS_ASSEMBLYVERSION_STR_L},
            {"Microsoft.VisualBasic.Compatibility.Data", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_VS_ASSEMBLYVERSION_STR_L},
            {"Microsoft.VisualC", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_VS_ASSEMBLYVERSION_STR_L},
            {"mscorlib", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Configuration", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Configuration.Install", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.OracleClient", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.SqlXml", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Deployment", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Design", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.DirectoryServices", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.DirectoryServices.Protocols", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Drawing", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Drawing.Design", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.EnterpriseServices", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Management", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Messaging", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.Remoting", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.Serialization.Formatters.Soap", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Security", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceProcess", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Transactions", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Web", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Mobile", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.RegularExpressions", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Services", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L}, // Has to be supported in AppX, because it is in transitive closure of supported assemblies
            {"System.Windows.Forms", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},

#if NDP4_AUTO_VERSION_ROLLFORWARD

            // Post-Everett FX 2.0 assemblies:
            {"AspNetMMCExt", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"sysglobl", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.Build.Engine", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.Build.Framework", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},

            // FX 3.0 assemblies:
            // Note: we shipped .NET 4.0 with entries in this list for PresentationCFFRasterizer and System.ServiceModel.Install
            // even though these assemblies did not ship with .NET 4.0. To maintain 100% compatibility with 4.0 we will keep
            // these in .NET 4.5, but we should remove them in a future SxS version of the Framework.
            {"PresentationCFFRasterizer", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L}, // See note above
            {"PresentationCore", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework.Aero", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework.Classic", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework.Luna", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework.Royale", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"PresentationUI", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"ReachFramework", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Printing", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Speech", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"UIAutomationClient", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"UIAutomationClientsideProviders", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"UIAutomationProvider", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"UIAutomationTypes", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"WindowsBase", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"WindowsFormsIntegration", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"SMDiagnostics", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.IdentityModel", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.IdentityModel.Selectors", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.IO.Log", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.Serialization", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Install", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L}, // See note above
            {"System.ServiceModel.WasHosting", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Workflow.Activities", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Workflow.ComponentModel", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Workflow.Runtime", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.Transactions.Bridge", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.Transactions.Bridge.Dtc", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},

            // FX 3.5 assemblies:
            {"System.AddIn", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.AddIn.Contract", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ComponentModel.Composition", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L}, // Shipping out-of-band
            {"System.Core", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.DataSetExtensions", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.Linq", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml.Linq", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.DirectoryServices.AccountManagement", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Management.Instrumentation", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Net", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Web", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L}, // Needed for portable libraries
            {"System.Web.Extensions", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Extensions.Design", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Presentation", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.WorkflowServices", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            // Microsoft.Data.Entity.Build.Tasks.dll should not be unified on purpose - it is supported SxS, i.e. both 3.5 and 4.0 versions can be loaded into CLR 4.0+.
            // {"Microsoft.Data.Entity.Build.Tasks", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},

            // FX 3.5 SP1 assemblies:
            {"System.ComponentModel.DataAnnotations", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.Entity", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.Entity.Design", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.Services", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.Services.Client", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.Services.Design", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Abstractions", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.DynamicData", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.DynamicData.Design", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Entity", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Entity.Design", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Routing", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},

            // FX 4.0 assemblies:
            {"Microsoft.Build", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.CSharp", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Dynamic", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Numerics", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Xaml", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            // Microsoft.Workflow.Compiler.exe:
            // System.Workflow.ComponentModel.dll started to depend on Microsoft.Workflow.Compiler.exe in 4.0 RTM
            {"Microsoft.Workflow.Compiler", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},

            // FX 4.5 assemblies:
            {"Microsoft.Activities.Build", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.Build.Conversion.v4.0", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.Build.Tasks.v4.0", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.Build.Utilities.v4.0", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.Internal.Tasks.Dataflow", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.VisualBasic.Activities.Compiler", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_VS_ASSEMBLYVERSION_STR_L},
            {"Microsoft.VisualC.STLCLR", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_VC_STLCLR_ASSEMBLYVERSION_STR_L},
            {"Microsoft.Windows.ApplicationServer.Applications", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"PresentationBuildTasks", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework.Aero2", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework.AeroLite", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework-SystemCore", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework-SystemData", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework-SystemDrawing", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework-SystemXml", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework-SystemXmlLinq", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Activities", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Activities.Core.Presentation", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Activities.DurableInstancing", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Activities.Presentation", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ComponentModel.Composition.Registration", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Device", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.IdentityModel.Services", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.IO.Compression", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.IO.Compression.FileSystem", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Net.Http", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Net.Http.WebRequest", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Reflection.Context", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.Caching", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.DurableInstancing", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.WindowsRuntime", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.WindowsRuntime.UI.Xaml", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Activation", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Activities", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Channels", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Discovery", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Internals", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Routing", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.ServiceMoniker40", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.ApplicationServices", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L}, // Has to be supported in AppX, because it is in transitive closure of supported assemblies
            {"System.Web.DataVisualization", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.DataVisualization.Design", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Controls.Ribbon", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms.DataVisualization", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms.DataVisualization.Design", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Input.Manipulations", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Xaml.Hosting", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"XamlBuildTask", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"XsdBuildTask", s_SHAREDLIB_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Numerics.Vectors", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},

            // FX 4.5 facade assemblies:
            {"System.Collections", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Collections.Concurrent", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ComponentModel", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ComponentModel.Annotations", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ComponentModel.EventBasedAsync", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Diagnostics.Contracts", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Diagnostics.Debug", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Diagnostics.Tools", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Diagnostics.Tracing", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Dynamic.Runtime", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Globalization", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.IO", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Linq", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Linq.Expressions", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Linq.Parallel", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Linq.Queryable", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Net.Http.Rtc", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Net.NetworkInformation", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Net.Primitives", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Net.Requests", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ObjectModel", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Reflection", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Reflection.Emit", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Reflection.Emit.ILGeneration", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Reflection.Emit.Lightweight", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Reflection.Extensions", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Reflection.Primitives", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Resources.ResourceManager", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.Extensions", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.Handles", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.InteropServices", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.InteropServices.WindowsRuntime", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.Numerics", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.Serialization.Json", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.Serialization.Primitives", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.Serialization.Xml", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Security.Principal", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Duplex", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Http", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.NetTcp", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Primitives", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Security", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Text.Encoding", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Text.Encoding.Extensions", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Text.RegularExpressions", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Threading", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Threading.Tasks", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Threading.Tasks.Parallel", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Threading.Timer", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml.ReaderWriter", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml.XDocument", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml.XmlSerializer", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            // Manually added facades
            {"System.Windows", s_MICROSOFT_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml.Serialization", s_ECMA_PUBLICKEY_STR_L, s_VER_ASSEMBLYVERSION_STR_L},
#endif // NDP4_AUTO_VERSION_ROLLFORWARD
        };
    }
}
