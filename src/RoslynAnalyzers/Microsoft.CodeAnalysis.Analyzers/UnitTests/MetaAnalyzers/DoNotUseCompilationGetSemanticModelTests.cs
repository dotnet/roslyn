// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.DoNotUseCompilationGetSemanticModelAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.DoNotUseCompilationGetSemanticModelAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class DoNotUseCompilationGetSemanticModelTests
    {
        [Fact]
        public async Task CallInInitializeAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                class Analyzer : DiagnosticAnalyzer
                {
                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw null;

                    public override void Initialize(AnalysisContext context)
                    {
                        context.RegisterCompilationStartAction(csac =>
                        {
                            csac.Compilation.GetSemanticModel(null);
                        });
                    }
                }
                """,
                GetCSharpExpectedDiagnostic(13, 13));

            await VerifyVB.VerifyAnalyzerAsync("""
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                Class Analyzer
                    Inherits DiagnosticAnalyzer

                    Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Throw New System.Exception
                        End Get
                    End Property

                    Public Overrides Sub Initialize(ByVal context As AnalysisContext)
                        context.RegisterCompilationStartAction(
                            Function(csac)
                                csac.Compilation.GetSemanticModel(Nothing)
                            End Function)
                    End Sub
                End Class
                """,
                GetBasicExpectedDiagnostic(17, 17));
        }

        [Fact]
        public async Task CallInSeparateMethodAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                class Analyzer : DiagnosticAnalyzer
                {
                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw null;

                    public override void Initialize(AnalysisContext context)
                    {
                        DoSomething(context);
                    }

                    private void DoSomething(AnalysisContext context)
                    {
                        context.RegisterOperationAction(oac =>
                        {
                            var semanticModel = oac.Compilation.GetSemanticModel(null);
                        }, OperationKind.Invocation);
                    }
                }
                """,
                GetCSharpExpectedDiagnostic(18, 33));

            await VerifyVB.VerifyAnalyzerAsync("""
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                Class Analyzer
                    Inherits DiagnosticAnalyzer

                    Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Throw New System.Exception
                        End Get
                    End Property

                    Public Overrides Sub Initialize(ByVal context As AnalysisContext)
                        DoSomething(context)
                    End Sub

                    Private Sub DoSomething(ByVal context As AnalysisContext)
                        context.RegisterOperationAction(
                            Function(oac)
                                Dim semanticModel = oac.Compilation.GetSemanticModel(Nothing)
                            End Function,
                            OperationKind.Invocation)
                    End Sub
                End Class
                """,
                GetBasicExpectedDiagnostic(21, 37));
        }

        [Fact]
        public async Task CastedCallAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.Diagnostics;

                class Analyzer : DiagnosticAnalyzer
                {
                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw null;

                    public override void Initialize(AnalysisContext context)
                    {
                        context.RegisterCompilationStartAction(csac =>
                        {
                            var csharpCompilation = csac.Compilation as CSharpCompilation;
                            csharpCompilation.GetSemanticModel(null);
                        });
                    }
                }
                """,
                GetCSharpExpectedDiagnostic(15, 13));

            await VerifyVB.VerifyAnalyzerAsync("""
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics
                Imports Microsoft.CodeAnalysis.VisualBasic

                Class Analyzer
                    Inherits DiagnosticAnalyzer

                    Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Throw New System.Exception
                        End Get
                    End Property

                    Public Overrides Sub Initialize(ByVal context As AnalysisContext)
                        context.RegisterCompilationStartAction(
                            Function(csac)
                                Dim basicCompilation = TryCast(csac.Compilation, VisualBasicCompilation)
                                basicCompilation.GetSemanticModel(Nothing)
                            End Function)
                    End Sub
                End Class
                """,
                GetBasicExpectedDiagnostic(19, 17));
        }

        [Fact]
        public async Task CallInNonDiagnosticAnalyzerClassAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("""
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                class NotADiagnosticAnalyzer
                {
                    public void Foo(AnalysisContext context)
                    {
                        context.RegisterCompilationStartAction(csac =>
                        {
                            csac.Compilation.GetSemanticModel(null);
                        });
                    }
                }
                """);

            await VerifyVB.VerifyAnalyzerAsync("""
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                Class NotADiagnosticAnalyzer
                    Public Sub Foo(ByVal context As AnalysisContext)
                        context.RegisterCompilationStartAction(
                            Function(csac)
                                csac.Compilation.GetSemanticModel(Nothing)
                            End Function)
                    End Sub
                End Class
                """);
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column) =>
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic().WithLocation(line, column);
#pragma warning restore RS0030 // Do not use banned APIs

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column) =>
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyVB.Diagnostic().WithLocation(line, column);
#pragma warning restore RS0030 // Do not use banned APIs
    }
}
