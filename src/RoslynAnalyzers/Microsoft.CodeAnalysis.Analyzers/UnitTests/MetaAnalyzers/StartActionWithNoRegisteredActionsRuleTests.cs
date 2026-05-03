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
    public class StartActionWithNoRegisteredActionsRuleTests
    {
        [Fact]
        public async Task CSharp_VerifyDiagnosticAsync()
        {
            DiagnosticResult[] expected =
            [
                GetCSharpExpectedDiagnostic(20, 48, parameterName: "compilationContext", kind: StartActionKind.CompilationStartAction),
                GetCSharpExpectedDiagnostic(33, 47, parameterName: "codeBlockContext", kind: StartActionKind.CodeBlockStartAction),
                GetCSharpExpectedDiagnostic(37, 52, parameterName: "operationBlockContext", kind: StartActionKind.OperationBlockStartAction)
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
                    }

                    private static void AnalyzeOperationBlockStart(OperationBlockStartAnalysisContext operationBlockContext)
                    {
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
                GetBasicExpectedDiagnostic(30, 46, parameterName: "codeBlockContext", kind: StartActionKind.CodeBlockStartAction),
                GetBasicExpectedDiagnostic(33, 51, parameterName: "operationBlockContext", kind: StartActionKind.OperationBlockStartAction)
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
                            End Sub
                        )

                        context.RegisterSyntaxNodeAction(AddressOf AnalyzeSyntax, SyntaxKind.InvocationExpression)
                        context.RegisterCodeBlockStartAction(Of SyntaxKind)(AddressOf AnalyzeCodeBlockStart)
                        context.RegisterOperationBlockStartAction(AddressOf AnalyzeOperationBlockStart)
                    End Sub

                    Private Shared Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
                    End Sub

                    Private Shared Sub AnalyzeCodeBlockStart(codeBlockContext As CodeBlockStartAnalysisContext(Of SyntaxKind))
                    End Sub

                    Private Shared Sub AnalyzeOperationBlockStart(operationBlockContext As OperationBlockStartAnalysisContext)
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
                        });
                    }

                    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
                    {
                    }

                    private static void AnalyzeCodeBlockStart(CodeBlockStartAnalysisContext<SyntaxKind> context)
                    {
                        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
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
                        });
                    }

                    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
                    {
                    }

                    private static void AnalyzeCodeBlockStart(CodeBlockStartAnalysisContext<SyntaxKind> context)
                    {
                        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
                    }
                }
                """);

        [Fact]
        public Task CSharp_NoDiagnosticCases_OperationAnalyzerRegistrationAsync()
            => VerifyCS.VerifyAnalyzerAsync("""
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
                        context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
                    }

                    private static void AnalyzeOperation(OperationAnalysisContext context)
                    {
                    }
                }

                [DiagnosticAnalyzer(LanguageNames.CSharp)]
                class MyAnalyzer2 : DiagnosticAnalyzer
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
                        context.RegisterOperationBlockAction(AnalyzeOperationBlock);
                    }

                    private static void AnalyzeOperationBlock(OperationBlockAnalysisContext context)
                    {
                    }
                }
                """);

        [Fact]
        public Task CSharp_NoDiagnosticCases_NestedOperationAnalyzerRegistrationAsync()
            => VerifyCS.VerifyAnalyzerAsync("""
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
                        context.RegisterCompilationStartAction(compilationContext =>
                        {
                            compilationContext.RegisterOperationBlockStartAction(operationBlockContext =>
                            {
                                AnalyzeOperationBlockStart(operationBlockContext);
                            });
                        });

                        context.RegisterCompilationStartAction(compilationContext =>
                        {
                            compilationContext.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
                        });

                        context.RegisterCompilationStartAction(compilationContext =>
                        {
                            compilationContext.RegisterOperationBlockAction(AnalyzeOperationBlock);
                        });
                    }

                    private static void AnalyzeOperation(OperationAnalysisContext context)
                    {
                    }

                    private static void AnalyzeOperationBlock(OperationBlockAnalysisContext context)
                    {
                    }

                    private static void AnalyzeOperationBlockStart(OperationBlockStartAnalysisContext context)
                    {
                        context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
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
                            End Sub
                        )
                    End Sub

                    Private Shared Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
                    End Sub

                    Private Shared Sub AnalyzeCodeBlockStart(context As CodeBlockStartAnalysisContext(Of SyntaxKind))
                        context.RegisterSyntaxNodeAction(AddressOf AnalyzeSyntax, SyntaxKind.InvocationExpression)
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
                            End Sub
                        )
                    End Sub

                    Private Shared Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
                    End Sub

                    Private Shared Sub AnalyzeCodeBlockStart(context As CodeBlockStartAnalysisContext(Of SyntaxKind))
                        context.RegisterSyntaxNodeAction(AddressOf AnalyzeSyntax, SyntaxKind.InvocationExpression)
                    End Sub
                End Class
                """);

        [Fact]
        public Task VisualBasic_NoDiagnosticCases_OperationAnalyzerRegistrationAsync()
            => VerifyVB.VerifyAnalyzerAsync("""
                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.VisualBasic)> _
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer
                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Throw New NotImplementedException()
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                		context.RegisterOperationAction(AddressOf AnalyzeOperation, OperationKind.Invocation)
                	End Sub

                	Private Shared Sub AnalyzeOperation(context As OperationAnalysisContext)
                	End Sub
                End Class

                <DiagnosticAnalyzer(LanguageNames.VisualBasic)> _
                Class MyAnalyzer2
                	Inherits DiagnosticAnalyzer
                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Throw New NotImplementedException()
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                		context.RegisterOperationBlockAction(AddressOf AnalyzeOperationBlock)
                	End Sub

                	Private Shared Sub AnalyzeOperationBlock(context As OperationBlockAnalysisContext)
                	End Sub
                End Class
                """);

        [Fact]
        public Task VisualBasic_NoDiagnosticCases_NestedOperationAnalyzerRegistrationAsync()
            => VerifyVB.VerifyAnalyzerAsync("""
                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.VisualBasic)> _
                MustInherit Class MyAnalyzer
                	Inherits DiagnosticAnalyzer
                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Throw New NotImplementedException()
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                		context.RegisterCompilationStartAction(Function(compilationContext) 
                      		                                        compilationContext.RegisterOperationBlockStartAction(Function(operationBlockContext) 
                		                                                                                                    AnalyzeOperationBlockStart(operationBlockContext)
                                                                                                                         End Function)
                                                               End Function)

                		context.RegisterCompilationStartAction(Function(compilationContext) 
                		                                         compilationContext.RegisterOperationAction(AddressOf AnalyzeOperation, OperationKind.Invocation)
                                                               End Function)

                		context.RegisterCompilationStartAction(Function(compilationContext) 
                		                                            compilationContext.RegisterOperationBlockAction(AddressOf AnalyzeOperationBlock)
                                                               End Function)
                	End Sub

                	Private Shared Sub AnalyzeOperation(context As OperationAnalysisContext)
                	End Sub

                	Private Shared Sub AnalyzeOperationBlock(context As OperationBlockAnalysisContext)
                	End Sub

                	Private Shared Sub AnalyzeOperationBlockStart(context As OperationBlockStartAnalysisContext)
                		context.RegisterOperationAction(AddressOf AnalyzeOperation, OperationKind.Invocation)
                	End Sub
                End Class
                """);

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, string parameterName, StartActionKind kind) =>
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(CSharpRegisterActionAnalyzer.StartActionWithNoRegisteredActionsRule)
                .WithLocation(line, column)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithArguments(GetExpectedArguments(parameterName, kind));

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, string parameterName, StartActionKind kind) =>
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyVB.Diagnostic(BasicRegisterActionAnalyzer.StartActionWithNoRegisteredActionsRule)
                .WithLocation(line, column)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithArguments(GetExpectedArguments(parameterName, kind));

        private static string[] GetExpectedArguments(string parameterName, StartActionKind kind)
        {
            var arg2 = kind switch
            {
                StartActionKind.CompilationStartAction => "Initialize",
                StartActionKind.CodeBlockStartAction
                or StartActionKind.OperationBlockStartAction => "Initialize, CompilationStartAction",
                _ => throw new ArgumentException("Unsupported action kind", nameof(kind)),
            };

            return [parameterName, arg2];
        }

        private enum StartActionKind
        {
            CompilationStartAction,
            CodeBlockStartAction,
            OperationBlockStartAction
        }
    }
}
