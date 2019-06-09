// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class AssemblyIdentityComparerTests : AssemblyIdentityTestBase
    {
        private void TestMatch(
            string displayName1,
            string displayName2,
            AssemblyIdentityComparer.ComparisonResult match,
            AssemblyIdentityComparer.ComparisonResult? fusionMatch = null,
            bool ignoreVersion = false,
            bool partial = false,
            bool unificationApplied = false,
            bool? fusionUnificationApplied = null,
            string policyPath = null)
        {
            if (fusionMatch == null)
            {
                fusionMatch = match;
            }

            using (var fusionPolicy = policyPath != null ? FusionAssemblyPortabilityPolicy.LoadFromFile(policyPath) : null)
            {
                var comparer = DesktopAssemblyIdentityComparer.Default;

                var policy = default(AssemblyPortabilityPolicy);
                if (policyPath != null)
                {
                    using (var policyStream = new FileStream(policyPath, FileMode.Open, FileAccess.Read))
                    {
                        policy = AssemblyPortabilityPolicy.LoadFromXml(policyStream);
                        comparer = new DesktopAssemblyIdentityComparer(policy);
                    }
                }

                bool fusionUnificationApplied1;
                var fusionResult1 = FusionAssemblyIdentityComparer.CompareAssemblyIdentity(displayName1, displayName2, ignoreVersion, policy: fusionPolicy, unificationApplied: out fusionUnificationApplied1);
                Assert.Equal(fusionMatch, fusionResult1);
                Assert.Equal(fusionUnificationApplied ?? unificationApplied, fusionUnificationApplied1);

                AssemblyIdentity id1, id2;
                AssemblyIdentityParts parts1, parts2;

                Assert.True(AssemblyIdentity.TryParseDisplayName(displayName1, out id1, out parts1));
                Assert.Equal(partial, !AssemblyIdentity.IsFullName(parts1));

                Assert.True(AssemblyIdentity.TryParseDisplayName(displayName2, out id2, out parts2));
                Assert.True(AssemblyIdentity.IsFullName(parts2), "Expected full name");

                bool unificationApplied1;
                var actual1 = comparer.Compare(null, displayName1, id2, out unificationApplied1, ignoreVersion);
                Assert.Equal(match, actual1);
                Assert.Equal(unificationApplied, unificationApplied1);

                if (!partial && id1 != null)
                {
                    bool unificationApplied2;
                    var actual2 = comparer.Compare(id1, null, id2, out unificationApplied2, ignoreVersion);
                    Assert.Equal(match, actual2);
                    Assert.Equal(unificationApplied, unificationApplied2);
                }
            }
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void Mscorlib()
        {
            // mscorlib is special - all identities with simple name "mscorlib" are considered equivalent
            TestMatch(
                "mscorlib, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                "mscorlib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=FEFEFEFEFEFEFEFE",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "mscorlib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                "mscorlib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=FEFEFEFEFEFEFEFE",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: false);

            TestMatch(
                "mscorlib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                "mscorlib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=FEFEFEFEFEFEFEFE",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                fusionMatch: AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: false);

            TestMatch(
                "mscorlib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                "mscorlib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=FEFEFEFEFEFEFEFE, ContentType=WindowsRuntime",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                fusionMatch: AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: false);

            TestMatch(
                "mscorlib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                "mscorlib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=FEFEFEFEFEFEFEFE, ContentType=WindowsRuntime",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                fusionMatch: AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: false);

            TestMatch(
                "mscorlib, Version=1.0.0.0, PublicKeyToken=0123456789ABCDEF",
                "mscorlib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=FEFEFEFEFEFEFEFE, ContentType=WindowsRuntime",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                fusionMatch: AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "mscorlib, Version=1.0.0.0, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                "mscorlib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=FEFEFEFEFEFEFEFE",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                fusionMatch: AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "mscorlib, ContentType=WindowsRuntime",
                "mscorlib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=FEFEFEFEFEFEFEFE, ContentType=WindowsRuntime",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void SimpleName()
        {
            TestMatch(
                "Goo",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo2",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: true);

            TestMatch(
                "Goo1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                "Goo2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: false);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void Version_StrongDefinition()
        {
            TestMatch(
                "Goo, Version=1.0",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0.0",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0",
                "Goo, Version=1.0.65535.65535, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0.0",
                "Goo, Version=1.0.0.65535, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0.0.65535",
                "Goo, Version=1.0.0.65535, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0.65535.65535",
                "Goo, Version=1.0.65535.65535, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, PublicKeyToken=0123456789ABCDEF",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0.0.0, PublicKeyToken=0123456789ABCDEF",
                "Goo, Version=1.0.0.1, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0.0.0, PublicKeyToken=0123456789ABCDEF",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: false);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void Version_WeakDefinition()
        {
            // if the reference is partial version is ignored

            TestMatch(
                "Goo, Version=1.0",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0",
                "Goo, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0.0",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0.0.",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0..0",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0..0",
                "Goo, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0",
                "Goo, Version=1.0.65535.65535, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0.0",
                "Goo, Version=1.0.0.65535, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0.0.65535",
                "Goo, Version=1.0.0.65535, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0.65535.65535",
                "Goo, Version=1.0.65535.65535, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, PublicKeyToken=null",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0.0.0, PublicKeyToken=null",
                "Goo, Version=1.0.0.1, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0.0.0, PublicKeyToken=null",
                "Goo, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0, Culture=neutral, PublicKeyToken=null",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0.0, Culture=neutral, PublicKeyToken=null",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                "Goo, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: false);

            TestMatch(
                "Goo, Version=., Culture=neutral, PublicKeyToken=null",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void Culture_StrongDefinition()
        {
            TestMatch(
                "Goo, Culture=en-US, PublicKeyToken=0123456789ABCDEF",
                "Goo, Version=1.0.0.0, Culture=en-US, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Culture=en-US, PublicKeyToken=0123456789ABCDEF",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: true);

            TestMatch(
                "Goo, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                "Goo, Version=1.0.0.0, Culture=en-US, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: true);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void Culture_WeakDefinition()
        {
            TestMatch(
                "Goo, Version=1.0.0.0, Culture=en-US, PublicKeyToken=null",
                "Goo, Version=1.0.0.0, Culture=en-US, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: false);

            TestMatch(
                "Goo, Version=1.0.0.0, Culture=en-US, PublicKeyToken=null",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: false);

            TestMatch(
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                "Goo, Version=1.0.0.0, Culture=en-US, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: false);

            TestMatch(
                "Goo, Culture=en-US, PublicKeyToken=null",
                "Goo, Version=1.0.0.0, Culture=en-US, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Culture=en-US, PublicKeyToken=null",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: true);

            TestMatch(
                "Goo, Culture=neutral, PublicKeyToken=null",
                "Goo, Version=1.0.0.0, Culture=en-US, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: true);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void PublicKeyToken()
        {
            TestMatch(
                "Goo",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, PublicKeyToken=0123456789ABCDEF",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: true);

            TestMatch(
                "Goo, PublicKeyToken=null",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: true);

            TestMatch(
                "Goo, PublicKeyToken=1111111111111111",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=2222222222222222",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: true);

            TestMatch(
                "Goo, PublicKeyToken=null",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: false);

            TestMatch(
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: false);

            TestMatch(
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=1111111111111111",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=2222222222222222",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: false);

            TestMatch(
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: false);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void IgnoreOrFwUnifyVersion()
        {
            TestMatch(
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                "Goo, Version=2.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.EquivalentIgnoringVersion,
                ignoreVersion: true);

            TestMatch(
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                "Goo, Version=2.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                AssemblyIdentityComparer.ComparisonResult.EquivalentIgnoringVersion,
                ignoreVersion: true);

            TestMatch(
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                "Goo, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                ignoreVersion: true);

            TestMatch(
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime",
                "Goo, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                ignoreVersion: true);

            TestMatch(
                "System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                unificationApplied: true);

            // Fx assemblies aren't WinRT
            TestMatch(
                "System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089, ContentType=WindowsRuntime",
                "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089, ContentType=WindowsRuntime",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                fusionMatch: AssemblyIdentityComparer.ComparisonResult.Equivalent,
                fusionUnificationApplied: true);

            TestMatch(
                "System.Net, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Net, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                unificationApplied: true);

            TestMatch(
                "System.Net, Version=2.0.0.0, Culture=en-US, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Net, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent);

            TestMatch(
                "System.Net, Version=2.0.0.0, Culture=en-US, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Net, Version=4.0.0.0, Culture=en-US, PublicKeyToken=b03f5f7f11d50a3a",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                unificationApplied: true);

            // build and revision numbers are ignored
            TestMatch(
                "System.Net, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Net, Version=4.0.0.1, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                unificationApplied: true);

            // build and revision numbers are ignored
            TestMatch(
                "System.Net, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Net, Version=4.0.2.1, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                unificationApplied: true);

            TestMatch(
                "System.Net, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Net, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent);

            // lesser version than FW version (4.0)
            TestMatch(
                "System.Net, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Net, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                unificationApplied: true);

            TestMatch(
                "System.Runtime.Handles, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Runtime.Handles, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                unificationApplied: true);

            TestMatch(
                "System.Numerics.Vectors, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Numerics.Vectors, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                unificationApplied: true);

            // greater version than FW version (4.0)
            TestMatch(
                "System.Net, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Net, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent);

            TestMatch(
                "System.Runtime.Handles, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Runtime.Handles, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent);

            TestMatch(
                "System.Numerics.Vectors, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Numerics.Vectors, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent);

            // works correctly for names with CLR invalid characters:
            foreach (var c in AssemblyIdentityTests.ClrInvalidCharacters)
            {
                string name = "x" + c + "x";

                TestMatch(
                    name + ", Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                    name + ", Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                    AssemblyIdentityComparer.ComparisonResult.Equivalent,
                    partial: false);
            }
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void AsymmetricUnification()
        {
            // Note:
            // System.Numerics.Vectors, Version=4.0 is an FX assembly
            // System.Numerics.Vectors, Version=4.1+ is not an FX assembly
            //
            // It seems like a bug in fusion: it only determines whether the definition is an FX assembly 
            // and calculates the result based upon that, regardless of whether the reference is an FX assembly or not.
            // We do replicate that behavior.
            TestMatch(
                "System.Numerics.Vectors, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Numerics.Vectors, Version=4.1.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent);

            TestMatch(
                "System.Numerics.Vectors, Version=4.1.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Numerics.Vectors, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                unificationApplied: true);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void Portability()
        {
            TestMatch(
                "System, Version=5.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
                "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: false);

            // WinRT should not be ported
            TestMatch(
                "System, Version=5.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e, ContentType=WindowsRuntime",
                "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089, ContentType=WindowsRuntime",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                fusionMatch: AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: false);

            var appConfig = Temp.CreateFile().WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
       <supportPortability PKT=""7cec85d7bea7798e"" enable=""false""/>
    </assemblyBinding>
  </runtime>
</configuration>
");
            // Checks all types of equivalence
            TestMatch(
                "System, Version=5.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
                "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: false,
                policyPath: appConfig.Path);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void Retargetable_Reference()
        {
            TestMatch(
                "System.Windows.Forms.DataGrid, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=969db8053d3322ac",
                "System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent);

            TestMatch(
                "System.Windows.Forms.DataGrid, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=969db8053d3322ac, Retargetable=Yes",
                "System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                AssemblyIdentityComparer.ComparisonResult.Equivalent);

            TestMatch(
                "System.Windows.Forms.DataGrid, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=null, Retargetable=Yes",
                "System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void Retargetable_Reference_Partial()
        {
            TestMatch(
                "System.Windows.Forms.DataGrid, Version=1.0.5000.0, PublicKeyToken=969db8053d3322ac, Retargetable=Yes",
                "System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: true);

            TestMatch(
                "System.Windows.Forms.DataGrid, Culture=neutral, PublicKeyToken=969db8053d3322ac, Retargetable=Yes",
                "System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: true);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void Retargetable_Reference_Portable()
        {
            TestMatch(
                "System.ComponentModel.DataAnnotations, Version=2.0.5.0, Culture=neutral, PublicKeyToken=ddd0da4d3e678217, Retargetable=Yes",
                "System.ComponentModel.DataAnnotations, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
                AssemblyIdentityComparer.ComparisonResult.Equivalent);

            // the reference is first retargeted to (4.0.0.0, 31bf3856ad364e35) and then unified with V2.0.5.0.
            TestMatch(
                "System.ComponentModel.DataAnnotations, Version=2.0.5.0, Culture=neutral, PublicKeyToken=ddd0da4d3e678217, Retargetable=Yes",
                "System.ComponentModel.DataAnnotations, Version=2.0.5.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                unificationApplied: true);

            TestMatch(
                "System.ComponentModel.DataAnnotations, Version=2.0.5.0, Culture=neutral, PublicKeyToken=ddd0da4d3e678217, Retargetable=Yes",
                "System.ComponentModel.DataAnnotations, Version=2.0.5.0, Culture=neutral, PublicKeyToken=ddd0da4d3e678217",
                AssemblyIdentityComparer.ComparisonResult.Equivalent);

            TestMatch(
                "System.ComponentModel.DataAnnotations, Version=2.0.5.0, Culture=neutral, PublicKeyToken=ddd0da4d3e678217, Retargetable=Yes",
                "System.ComponentModel.DataAnnotations, Version=2.0.5.0, Culture=neutral, PublicKeyToken=ddd0da4d3e678217",
                AssemblyIdentityComparer.ComparisonResult.Equivalent);

            TestMatch(
                "System.ComponentModel.DataAnnotations, Version=2.0.5.0, PublicKeyToken=ddd0da4d3e678217, Retargetable=Yes",
                "System.ComponentModel.DataAnnotations, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: true);

            TestMatch(
                "System.ComponentModel.DataAnnotations, Version=2.0.5.0, PublicKeyToken=ddd0da4d3e678217, Retargetable=Yes",
                "System.ComponentModel.DataAnnotations, Version=4.0.0.0, Culture=neutral, PublicKeyToken=ddd0da4d3e678217",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: true);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void Retargetable_RefAndDef()
        {
            TestMatch(
                "System.Windows.Forms.DataGrid, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=969db8053d3322ac, Retargetable=Yes",
                "System.Windows.Forms.DataGrid, Version=1.0.5500.0, Culture=neutral, PublicKeyToken=969db8053d3322ac, Retargetable=Yes",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                unificationApplied: true);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsFusion)]
        public void WinRT_Basic()
        {
            TestMatch(
                "Goo, ContentType=WindowsRuntime",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, ContentType=WindowsRuntime",
                "Goo2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: true);

            TestMatch(
                "Goo, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0.0.0, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0.0.0, PublicKeyToken=1123456789ABCDEF, ContentType=WindowsRuntime",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=2123456789ABCDEF, ContentType=WindowsRuntime",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: false);

            TestMatch(
                "Goo, Version=1.0.0.0, Culture=en-US, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: false);

            TestMatch(
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                "Goo, Version=2.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                partial: false);

            // comparing WinRT with Default or vice versa:
            TestMatch(
                "Goo",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                fusionMatch: AssemblyIdentityComparer.ComparisonResult.Equivalent,
                partial: true);

            TestMatch(
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                "Goo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                fusionMatch: AssemblyIdentityComparer.ComparisonResult.Equivalent);

            TestMatch(
                "System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089, ContentType=WindowsRuntime",
                "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089, ContentType=WindowsRuntime",
                AssemblyIdentityComparer.ComparisonResult.EquivalentIgnoringVersion,
                fusionMatch: AssemblyIdentityComparer.ComparisonResult.Equivalent,
                fusionUnificationApplied: true,
                ignoreVersion: true);

            TestMatch(
                "Windows, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                "Windows, Version=2.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent);

            TestMatch(
                "Windows, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                "Windows, Version=2.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.EquivalentIgnoringVersion,
                ignoreVersion: true);

            TestMatch(
                "Windows, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                "Windows, Version=2.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent);

            TestMatch(
                "Windows, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                "Windows, Version=2.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                AssemblyIdentityComparer.ComparisonResult.EquivalentIgnoringVersion,
                ignoreVersion: true);

            TestMatch(
                "Windows.Foundation, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                "Windows.Foundation, Version=2.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                fusionMatch: AssemblyIdentityComparer.ComparisonResult.Equivalent);

            TestMatch(
                "Windows.Foundation, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                "Windows.Foundation, Version=2.0.0.0, Culture=neutral, PublicKeyToken=0123456789ABCDEF, ContentType=WindowsRuntime",
                AssemblyIdentityComparer.ComparisonResult.NotEquivalent,
                fusionMatch: AssemblyIdentityComparer.ComparisonResult.Equivalent);
        }
    }
}

