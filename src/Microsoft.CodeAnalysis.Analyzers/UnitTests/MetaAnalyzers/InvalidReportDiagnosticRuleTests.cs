// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Analyzer.Utilities;
using Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers;
using Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class InvalidReportDiagnosticRuleTests : DiagnosticAnalyzerTestBase
    {
        [Fact]
        public void CSharp_VerifyDiagnostic()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor descriptor1 = new DiagnosticDescriptor(""MyDiagnosticId1"", null, null, null, DiagnosticSeverity.Warning, false);
    private static readonly DiagnosticDescriptor descriptor2 = new DiagnosticDescriptor(""MyDiagnosticId2"", null, null, null, DiagnosticSeverity.Warning, false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            return ImmutableArray.Create(descriptor1);
        }
    }

    public override void Initialize(AnalysisContext context)
    {
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        context.ReportDiagnostic(Diagnostic.Create(descriptor2, Location.None));

        var diag = Diagnostic.Create(descriptor2, Location.None);
        context.ReportDiagnostic(diag);
    }

    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        context.ReportDiagnostic(Diagnostic.Create(descriptor2, Location.None));

        Diagnostic diag = Diagnostic.Create(descriptor2, Location.None), diag2 = Diagnostic.Create(descriptor2, Location.None); 
        context.ReportDiagnostic(diag);
    }

    private static void AnalyzeOperation(OperationAnalysisContext context)
    {
        context.ReportDiagnostic(Diagnostic.Create(descriptor2, Location.None));

        var diag = Diagnostic.Create(descriptor2, Location.None);
        context.ReportDiagnostic(diag);
    }
}";
            DiagnosticResult[] expected = new[]
            {
                GetCSharpExpectedDiagnostic(27, 9, unsupportedDescriptorName: "descriptor2"),
                GetCSharpExpectedDiagnostic(30, 9, unsupportedDescriptorName: "descriptor2"),
                GetCSharpExpectedDiagnostic(35, 9, unsupportedDescriptorName: "descriptor2"),
                GetCSharpExpectedDiagnostic(38, 9, unsupportedDescriptorName: "descriptor2"),
                GetCSharpExpectedDiagnostic(43, 9, unsupportedDescriptorName: "descriptor2"),
                GetCSharpExpectedDiagnostic(46, 9, unsupportedDescriptorName: "descriptor2")
            };

            VerifyCSharp(source, expected);
        }

        [Fact, WorkItem(1689, "https://github.com/dotnet/roslyn-analyzers/issues/1689")]
        public void CSharp_VerifyDiagnostic_PropertyInitializer()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor descriptor1 = new DiagnosticDescriptor(""MyDiagnosticId1"", null, null, null, DiagnosticSeverity.Warning, false);
    private static readonly DiagnosticDescriptor descriptor2 = new DiagnosticDescriptor(""MyDiagnosticId2"", null, null, null, DiagnosticSeverity.Warning, false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(descriptor1);

    public override void Initialize(AnalysisContext context)
    {
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        context.ReportDiagnostic(Diagnostic.Create(descriptor2, Location.None));

        var diag = Diagnostic.Create(descriptor2, Location.None);
        context.ReportDiagnostic(diag);
    }
}";
            DiagnosticResult[] expected = new[]
            {
                GetCSharpExpectedDiagnostic(21, 9, unsupportedDescriptorName: "descriptor2"),
                GetCSharpExpectedDiagnostic(24, 9, unsupportedDescriptorName: "descriptor2")
            };

            VerifyCSharp(source, expected);
        }

        [Fact]
        public void VisualBasic_VerifyDiagnostic()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

<DiagnosticAnalyzer(LanguageNames.CSharp)>
Class MyAnalyzer
    Inherits DiagnosticAnalyzer

    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = New DiagnosticDescriptor(""MyDiagnosticId1"", Nothing, Nothing, Nothing, DiagnosticSeverity.Warning, False)
    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = New DiagnosticDescriptor(""MyDiagnosticId2"", Nothing, Nothing, Nothing, DiagnosticSeverity.Warning, False)

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Return ImmutableArray.Create(descriptor1)
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
    End Sub

    Private Shared Sub AnalyzeSymbol(context As SymbolAnalysisContext)
        context.ReportDiagnostic(Diagnostic.Create(descriptor2, Location.None))

        Dim diag = Diagnostic.Create(descriptor2, Location.None)
        context.ReportDiagnostic(diag)
    End Sub

    Private Shared Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
        context.ReportDiagnostic(Diagnostic.Create(descriptor2, Location.None))

        Dim diag = Diagnostic.Create(descriptor2, Location.None), diag2 = Diagnostic.Create(descriptor2, Location.None)
        context.ReportDiagnostic(diag)
    End Sub

    Private Shared Sub AnalyzeOperation(context As OperationAnalysisContext)
        context.ReportDiagnostic(Diagnostic.Create(descriptor2, Location.None))

        Dim diag = Diagnostic.Create(descriptor2, Location.None)
        context.ReportDiagnostic(diag)
    End Sub
