// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Analyzers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Analyzers.MetaAnalyzers
{
    public class MissingKindArgumentToRegisterActionRuleTests : CodeFixTestBase
    {
        [Fact]
        public void CSharp_VerifyRegisterSymbolActionDiagnostic()
        {
            var source = @"
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
        context.RegisterSymbolAction(AnalyzeSymbol);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
    }

    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
    }
}";
            DiagnosticResult expected = GetCSharpExpectedDiagnostic(20, 9, isSymbolKindDiagnostic: true);
            VerifyCSharp(source, expected);
        }

        [Fact]
        public void VisualBasic_VerifyRegisterSymbolActionDiagnostic()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

<DiagnosticAnalyzer(LanguageNames.CSharp)>
Class MyAnalyzer
    Inherits DiagnosticAnalyzer
    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
        context.RegisterSymbolAction(AddressOf AnalyzeSymbol)
    End Sub

    Private Shared Sub AnalyzeSymbol(context As SymbolAnalysisContext)
    End Sub

    Private Shared Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
    End Sub
End Class
";
            DiagnosticResult expected = GetBasicExpectedDiagnostic(17, 9, isSymbolKindDiagnostic: true);
            VerifyBasic(source, expected);
        }

        [Fact]
        public void CSharp_VerifyRegisterSyntaxActionDiagnostic()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        context.RegisterSyntaxNodeAction<SyntaxKind>(AnalyzeSyntax);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
    }

    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
    }
}";
            DiagnosticResult expected = GetCSharpExpectedDiagnostic(21, 9, isSymbolKindDiagnostic: false);
            VerifyCSharp(source, expected);
        }

        [Fact]
        public void VisualBasic_VerifyRegisterSyntaxActionDiagnostic()
        {
            var source = @"
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

<DiagnosticAnalyzer(LanguageNames.CSharp)>
Class MyAnalyzer
    Inherits DiagnosticAnalyzer
    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
        context.RegisterSyntaxNodeAction(Of SyntaxKind)(AddressOf AnalyzeSyntax)
    End Sub

    Private Shared Sub AnalyzeSymbol(context As SymbolAnalysisContext)
    End Sub

    Private Shared Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
    End Sub
End Class
";
            DiagnosticResult expected = GetBasicExpectedDiagnostic(17, 9, isSymbolKindDiagnostic: false);
            VerifyBasic(source, expected);
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
            return new CSharpRegisterActionAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicRegisterActionAnalyzer();
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, bool isSymbolKindDiagnostic)
        {
            return GetExpectedDiagnostic(LanguageNames.CSharp, line, column, isSymbolKindDiagnostic);
        }

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, bool isSymbolKindDiagnostic)
        {
            return GetExpectedDiagnostic(LanguageNames.VisualBasic, line, column, isSymbolKindDiagnostic);
        }

        private static DiagnosticResult GetExpectedDiagnostic(string language, int line, int column, bool isSymbolKindDiagnostic)
        {
            string fileName = language == LanguageNames.CSharp ? "Test0.cs" : "Test0.vb";
            object[] messageArguments = isSymbolKindDiagnostic ? new object[] { "SymbolKind", "symbol" } : new object[] { "SyntaxKind", "syntax" };
            return new DiagnosticResult
            {
                Id = DiagnosticIds.MissingKindArgumentToRegisterActionRuleId,
                Message = string.Format(CodeAnalysisDiagnosticsResources.MissingKindArgumentToRegisterActionMessage, messageArguments),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation(fileName, line, column)
                }
            };
        }
    }
}
