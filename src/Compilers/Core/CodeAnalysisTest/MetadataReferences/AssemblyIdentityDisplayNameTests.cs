// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class AssemblyIdentityDisplayNameTests : AssemblyIdentityTestBase
    {
        private const AssemblyIdentityParts N = AssemblyIdentityParts.Name;
        private const AssemblyIdentityParts NV = N | AssemblyIdentityParts.Version;
        private const AssemblyIdentityParts NVK = NV | AssemblyIdentityParts.PublicKey;
        private const AssemblyIdentityParts NVT = NV | AssemblyIdentityParts.PublicKeyToken;
        private const AssemblyIdentityParts NVC = NV | AssemblyIdentityParts.Culture;
        private const AssemblyIdentityParts NVCK = NVC | AssemblyIdentityParts.PublicKey;
        private const AssemblyIdentityParts NVCT = NVC | AssemblyIdentityParts.PublicKeyToken;

        private void TestParseVersionInvalid(string value)
        {
            AssemblyIdentityParts actualParts;
            ulong actual;
            Assert.False(AssemblyIdentity.TryParseVersion(value, out actual, out actualParts));

            // compare with fusion
            var fusionName = FusionAssemblyIdentity.ToAssemblyNameObject("Name, Version=" + value);
            if (fusionName != null)
            {
                AssemblyIdentityParts fusionParts = 0;
                var fusionVersion = FusionAssemblyIdentity.GetVersion(fusionName, out fusionParts);

                // name parsing succeeds but there is no version:
                Assert.Equal((AssemblyIdentityParts)0, fusionParts);
            }
        }

        private void TestParseVersion(string value, int major, int minor, int build, int revision, AssemblyIdentityParts expectedParts)
        {
            AssemblyIdentityParts actualParts;
            ulong actual;
            Assert.True(AssemblyIdentity.TryParseVersion(value, out actual, out actualParts));
            Assert.Equal(expectedParts, actualParts);

            Version actualVersion = AssemblyIdentity.ToVersion(actual);
            Assert.Equal(new Version(major, minor, build, revision), actualVersion);

            // compare with fusion
            var fusionName = FusionAssemblyIdentity.ToAssemblyNameObject("Name, Version=" + value);
            Assert.NotNull(fusionName);

            AssemblyIdentityParts fusionParts = 0;
            var fusionVersion = FusionAssemblyIdentity.GetVersion(fusionName, out fusionParts);
            Assert.Equal(fusionVersion, actualVersion);

            // Test limitation:
            // When constructing INameObject with CANOF.PARSE_DISPLAY_NAME option,
            // the Version=* is treated as unspecified version. That's also done by TryParseDisplayName,
            // but outside of TryParseVersion, which we are testing here.
            if (value == "*")
            {
                Assert.Equal((AssemblyIdentityParts)0, fusionParts);
            }
            else
            {
                Assert.Equal(expectedParts, fusionParts);
            }
        }

        private void TestParseVersion(string value)
        {
            string displayName = "Goo, Version=" + value;
            var fusion = FusionAssemblyIdentity.ToAssemblyIdentity(FusionAssemblyIdentity.ToAssemblyNameObject(displayName));

            AssemblyIdentity id = null;
            bool success = AssemblyIdentity.TryParseDisplayName(displayName, out id);

            Assert.Equal(fusion != null, success);

            if (success)
            {
                Assert.Equal(fusion.Version, id.Version);
            }
        }

        private void TestParseSimpleName(string displayName, string expected)
        {
            TestParseSimpleName(displayName, expected, expected);
        }

        private void TestParseSimpleName(string displayName, string expected, string expectedFusion)
        {
            var fusionName = FusionAssemblyIdentity.ToAssemblyNameObject(displayName);
            var actual = (fusionName != null) ? FusionAssemblyIdentity.GetName(fusionName) : null;
            Assert.Equal(expectedFusion, actual);

            AssemblyIdentity id;
            actual = AssemblyIdentity.TryParseDisplayName(displayName, out id) ? id.Name : null;
            Assert.Equal(expected, actual);
        }

        private void TestParseDisplayName(string displayName, AssemblyIdentity expected, AssemblyIdentityParts expectedParts = 0)
        {
            TestParseDisplayName(displayName, expected, expectedParts, expected);
        }

        private void TestParseDisplayName(string displayName, AssemblyIdentity expected, AssemblyIdentityParts expectedParts, AssemblyIdentity expectedFusion)
        {
            var fusion = FusionAssemblyIdentity.ToAssemblyIdentity(FusionAssemblyIdentity.ToAssemblyNameObject(displayName));
            Assert.Equal(expectedFusion, fusion);

            AssemblyIdentity id = null;
            AssemblyIdentityParts actualParts;
            bool success = AssemblyIdentity.TryParseDisplayName(displayName, out id, out actualParts);
            Assert.Equal(expected, id);
            Assert.Equal(success, id != null);
            Assert.Equal(expectedParts, actualParts);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void GetDisplayName()
        {
            var id = new AssemblyIdentity("goo");
            Assert.Equal("goo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", id.GetDisplayName());

            id = new AssemblyIdentity("goo", new Version(1, 2, 3, 4));
            Assert.Equal("goo, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", id.GetDisplayName());

            id = new AssemblyIdentity("goo", cultureName: "en-US");
            Assert.Equal("goo, Version=0.0.0.0, Culture=en-US, PublicKeyToken=null", id.GetDisplayName());

            id = new AssemblyIdentity("goo", publicKeyOrToken: new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF }.AsImmutableOrNull());
            Assert.Equal("goo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=0123456789abcdef", id.GetDisplayName(), StringComparer.OrdinalIgnoreCase);

            id = new AssemblyIdentity("goo", isRetargetable: true);
            Assert.Equal("goo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null, Retargetable=Yes", id.GetDisplayName());

            id = new AssemblyIdentity("goo", contentType: AssemblyContentType.WindowsRuntime);
            Assert.Equal("goo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime", id.GetDisplayName());

            id = new AssemblyIdentity("Goo", publicKeyOrToken: RoPublicKey1, hasPublicKey: true);
            string dn1 = id.GetDisplayName();
            string dn2 = id.GetDisplayName(fullKey: false);
            Assert.True(ReferenceEquals(dn1, dn2), "cached full name expected");
            Assert.Equal("Goo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=" + StrPublicKeyToken1, dn1);

            string dnFull = id.GetDisplayName(fullKey: true);
            Assert.Equal("Goo, Version=0.0.0.0, Culture=neutral, PublicKey=" + StrPublicKey1, dnFull);

            id = new AssemblyIdentity("Goo", cultureName: "neutral");
            Assert.Equal("Goo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", id.GetDisplayName());

            id = new AssemblyIdentity("Goo", cultureName: "  '\t\r\n\\=,  ");
            Assert.Equal(@"Goo, Version=0.0.0.0, Culture=""  \'\t\r\n\\\=\,  "", PublicKeyToken=null", id.GetDisplayName());
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void TryParseDisplayName_QuotingAndEscaping()
        {
            // escapes:
            TestParseSimpleName("/, Version=1.0.0.0", expected: "/");
            TestParseSimpleName("\\\\, Version=1.0.0.0", expected: "\\");
            TestParseSimpleName("\\,\\=, Version=1.0.0.0", expected: ",=");
            TestParseSimpleName("\\\\, Version=1.0.0.0", expected: "\\");
            TestParseSimpleName("\\/, Version=1.0.0.0", expected: "/");
            TestParseSimpleName("\\\", Version=1.0.0.0", expected: "\"");
            TestParseSimpleName("\\\', Version=1.0.0.0", expected: "\'");
            TestParseSimpleName("a\\tb, Version=1.0.0.0", expected: "a\tb");
            TestParseSimpleName("a\\rb, Version=1.0.0.0", expected: "a\rb");
            TestParseSimpleName("a\\nb, Version=1.0.0.0", expected: "a\nb");
            TestParseSimpleName("a\\vb, Version=1.0.0.0", expected: null);
            TestParseSimpleName("a\\fb, Version=1.0.0.0", expected: null);
            TestParseSimpleName("a , Version=1.0.0.0", expected: "a");
            TestParseSimpleName("a\\a, Version=1.0.0.0", expected: null);
            TestParseSimpleName("a\\ , Version=1.0.0.0", expected: null);
            TestParseSimpleName("a\\ b, Version=1.0.0.0", expected: null);
            TestParseSimpleName("a\\\tb, Version=1.0.0.0", expected: null);
            TestParseSimpleName("a\\\rb, Version=1.0.0.0", expected: null);
            TestParseSimpleName("a\\\nb, Version=1.0.0.0", expected: null);
            TestParseSimpleName("a\\u0;b, Version=1.0.0.0", expected: null, expectedFusion: "a"); // fusion bug
            TestParseSimpleName("a\\u20;b, Version=1.0.0.0", expected: "a b");
            TestParseSimpleName("a\\u020;b, Version=1.0.0.0", expected: "a b");
            TestParseSimpleName("a\\u0020;b, Version=1.0.0.0", expected: "a b");
            TestParseSimpleName("a\\u1234", expected: null);
            TestParseSimpleName("\\u12345;", expected: "\U00012345");
            TestParseSimpleName("\\u100000;", expected: "\U00100000");
            TestParseSimpleName("\\u10fFfF;", expected: "\U0010ffff");
            TestParseSimpleName("\\u110000;", expected: null);
            TestParseSimpleName("\\u1000000;", expected: null);
            TestParseSimpleName("\\taa, Version=1.0.0.0", expected: "\taa");
            TestParseSimpleName("a\\", expected: null);

            // double quotes (unescaped can't be in the middle):
            TestParseSimpleName("\"a\"", expected: "a");
            TestParseSimpleName("\"a'a\", Version=1.0.0.0", expected: "a'a");
            TestParseSimpleName("\\\"aa\\\", Version=1.0.0.0", expected: "\"aa\"");
            TestParseSimpleName("\\\"a'a\\\", Version=1.0.0.0", expected: null);
            TestParseSimpleName("\\\"a, Version=1.0.0.0", expected: "\"a");
            TestParseSimpleName("\", Version=1.0.0.0\", Version=1.0.0.0", expected: ", Version=1.0.0.0");
            TestParseSimpleName("\", Version=1.0.0.0", expected: ", Version=1.0.0.0");
            TestParseSimpleName("\\\", Version=1.0.0.0", expected: "\"");
            TestParseSimpleName("xx\\\"abc\\\"xx", expected: "xx\"abc\"xx");
            TestParseSimpleName("aaa\\\"bbb, Version=1.0.0.0", expected: "aaa\"bbb");
            TestParseSimpleName("\"b\", Version=1.0.0.0", expected: "b");
            TestParseSimpleName("  \"b\"  , Version=1.0.0.0", expected: "b");
            TestParseSimpleName("\"abc', Version=1.0.0.0", expected: "abc', Version=1.0.0.0");
            TestParseSimpleName("'\"a\"', Version=1.0.0.0", expected: "\"a\"");
            TestParseSimpleName("'xxx\"xxx\"xxx', Version=1.0.0.0", expected: "xxx\"xxx\"xxx");
            TestParseSimpleName("'xxx\\\"xxx\\'xxx', Version=1.0.0.0", expected: "xxx\"xxx'xxx");
            TestParseSimpleName("b\", Version=1.0.0.0", expected: null);
            TestParseSimpleName("aaa\"b\"bb, Version=1.0.0.0", expected: null);
            TestParseSimpleName("a\"b, Version=1.0.0.0", expected: null);
            TestParseSimpleName("\"\", Version=1.0.0.0", expected: null);
            TestParseSimpleName("\"\"a\"\", Version=1.0.0.0", expected: null);

            // single quotes  (unescaped can't be in the middle):
            TestParseSimpleName("'a'", expected: "a");
            TestParseSimpleName("'a\"a', Version=1.0.0.0", expected: "a\"a");
            TestParseSimpleName("\\'aa\\', Version=1.0.0.0", expected: "'aa'");
            TestParseSimpleName("\\'a\"a\\', Version=1.0.0.0", expected: null);
            TestParseSimpleName("\\'a,Version=1.0.0.0", expected: "'a");
            TestParseSimpleName("', Version=1.0.0.0', Version=1.0.0.0", expected: ", Version=1.0.0.0");
            TestParseSimpleName("', Version=1.0.0.0", expected: ", Version=1.0.0.0");
            TestParseSimpleName("\\', Version=1.0.0.0", expected: "'");
            TestParseSimpleName("xx\\'abc\\'xx", expected: "xx'abc'xx");
            TestParseSimpleName("aaa\\'bbb, Version=1.0.0.0", expected: "aaa'bbb");
            TestParseSimpleName("'b', Version=1.0.0.0", expected: "b");
            TestParseSimpleName("  'b'  , Version=1.0.0.0", expected: "b");
            TestParseSimpleName("'abc\", Version=1.0.0.0", expected: "abc\", Version=1.0.0.0");
            TestParseSimpleName("\"'a'\", Version=1.0.0.0", expected: "'a'");
            TestParseSimpleName("\"xxx'xxx'xxx\", Version=1.0.0.0", expected: "xxx'xxx'xxx");
            TestParseSimpleName("\"xxx\\\"xxx\\'xxx\", Version=1.0.0.0", expected: "xxx\"xxx'xxx");
            TestParseSimpleName("b', Version=1.0.0.0", expected: null);
            TestParseSimpleName("aaa'b'bb, Version=1.0.0.0", expected: null);
            TestParseSimpleName("a'b, Version=1.0.0.0", expected: null);
            TestParseSimpleName("'', Version=1.0.0.0", expected: null);
            TestParseSimpleName("''a'', Version=1.0.0.0", expected: null);

            // Unicode quotes
            TestParseSimpleName("\u201ca\u201d", expected: "\u201ca\u201d");
            TestParseSimpleName("\\u201c;a\\u201d;", expected: "\u201ca\u201d");
            TestParseSimpleName("\u201ca", expected: "\u201ca");
            TestParseSimpleName("\\u201c;a", expected: "\u201ca");
            TestParseSimpleName("a\u201d", expected: "a\u201d");
            TestParseSimpleName("a\\u201d;", expected: "a\u201d");
            TestParseSimpleName("\u201ca\u201d       ", expected: "\u201ca\u201d");
            TestParseSimpleName("\\u201c;a\\u201d;       ", expected: "\u201ca\u201d");
            TestParseSimpleName("\u2018a\u2019", expected: "\u2018a\u2019");
            TestParseSimpleName("\\u2018;a\\u2019;", expected: "\u2018a\u2019");
            TestParseSimpleName("\u2018a", expected: "\u2018a");
            TestParseSimpleName("\\u2018;a", expected: "\u2018a");
            TestParseSimpleName("a\u2019", expected: "a\u2019");
            TestParseSimpleName("a\\u2019;", expected: "a\u2019");
            TestParseSimpleName("\u2018a\u2019       ", expected: "\u2018a\u2019");
            TestParseSimpleName("\\u2018;a\\u2019;       ", expected: "\u2018a\u2019");

            // NUL characters in the name:
            TestParseSimpleName("  \0  , Version=1.0.0.0", expected: null);
            TestParseSimpleName("zzz, Version=1.0.0\0.0", null);
            TestParseSimpleName("\0", expected: null);

            // can't be whitespace only
            TestParseSimpleName("\t, Version=1.0.0.0", expected: null);
            TestParseSimpleName("\r, Version=1.0.0.0", expected: null);
            TestParseSimpleName("\n, Version=1.0.0.0", expected: null);
            TestParseSimpleName("    , Version=1.0.0.0", expected: null);

            // single or double quote if name starts or ends with whitespace:
            TestParseSimpleName("\"    a    \"", expected: "    a    ");
            TestParseSimpleName("'    a    '", expected: "    a    ");
            TestParseSimpleName("'\r\t\n', Version=1.0.0.0", expected: "\r\t\n");
            TestParseSimpleName("\"\r\t\n\", Version=1.0.0.0", expected: "\r\t\n");
            TestParseSimpleName("x\n\t\nx, Version=1.0.0.0", expected: "x\n\t\nx");

            // Missing parts
            TestParseSimpleName("=", null);
            TestParseSimpleName(",", null);
            TestParseSimpleName("a,", null);
            TestParseSimpleName("a   ,", null);
            TestParseSimpleName("\"a\"=", expected: null);
            TestParseSimpleName("\"a\" =", expected: null);
            TestParseSimpleName("\"a\",", expected: null);
            TestParseSimpleName("\"a\" ,", expected: null);
            TestParseSimpleName("'a'=", expected: null);
            TestParseSimpleName("'a' =", expected: null);
            TestParseSimpleName("'a',", expected: null);
            TestParseSimpleName("'a' ,", expected: null);

            // skips initial and trailing whitespace characters (' ', \t, \r, \n):
            TestParseSimpleName("    \"a\"    ", expected: "a");
            TestParseSimpleName("    'a'    ", expected: "a");
            TestParseSimpleName(" x, Version=1.0.0.0", expected: "x");
            TestParseSimpleName(" x\t\r\n , Version=1.0.0.0", expected: "x");
            TestParseSimpleName("\u0008x, Version=1.0.0.0", expected: "\u0008x");
            TestParseSimpleName("\u0085x, Version=1.0.0.0", expected: "\u0085x");
            TestParseSimpleName("\u00A0x, Version=1.0.0.0", expected: "\u00A0x");
            TestParseSimpleName("\u2000x, Version=1.0.0.0", expected: "\u2000x");
            TestParseSimpleName("x x, Version=1.0.0.0", expected: "x x");
            TestParseSimpleName("   \"a'a\"   , Version=1.0.0.0", expected: "a'a");
            TestParseSimpleName("   \"aa\"  x , Version=1.0.0.0", expected: null);
            TestParseSimpleName("   \"aa\"  \"\" , Version=1.0.0.0", expected: null);
            TestParseSimpleName("   \"aa\"  \'\' , Version=1.0.0.0", expected: null);
            TestParseSimpleName("  A", "A");
            TestParseSimpleName("A  ", "A");
            TestParseSimpleName("  A  ", "A");
            TestParseSimpleName("  A, Version=1.0.0.0", "A");
            TestParseSimpleName("A  , Version=1.0.0.0", "A");
            TestParseSimpleName("A  , Version=1.0.0.0", "A");

            // invalid characters:
            foreach (var c in ClrInvalidCharacters)
            {
                TestParseSimpleName("goo" + c, "goo" + c);
            }

            TestParseSimpleName("'*', Version=1.0.0.0", expected: "*", expectedFusion: null);
            TestParseSimpleName("*, Version=1.0.0.0", expected: "*", expectedFusion: null);
            TestParseSimpleName("hello 'xxx', Version=1.0.0.0", expected: null);
        }

        private void TestQuotingAndEscaping(string simpleName, string expectedSimpleName)
        {
            var ai = new AssemblyIdentity(simpleName);
            var dn = ai.GetDisplayName();
            Assert.Equal(expectedSimpleName + ", Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", dn);

            TestParseSimpleName(dn, simpleName);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void GetDisplayName_QuotingAndEscaping()
        {
            TestQuotingAndEscaping(",", "\\,");
            TestQuotingAndEscaping("'", "\\'");
            TestQuotingAndEscaping("\"", "\\\"");
            TestQuotingAndEscaping("/", "/");
            TestQuotingAndEscaping("\\", "\\\\");
            TestQuotingAndEscaping("x\rx", "x\\rx");
            TestQuotingAndEscaping("x\nx", "x\\nx");
            TestQuotingAndEscaping("x\tx", "x\\tx");
            TestQuotingAndEscaping("\rx", "\"\\rx\"");
            TestQuotingAndEscaping("\nx", "\"\\nx\"");
            TestQuotingAndEscaping("\tx", "\"\\tx\"");
            TestQuotingAndEscaping(" ", "\" \"");
            TestQuotingAndEscaping(" x", "\" x\"");
            TestQuotingAndEscaping("x ", "\"x \"");
            TestQuotingAndEscaping("\u0008", "\u0008");
            TestQuotingAndEscaping("\u0085", "\u0085");
            TestQuotingAndEscaping("\u00A0", "\u00A0");
            TestQuotingAndEscaping("\u2000", "\u2000");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void TryParseDisplayName()
        {
            string V = "\\u" + ((int)'V').ToString("X4") + ";";

            TestParseDisplayName("  \"fo'o\"  , " + V + "ersion=1.0.0.0\t, \rCulture=zz-ZZ\n, PublicKeyToken=" + StrPublicKeyToken1,
                new AssemblyIdentity("fo'o", new Version(1, 0, 0, 0), "zz-ZZ", RoPublicKeyToken1, hasPublicKey: false, isRetargetable: false, contentType: AssemblyContentType.Default),
                NVCT);

            // invalid:
            AssemblyIdentity id;
            Assert.Throws<ArgumentNullException>(() => AssemblyIdentity.TryParseDisplayName(null, out id));

            TestParseDisplayName("", null);
            TestParseDisplayName("fo=o, Culture=neutral, Version=1.0.0.0", null);
            TestParseDisplayName("goo, Culture=neutral, Version,1.0.0.0", null);

            // custom properties:
            TestParseDisplayName("goo, A=B",
                new AssemblyIdentity("goo"), N | AssemblyIdentityParts.Unknown);

            // we don't allow CT=WinRT + Retargetable, fusion does.
            Assert.False(
                AssemblyIdentity.TryParseDisplayName("goo, Version=1.0.0.0, Culture=en-US, Retargetable=Yes, ContentType=WindowsRuntime, PublicKeyToken=" + StrPublicKeyToken1, out id));

            // order
            TestParseDisplayName("goo, Culture=neutral, Version=1.0.0.0",
                new AssemblyIdentity("goo", new Version(1, 0, 0, 0)), NVC);

            TestParseDisplayName("goo, Version=1.0.0.0, Culture=en-US, Retargetable=Yes, PublicKeyToken=" + StrPublicKeyToken1,
                new AssemblyIdentity("goo", new Version(1, 0, 0, 0), "en-US", RoPublicKeyToken1, hasPublicKey: false, isRetargetable: true),
                NVCT | AssemblyIdentityParts.Retargetability);

            TestParseDisplayName("goo, PublicKey=" + StrPublicKey1 + ", Version=1.0.0.1",
                new AssemblyIdentity("goo", new Version(1, 0, 0, 1), publicKeyOrToken: RoPublicKey1, hasPublicKey: true),
                NVK);

            TestParseDisplayName(@"Goo, Version=0.0.0.0, Culture=""  \'\t\r\n\\\=\,  "", PublicKeyToken=null",
                new AssemblyIdentity("Goo", cultureName: "  '\t\r\n\\=,  "),
                NVCT);

            // duplicates
            TestParseDisplayName("goo, Version=1.0.0.0, Version=1.0.0.0", null);
            TestParseDisplayName("goo, Version=1.0.0.0, Version=2.0.0.0", null);
            TestParseDisplayName("goo, Culture=neutral, Version=1.0.0.0, Culture=en-US", null);
        }

        [Fact]
        [WorkItem(39647, "https://github.com/dotnet/roslyn/issues/39647")]
        public void AssemblyIdentity_EmptyName()
        {
            var identity = new AssemblyIdentity(noThrow: true, name: "");
            var name = identity.GetDisplayName();
            Assert.Equal(", Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", name);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void TryParseDisplayName_Version()
        {
            TestParseDisplayName("Version=1.2.3.4, goo", null);
            TestParseDisplayName("Version=1.2.3.4", null);

            TestParseDisplayName("Version=", null);
            TestParseDisplayName("goo, Version=", null);
            TestParseDisplayName("goo, Version", null);

            TestParseDisplayName("goo, Version=1",
                new AssemblyIdentity("goo", new Version(1, 0, 0, 0)), N | AssemblyIdentityParts.VersionMajor);

            TestParseDisplayName("goo, Version=.", new AssemblyIdentity("goo"), N);
            TestParseDisplayName("goo, Version=..", new AssemblyIdentity("goo"), N);
            TestParseDisplayName("goo, Version=...", new AssemblyIdentity("goo"), N);

            TestParseDisplayName("goo, Version=*, Culture=en-US",
                new AssemblyIdentity("goo", cultureName: "en-US"),
                AssemblyIdentityParts.Name | AssemblyIdentityParts.Culture);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void TestParseVersion_Parts()
        {
            TestParseVersionInvalid("a");
            TestParseVersionInvalid("-1");
            TestParseVersionInvalid("2*");
            TestParseVersionInvalid("2.1*");
            TestParseVersionInvalid("2.0*.0.0");
            TestParseVersionInvalid("2.*0.0.0");

            TestParseVersion("1", 1, 0, 0, 0, AssemblyIdentityParts.VersionMajor);
            TestParseVersion("65535", 65535, 0, 0, 0, AssemblyIdentityParts.VersionMajor);
            TestParseVersionInvalid("65536");

            TestParseVersion(".", 0, 0, 0, 0, 0);
            TestParseVersion("1.", 1, 0, 0, 0, AssemblyIdentityParts.VersionMajor);
            TestParseVersion("0.1", 0, 1, 0, 0, AssemblyIdentityParts.VersionMajor | AssemblyIdentityParts.VersionMinor);
            TestParseVersion("1.2", 1, 2, 0, 0, AssemblyIdentityParts.VersionMajor | AssemblyIdentityParts.VersionMinor);
            TestParseVersionInvalid("1. 2");
            TestParseVersionInvalid("1 . 2");

            TestParseVersion("1..", 1, 0, 0, 0, AssemblyIdentityParts.VersionMajor);
            TestParseVersion("1.2.", 1, 2, 0, 0, AssemblyIdentityParts.VersionMajor | AssemblyIdentityParts.VersionMinor);
            TestParseVersion("1.2.3", 1, 2, 3, 0, AssemblyIdentityParts.VersionMajor | AssemblyIdentityParts.VersionMinor | AssemblyIdentityParts.VersionBuild);
            TestParseVersion(".2.3", 0, 2, 3, 0, AssemblyIdentityParts.VersionMinor | AssemblyIdentityParts.VersionBuild);
            TestParseVersion("1..3", 1, 0, 3, 0, AssemblyIdentityParts.VersionMajor | AssemblyIdentityParts.VersionBuild);

            TestParseVersion("1.2.3.", 1, 2, 3, 0, AssemblyIdentityParts.VersionMajor | AssemblyIdentityParts.VersionMinor | AssemblyIdentityParts.VersionBuild);
            TestParseVersion("1.2..4", 1, 2, 0, 4, AssemblyIdentityParts.VersionMajor | AssemblyIdentityParts.VersionMinor | AssemblyIdentityParts.VersionRevision);
            TestParseVersion("1.2..", 1, 2, 0, 0, AssemblyIdentityParts.VersionMajor | AssemblyIdentityParts.VersionMinor);
            TestParseVersion("1.2.3.4", 1, 2, 3, 4, AssemblyIdentityParts.VersionMajor | AssemblyIdentityParts.VersionMinor | AssemblyIdentityParts.VersionBuild | AssemblyIdentityParts.VersionRevision);
            TestParseVersionInvalid("1. 2.3.4");
            TestParseVersionInvalid("1.2.3. 4");
            TestParseVersion("65535.65535.65535.65535", 65535, 65535, 65535, 65535,
                AssemblyIdentityParts.VersionMajor | AssemblyIdentityParts.VersionMinor | AssemblyIdentityParts.VersionBuild | AssemblyIdentityParts.VersionRevision);
            TestParseVersionInvalid("65535.65535.65535.65536");

            TestParseVersion("*", 0, 0, 0, 0, AssemblyIdentityParts.VersionMajor);
            TestParseVersion("1.*", 1, 0, 0, 0, AssemblyIdentityParts.VersionMajor | AssemblyIdentityParts.VersionMinor);
            TestParseVersion("1.2.*", 1, 2, 0, 0, AssemblyIdentityParts.VersionMajor | AssemblyIdentityParts.VersionMinor | AssemblyIdentityParts.VersionBuild);
            TestParseVersion("1.2.3.*", 1, 2, 3, 0, AssemblyIdentityParts.Version);
            TestParseVersion("1.*.2.*", 1, 0, 2, 0, AssemblyIdentityParts.Version);

            TestParseVersionInvalid("1.2.3.4.");
            TestParseVersionInvalid("1.2.3.4.5");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void TestParseVersionAll()
        {
            // all combinations:
            var possibleParts = new[] { "0", "3", ".", "", "*", null };
            foreach (var part1 in possibleParts)
            {
                foreach (var part2 in possibleParts)
                {
                    foreach (var part3 in possibleParts)
                    {
                        foreach (var part4 in possibleParts)
                        {
                            TestParseVersion(
                                (part1 ?? "") +
                                (part2 != null ? "." + part2 : "") +
                                (part3 != null ? "." + part3 : "") +
                                (part4 != null ? "." + part4 : ""));
                        }
                    }
                }
            }
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void TryParseDisplayName_Culture()
        {
            TestParseDisplayName("goo, Version=1.0.0.1, Culture=null",
                new AssemblyIdentity("goo", new Version(1, 0, 0, 1), cultureName: "null"), NVC);

            TestParseDisplayName("goo, Version=1.0.0.1, cULture=en-US",
                new AssemblyIdentity("goo", new Version(1, 0, 0, 1), cultureName: "en-US"), NVC);

            TestParseDisplayName("goo, Version=1.0.0.1, Language=en-US",
                new AssemblyIdentity("goo", new Version(1, 0, 0, 1), cultureName: "en-US"), NVC);

            TestParseDisplayName("goo, Version=1.0.0.1, languagE=en-US",
                new AssemblyIdentity("goo", new Version(1, 0, 0, 1), cultureName: "en-US"), NVC);

            TestParseDisplayName("goo, Culture=*, Version=1.0.0.1",
                new AssemblyIdentity("goo", new Version(1, 0, 0, 1)), NV);

            TestParseDisplayName("goo, Culture=*", new AssemblyIdentity("goo"), N);

            TestParseDisplayName("goo, Culture=*, Culture=en-US, Version=1.0.0.1", null);

            TestParseDisplayName("Goo, Version=1.0.0.0, Culture='neutral', PublicKeyToken=null",
                new AssemblyIdentity("Goo", new Version(1, 0, 0, 0), cultureName: null), NVCT);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void TryParseDisplayName_Keys()
        {
            // empty keys:
            TestParseDisplayName("goo, PublicKeyToken=null, Version=1.0.0.1",
                new AssemblyIdentity("goo", new Version(1, 0, 0, 1)), NVT);

            TestParseDisplayName("goo, PublicKeyToken=neutral, Version=1.0.0.1",
                new AssemblyIdentity("goo", new Version(1, 0, 0, 1)), NVT);

            TestParseDisplayName("goo, PublicKeyToken=*, Version=1.0.0.1",
                new AssemblyIdentity("goo", new Version(1, 0, 0, 1)), NV);

            TestParseDisplayName("goo, PublicKey=null, Version=1.0.0.1", null);
            TestParseDisplayName("goo, PublicKey=neutral, Version=1.0.0.1", null);

            TestParseDisplayName("goo, PublicKey=*, Version=1.0.0.1",
                new AssemblyIdentity("goo", new Version(1, 0, 0, 1)), NV);

            // keys
            TestParseDisplayName("goo, PublicKeyToken=, Version=1.0.0.1", null);
            TestParseDisplayName("goo, PublicKeyToken=1, Version=1.0.0.1", null);
            TestParseDisplayName("goo, PublicKeyToken=111111111111111, Version=1.0.0.1", null);
            TestParseDisplayName("goo, PublicKeyToken=1111111111111111111, Version=1.0.0.1", null);
            TestParseDisplayName("goo, PublicKey=1, Version=1.0.0.1", null);
            TestParseDisplayName("goo, PublicKey=1000000040000000", null);
            TestParseDisplayName("goo, PublicKey=11, Version=1.0.0.1", null);

            // TODO: need to calculate the correct token for the ECMA key.
            // TestParseDisplayName("goo, PublicKey=0000000040000000",
            //    expectedParts: 0,
            //    expectedFusion: null, // Fusion rejects the ECMA key.
            //    expected: new AssemblyIdentity("goo", hasPublicKey: true, publicKeyOrToken: new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0 }.AsImmutable()));

            // if public key token calculated from public key matches, then it's ok to specify both
            TestParseDisplayName("goo, Culture=neutral, Version=1.0.0.0, PublicKey=" + StrPublicKey1 + ", PublicKeyToken=" + StrPublicKeyToken1,
                new AssemblyIdentity("goo", new Version(1, 0, 0, 0), publicKeyOrToken: RoPublicKey1, hasPublicKey: true), NVC | AssemblyIdentityParts.PublicKeyOrToken);

            TestParseDisplayName("goo, Culture=neutral, Version=1.0.0.0, PublicKey=" + StrPublicKey1 + ", PublicKeyToken=1111111111111111", null);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void TryParseDisplayName_ContentType()
        {
            TestParseDisplayName("goo, Version=1.0.0.1, ContentType=WindowsRuntime",
                new AssemblyIdentity("goo", new Version(1, 0, 0, 1), contentType: AssemblyContentType.WindowsRuntime), NV | AssemblyIdentityParts.ContentType);

            TestParseDisplayName("goo, Version=1.0.0.1, ContentType=*",
                new AssemblyIdentity("goo", new Version(1, 0, 0, 1)), NV);

            TestParseDisplayName("goo, Version=1.0.0.1, ContentType=Default", null);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void TryParseDisplayName_Retargetable()
        {
            // for some reason the Fusion rejects to parse Retargetable if they are not full names

            TestParseDisplayName("goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=" + StrPublicKeyToken1 + ", Retargetable=yEs",
                new AssemblyIdentity("goo", new Version(1, 0, 0, 0), publicKeyOrToken: RoPublicKeyToken1, isRetargetable: true),
                NVCT | AssemblyIdentityParts.Retargetability);

            TestParseDisplayName("goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=" + StrPublicKeyToken1 + ", Retargetable=*",
                new AssemblyIdentity("goo", new Version(1, 0, 0, 0), publicKeyOrToken: RoPublicKeyToken1),
                NVCT);

            TestParseDisplayName("goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=" + StrPublicKeyToken1 + ", retargetable=NO",
                new AssemblyIdentity("goo", new Version(1, 0, 0, 0), publicKeyOrToken: RoPublicKeyToken1),
                NVCT | AssemblyIdentityParts.Retargetability);

            TestParseDisplayName("goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=" + StrPublicKeyToken1 + ", Retargetable=Bar",
                null);

            TestParseDisplayName("goo, Version=1.0.0.1, Retargetable=*",
                new AssemblyIdentity("goo", new Version(1, 0, 0, 1)), NV);

            TestParseDisplayName("goo, Version=1.0.0.1, Retargetable=No",
                new AssemblyIdentity("goo", new Version(1, 0, 0, 1)), NV | AssemblyIdentityParts.Retargetability,
                expectedFusion: null);

            TestParseDisplayName("goo, Version=1.0.0.1, retargetable=YEs",
                new AssemblyIdentity("goo", new Version(1, 0, 0, 1), isRetargetable: true), NV | AssemblyIdentityParts.Retargetability,
                expectedFusion: null);
        }
    }
}

