// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

    partial class DesktopAssemblyIdentityComparer
    {
        private sealed class FrameworkAssemblyDictionary : Dictionary<string, FrameworkAssemblyDictionary.Value>
        {
            public FrameworkAssemblyDictionary()
                : base(SimpleNameComparer)
            {
            }

            public struct Value
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

            public struct Key : IEquatable<Key>
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

                public override bool Equals(object obj)
                {
                    return obj is Key && Equals((Key)obj);
                }

                public override int GetHashCode()
                {
                    return SimpleNameComparer.GetHashCode(Name) ^ PublicKeyToken[0];
                }
            }

            public struct Value
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
                object versionHightNull,
                string newName,
                ImmutableArray<byte> newPublicKeyToken,
                AssemblyVersion newVersion)
            {
                List<Value> values;
                var key = new Key(name, publicKeyToken);
                if (!TryGetValue(key, out values))
                {
                    Add(key, values = new List<Value>());
                }

                values.Add(new Value(versionLow, default(AssemblyVersion), newName, newPublicKeyToken, newVersion, isPortable: false));
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
                List<Value> values;
                var key = new Key(name, publicKeyToken);
                if (!TryGetValue(key, out values))
                {
                    Add(key, values = new List<Value>());
                }

                values.Add(new Value(versionLow, versionHigh, newName, newPublicKeyToken, newVersion, isPortable));
            }

            public bool TryGetValue(AssemblyIdentity identity, out Value value)
            {
                List<Value> values;
                if (!TryGetValue(new Key(identity.Name, identity.PublicKeyToken), out values))
                {
                    value = default(Value);
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

                value = default(Value);
                return false;
            }
        }

        private static readonly ImmutableArray<byte> NETCF_PUBLIC_KEY_TOKEN_1 =             ImmutableArray.Create(new byte[] { 0x1c, 0x9e, 0x25, 0x96, 0x86, 0xf9, 0x21, 0xe0 });
        private static readonly ImmutableArray<byte> NETCF_PUBLIC_KEY_TOKEN_2 =             ImmutableArray.Create(new byte[] { 0x5f, 0xd5, 0x7c, 0x54, 0x3a, 0x9c, 0x02, 0x47 });
        private static readonly ImmutableArray<byte> NETCF_PUBLIC_KEY_TOKEN_3 =             ImmutableArray.Create(new byte[] { 0x96, 0x9d, 0xb8, 0x05, 0x3d, 0x33, 0x22, 0xac });
        private static readonly ImmutableArray<byte> SQL_PUBLIC_KEY_TOKEN =                 ImmutableArray.Create(new byte[] { 0x89, 0x84, 0x5d, 0xcd, 0x80, 0x80, 0xcc, 0x91 });
        private static readonly ImmutableArray<byte> SQL_MOBILE_PUBLIC_KEY_TOKEN =          ImmutableArray.Create(new byte[] { 0x3b, 0xe2, 0x35, 0xdf, 0x1c, 0x8d, 0x2a, 0xd3 });
        private static readonly ImmutableArray<byte> ECMA_PUBLICKEY_STR_L =                 ImmutableArray.Create(new byte[] { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 });
        private static readonly ImmutableArray<byte> SHAREDLIB_PUBLICKEY_STR_L =            ImmutableArray.Create(new byte[] { 0x31, 0xbf, 0x38, 0x56, 0xad, 0x36, 0x4e, 0x35 });
        private static readonly ImmutableArray<byte> MICROSOFT_PUBLICKEY_STR_L =            ImmutableArray.Create(new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a });
        private static readonly ImmutableArray<byte> SILVERLIGHT_PLATFORM_PUBLICKEY_STR_L = ImmutableArray.Create(new byte[] { 0x7c, 0xec, 0x85, 0xd7, 0xbe, 0xa7, 0x79, 0x8e });
        private static readonly ImmutableArray<byte> SILVERLIGHT_PUBLICKEY_STR_L =          ImmutableArray.Create(new byte[] { 0x31, 0xbf, 0x38, 0x56, 0xad, 0x36, 0x4e, 0x35 });
        private static readonly ImmutableArray<byte> RIA_SERVICES_KEY_TOKEN =               ImmutableArray.Create(new byte[] { 0xdd, 0xd0, 0xda, 0x4d, 0x3e, 0x67, 0x82, 0x17 });

        private static readonly AssemblyVersion VER_VS_COMPATIBILITY_ASSEMBLYVERSION_STR_L = new AssemblyVersion(8, 0, 0, 0);
        private static readonly AssemblyVersion VER_VS_ASSEMBLYVERSION_STR_L = new AssemblyVersion(10, 0, 0, 0);
        private static readonly AssemblyVersion VER_SQL_ASSEMBLYVERSION_STR_L = new AssemblyVersion(9, 0, 242, 0);
        private static readonly AssemblyVersion VER_LINQ_ASSEMBLYVERSION_STR_L = new AssemblyVersion(3, 0, 0, 0);
        private static readonly AssemblyVersion VER_LINQ_ASSEMBLYVERSION_STR_2_L = new AssemblyVersion(3, 5, 0, 0);
        private static readonly AssemblyVersion VER_SQL_ORCAS_ASSEMBLYVERSION_STR_L = new AssemblyVersion(3, 5, 0, 0);
        private static readonly AssemblyVersion VER_ASSEMBLYVERSION_STR_L = new AssemblyVersion(4, 0, 0, 0);
        private static readonly AssemblyVersion VER_VC_STLCLR_ASSEMBLYVERSION_STR_L = new AssemblyVersion(2, 0, 0, 0);
        private const string NULL = null;
        private const bool TRUE = true;

        // Replace:
        // "([0-9]+)\.([0-9]+)\.([0-9]+)\.([0-9]+)"  ->   new AssemblyVersion($1, $2, $3, $4)
        // ,[ ]+FxPolicyHelper::AppXBinder_[A-Z]+    -> 
        // " -> "   (whole word, case sensitive)
        // :: -> .

        // copied from ndp\clr\src\fusion\binder\fxretarget.cpp
        private static readonly FrameworkRetargetingDictionary g_arRetargetPolicy = new FrameworkRetargetingDictionary()
        {
            // ECMA v1.0 redirect    
            {"System", ECMA_PUBLICKEY_STR_L, new AssemblyVersion(1, 0, 0, 0), NULL, NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml", ECMA_PUBLICKEY_STR_L, new AssemblyVersion(1, 0, 0, 0), NULL, NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            
            // Compat framework redirect
            {"System", NETCF_PUBLIC_KEY_TOKEN_1, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System", NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Drawing", NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Services", NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms", NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml", NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Drawing", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Services", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.VisualBasic", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(7, 0, 5000, 0), NULL, NULL, MICROSOFT_PUBLICKEY_STR_L, VER_VS_ASSEMBLYVERSION_STR_L},
            {"System", NETCF_PUBLIC_KEY_TOKEN_1, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System", NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Drawing", NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Services", NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms", NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml", NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Drawing", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Services", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.VisualBasic", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(7, 0, 5500, 0), NULL, NULL, MICROSOFT_PUBLICKEY_STR_L, VER_VS_ASSEMBLYVERSION_STR_L},
            {"Microsoft.WindowsCE.Forms", NETCF_PUBLIC_KEY_TOKEN_1, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, NETCF_PUBLIC_KEY_TOKEN_3, VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.WindowsCE.Forms", NETCF_PUBLIC_KEY_TOKEN_1, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, NETCF_PUBLIC_KEY_TOKEN_3, VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.WindowsCE.Forms", NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, NETCF_PUBLIC_KEY_TOKEN_3, VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.WindowsCE.Forms", NETCF_PUBLIC_KEY_TOKEN_2, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, NETCF_PUBLIC_KEY_TOKEN_3, VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.WindowsCE.Forms", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5000, 0), NULL, NULL, NETCF_PUBLIC_KEY_TOKEN_3, VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.WindowsCE.Forms", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5500, 0), NULL, NULL, NETCF_PUBLIC_KEY_TOKEN_3, VER_ASSEMBLYVERSION_STR_L},

            // Compat framework name redirect
            {"System.Data.SqlClient", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5000, 0), NULL, "System.Data", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.SqlClient", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5500, 0), NULL, "System.Data", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.Common", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5000, 0), NULL, "System.Data", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.Common", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5500, 0), NULL, "System.Data", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms.DataGrid", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5000, 0), NULL, "System.Windows.Forms", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms.DataGrid", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(1, 0, 5500, 0), NULL, "System.Windows.Forms", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},

            // v2.0 Compact framework redirect
            {"System", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(2, 0, 0, 0), new AssemblyVersion(2, 0, 10, 0), NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(2, 0, 0, 0), new AssemblyVersion(2, 0, 10, 0), NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Drawing", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(2, 0, 0, 0), new AssemblyVersion(2, 0, 10, 0), NULL, MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Services", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(2, 0, 0, 0), new AssemblyVersion(2, 0, 10, 0), NULL, MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(2, 0, 0, 0), new AssemblyVersion(2, 0, 10, 0), NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(2, 0, 0, 0), new AssemblyVersion(2, 0, 10, 0), NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Messaging", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(2, 0, 0, 0), new AssemblyVersion(2, 0, 10, 0), NULL, MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.SqlClient", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(2, 0, 0, 0), new AssemblyVersion(2, 0, 10, 0), "System.Data", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.Common", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(2, 0, 0, 0), new AssemblyVersion(2, 0, 10, 0), "System.Data", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms.DataGrid", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(2, 0, 0, 0), new AssemblyVersion(2, 0, 10, 0), "System.Windows.Forms", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.VisualBasic", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(8, 0, 0, 0), new AssemblyVersion(8, 0, 10, 0), NULL, MICROSOFT_PUBLICKEY_STR_L, VER_VS_ASSEMBLYVERSION_STR_L},

            // v3.5 Compact framework redirect
            {"System", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Drawing", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Services", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Messaging", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.SqlClient", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), "System.Data", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms.DataGrid", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), "System.Windows.Forms", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.VisualBasic", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(8, 1, 0, 0), new AssemblyVersion(8, 1, 5, 0), "Microsoft.VisualBasic", MICROSOFT_PUBLICKEY_STR_L, VER_VS_ASSEMBLYVERSION_STR_L},

            // SQL Everywhere redirect for Orcas
            {"System.Data.SqlClient", SQL_MOBILE_PUBLIC_KEY_TOKEN, new AssemblyVersion(3, 5, 0, 0), NULL, "System.Data", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.SqlServerCe", SQL_MOBILE_PUBLIC_KEY_TOKEN, new AssemblyVersion(3, 5, 0, 0), NULL, NULL, SQL_PUBLIC_KEY_TOKEN, VER_SQL_ORCAS_ASSEMBLYVERSION_STR_L},
            {"System.Data.SqlServerCe", SQL_MOBILE_PUBLIC_KEY_TOKEN, new AssemblyVersion(3, 5, 1, 0), new AssemblyVersion(3, 5, 200, 999), NULL, SQL_PUBLIC_KEY_TOKEN, VER_SQL_ORCAS_ASSEMBLYVERSION_STR_L},

            // SQL CE redirect
            {"System.Data.SqlClient", SQL_MOBILE_PUBLIC_KEY_TOKEN, new AssemblyVersion(3, 0, 3600, 0), NULL, "System.Data", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.SqlServerCe", SQL_MOBILE_PUBLIC_KEY_TOKEN, new AssemblyVersion(3, 0, 3600, 0), NULL, NULL, SQL_PUBLIC_KEY_TOKEN, VER_SQL_ASSEMBLYVERSION_STR_L},

            // Linq and friends redirect
            {"system.xml.linq", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, ECMA_PUBLICKEY_STR_L, VER_LINQ_ASSEMBLYVERSION_STR_2_L},
            {"system.data.DataSetExtensions", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, ECMA_PUBLICKEY_STR_L, VER_LINQ_ASSEMBLYVERSION_STR_2_L},
            {"System.Core", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, ECMA_PUBLICKEY_STR_L, VER_LINQ_ASSEMBLYVERSION_STR_2_L},
            {"System.ServiceModel", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, ECMA_PUBLICKEY_STR_L, VER_LINQ_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.Serialization", NETCF_PUBLIC_KEY_TOKEN_3, new AssemblyVersion(3, 5, 0, 0), new AssemblyVersion(3, 9, 0, 0), NULL, ECMA_PUBLICKEY_STR_L, VER_LINQ_ASSEMBLYVERSION_STR_L},

            // Portable Library redirects
            {"mscorlib",                           SILVERLIGHT_PLATFORM_PUBLICKEY_STR_L,   new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, ECMA_PUBLICKEY_STR_L,      VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System",                             SILVERLIGHT_PLATFORM_PUBLICKEY_STR_L,   new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, ECMA_PUBLICKEY_STR_L,      VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.ComponentModel.Composition",  SILVERLIGHT_PUBLICKEY_STR_L,            new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, ECMA_PUBLICKEY_STR_L,      VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.ComponentModel.DataAnnotations",RIA_SERVICES_KEY_TOKEN,               new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.Core",                        SILVERLIGHT_PLATFORM_PUBLICKEY_STR_L,   new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, ECMA_PUBLICKEY_STR_L,      VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.Net",                         SILVERLIGHT_PLATFORM_PUBLICKEY_STR_L,   new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.Numerics",                    SILVERLIGHT_PUBLICKEY_STR_L,            new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, ECMA_PUBLICKEY_STR_L,      VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"Microsoft.CSharp",                   SILVERLIGHT_PUBLICKEY_STR_L,            new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.Runtime.Serialization",       SILVERLIGHT_PLATFORM_PUBLICKEY_STR_L,   new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, ECMA_PUBLICKEY_STR_L,      VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.ServiceModel",                SILVERLIGHT_PUBLICKEY_STR_L,            new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, ECMA_PUBLICKEY_STR_L,      VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.ServiceModel.Web",            SILVERLIGHT_PLATFORM_PUBLICKEY_STR_L,   new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.Xml",                         SILVERLIGHT_PLATFORM_PUBLICKEY_STR_L,   new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, ECMA_PUBLICKEY_STR_L,      VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.Xml.Linq",                    SILVERLIGHT_PUBLICKEY_STR_L,            new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, ECMA_PUBLICKEY_STR_L,      VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.Xml.Serialization",           SILVERLIGHT_PUBLICKEY_STR_L,            new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, ECMA_PUBLICKEY_STR_L,      VER_ASSEMBLYVERSION_STR_L, TRUE},
            {"System.Windows",                     SILVERLIGHT_PLATFORM_PUBLICKEY_STR_L,   new AssemblyVersion(2, 0, 5, 0), new AssemblyVersion(99, 0, 0, 0), NULL, MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L, TRUE},

        };

        // Copied from ndp\clr\src\inc\fxretarget.h
        private static readonly FrameworkAssemblyDictionary g_arFxPolicy = new FrameworkAssemblyDictionary()
        {
            {"Accessibility", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"CustomMarshalers", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"ISymWrapper", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.JScript", MICROSOFT_PUBLICKEY_STR_L, VER_VS_ASSEMBLYVERSION_STR_L},
            {"Microsoft.VisualBasic", MICROSOFT_PUBLICKEY_STR_L, VER_VS_ASSEMBLYVERSION_STR_L},
            {"Microsoft.VisualBasic.Compatibility", MICROSOFT_PUBLICKEY_STR_L, VER_VS_ASSEMBLYVERSION_STR_L},
            {"Microsoft.VisualBasic.Compatibility.Data", MICROSOFT_PUBLICKEY_STR_L, VER_VS_ASSEMBLYVERSION_STR_L},
            {"Microsoft.VisualC", MICROSOFT_PUBLICKEY_STR_L, VER_VS_ASSEMBLYVERSION_STR_L},
            {"mscorlib", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Configuration", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Configuration.Install", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.OracleClient", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.SqlXml", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Deployment", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Design", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.DirectoryServices", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.DirectoryServices.Protocols", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Drawing", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Drawing.Design", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.EnterpriseServices", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Management", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Messaging", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.Remoting", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.Serialization.Formatters.Soap", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Security", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceProcess", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Transactions", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Web", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Mobile", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.RegularExpressions", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Services", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L}, // Has to be supported in AppX, because it is in transitive closure of supported assemblies
            {"System.Windows.Forms", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},

#if NDP4_AUTO_VERSION_ROLLFORWARD
    
            // Post-Everett FX 2.0 assemblies:
            {"AspNetMMCExt", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"sysglobl", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.Build.Engine", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.Build.Framework", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},

            // FX 3.0 assemblies:
            // Note: we shipped .NET 4.0 with entries in this list for PresentationCFFRasterizer and System.ServiceModel.Install
            // even though these assemblies did not ship with .NET 4.0. To maintain 100% compatibility with 4.0 we will keep
            // these in .NET 4.5, but we should remove them in a future SxS version of the Framework.
            {"PresentationCFFRasterizer", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L}, // See note above
            {"PresentationCore", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework.Aero", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework.Classic", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework.Luna", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework.Royale", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"PresentationUI", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"ReachFramework", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Printing", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Speech", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"UIAutomationClient", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"UIAutomationClientsideProviders", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"UIAutomationProvider", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"UIAutomationTypes", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"WindowsBase", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"WindowsFormsIntegration", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"SMDiagnostics", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.IdentityModel", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.IdentityModel.Selectors", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},    
            {"System.IO.Log", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.Serialization", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Install", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L}, // See note above
            {"System.ServiceModel.WasHosting", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Workflow.Activities", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Workflow.ComponentModel", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Workflow.Runtime", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.Transactions.Bridge", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.Transactions.Bridge.Dtc", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},

            // FX 3.5 assemblies:
            {"System.AddIn", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.AddIn.Contract", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ComponentModel.Composition", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L}, // Shipping out-of-band
            {"System.Core", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.DataSetExtensions", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.Linq", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml.Linq", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.DirectoryServices.AccountManagement", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Management.Instrumentation", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Net", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Web", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L}, // Needed for portable libraries
            {"System.Web.Extensions", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Extensions.Design", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Presentation", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.WorkflowServices", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            // Microsoft.Data.Entity.Build.Tasks.dll should not be unified on purpose - it is supported SxS, i.e. both 3.5 and 4.0 versions can be loaded into CLR 4.0+.
            // {"Microsoft.Data.Entity.Build.Tasks", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},

            // FX 3.5 SP1 assemblies:
            {"System.ComponentModel.DataAnnotations", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.Entity", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.Entity.Design", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.Services", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.Services.Client", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Data.Services.Design", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Abstractions", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.DynamicData", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.DynamicData.Design", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Entity", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Entity.Design", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.Routing", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},

            // FX 4.0 assemblies:
            {"Microsoft.Build", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.CSharp", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Dynamic", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Numerics", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Xaml", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            // Microsoft.Workflow.Compiler.exe:
            // System.Workflow.ComponentModel.dll started to depend on Microsoft.Workflow.Compiler.exe in 4.0 RTM
            {"Microsoft.Workflow.Compiler", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},

            // FX 4.5 assemblies:
            {"Microsoft.Activities.Build", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.Build.Conversion.v4.0", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.Build.Tasks.v4.0", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.Build.Utilities.v4.0", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.Internal.Tasks.Dataflow", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"Microsoft.VisualBasic.Activities.Compiler", MICROSOFT_PUBLICKEY_STR_L, VER_VS_ASSEMBLYVERSION_STR_L},
            {"Microsoft.VisualC.STLCLR", MICROSOFT_PUBLICKEY_STR_L, VER_VC_STLCLR_ASSEMBLYVERSION_STR_L},
            {"Microsoft.Windows.ApplicationServer.Applications", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"PresentationBuildTasks", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework.Aero2", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework.AeroLite", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework-SystemCore", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework-SystemData", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework-SystemDrawing", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework-SystemXml", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"PresentationFramework-SystemXmlLinq", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Activities", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Activities.Core.Presentation", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Activities.DurableInstancing", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Activities.Presentation", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ComponentModel.Composition.Registration", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Device", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.IdentityModel.Services", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.IO.Compression", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.IO.Compression.FileSystem", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Net.Http", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Net.Http.WebRequest", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Reflection.Context", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.Caching", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.DurableInstancing", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.WindowsRuntime", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.WindowsRuntime.UI.Xaml", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Activation", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Activities", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Channels", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Discovery", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Internals", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Routing", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.ServiceMoniker40", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.ApplicationServices", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L}, // Has to be supported in AppX, because it is in transitive closure of supported assemblies
            {"System.Web.DataVisualization", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Web.DataVisualization.Design", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Controls.Ribbon", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms.DataVisualization", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Forms.DataVisualization.Design", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows.Input.Manipulations", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Xaml.Hosting", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"XamlBuildTask", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"XsdBuildTask", SHAREDLIB_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},

            // FX 4.5 facade assemblies:
            {"System.Collections", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Collections.Concurrent", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ComponentModel", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ComponentModel.Annotations", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ComponentModel.EventBasedAsync", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Diagnostics.Contracts", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Diagnostics.Debug", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Diagnostics.Tools", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Diagnostics.Tracing", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Dynamic.Runtime", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Globalization", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.IO", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Linq", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Linq.Expressions", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Linq.Parallel", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Linq.Queryable", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Net.NetworkInformation", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Net.Primitives", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Net.Requests", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ObjectModel", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Reflection", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Reflection.Emit", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Reflection.Emit.ILGeneration", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Reflection.Emit.Lightweight", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Reflection.Extensions", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Reflection.Primitives", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Resources.ResourceManager", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.Extensions", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.InteropServices", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.InteropServices.WindowsRuntime", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.Numerics", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.Serialization.Json", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.Serialization.Primitives", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Runtime.Serialization.Xml", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Security.Principal", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Duplex", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Http", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.NetTcp", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Primitives", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.ServiceModel.Security", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Text.Encoding", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Text.Encoding.Extensions", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Text.RegularExpressions", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Threading", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Threading.Tasks", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Threading.Tasks.Parallel", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Threading.Timer", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L}, 
            {"System.Xml.ReaderWriter", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml.XDocument", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml.XmlSerializer", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            // Manually added facades
            {"System.Net.Http.Rtc", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Windows", MICROSOFT_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},
            {"System.Xml.Serialization", ECMA_PUBLICKEY_STR_L, VER_ASSEMBLYVERSION_STR_L},

#endif // NDP4_AUTO_VERSION_ROLLFORWARD

        };
    }
}
