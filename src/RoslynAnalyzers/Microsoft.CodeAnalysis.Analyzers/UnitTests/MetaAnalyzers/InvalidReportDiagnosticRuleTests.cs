// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.CSharpReportDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers.BasicReportDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class InvalidReportDiagnosticRuleTests
    {
        [Fact]
        public async Task CSharp_VerifyDiagnosticAsync()
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
            DiagnosticResult[] expected =
            [
                GetCSharpExpectedDiagnostic(27, 9, unsupportedDescriptorName: "descriptor2"),
                GetCSharpExpectedDiagnostic(30, 9, unsupportedDescriptorName: "descriptor2"),
                GetCSharpExpectedDiagnostic(35, 9, unsupportedDescriptorName: "descriptor2"),
                GetCSharpExpectedDiagnostic(38, 9, unsupportedDescriptorName: "descriptor2"),
                GetCSharpExpectedDiagnostic(43, 9, unsupportedDescriptorName: "descriptor2"),
                GetCSharpExpectedDiagnostic(46, 9, unsupportedDescriptorName: "descriptor2")
            ];

            await VerifyCS.VerifyAnalyzerAsync(source, expected);
        }

        [Fact, WorkItem(1689, "https://github.com/dotnet/roslyn-analyzers/issues/1689")]
        public async Task CSharp_VerifyDiagnostic_PropertyInitializerAsync()
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
            DiagnosticResult[] expected =
            [
                GetCSharpExpectedDiagnostic(21, 9, unsupportedDescriptorName: "descriptor2"),
                GetCSharpExpectedDiagnostic(24, 9, unsupportedDescriptorName: "descriptor2")
            ];

            await VerifyCS.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task VisualBasic_VerifyDiagnosticAsync()
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
            DiagnosticResult[] expected =
            [
                GetBasicExpectedDiagnostic(24, 9, unsupportedDescriptorName: "descriptor2"),
                GetBasicExpectedDiagnostic(27, 9, unsupportedDescriptorName: "descriptor2"),
                GetBasicExpectedDiagnostic(31, 9, unsupportedDescriptorName: "descriptor2"),
                GetBasicExpectedDiagnostic(34, 9, unsupportedDescriptorName: "descriptor2"),
                GetBasicExpectedDiagnostic(38, 9, unsupportedDescriptorName: "descriptor2"),
                GetBasicExpectedDiagnostic(41, 9, unsupportedDescriptorName: "descriptor2")
            ];

            await VerifyVB.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task CSharp_NoDiagnosticCasesAsync()
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
        context.{|CS1501:ReportDiagnostic|}(Diagnostic.Create(descriptor2, Location.None), null);
        context.ReportDiagnostic(Diagnostic.{|CS0121:Create|}(descriptor2, Location.None, null));
        context.ReportDiagnostic({|CS0841:diag|});

        // Needs flow analysis
        var diag = Diagnostic.Create(descriptor2, Location.None);
        var diag2 = diag;
        context.ReportDiagnostic(diag2);
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task VisualBasic_NoDiagnosticCasesAsync()
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
        context.ReportDiagnostic(Diagnostic.Create(descriptor2, Location.None), {|BC30057:Nothing|})
        context.ReportDiagnostic(Diagnostic.{|BC30521:Create|}(descriptor2, Location.None, Nothing))
        context.ReportDiagnostic({|BC32000:diag|})

        ' Needs flow analysis
        Dim diag = Diagnostic.Create(descriptor2, Location.None)
        Dim diag2 = diag
        context.ReportDiagnostic(diag2)
    End Sub
End Class
";

            await VerifyVB.VerifyAnalyzerAsync(source);
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, string unsupportedDescriptorName) =>
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic()
                .WithLocation(line, column)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithArguments(unsupportedDescriptorName);

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, string unsupportedDescriptorName) =>
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyVB.Diagnostic()
                .WithLocation(line, column)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithArguments(unsupportedDescriptorName);
    }
}
