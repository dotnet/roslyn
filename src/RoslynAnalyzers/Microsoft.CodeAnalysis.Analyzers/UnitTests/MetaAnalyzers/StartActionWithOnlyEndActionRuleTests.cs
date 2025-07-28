// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.CSharpRegisterActionAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers.BasicRegisterActionAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class StartActionWithOnlyEndActionRuleTests
    {
        [Fact]
        public async Task CSharp_VerifyDiagnosticAsync()
        {
            DiagnosticResult[] expected =
            [
                GetCSharpExpectedDiagnostic(20, 48, parameterName: "compilationContext", kind: StartActionKind.CompilationStartAction),
                GetCSharpExpectedDiagnostic(34, 47, parameterName: "codeBlockContext", kind: StartActionKind.CodeBlockStartAction),
                GetCSharpExpectedDiagnostic(39, 52, parameterName: "operationBlockContext", kind: StartActionKind.OperationBlockStartAction)
            ];

            await VerifyCS.VerifyAnalyzerAsync("""
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
                }
                """, expected);
        }

        [Fact]
        public async Task VisualBasic_VerifyDiagnosticAsync()
        {
            DiagnosticResult[] expected =
            [
                GetBasicExpectedDiagnostic(18, 17, parameterName: "compilationContext", kind: StartActionKind.CompilationStartAction),
                GetBasicExpectedDiagnostic(31, 46, parameterName: "codeBlockContext", kind: StartActionKind.CodeBlockStartAction),
                GetBasicExpectedDiagnostic(35, 51, parameterName: "operationBlockContext", kind: StartActionKind.OperationBlockStartAction)
            ];

            await VerifyVB.VerifyAnalyzerAsync("""
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
                """, expected);
        }

        [Fact]
        public Task CSharp_NoDiagnosticCasesAsync()
            => VerifyCS.VerifyAnalyzerAsync("""
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
                }
                """);

        [Fact]
        public Task CSharp_NoDiagnosticCases_2Async()
            => VerifyCS.VerifyAnalyzerAsync("""
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
                }
                """);

        [Fact]
        public Task VisualBasic_NoDiagnosticCasesAsync()
            => VerifyVB.VerifyAnalyzerAsync("""
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
                """);

        [Fact]
        public Task VisualBasic_NoDiagnosticCases_2Async()
            => VerifyVB.VerifyAnalyzerAsync("""
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
                """);

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, string parameterName, StartActionKind kind) =>
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(CSharpRegisterActionAnalyzer.StartActionWithOnlyEndActionRule)
                .WithLocation(line, column)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithArguments(GetExpectedArguments(parameterName, kind));

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, string parameterName, StartActionKind kind) =>
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyVB.Diagnostic(BasicRegisterActionAnalyzer.StartActionWithOnlyEndActionRule)
                .WithLocation(line, column)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithArguments(GetExpectedArguments(parameterName, kind));

        private static string[] GetExpectedArguments(string parameterName, StartActionKind kind)
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

            return [parameterName, endActionName, statelessActionName, arg4];
        }

        private enum StartActionKind
        {
            CompilationStartAction,
            CodeBlockStartAction,
            OperationBlockStartAction
        }
    }
}
