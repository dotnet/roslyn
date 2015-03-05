// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;

namespace System.Runtime.Analyzers.UnitTests
{
    public class MarkAllAssembliesWithComVisibleTests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new MarkAllAssembliesWithComVisibleAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new MarkAllAssembliesWithComVisibleAnalyzer();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void NoTypesComVisibleMissing()
        {
            VerifyCSharp("");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void NoTypesComVisibleTrue()
        {
            VerifyCSharp(@"
using System.Runtime.InteropServices;

[assembly: ComVisible(true)]");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void NoTypesComVisibleFalse()
        {
            VerifyCSharp(@"
using System.Runtime.InteropServices;

[assembly: ComVisible(false)]");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void PublicTypeComVisibleMissing()
        {
            VerifyCSharp(@"
public class C
{
}",
                GetAddComVisibleFalseResult());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void PublicTypeComVisibleTrue()
        {
            VerifyCSharp(@"
using System.Runtime.InteropServices;

[assembly: ComVisible(true)]

public class C
{
}",
                GetExposeIndividualTypesResult());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void PublicTypeComVisibleFalse()
        {
            VerifyCSharp(@"
using System.Runtime.InteropServices;

[assembly: ComVisible(false)]

public class C
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void InternalTypeComVisibleMissing()
        {
            VerifyCSharp(@"
internal class C
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void InternalTypeComVisibleTrue()
        {
            VerifyCSharp(@"
using System.Runtime.InteropServices;

[assembly: ComVisible(true)]

internal class C
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void InternalTypeComVisibleFalse()
        {
            VerifyCSharp(@"
using System.Runtime.InteropServices;

[assembly: ComVisible(false)]

internal class C
{
}");
        }

        private static DiagnosticResult GetExposeIndividualTypesResult()
        {
            return GetGlobalResult(MarkAllAssembliesWithComVisibleAnalyzer.RuleId, string.Format(SystemRuntimeAnalyzersResources.CA1017_AttributeTrue, "TestProject"));
        }

        private static DiagnosticResult GetAddComVisibleFalseResult()
        {
            return GetGlobalResult(MarkAllAssembliesWithComVisibleAnalyzer.RuleId, string.Format(SystemRuntimeAnalyzersResources.CA1017_NoAttribute, "TestProject"));
        }
    }
}
