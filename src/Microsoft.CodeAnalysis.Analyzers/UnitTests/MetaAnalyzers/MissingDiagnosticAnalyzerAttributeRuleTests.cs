// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.Fixers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers.CodeFixes;
using Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class MissingDiagnosticAnalyzerAttributeRuleTests : CodeFixTestBase
    {
        [Fact]
        public void CSharp_VerifyDiagnosticAndFixes()
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
            DiagnosticResult expected = GetCSharpExpectedDiagnostic(7, 7);
            VerifyCSharp(source, expected);

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

            VerifyCSharpFix(source, fixedCode_WithCSharpAttribute, codeFixIndex: 0);

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

            VerifyCSharpFix(source, fixedCode_WithCSharpAndVBAttributes, codeFixIndex: 2);
        }

        [Fact]
        public void VisualBasic_VerifyDiagnosticAndFixes()
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
            DiagnosticResult expected = GetBasicExpectedDiagnostic(7, 7);
            VerifyBasic(source, expected);

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

            VerifyBasicFix(source, fixedCode_WithVBAttribute, codeFixIndex: 1);

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

            VerifyBasicFix(source, fixedCode_WithCSharpAndVBAttributes, codeFixIndex: 2);
        }

        [Fact]
        public void CSharp_NoDiagnosticCases()
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
            VerifyCSharp(source);
        }

        [Fact]
        public void VisualBasic_NoDiagnosticCases()
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
            VerifyBasic(source);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CSharpApplyDiagnosticAnalyzerAttributeFix();
        }

        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return new BasicApplyDiagnosticAnalyzerAttributeFix();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new DiagnosticAnalyzerAttributeAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new DiagnosticAnalyzerAttributeAnalyzer();
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column)
        {
            return GetExpectedDiagnostic(line, column);
        }

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column)
        {
            return GetExpectedDiagnostic(line, column);
        }

        private static DiagnosticResult GetExpectedDiagnostic(int line, int column)
        {
            return new DiagnosticResult(DiagnosticIds.MissingDiagnosticAnalyzerAttributeRuleId, DiagnosticHelpers.DefaultDiagnosticSeverity)
                .WithLocation(line, column)
                .WithMessageFormat(CodeAnalysisDiagnosticsResources.MissingAttributeMessage)
                .WithArguments(DiagnosticAnalyzerCorrectnessAnalyzer.DiagnosticAnalyzerAttributeFullName);
        }
    }
}
