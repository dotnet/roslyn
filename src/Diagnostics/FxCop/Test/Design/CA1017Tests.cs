// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Design;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class CA1017Tests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new CA1017DiagnosticAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CA1017DiagnosticAnalyzer();
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
            return GetGlobalResult(CA1017DiagnosticAnalyzer.RuleId, string.Format(FxCopRulesResources.CA1017_AttributeTrue, "TestProject"));
        }

        private static DiagnosticResult GetAddComVisibleFalseResult()
        {
            return GetGlobalResult(CA1017DiagnosticAnalyzer.RuleId, string.Format(FxCopRulesResources.CA1017_NoAttribute, "TestProject"));
        }
    }
}
