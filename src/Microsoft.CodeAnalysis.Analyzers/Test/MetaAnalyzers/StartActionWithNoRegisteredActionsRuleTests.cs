// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Analyzers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Analyzers.MetaAnalyzers
{
    public class StartActionWithNoRegisteredActionsRuleTests : CodeFixTestBase
    {
        [Fact]
        public void CSharp_VerifyDiagnostic()
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
        context.RegisterCompilationStartAction(compilationContext =>
        {
        });

        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
        context.RegisterCodeBlockStartAction<SyntaxKind>(AnalyzeCodeBlockStart);
    }

    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
    }

    private static void AnalyzeCodeBlockStart(CodeBlockStartAnalysisContext<SyntaxKind> codeBlockContext)
    {
    }
}";
            DiagnosticResult[] expected = new[]
            {
                GetCSharpExpectedDiagnostic(21, 48, parameterName: "compilationContext", isCompilationStartAction: true),
                GetCSharpExpectedDiagnostic(33, 47, parameterName: "codeBlockContext", isCompilationStartAction: false)
            };

            VerifyCSharp(source, addLanguageSpecificCodeAnalysisReference: true, expected: expected);
        }

        [Fact]
        public void VisualBasic_VerifyDiagnostic()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Class MyAnalyzer
    Inherits DiagnosticAnalyzer
    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
        context.RegisterCompilationStartAction(
            Sub(compilationContext As CompilationStartAnalysisContext)
            End Sub
        )

        context.RegisterSyntaxNodeAction(AddressOf AnalyzeSyntax, SyntaxKind.InvocationExpression)
        context.RegisterCodeBlockStartAction(Of SyntaxKind)(AddressOf AnalyzeCodeBlockStart)
    End Sub

    Private Shared Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
    End Sub

    Private Shared Sub AnalyzeCodeBlockStart(codeBlockContext As CodeBlockStartAnalysisContext(Of SyntaxKind))
    End Sub
End Class
";
            DiagnosticResult[] expected = new[]
            {
                GetBasicExpectedDiagnostic(19, 17, parameterName: "compilationContext", isCompilationStartAction: true),
                GetBasicExpectedDiagnostic(30, 46, parameterName: "codeBlockContext", isCompilationStartAction: false)
            };

            VerifyBasic(source, addLanguageSpecificCodeAnalysisReference: true, expected: expected);
        }

        [Fact]
        public void CSharp_NoDiagnosticCases()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
abstract class MyAnalyzer<T> : DiagnosticAnalyzer
    where T : struct
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
        context.RegisterCompilationStartAction(compilationContext =>
        {
            compilationContext.RegisterCodeBlockStartAction<SyntaxKind>(AnalyzeCodeBlockStart);
        });
    }

    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
    }

    private static void AnalyzeCodeBlockStart(CodeBlockStartAnalysisContext<SyntaxKind> context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
    }
}";

            VerifyCSharp(source, addLanguageSpecificCodeAnalysisReference: true);
        }

        [Fact]
        public void CSharp_NoDiagnosticCases_2()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
abstract class MyAnalyzer<T> : DiagnosticAnalyzer
    where T : struct
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
        context.RegisterCompilationStartAction(compilationContext =>
        {
            compilationContext.RegisterCodeBlockStartAction<SyntaxKind>(codeBlockContext =>
            {
                AnalyzeCodeBlockStart(codeBlockContext);
            });
        });
    }

    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
    }

    private static void AnalyzeCodeBlockStart(CodeBlockStartAnalysisContext<SyntaxKind> context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
    }
}";

            VerifyCSharp(source, addLanguageSpecificCodeAnalysisReference: true);
        }

        [Fact]
        public void VisualBasic_NoDiagnosticCases()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Class MyAnalyzer(Of T As Structure)
    Inherits DiagnosticAnalyzer

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
        context.RegisterCompilationStartAction(
            Sub(compilationContext As CompilationStartAnalysisContext)
                compilationContext.RegisterCodeBlockStartAction(Of SyntaxKind)(AddressOf AnalyzeCodeBlockStart)
            End Sub
        )
    End Sub

    Private Shared Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
    End Sub

    Private Shared Sub AnalyzeCodeBlockStart(context As CodeBlockStartAnalysisContext(Of SyntaxKind))
        context.RegisterSyntaxNodeAction(AddressOf AnalyzeSyntax, SyntaxKind.InvocationExpression)
    End Sub
End Class
";

            VerifyBasic(source, addLanguageSpecificCodeAnalysisReference: true);
        }

        [Fact]
        public void VisualBasic_NoDiagnosticCases_2()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Class MyAnalyzer(Of T As Structure)
    Inherits DiagnosticAnalyzer

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
        context.RegisterCompilationStartAction(
            Sub(compilationContext As CompilationStartAnalysisContext)
                compilationContext.RegisterCodeBlockStartAction(Of SyntaxKind)(
                    Sub(codeBlockContext As CodeBlockStartAnalysisContext(Of SyntaxKind))
                        AnalyzeCodeBlockStart(codeBlockContext)
                    End Sub
                )
            End Sub
        )
    End Sub

    Private Shared Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
    End Sub

    Private Shared Sub AnalyzeCodeBlockStart(context As CodeBlockStartAnalysisContext(Of SyntaxKind))
        context.RegisterSyntaxNodeAction(AddressOf AnalyzeSyntax, SyntaxKind.InvocationExpression)
    End Sub
End Class
";

            VerifyBasic(source, addLanguageSpecificCodeAnalysisReference: true);
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

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, string parameterName, bool isCompilationStartAction)
        {
            return GetExpectedDiagnostic(LanguageNames.CSharp, line, column, parameterName, isCompilationStartAction);
        }

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, string parameterName, bool isCompilationStartAction)
        {
            return GetExpectedDiagnostic(LanguageNames.VisualBasic, line, column, parameterName, isCompilationStartAction);
        }

        private static DiagnosticResult GetExpectedDiagnostic(string language, int line, int column, string parameterName, bool isCompilationStartAction)
        {
            string arg2 = isCompilationStartAction ? "Initialize" : "Initialize, CompilationStartAction";
            string message = string.Format(CodeAnalysisDiagnosticsResources.StartActionWithNoRegisteredActionsMessage, parameterName, arg2);

            string fileName = language == LanguageNames.CSharp ? "Test0.cs" : "Test0.vb";
            return new DiagnosticResult
            {
                Id = DiagnosticIds.StartActionWithNoRegisteredActionsRuleId,
                Message = message,
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation(fileName, line, column)
                }
            };
        }
    }
}
