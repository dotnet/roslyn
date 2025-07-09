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
    public class MissingKindArgumentToRegisterActionRuleTests
    {
        [Fact]
        public async Task CSharp_VerifyRegisterSymbolActionDiagnosticAsync()
        {
            DiagnosticResult expected = GetCSharpExpectedDiagnostic(19, 9, MissingKindArgument.SymbolKind);
            await VerifyCS.VerifyAnalyzerAsync("""
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
                }
                """, expected);
        }

        [Fact]
        public async Task VisualBasic_VerifyRegisterSymbolActionDiagnosticAsync()
        {
            DiagnosticResult expected = GetBasicExpectedDiagnostic(16, 9, MissingKindArgument.SymbolKind);
            await VerifyVB.VerifyAnalyzerAsync("""
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
                """, expected);
        }

        [Fact]
        public async Task CSharp_VerifyRegisterSyntaxActionDiagnosticAsync()
        {
            DiagnosticResult expected = GetCSharpExpectedDiagnostic(20, 9, MissingKindArgument.SyntaxKind);
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
                        context.RegisterSyntaxNodeAction<SyntaxKind>(AnalyzeSyntax);
                    }

                    private static void AnalyzeSymbol(SymbolAnalysisContext context)
                    {
                    }

                    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
                    {
                    }
                }
                """, expected);
        }

        [Fact]
        public async Task VisualBasic_VerifyRegisterSyntaxActionDiagnosticAsync()
        {
            DiagnosticResult expected = GetBasicExpectedDiagnostic(17, 9, MissingKindArgument.SyntaxKind);
            await VerifyVB.VerifyAnalyzerAsync("""
                Imports System
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
                """, expected);
        }

        [Fact]
        public async Task CSharp_VerifyRegisterOperationActionDiagnosticAsync()
        {
            DiagnosticResult expected = GetCSharpExpectedDiagnostic(19, 9, MissingKindArgument.OperationKind);
            await VerifyCS.VerifyAnalyzerAsync("""
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
                        context.RegisterOperationAction(AnalyzeOperation);
                    }

                    private static void AnalyzeOperation(OperationAnalysisContext context)
                    {
                    }
                }
                """, expected);
        }

        [Fact]
        public async Task VisualBasic_VerifyRegisterOperationActionDiagnosticAsync()
        {
            DiagnosticResult expected = GetBasicExpectedDiagnostic(16, 9, MissingKindArgument.OperationKind);
            await VerifyVB.VerifyAnalyzerAsync("""
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
                        context.RegisterOperationAction(AddressOf AnalyzeOperation)
                    End Sub

                    Private Shared Sub AnalyzeOperation(context As OperationAnalysisContext)
                    End Sub
                End Class
                """, expected);
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, MissingKindArgument kind)
        {
            var rule = kind switch
            {
                MissingKindArgument.SymbolKind => CSharpRegisterActionAnalyzer.MissingSymbolKindArgumentRule,
                MissingKindArgument.SyntaxKind => CSharpRegisterActionAnalyzer.MissingSyntaxKindArgumentRule,
                MissingKindArgument.OperationKind => CSharpRegisterActionAnalyzer.MissingOperationKindArgumentRule,
                _ => throw new ArgumentException("Unsupported argument kind", nameof(kind)),
            };

#pragma warning disable RS0030 // Do not use banned APIs
            return VerifyCS.Diagnostic(rule).WithLocation(line, column);
#pragma warning restore RS0030 // Do not use banned APIs
        }

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, MissingKindArgument kind)
        {
            var rule = kind switch
            {
                MissingKindArgument.SymbolKind => BasicRegisterActionAnalyzer.MissingSymbolKindArgumentRule,
                MissingKindArgument.SyntaxKind => BasicRegisterActionAnalyzer.MissingSyntaxKindArgumentRule,
                MissingKindArgument.OperationKind => BasicRegisterActionAnalyzer.MissingOperationKindArgumentRule,
                _ => throw new ArgumentException("Unsupported argument kind", nameof(kind)),
            };

#pragma warning disable RS0030 // Do not use banned APIs
            return VerifyVB.Diagnostic(rule).WithLocation(line, column);
#pragma warning restore RS0030 // Do not use banned APIs
        }

        private enum MissingKindArgument
        {
            SymbolKind,
            SyntaxKind,
            OperationKind
        }
    }
}
