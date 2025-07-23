// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    public class UnsupportedSymbolKindArgumentRuleTests
    {
        [Fact]
        public async Task CSharp_VerifyDiagnosticAsync()
        {
            DiagnosticResult[] expected =
            [
                GetCSharpExpectedDiagnostic(20, 13, unsupportedSymbolKind: SymbolKind.Alias),
                GetCSharpExpectedDiagnostic(21, 13, unsupportedSymbolKind: SymbolKind.ArrayType),
                GetCSharpExpectedDiagnostic(22, 13, unsupportedSymbolKind: SymbolKind.Assembly),
                GetCSharpExpectedDiagnostic(23, 13, unsupportedSymbolKind: SymbolKind.Discard),
                GetCSharpExpectedDiagnostic(24, 13, unsupportedSymbolKind: SymbolKind.DynamicType),
                GetCSharpExpectedDiagnostic(25, 13, unsupportedSymbolKind: SymbolKind.ErrorType),
                GetCSharpExpectedDiagnostic(28, 13, unsupportedSymbolKind: SymbolKind.Label),
                GetCSharpExpectedDiagnostic(29, 13, unsupportedSymbolKind: SymbolKind.Local),
                GetCSharpExpectedDiagnostic(31, 13, unsupportedSymbolKind: SymbolKind.NetModule),
                GetCSharpExpectedDiagnostic(35, 13, unsupportedSymbolKind: SymbolKind.PointerType),
                GetCSharpExpectedDiagnostic(37, 13, unsupportedSymbolKind: SymbolKind.Preprocessing),
                GetCSharpExpectedDiagnostic(38, 13, unsupportedSymbolKind: SymbolKind.RangeVariable),
                GetCSharpExpectedDiagnostic(39, 13, unsupportedSymbolKind: SymbolKind.TypeParameter),
            ];

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
                        context.RegisterSymbolAction(AnalyzeSymbol,
                            SymbolKind.Alias,
                            SymbolKind.ArrayType,
                            SymbolKind.Assembly,
                            SymbolKind.Discard,
                            SymbolKind.DynamicType,
                            SymbolKind.ErrorType,
                            SymbolKind.Event,
                            SymbolKind.Field,
                            SymbolKind.Label,
                            SymbolKind.Local,
                            SymbolKind.Method,
                            SymbolKind.NetModule,
                            SymbolKind.NamedType,
                            SymbolKind.Namespace,
                            SymbolKind.Parameter,
                            SymbolKind.PointerType,
                            SymbolKind.Property,
                            SymbolKind.Preprocessing,
                            SymbolKind.RangeVariable,
                            SymbolKind.TypeParameter);
                    }

                    private static void AnalyzeSymbol(SymbolAnalysisContext context)
                    {
                    }
                }
                """, expected);
        }

        [Fact]
        public async Task VisualBasic_VerifyRegisterSymbolActionDiagnosticAsync()
        {
            DiagnosticResult[] expected =
            [
                GetBasicExpectedDiagnostic(17, 13, unsupportedSymbolKind: SymbolKind.Alias),
                GetBasicExpectedDiagnostic(18, 13, unsupportedSymbolKind: SymbolKind.ArrayType),
                GetBasicExpectedDiagnostic(19, 13, unsupportedSymbolKind: SymbolKind.Assembly),
                GetBasicExpectedDiagnostic(20, 13, unsupportedSymbolKind: SymbolKind.Discard),
                GetBasicExpectedDiagnostic(21, 13, unsupportedSymbolKind: SymbolKind.DynamicType),
                GetBasicExpectedDiagnostic(22, 13, unsupportedSymbolKind: SymbolKind.ErrorType),
                GetBasicExpectedDiagnostic(25, 13, unsupportedSymbolKind: SymbolKind.Label),
                GetBasicExpectedDiagnostic(26, 13, unsupportedSymbolKind: SymbolKind.Local),
                GetBasicExpectedDiagnostic(28, 13, unsupportedSymbolKind: SymbolKind.NetModule),
                GetBasicExpectedDiagnostic(32, 13, unsupportedSymbolKind: SymbolKind.PointerType),
                GetBasicExpectedDiagnostic(34, 13, unsupportedSymbolKind: SymbolKind.Preprocessing),
                GetBasicExpectedDiagnostic(35, 13, unsupportedSymbolKind: SymbolKind.RangeVariable),
                GetBasicExpectedDiagnostic(36, 13, unsupportedSymbolKind: SymbolKind.TypeParameter),
            ];

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
                        context.RegisterSymbolAction(AddressOf AnalyzeSymbol,
                            SymbolKind.Alias,
                            SymbolKind.ArrayType,
                            SymbolKind.Assembly,
                            SymbolKind.Discard,
                            SymbolKind.DynamicType,
                            SymbolKind.ErrorType,
                            SymbolKind.Event,
                            SymbolKind.Field,
                            SymbolKind.Label,
                            SymbolKind.Local,
                            SymbolKind.Method,
                            SymbolKind.NetModule,
                            SymbolKind.NamedType,
                            SymbolKind.Namespace,
                            SymbolKind.Parameter,
                            SymbolKind.PointerType,
                            SymbolKind.Property,
                            SymbolKind.Preprocessing,
                            SymbolKind.RangeVariable,
                            SymbolKind.TypeParameter)
                    End Sub

                    Private Shared Sub AnalyzeSymbol(context As SymbolAnalysisContext)
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
                        // Valid symbol kinds.
                        context.RegisterSymbolAction(AnalyzeSymbol,
                            SymbolKind.Event,
                            SymbolKind.Field,
                            SymbolKind.Method,
                            SymbolKind.NamedType,
                            SymbolKind.Namespace,
                            SymbolKind.Property);

                        // Overload resolution failure
                        context.RegisterSymbolAction({|CS1503:AnalyzeSyntax|},
                            SymbolKind.Event,
                            SymbolKind.Field,
                            SymbolKind.Method,
                            SymbolKind.NamedType,
                            SymbolKind.Namespace,
                            SymbolKind.Property);
                    }

                    private static void AnalyzeSymbol(SymbolAnalysisContext context)
                    {
                    }

                    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
                    {
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

                <DiagnosticAnalyzer(LanguageNames.CSharp)>
                Class MyAnalyzer
                    Inherits DiagnosticAnalyzer
                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Throw New NotImplementedException()
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)

                        ' Valid symbol kinds
                        context.RegisterSymbolAction(AddressOf AnalyzeSymbol,
                            SymbolKind.Event,
                            SymbolKind.Field,
                            SymbolKind.Method,
                            SymbolKind.NamedType,
                            SymbolKind.Namespace,
                            SymbolKind.Property)

                        ' Overload resolution failure
                        context.{|BC30518:RegisterSymbolAction|}(AddressOf AnalyzeSyntax,
                            SymbolKind.Alias)
                    End Sub

                    Private Shared Sub AnalyzeSymbol(context As SymbolAnalysisContext)
                    End Sub

                    Private Shared Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
                    End Sub
                End Class
                """);

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, SymbolKind unsupportedSymbolKind) =>
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(CSharpRegisterActionAnalyzer.UnsupportedSymbolKindArgumentRule)
                .WithLocation(line, column)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithArguments(unsupportedSymbolKind);

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, SymbolKind unsupportedSymbolKind) =>
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyVB.Diagnostic(BasicRegisterActionAnalyzer.UnsupportedSymbolKindArgumentRule)
                .WithLocation(line, column)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithArguments(unsupportedSymbolKind);
    }
}
