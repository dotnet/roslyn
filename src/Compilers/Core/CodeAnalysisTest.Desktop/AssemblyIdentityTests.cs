// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using System;
using System.Globalization;
using System.Reflection;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class AssemblyIdentityTests : AssemblyIdentityTestBase
    {
        [Fact]
        public void FromAssemblyDefinition()
        {
            var name = new AssemblyName("foo");
            name.Flags = AssemblyNameFlags.Retargetable | AssemblyNameFlags.PublicKey | AssemblyNameFlags.EnableJITcompileOptimizer | AssemblyNameFlags.EnableJITcompileTracking;
            name.CultureInfo = new CultureInfo("en-US");
            name.ContentType = AssemblyContentType.Default;
            name.Version = new Version(1, 2, 3, 4);
            name.ProcessorArchitecture = ProcessorArchitecture.X86;

            var id = AssemblyIdentity.FromAssemblyDefinition(name);
            Assert.Equal("foo", id.Name);
            Assert.True(id.IsRetargetable);
            Assert.Equal(new Version(1, 2, 3, 4), id.Version);
            Assert.Equal(AssemblyContentType.Default, id.ContentType);
            Assert.False(id.HasPublicKey);
            Assert.False(id.IsStrongName);

            name = new AssemblyName("foo");
            name.SetPublicKey(PublicKey1);
            name.Version = new Version(1, 2, 3, 4);

            id = AssemblyIdentity.FromAssemblyDefinition(name);
            Assert.Equal("foo", id.Name);
            Assert.Equal(new Version(1, 2, 3, 4), id.Version);
            Assert.True(id.HasPublicKey);
            Assert.True(id.IsStrongName);
            AssertEx.Equal(id.PublicKey, PublicKey1);

            name = new AssemblyName("foo");
            name.ContentType = AssemblyContentType.WindowsRuntime;

            id = AssemblyIdentity.FromAssemblyDefinition(name);
            Assert.Equal("foo", id.Name);
            Assert.Equal(AssemblyContentType.WindowsRuntime, id.ContentType);
        }

        [Fact]
        public void FullKeyAndToken()
        {
            string displayPkt = "Foo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=" + StrPublicKeyToken1;
            string displayPk = "Foo, Version=1.0.0.0, Culture=neutral, PublicKey=" + StrPublicKey1;

            bool equivalent;
            FusionAssemblyIdentityComparer.AssemblyComparisonResult result;
            int hr = FusionAssemblyIdentityComparer.DefaultModelCompareAssemblyIdentity(displayPkt, false, displayPk, false, out equivalent, out result, IntPtr.Zero);

            Assert.Equal(0, hr);
            Assert.True(equivalent, "Expected equivalent");
            Assert.Equal(FusionAssemblyIdentityComparer.AssemblyComparisonResult.EquivalentFullMatch, result);

            hr = FusionAssemblyIdentityComparer.DefaultModelCompareAssemblyIdentity(displayPk, false, displayPkt, false, out equivalent, out result, IntPtr.Zero);

            Assert.Equal(0, hr);
            Assert.True(equivalent, "Expected equivalent");
            Assert.Equal(FusionAssemblyIdentityComparer.AssemblyComparisonResult.EquivalentFullMatch, result);
        }
    }
}
