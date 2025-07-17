﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.EnableConcurrentExecutionAnalyzer,
    Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.Fixers.CSharpEnableConcurrentExecutionFix>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.EnableConcurrentExecutionAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers.CodeFixes.BasicEnableConcurrentExecutionFix>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class EnableConcurrentExecutionAnalyzerTests
    {
        [Fact]
        public Task TestSimpleCase_CSharpAsync()
            => VerifyCS.VerifyCodeFixAsync("""
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                class Analyzer : DiagnosticAnalyzer {
                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw null;
                    public override void Initialize(AnalysisContext [|context|])
                    {
                    }
                }
                """, """
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                class Analyzer : DiagnosticAnalyzer {
                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw null;
                    public override void Initialize(AnalysisContext context)
                    {
                        context.EnableConcurrentExecution();
                    }
                }
                """);

        [Fact]
        public Task TestSimpleCase_VisualBasicAsync()
            => VerifyVB.VerifyCodeFixAsync("""
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

                    Public Overrides Sub Initialize([|context|] As AnalysisContext)
                    End Sub
                End Class
                """, """
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

                    Public Overrides Sub Initialize([|context|] As AnalysisContext)
                        context.EnableConcurrentExecution()
                    End Sub
                End Class
                """);

        [Fact]
        public Task RenamedMethod_CSharpAsync()
            => VerifyCS.VerifyAnalyzerAsync("""
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                class Analyzer : DiagnosticAnalyzer {
                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw null;
                    public override void Initialize(AnalysisContext context)
                    {
                        context.EnableConcurrentExecution();
                    }

                    public void NotInitialize(AnalysisContext context)
                    {
                    }
                }
                """);

        [Fact]
        public Task RenamedMethod_VisualBasicAsync()
            => VerifyVB.VerifyAnalyzerAsync("""
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

                    Public Overrides Sub Initialize(context As AnalysisContext)
                        context.EnableConcurrentExecution()
                    End Sub

                    Public Sub NotInitialize(context As AnalysisContext)
                    End Sub
                End Class
                """);

        [Fact, WorkItem(2698, "https://github.com/dotnet/roslyn-analyzers/issues/2698")]
        public Task RS1026_ExpressionBodiedMethodAsync()
            => VerifyCS.VerifyCodeFixAsync("""
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                class Analyzer : DiagnosticAnalyzer {
                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw null;
                    public override void Initialize(AnalysisContext [|context|])
                        => context.RegisterCompilationAction(x => { });
                }
                """, """
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                class Analyzer : DiagnosticAnalyzer {
                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw null;
                    public override void Initialize(AnalysisContext context)
                    {
                        context.EnableConcurrentExecution();
                        context.RegisterCompilationAction(x => { });
                    }
                }
                """);
    }
}
