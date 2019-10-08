// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers;
using Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class StartActionWithOnlyEndActionRuleTests : DiagnosticAnalyzerTestBase
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
            compilationContext.RegisterCompilationEndAction(null);
        });

        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
        context.RegisterCodeBlockStartAction<SyntaxKind>(AnalyzeCodeBlockStart);
        context.RegisterOperationBlockStartAction(AnalyzeOperationBlockStart);
    }

    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
    }

    private static void AnalyzeCodeBlockStart(CodeBlockStartAnalysisContext<SyntaxKind> codeBlockContext)
    {
        codeBlockContext.RegisterCodeBlockEndAction(null);
    }

    private static void AnalyzeOperationBlockStart(OperationBlockStartAnalysisContext operationBlockContext)
    {
        operationBlockContext.RegisterOperationBlockEndAction(null);
    }
}";
            DiagnosticResult[] expected = new[]
            {
                GetCSharpExpectedDiagnostic(21, 48, parameterName: "compilationContext", kind: StartActionKind.CompilationStartAction),
                GetCSharpExpectedDiagnostic(35, 47, parameterName: "codeBlockContext", kind: StartActionKind.CodeBlockStartAction),
                GetCSharpExpectedDiagnostic(40, 52, parameterName: "operationBlockContext", kind: StartActionKind.OperationBlockStartAction)
            };

            VerifyCSharp(source, referenceFlags: ReferenceFlags.None, expected: expected);
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
                compilationContext.RegisterCompilationEndAction(Nothing)
            End Sub
        )

        context.RegisterSyntaxNodeAction(AddressOf AnalyzeSyntax, SyntaxKind.InvocationExpression)
        context.RegisterCodeBlockStartAction(Of SyntaxKind)(AddressOf AnalyzeCodeBlockStart)
        context.RegisterOperationBlockStartAction(AddressOf AnalyzeOperationBlockStart)
    End Sub

    Private Shared Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
    End Sub

    Private Shared Sub AnalyzeCodeBlockStart(codeBlockContext As CodeBlockStartAnalysisContext(Of SyntaxKind))
        codeBlockContext.RegisterCodeBlockEndAction(Nothing)
    End Sub

    Private Shared Sub AnalyzeOperationBlockStart(operationBlockContext As OperationBlockStartAnalysisContext)
        operationBlockContext.RegisterOperationBlockEndAction(Nothing)
    End Sub
End Class
";
            DiagnosticResult[] expected = new[]
            {
                GetBasicExpectedDiagnostic(19, 17, parameterName: "compilationContext", kind: StartActionKind.CompilationStartAction),
                GetBasicExpectedDiagnostic(32, 46, parameterName: "codeBlockContext", kind: StartActionKind.CodeBlockStartAction),
                GetBasicExpectedDiagnostic(36, 51, parameterName: "operationBlockContext", kind: StartActionKind.OperationBlockStartAction)
            };

            VerifyBasic(source, referenceFlags: ReferenceFlags.None, expected: expected);
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
            compilationContext.RegisterOperationBlockStartAction(AnalyzeOperationBlockStart);
            compilationContext.RegisterCompilationEndAction(null);
        });
    }

    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
    }

    private static void AnalyzeCodeBlockStart(CodeBlockStartAnalysisContext<SyntaxKind> context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
        context.RegisterCodeBlockEndAction(null);
    }

    private static void AnalyzeOperation(OperationAnalysisContext context)
    {
    }

    private static void AnalyzeOperationBlockStart(OperationBlockStartAnalysisContext context)
    {
        context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
        context.RegisterOperationBlockEndAction(null);
    }
}";

            VerifyCSharp(source, referenceFlags: ReferenceFlags.None);
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

            compilationContext.RegisterCompilationEndAction(null);
        });
    }

    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
    }

    private static void AnalyzeCodeBlockStart(CodeBlockStartAnalysisContext<SyntaxKind> context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
        context.RegisterCodeBlockEndAction(null);
    }
}";

            VerifyCSharp(source, referenceFlags: ReferenceFlags.None);
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
                compilationContext.RegisterOperationBlockStartAction(AddressOf AnalyzeOperationBlockStart)
                compilationContext.RegisterCompilationEndAction(Nothing)
            End Sub
        )
    End Sub

    Private Shared Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
    End Sub

    Private Shared Sub AnalyzeCodeBlockStart(context As CodeBlockStartAnalysisContext(Of SyntaxKind))
        context.RegisterSyntaxNodeAction(AddressOf AnalyzeSyntax, SyntaxKind.InvocationExpression)
        context.RegisterCodeBlockEndAction(Nothing)
    End Sub

    Private Shared Sub AnalyzeOperation(context As OperationAnalysisContext)
    End Sub

    Private Shared Sub AnalyzeOperationBlockStart(context As OperationBlockStartAnalysisContext)
        context.RegisterOperationAction(AddressOf AnalyzeOperation, OperationKind.Invocation)
        context.RegisterOperationBlockEndAction(Nothing)
    End Sub
End Class
";

            VerifyBasic(source, referenceFlags: ReferenceFlags.None);
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

                compilationContext.RegisterCompilationEndAction(Nothing)
            End Sub
        )
    End Sub

    Private Shared Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
    End Sub

    Private Shared Sub AnalyzeCodeBlockStart(context As CodeBlockStartAnalysisContext(Of SyntaxKind))
        context.RegisterSyntaxNodeAction(AddressOf AnalyzeSyntax, SyntaxKind.InvocationExpression)
        context.RegisterCodeBlockEndAction(Nothing)
    End Sub
End Class
";

            VerifyBasic(source, referenceFlags: ReferenceFlags.None);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpRegisterActionAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicRegisterActionAnalyzer();
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, string parameterName, StartActionKind kind)
        {
            return GetExpectedDiagnostic(line, column, parameterName, kind);
        }

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, string parameterName, StartActionKind kind)
        {
            return GetExpectedDiagnostic(line, column, parameterName, kind);
        }

        private static DiagnosticResult GetExpectedDiagnostic(int line, int column, string parameterName, StartActionKind kind)
        {
            string endActionName;
            string statelessActionName;
            string arg4;
            switch (kind)
            {
                case StartActionKind.CompilationStartAction:
                    endActionName = "CompilationEndAction";
                    statelessActionName = "RegisterCompilationAction";
                    arg4 = "Initialize";
                    break;

                case StartActionKind.CodeBlockStartAction:
                    endActionName = "CodeBlockEndAction";
                    statelessActionName = "RegisterCodeBlockAction";
                    arg4 = "Initialize, CompilationStartAction";
                    break;

                case StartActionKind.OperationBlockStartAction:
                    endActionName = "OperationBlockEndAction";
                    statelessActionName = "RegisterOperationBlockAction";
                    arg4 = "Initialize, CompilationStartAction";
                    break;

                default:
                    throw new ArgumentException("Unsupported argument kind", nameof(kind));
            }

            string message = string.Format(CodeAnalysisDiagnosticsResources.StartActionWithOnlyEndActionMessage, parameterName, endActionName, statelessActionName, arg4);

            return new DiagnosticResult(DiagnosticIds.StartActionWithOnlyEndActionRuleId, DiagnosticHelpers.DefaultDiagnosticSeverity)
                .WithLocation(line, column)
                .WithMessageFormat(message);
        }

        private enum StartActionKind
        {
            CompilationStartAction,
            CodeBlockStartAction,
            OperationBlockStartAction
        }
    }
}
