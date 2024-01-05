// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class AssemblyIdentityTests : AssemblyIdentityTestBase
    {
        [Fact]
        public void Equality()
        {
            var id1 = new AssemblyIdentity("Goo", new Version(1, 0, 0, 0), "", RoPublicKey1, hasPublicKey: true, isRetargetable: false);
            var id11 = new AssemblyIdentity("Goo", new Version(1, 0, 0, 0), "", RoPublicKey1, hasPublicKey: true, isRetargetable: false);
            var id2 = new AssemblyIdentity("Goo", new Version(1, 0, 0, 0), "", RoPublicKeyToken1, hasPublicKey: false, isRetargetable: false);
            var id22 = new AssemblyIdentity("Goo", new Version(1, 0, 0, 0), "", RoPublicKeyToken1, hasPublicKey: false, isRetargetable: false);

            var id3 = new AssemblyIdentity("Goo!", new Version(1, 0, 0, 0), "", RoPublicKeyToken1, hasPublicKey: false, isRetargetable: false);
            var id4 = new AssemblyIdentity("Goo", new Version(1, 0, 1, 0), "", RoPublicKeyToken1, hasPublicKey: false, isRetargetable: false);
            var id5 = new AssemblyIdentity("Goo", new Version(1, 0, 0, 0), "en-US", RoPublicKeyToken1, hasPublicKey: false, isRetargetable: false);
            var id6 = new AssemblyIdentity("Goo", new Version(1, 0, 0, 0), "", default(ImmutableArray<byte>), hasPublicKey: false, isRetargetable: false);
            var id7 = new AssemblyIdentity("Goo", new Version(1, 0, 0, 0), "", RoPublicKey1, hasPublicKey: true, isRetargetable: true);

            var win1 = new AssemblyIdentity("Goo", new Version(1, 0, 0, 0), "", RoPublicKey1, hasPublicKey: true, isRetargetable: false, contentType: AssemblyContentType.WindowsRuntime);
            var win2 = new AssemblyIdentity("Bar", new Version(1, 0, 0, 0), "", RoPublicKey1, hasPublicKey: true, isRetargetable: false, contentType: AssemblyContentType.WindowsRuntime);
            var win3 = new AssemblyIdentity("Goo", new Version(1, 0, 0, 0), "", RoPublicKey1, hasPublicKey: true, isRetargetable: false, contentType: AssemblyContentType.WindowsRuntime);

            Assert.True(id1.Equals(id1));
            Assert.True(id1.Equals(id2));
            Assert.True(id2.Equals(id1));
            Assert.True(id1.Equals(id11));
            Assert.True(id11.Equals(id1));
            Assert.True(id2.Equals(id22));
            Assert.True(id22.Equals(id2));

            Assert.False(id1.Equals(id3));
            Assert.False(id1.Equals(id4));
            Assert.False(id1.Equals(id5));
            Assert.False(id1.Equals(id6));
            Assert.False(id1.Equals(id7));

            Assert.Equal((object)id1, id1);
            Assert.NotNull(id1);
            Assert.False(id2.Equals((AssemblyIdentity)null));

            Assert.Equal(id1.GetHashCode(), id2.GetHashCode());

            Assert.False(win1.Equals(win2));
            Assert.False(win1.Equals(id1));
            Assert.True(win1.Equals(win3));

            Assert.Equal(win1.GetHashCode(), win3.GetHashCode());
        }

        [Fact]
        public void Equality_InvariantCulture()
        {
            var neutral1 = new AssemblyIdentity("Goo", new Version(1, 0, 0, 0), "NEUtral", RoPublicKeyToken1, hasPublicKey: false, isRetargetable: false);
            var neutral2 = new AssemblyIdentity("Goo", new Version(1, 0, 0, 0), null, RoPublicKeyToken1, hasPublicKey: false, isRetargetable: false);
            var neutral3 = new AssemblyIdentity("Goo", new Version(1, 0, 0, 0), "neutral", RoPublicKeyToken1, hasPublicKey: false, isRetargetable: false);
            var invariant = new AssemblyIdentity("Goo", new Version(1, 0, 0, 0), "", RoPublicKeyToken1, hasPublicKey: false, isRetargetable: false);

            Assert.True(neutral1.Equals(invariant));
            Assert.True(neutral2.Equals(invariant));
            Assert.True(neutral3.Equals(invariant));
        }

        [Fact]
        public void FromAssemblyDefinitionInvalidParameters()
        {
            Assembly asm = null;
            Assert.Throws<ArgumentNullException>(() => { AssemblyIdentity.FromAssemblyDefinition(asm); });
        }

        [Fact]
        public void FromAssemblyDefinition()
        {
            var name = new AssemblyName("goo");
            name.Flags = AssemblyNameFlags.Retargetable | AssemblyNameFlags.PublicKey | AssemblyNameFlags.EnableJITcompileOptimizer | AssemblyNameFlags.EnableJITcompileTracking;
            name.CultureInfo = new CultureInfo("en-US", useUserOverride: false);
            name.ContentType = AssemblyContentType.Default;
            name.Version = new Version(1, 2, 3, 4);
#pragma warning disable SYSLIB0037
            // warning SYSLIB0037: 'AssemblyName.ProcessorArchitecture' is obsolete: 'AssemblyName members HashAlgorithm, ProcessorArchitecture, and VersionCompatibility are obsolete and not supported.'
            name.ProcessorArchitecture = ProcessorArchitecture.X86;
#pragma warning restore SYSLIB0037

            var id = AssemblyIdentity.FromAssemblyDefinition(name);
            Assert.Equal("goo", id.Name);
            Assert.True(id.IsRetargetable);
            Assert.Equal(new Version(1, 2, 3, 4), id.Version);
            Assert.Equal(AssemblyContentType.Default, id.ContentType);
            Assert.False(id.HasPublicKey);
            Assert.False(id.IsStrongName);

            name = new AssemblyName("goo");
            name.SetPublicKey(PublicKey1);
            name.Version = new Version(1, 2, 3, 4);

            id = AssemblyIdentity.FromAssemblyDefinition(name);
            Assert.Equal("goo", id.Name);
            Assert.Equal(new Version(1, 2, 3, 4), id.Version);
            Assert.True(id.HasPublicKey);
            Assert.True(id.IsStrongName);
            AssertEx.Equal(id.PublicKey, PublicKey1);

            name = new AssemblyName("goo");
            name.ContentType = AssemblyContentType.WindowsRuntime;

            id = AssemblyIdentity.FromAssemblyDefinition(name);
            Assert.Equal("goo", id.Name);
            Assert.Equal(AssemblyContentType.WindowsRuntime, id.ContentType);
        }

        [Fact]
        public void FromAssemblyDefinition_InvariantCulture()
        {
            var name = new AssemblyName("goo");
            name.Flags = AssemblyNameFlags.None;
            name.CultureInfo = CultureInfo.InvariantCulture;
            name.ContentType = AssemblyContentType.Default;
            name.Version = new Version(1, 2, 3, 4);
#pragma warning disable SYSLIB0037
            name.ProcessorArchitecture = ProcessorArchitecture.X86;
#pragma warning restore SYSLIB0037

            var id = AssemblyIdentity.FromAssemblyDefinition(name);
            Assert.Equal("", id.CultureName);
        }

        [Fact]
        public void Properties()
        {
            var id = new AssemblyIdentity("Goo", hasPublicKey: false, isRetargetable: false);
            Assert.Equal("Goo", id.Name);
            Assert.Equal(new Version(0, 0, 0, 0), id.Version);
            Assert.Equal(AssemblyNameFlags.None, id.Flags);
            Assert.Equal("", id.CultureName);
            Assert.False(id.HasPublicKey);
            Assert.False(id.IsRetargetable);
            Assert.Equal(0, id.PublicKey.Length);
            Assert.Equal(0, id.PublicKeyToken.Length);
            Assert.Equal(AssemblyContentType.Default, id.ContentType);

            id = new AssemblyIdentity("Goo", publicKeyOrToken: RoPublicKey1, hasPublicKey: true, isRetargetable: false);
            Assert.Equal("Goo", id.Name);
            Assert.Equal(new Version(0, 0, 0, 0), id.Version);
            Assert.Equal(AssemblyNameFlags.PublicKey, id.Flags);
            Assert.Equal("", id.CultureName);
            Assert.True(id.HasPublicKey);
            Assert.False(id.IsRetargetable);
            AssertEx.Equal(PublicKey1, id.PublicKey);
            AssertEx.Equal(PublicKeyToken1, id.PublicKeyToken);
            Assert.Equal(AssemblyContentType.Default, id.ContentType);

            id = new AssemblyIdentity("Goo", publicKeyOrToken: RoPublicKeyToken1, hasPublicKey: false, isRetargetable: true);
            Assert.Equal("Goo", id.Name);
            Assert.Equal(new Version(0, 0, 0, 0), id.Version);
            Assert.Equal(AssemblyNameFlags.Retargetable, id.Flags);
            Assert.Equal("", id.CultureName);
            Assert.False(id.HasPublicKey);
            Assert.True(id.IsRetargetable);
            Assert.Equal(0, id.PublicKey.Length);
            AssertEx.Equal(PublicKeyToken1, id.PublicKeyToken);
            Assert.Equal(AssemblyContentType.Default, id.ContentType);

            id = new AssemblyIdentity("Goo", publicKeyOrToken: RoPublicKey1, hasPublicKey: true, isRetargetable: true);
            Assert.Equal("Goo", id.Name);
            Assert.Equal(new Version(0, 0, 0, 0), id.Version);
            Assert.Equal(AssemblyNameFlags.PublicKey | AssemblyNameFlags.Retargetable, id.Flags);
            Assert.Equal("", id.CultureName);
            Assert.True(id.HasPublicKey);
            Assert.True(id.IsRetargetable);
            AssertEx.Equal(PublicKey1, id.PublicKey);
            AssertEx.Equal(PublicKeyToken1, id.PublicKeyToken);
            Assert.Equal(AssemblyContentType.Default, id.ContentType);

            id = new AssemblyIdentity("Goo", publicKeyOrToken: RoPublicKey1, hasPublicKey: true, contentType: AssemblyContentType.WindowsRuntime);
            Assert.Equal("Goo", id.Name);
            Assert.Equal(new Version(0, 0, 0, 0), id.Version);
            Assert.Equal(AssemblyNameFlags.PublicKey, id.Flags);
            Assert.Equal("", id.CultureName);
            Assert.True(id.HasPublicKey);
            Assert.False(id.IsRetargetable);
            AssertEx.Equal(PublicKey1, id.PublicKey);
            AssertEx.Equal(PublicKeyToken1, id.PublicKeyToken);
            Assert.Equal(AssemblyContentType.WindowsRuntime, id.ContentType);
        }

        [Fact]
        public void IsStrongName()
        {
            var id1 = new AssemblyIdentity("Goo", new Version(1, 0, 0, 0), "", RoPublicKey1, hasPublicKey: true, isRetargetable: false);
            Assert.True(id1.IsStrongName);

            var id2 = new AssemblyIdentity("Goo", new Version(1, 0, 0, 0), "", RoPublicKeyToken1, hasPublicKey: false, isRetargetable: false);
            Assert.True(id2.IsStrongName);

            var id3 = new AssemblyIdentity("Goo", new Version(1, 0, 0, 0), "", ImmutableArray<byte>.Empty, hasPublicKey: false, isRetargetable: false);
            Assert.False(id3.IsStrongName);

            // for WinRT references "strong name" doesn't make sense:

            var id4 = new AssemblyIdentity("Goo", new Version(1, 0, 0, 0), "", ImmutableArray<byte>.Empty, hasPublicKey: false, isRetargetable: false, contentType: AssemblyContentType.WindowsRuntime);
            Assert.False(id4.IsStrongName);

            var id5 = new AssemblyIdentity("Goo", new Version(1, 0, 0, 0), "", RoPublicKeyToken1, hasPublicKey: false, isRetargetable: false, contentType: AssemblyContentType.WindowsRuntime);
            Assert.True(id5.IsStrongName);
        }

        [Fact]
        public void InvalidConstructorArgs()
        {
            Assert.Throws<ArgumentException>(() => new AssemblyIdentity("xx\0xx"));
            Assert.Throws<ArgumentException>(() => new AssemblyIdentity(""));
            Assert.Throws<ArgumentException>(() => new AssemblyIdentity(null));

            Assert.Throws<ArgumentException>(
                () => new AssemblyIdentity("Goo", new Version(1, 0, 0, 0), "", ImmutableArray<byte>.Empty, hasPublicKey: true, isRetargetable: false, contentType: AssemblyContentType.Default));

            Assert.Throws<ArgumentException>(
                () => new AssemblyIdentity("Goo", new Version(1, 0, 0, 0), "", new byte[] { 1, 2, 3 }.AsImmutableOrNull(), hasPublicKey: false, isRetargetable: false, contentType: AssemblyContentType.Default));

            foreach (var v in new Version[]
            {
                new Version(),
                new Version(0, 0),
                new Version(0, 0, 0),
                new Version(int.MaxValue, 0, 0, 0),
                new Version(0, int.MaxValue, 0, 0),
                new Version(0, 0, int.MaxValue, 0),
                new Version(0, 0, 0, int.MaxValue),
            })
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new AssemblyIdentity("Goo", v));
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => new AssemblyIdentity("Goo", contentType: (AssemblyContentType)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AssemblyIdentity("Goo", contentType: (AssemblyContentType)int.MaxValue));

            Assert.Throws<ArgumentException>(() =>
                new AssemblyIdentity("Goo", publicKeyOrToken: RoPublicKey1, hasPublicKey: true, isRetargetable: true, contentType: AssemblyContentType.WindowsRuntime));
        }

        [Fact]
        public void MetadataConstructor()
        {
            var id = new AssemblyIdentity(/*noThrow:*/true, "Goo", new Version(1, 2, 3, 4), "en-US", RoPublicKey1,
                hasPublicKey: true, isRetargetable: true, contentType: AssemblyContentType.Default);
            Assert.Equal("Goo", id.Name);
            Assert.Equal(new Version(1, 2, 3, 4), id.Version);
            Assert.Equal(AssemblyNameFlags.PublicKey | AssemblyNameFlags.Retargetable, id.Flags);
            Assert.Equal("en-US", id.CultureName);
            Assert.True(id.HasPublicKey);
            Assert.True(id.IsRetargetable);
            AssertEx.Equal(PublicKey1, id.PublicKey);
            AssertEx.Equal(PublicKeyToken1, id.PublicKeyToken);
            Assert.Equal(AssemblyContentType.Default, id.ContentType);

            // invalid content type:
            id = new AssemblyIdentity(/*noThrow:*/true, "Goo", new Version(1, 2, 3, 4), null, ImmutableArray<byte>.Empty,
                hasPublicKey: false, isRetargetable: false, contentType: (AssemblyContentType)2);
            Assert.Equal(AssemblyNameFlags.None, id.Flags);
            Assert.Equal("", id.CultureName);
            Assert.False(id.HasPublicKey);
            Assert.Equal(0, id.PublicKey.Length);
            Assert.Equal(0, id.PublicKeyToken.Length);
            Assert.Equal(AssemblyContentType.Default, id.ContentType);

            // default Retargetable=No if content type is WinRT
            id = new AssemblyIdentity(/*noThrow:*/true, "Goo", new Version(1, 2, 3, 4), null, ImmutableArray<byte>.Empty,
                hasPublicKey: false, isRetargetable: true, contentType: AssemblyContentType.WindowsRuntime);
            Assert.Equal("Goo", id.Name);
            Assert.Equal(new Version(1, 2, 3, 4), id.Version);
            Assert.Equal(AssemblyNameFlags.None, id.Flags);
            Assert.Equal("", id.CultureName);
            Assert.False(id.HasPublicKey);
            Assert.False(id.IsRetargetable);
            Assert.Equal(AssemblyContentType.WindowsRuntime, id.ContentType);

            // invalid culture:
            // The native compiler doesn't enforce that the culture be anything in particular. 
            // AssemblyIdentity should preserve user input even if it is of dubious utility.
            id = new AssemblyIdentity(/*noThrow:*/true, "Goo", new Version(1, 2, 3, 4), "blah,", ImmutableArray<byte>.Empty,
                hasPublicKey: false, isRetargetable: false, contentType: AssemblyContentType.Default);
            Assert.Equal("blah,", id.CultureName);

            id = new AssemblyIdentity(/*noThrow:*/true, "Goo", new Version(1, 2, 3, 4), "*", ImmutableArray<byte>.Empty,
                hasPublicKey: false, isRetargetable: false, contentType: AssemblyContentType.Default);
            Assert.Equal("*", id.CultureName);

            id = new AssemblyIdentity(/*noThrow:*/true, "Goo", new Version(1, 2, 3, 4), "neutral", ImmutableArray<byte>.Empty,
                hasPublicKey: false, isRetargetable: false, contentType: AssemblyContentType.Default);
            Assert.Equal("", id.CultureName);
        }

        [Fact]
        public void ToAssemblyName()
        {
            var ai = new AssemblyIdentity("goo");
            var an = ai.ToAssemblyName();
            Assert.Equal("goo", an.Name);
            Assert.Equal(new Version(0, 0, 0, 0), an.Version);
            Assert.Equal(CultureInfo.InvariantCulture, an.CultureInfo);
            AssertEx.Equal(new byte[0], an.GetPublicKeyToken());
            AssertEx.Equal(null, an.GetPublicKey());
            Assert.Equal(AssemblyNameFlags.None, an.Flags);
#pragma warning disable SYSLIB0044
            // warning SYSLIB0044: 'AssemblyName.CodeBase' is obsolete: 'AssemblyName.CodeBase and AssemblyName.EscapedCodeBase are obsolete. Using them for loading an assembly is not supported.'
            Assert.Null(an.CodeBase);
#pragma warning restore SYSLIB0044

            ai = new AssemblyIdentity("goo", new Version(1, 2, 3, 4), "en-US", RoPublicKey1,
                hasPublicKey: true,
                isRetargetable: true);

            an = ai.ToAssemblyName();
            Assert.Equal("goo", an.Name);
            Assert.Equal(new Version(1, 2, 3, 4), an.Version);
            Assert.Equal(CultureInfo.GetCultureInfo("en-US"), an.CultureInfo);
            AssertEx.Equal(PublicKeyToken1, an.GetPublicKeyToken());
            AssertEx.Equal(PublicKey1, an.GetPublicKey());
            Assert.Equal(AssemblyNameFlags.PublicKey | AssemblyNameFlags.Retargetable, an.Flags);
#pragma warning disable SYSLIB0044
            Assert.Null(an.CodeBase);
#pragma warning restore SYSLIB0044

            // invalid characters are ok in the name, the FullName can't be built though:
            foreach (char c in ClrInvalidCharacters)
            {
                ai = new AssemblyIdentity(c.ToString());
                an = ai.ToAssemblyName();

                Assert.Equal(c.ToString(), an.Name);
                Assert.Equal(new Version(0, 0, 0, 0), an.Version);
                Assert.Equal(CultureInfo.InvariantCulture, an.CultureInfo);
                AssertEx.Equal(new byte[0], an.GetPublicKeyToken());
                AssertEx.Equal(null, an.GetPublicKey());
                Assert.Equal(AssemblyNameFlags.None, an.Flags);
#pragma warning disable SYSLIB0044
                AssertEx.Equal(null, an.CodeBase);
#pragma warning restore SYSLIB0044
            }
        }

        [ConditionalFact(typeof(DesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsDesktopTypes)]
        public void ToAssemblyName_Errors()
        {
            var ai = new AssemblyIdentity("goo", cultureName: "*");
            Assert.Throws<CultureNotFoundException>(() => ai.ToAssemblyName());
        }

        [Fact]
        public void Keys()
        {
            var an = new AssemblyName();
            an.Name = "Goo";
            an.Version = new Version(1, 0, 0, 0);
            an.SetPublicKey(PublicKey1);
            var anPkt = an.GetPublicKeyToken();
            var aiPkt = AssemblyIdentity.CalculatePublicKeyToken(RoPublicKey1);
            AssertEx.Equal(PublicKeyToken1, anPkt);
            AssertEx.Equal(PublicKeyToken1, aiPkt);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void FullKeyAndToken()
        {
            string displayPkt = "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=" + StrPublicKeyToken1;
            string displayPk = "Goo, Version=1.0.0.0, Culture=neutral, PublicKey=" + StrPublicKey1;

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

