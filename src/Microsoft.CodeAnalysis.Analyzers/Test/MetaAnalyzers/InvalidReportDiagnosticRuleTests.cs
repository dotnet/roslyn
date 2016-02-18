// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Analyzers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Analyzers.MetaAnalyzers
{
    public class InvalidReportDiagnosticRuleTests : CodeFixTestBase
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
    private static readonly DiagnosticDescriptor descriptor = new TriggerDiagnosticDescriptor(""MyDiagnosticId"");
    private static readonly DiagnosticDescriptor descriptor2 = new TriggerDiagnosticDescriptor(""MyDiagnosticId2"");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            return ImmutableArray.Create(descriptor);
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
}";
            DiagnosticResult[] expected = new[]
            {
                GetCSharpExpectedDiagnostic(27, 9, unsupportedDescriptorName: "descriptor2"),
                GetCSharpExpectedDiagnostic(30, 9, unsupportedDescriptorName: "descriptor2"),
                GetCSharpExpectedDiagnostic(35, 9, unsupportedDescriptorName: "descriptor2"),
                GetCSharpExpectedDiagnostic(38, 9, unsupportedDescriptorName: "descriptor2")
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

    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = New TriggerDiagnosticDescriptor(""MyDiagnosticId"")
    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = New TriggerDiagnosticDescriptor(""MyDiagnosticId2"")

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
End Class
";
            DiagnosticResult[] expected = new[]
            {
                GetBasicExpectedDiagnostic(24, 9, unsupportedDescriptorName: "descriptor2"),
                GetBasicExpectedDiagnostic(27, 9, unsupportedDescriptorName: "descriptor2"),
                GetBasicExpectedDiagnostic(31, 9, unsupportedDescriptorName: "descriptor2"),
                GetBasicExpectedDiagnostic(34, 9, unsupportedDescriptorName: "descriptor2")
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
    private static readonly DiagnosticDescriptor descriptor = new TriggerDiagnosticDescriptor(""MyDiagnosticId"");
    private static readonly DiagnosticDescriptor descriptor2 = new TriggerDiagnosticDescriptor(""MyDiagnosticId2"");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            return ImmutableArray.Create(descriptor);
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

<DiagnosticAnalyzer(LanguageNames.CSharp)>
Class MyAnalyzer
    Inherits DiagnosticAnalyzer

    Private Shared ReadOnly descriptor1 As DiagnosticDescriptor = New TriggerDiagnosticDescriptor(""MyDiagnosticId"")
    Private Shared ReadOnly descriptor2 As DiagnosticDescriptor = New TriggerDiagnosticDescriptor(""MyDiagnosticId2"")

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

            VerifyBasic(source);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return null;
        }

        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return null;
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
            return new DiagnosticResult
            {
                Id = DiagnosticIds.InvalidReportDiagnosticRuleId,
                Message = string.Format(CodeAnalysisDiagnosticsResources.InvalidReportDiagnosticMessage, unsupportedDescriptorName),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation(fileName, line, column)
                }
            };
        }
    }
}
