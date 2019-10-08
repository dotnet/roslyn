// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.DiagnosticAnalyzerAttributeAnalyzer,
    Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.Fixers.CSharpApplyDiagnosticAnalyzerAttributeFix>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.DiagnosticAnalyzerAttributeAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers.CodeFixes.BasicApplyDiagnosticAnalyzerAttributeFix>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class MissingDiagnosticAnalyzerAttributeRuleTests
    {
        [Fact]
        public async Task CSharp_VerifyDiagnosticAndFixes()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

class MyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public override void Initialize(AnalysisContext context)
    {
    }
}";
            DiagnosticResult expected = VerifyCS.Diagnostic(DiagnosticAnalyzerAttributeAnalyzer.MissingDiagnosticAnalyzerAttributeRule).WithLocation(7, 7).WithArguments(DiagnosticAnalyzerCorrectnessAnalyzer.DiagnosticAnalyzerAttributeFullName);
            await VerifyCS.VerifyAnalyzerAsync(source, expected);

            var fixedCode_WithCSharpAttribute = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
class MyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public override void Initialize(AnalysisContext context)
    {
    }
}";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    ExpectedDiagnostics = { expected },
                },
                FixedState = { Sources = { fixedCode_WithCSharpAttribute } },
                CodeFixIndex = 0,
                CodeFixEquivalenceKey = "Apply DiagnosticAnalyzer attribute for 'C#'.",
            }.RunAsync();

            var fixedCode_WithCSharpAndVBAttributes = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public override void Initialize(AnalysisContext context)
    {
    }
}";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    ExpectedDiagnostics = { expected },
                },
                FixedState = { Sources = { fixedCode_WithCSharpAndVBAttributes } },
                CodeFixIndex = 2,
                CodeFixEquivalenceKey = "Apply DiagnosticAnalyzer attribute for both 'C#' and 'Visual Basic'.",
            }.RunAsync();
        }

        [Fact]
        public async Task VisualBasic_VerifyDiagnosticAndFixes()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

Class MyAnalyzer
	Inherits DiagnosticAnalyzer
	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
		Get
			Throw New NotImplementedException()
		End Get
	End Property

	Public Overrides Sub Initialize(context As AnalysisContext)
	End Sub
End Class
";
            DiagnosticResult expected = VerifyVB.Diagnostic(DiagnosticAnalyzerAttributeAnalyzer.MissingDiagnosticAnalyzerAttributeRule).WithLocation(7, 7).WithArguments(DiagnosticAnalyzerCorrectnessAnalyzer.DiagnosticAnalyzerAttributeFullName);
            await VerifyVB.VerifyAnalyzerAsync(source, expected);

            var fixedCode_WithVBAttribute = @"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Class MyAnalyzer
	Inherits DiagnosticAnalyzer
	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
		Get
			Throw New NotImplementedException()
		End Get
	End Property

	Public Overrides Sub Initialize(context As AnalysisContext)
	End Sub
End Class
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    ExpectedDiagnostics = { expected },
                },
                FixedState = { Sources = { fixedCode_WithVBAttribute } },
                CodeFixIndex = 1,
                CodeFixEquivalenceKey = "Apply DiagnosticAnalyzer attribute for 'Visual Basic'.",
            }.RunAsync();

            var fixedCode_WithCSharpAndVBAttributes = @"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

<DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
Class MyAnalyzer
	Inherits DiagnosticAnalyzer
	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
		Get
			Throw New NotImplementedException()
		End Get
	End Property

	Public Overrides Sub Initialize(context As AnalysisContext)
	End Sub
End Class
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    ExpectedDiagnostics = { expected },
                },
                FixedState = { Sources = { fixedCode_WithCSharpAndVBAttributes } },
                CodeFixIndex = 2,
                CodeFixEquivalenceKey = "Apply DiagnosticAnalyzer attribute for both 'C#' and 'Visual Basic'.",
            }.RunAsync();
        }

        [Fact]
        public async Task CSharp_NoDiagnosticCases()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
class MyAnalyzerWithLanguageSpecificAttribute : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public override void Initialize(AnalysisContext context)
    {
    }
}

public abstract class MyAbstractAnalyzerWithoutAttribute : DiagnosticAnalyzer
{
}
";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task VisualBasic_NoDiagnosticCases()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Class MyAnalyzerWithLanguageSpecificAttribute
	Inherits DiagnosticAnalyzer
	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
		Get
			Throw New NotImplementedException()
		End Get
	End Property

	Public Overrides Sub Initialize(context As AnalysisContext)
	End Sub
End Class

Public MustInherit Class MyAbstractAnalyzerWithoutAttribute
	Inherits DiagnosticAnalyzer
End Class
";
            await VerifyVB.VerifyAnalyzerAsync(source);
        }
    }
}