End Class
";
            DiagnosticResult[] expected = new[]
            {
                GetBasicExpectedDiagnostic(24, 9, unsupportedDescriptorName: "descriptor2"),
                GetBasicExpectedDiagnostic(27, 9, unsupportedDescriptorName: "descriptor2"),
                GetBasicExpectedDiagnostic(31, 9, unsupportedDescriptorName: "descriptor2"),
                GetBasicExpectedDiagnostic(34, 9, unsupportedDescriptorName: "descriptor2"),
                GetBasicExpectedDiagnostic(38, 9, unsupportedDescriptorName: "descriptor2"),
                GetBasicExpectedDiagnostic(41, 9, unsupportedDescriptorName: "descriptor2")
            };

            VerifyBasic(source, expected);
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
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor descriptor1 = new DiagnosticDescriptor(""MyDiagnosticId1"", null, null, null, DiagnosticSeverity.Warning, false);
    private static readonly DiagnosticDescriptor descriptor2 = new DiagnosticDescriptor(""MyDiagnosticId2"", null, null, null, DiagnosticSeverity.Warning, false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            return ImmutableArray.Create(descriptor1);
        }
    }

    public override void Initialize(AnalysisContext context)
    {
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        // Overload resolution failures
        context.ReportDiagnostic(Diagnostic.Create(descriptor2, Location.None), null);
        context.ReportDiagnostic(Diagnostic.Create(descriptor2, Location.None, null));
        context.ReportDiagnostic(diag);

        // Needs flow analysis
        var diag = Diagnostic.Create(descriptor2, Location.None);
        var diag2 = diag;
        context.ReportDiagnostic(diag2);
    }
}";

            VerifyCSharp(source, TestValidationMode.AllowCompileErrors);
        }

        [Fact]
        public void VisualBasic_NoDiagnosticCases()
        {
            var source = @"
Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

<DiagnosticAnalyzer(LanguageNames.CSharp)>
Class MyAnalyzer
    Inherits DiagnosticAnalyzer

    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = New DiagnosticDescriptor(""MyDiagnosticId1"", Nothing, Nothing, Nothing, DiagnosticSeverity.Warning, False)
    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = New DiagnosticDescriptor(""MyDiagnosticId2"", Nothing, Nothing, Nothing, DiagnosticSeverity.Warning, False)

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Return ImmutableArray.Create(descriptor1)
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
    End Sub

    Private Shared Sub AnalyzeSymbol(context As SymbolAnalysisContext)
        ' Overload resolution failures
        context.ReportDiagnostic(Diagnostic.Create(descriptor2, Location.None), Nothing)
        context.ReportDiagnostic(Diagnostic.Create(descriptor2, Location.None, Nothing))
        context.ReportDiagnostic(diag)

        ' Needs flow analysis
        Dim diag = Diagnostic.Create(descriptor2, Location.None)
        Dim diag2 = diag
        context.ReportDiagnostic(diag2)
    End Sub
End Class
";

            VerifyBasic(source, TestValidationMode.AllowCompileErrors);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpReportDiagnosticAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicReportDiagnosticAnalyzer();
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, string unsupportedDescriptorName)
        {
            return GetExpectedDiagnostic(LanguageNames.CSharp, line, column, unsupportedDescriptorName);
        }

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, string unsupportedDescriptorName)
        {
            return GetExpectedDiagnostic(LanguageNames.VisualBasic, line, column, unsupportedDescriptorName);
        }

        private static DiagnosticResult GetExpectedDiagnostic(string language, int line, int column, string unsupportedDescriptorName)
        {
            string fileName = language == LanguageNames.CSharp ? "Test0.cs" : "Test0.vb";
            return new DiagnosticResult(DiagnosticIds.InvalidReportDiagnosticRuleId, DiagnosticHelpers.DefaultDiagnosticSeverity)
                .WithLocation(fileName, line, column)
                .WithMessageFormat(CodeAnalysisDiagnosticsResources.InvalidReportDiagnosticMessage)
                .WithArguments(unsupportedDescriptorName);
        }
    }
}
