// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using System.Collections.Immutable;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class GlobalAssemblyCacheTests
    {
        [MonoOnlyFact("https://github.com/dotnet/roslyn/issues/6179")]
        public void GetAssemblyIdentities()
        {
            var gac = GlobalAssemblyCache.Instance;

            AssemblyIdentity[] names;

            names = gac.GetAssemblyIdentities(new AssemblyName("mscorlib")).ToArray();
            Assert.True(names.Length >= 1, "At least 1 mscorlib");
            foreach (var name in names)
            {
                Assert.Equal("mscorlib", name.Name);
            }

            names = gac.GetAssemblyIdentities(new AssemblyName("mscorlib"), ImmutableArray.Create(ProcessorArchitecture.MSIL, ProcessorArchitecture.X86)).ToArray();
            Assert.True(names.Length >= 1, "At least one 32bit mscorlib");
            foreach (var name in names)
            {
                Assert.Equal("mscorlib", name.Name);
            }

            names = gac.GetAssemblyIdentities("mscorlib").ToArray();
            Assert.True(names.Length >= 1, "At least 1 mscorlib");
            foreach (var name in names)
            {
                Assert.Equal("mscorlib", name.Name);
            }

            names = gac.GetAssemblyIdentities("System.Core, Version=4.0.0.0").ToArray();
            Assert.True(names.Length >= 1, "At least System.Core");
            foreach (var name in names)
            {
                Assert.Equal("System.Core", name.Name);
                Assert.Equal(new Version(4, 0, 0, 0), name.Version);
                Assert.True(name.GetDisplayName().Contains("PublicKeyToken=b77a5c561934e089"), "PublicKeyToken matches");
            }

            names = gac.GetAssemblyIdentities("System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089").ToArray();
            Assert.True(names.Length >= 1, "At least System.Core");
            foreach (var name in names)
            {
                Assert.Equal("System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", name.GetDisplayName());
            }

            var n = new AssemblyName("System.Core");
            n.Version = new Version(4, 0, 0, 0);
            n.SetPublicKeyToken(new byte[] { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 });
            names = gac.GetAssemblyIdentities(n).ToArray();

            Assert.True(names.Length >= 1, "At least System.Core");
            foreach (var name in names)
            {
                Assert.Equal("System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", name.GetDisplayName());
            }

            names = gac.GetAssemblyIdentities("x\u0002").ToArray();
            Assert.Equal(0, names.Length);

            names = gac.GetAssemblyIdentities("\0").ToArray();
            Assert.Equal(0, names.Length);

            names = gac.GetAssemblyIdentities("xxxx\0xxxxx").ToArray();
            Assert.Equal(0, names.Length);

            // fusion API CreateAssemblyEnum returns S_FALSE for this name
            names = gac.GetAssemblyIdentities("nonexistingassemblyname" + Guid.NewGuid().ToString()).ToArray();
            Assert.Equal(0, names.Length);
        }

        [MonoOnlyFact("https://github.com/dotnet/roslyn/pull/39369")]
        public void GetFacadeAssemblyIdentities()
        {
            var gac = GlobalAssemblyCache.Instance;

            AssemblyIdentity[] names;

            // One netstandard.dll should resolve from Facades on Mono
            names = gac.GetAssemblyIdentities(new AssemblyName("netstandard")).ToArray();
            Assert.Collection(names, name =>
            {
                Assert.Equal("netstandard", name.Name);
                Assert.True(name.Version >= new Version("2.0.0.0"), "netstandard version must be >= 2.0.0.0");
            });
        }

        [ClrOnlyFact(ClrOnlyReason.Fusion)]
        public void AssemblyAndGacLocation()
        {
            var names = ClrGlobalAssemblyCache.GetAssemblyObjects(partialNameFilter: null, architectureFilter: default(ImmutableArray<ProcessorArchitecture>)).ToArray();
            Assert.True(names.Length > 100, "There are at least 100 assemblies in the GAC");

            var gacLocationsUpper = GlobalAssemblyCacheLocation.RootLocations.Select(location => location.ToUpper());
            foreach (var name in names)
            {
                string location = ClrGlobalAssemblyCache.GetAssemblyLocation(name);
                Assert.NotNull(location);
                Assert.True(gacLocationsUpper.Any(gac => location.StartsWith(gac, StringComparison.OrdinalIgnoreCase)), "Path within some GAC root");
                Assert.Equal(Path.GetFullPath(location), location);
            }
        }
    }
}
