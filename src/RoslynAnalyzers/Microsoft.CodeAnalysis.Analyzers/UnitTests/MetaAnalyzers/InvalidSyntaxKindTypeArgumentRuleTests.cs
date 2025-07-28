// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Helpers;
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
    public class InvalidSyntaxKindTypeArgumentRuleTests
    {
        [Fact]
        public async Task CSharp_VerifyDiagnosticAsync()
        {
            DiagnosticResult[] expected =
            [
                GetCSharpExpectedDiagnostic(23, 9, typeArgumentName: "Int32", registerMethodName: DiagnosticWellKnownNames.RegisterSyntaxNodeActionName),
                GetCSharpExpectedDiagnostic(24, 9, typeArgumentName: "Int32", registerMethodName: DiagnosticWellKnownNames.RegisterCodeBlockStartActionName)
            ];

            await VerifyCS.VerifyAnalyzerAsync("""
                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.Diagnostics;

                #pragma warning disable RS1012
                #pragma warning disable RS1013

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
                        context.RegisterSyntaxNodeAction(AnalyzeSyntax, 0);
                        context.RegisterCodeBlockStartAction<int>(AnalyzeCodeBlockStart);
                    }

                    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
                    {
                    }

                    private static void AnalyzeCodeBlockStart(CodeBlockStartAnalysisContext<int> context)
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
                GetBasicExpectedDiagnostic(19, 9, typeArgumentName: "Int32", registerMethodName: DiagnosticWellKnownNames.RegisterSyntaxNodeActionName),
                GetBasicExpectedDiagnostic(20, 9, typeArgumentName: "Int32", registerMethodName: DiagnosticWellKnownNames.RegisterCodeBlockStartActionName)
            ];

            await VerifyVB.VerifyAnalyzerAsync("""
                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                #Disable Warning RS1012
                #Disable Warning RS1013

                <DiagnosticAnalyzer(LanguageNames.CSharp)>
                Class MyAnalyzer
                    Inherits DiagnosticAnalyzer
                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Throw New NotImplementedException()
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)
                        context.RegisterSyntaxNodeAction(AddressOf AnalyzeSyntax, 0)
                        context.RegisterCodeBlockStartAction(Of Int32)(AddressOf AnalyzeCodeBlockStart)
                    End Sub

                    Private Shared Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
                    End Sub

                    Private Shared Sub AnalyzeCodeBlockStart(context As CodeBlockStartAnalysisContext(Of Int32))
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

                #pragma warning disable RS1012
                #pragma warning disable RS1013

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
                        context.{|CS0411:RegisterSyntaxNodeAction|}(AnalyzeSyntax, null);              // Overload resolution failure
                        context.RegisterSyntaxNodeAction<{|CS0246:ErrorType|}>(AnalyzeSyntax, null);   // Error type argument
                        context.RegisterCodeBlockStartAction<T>(AnalyzeCodeBlockStart);     // NYI: Type param as a type argument
                    }

                    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
                    {
                    }

                    private static void AnalyzeCodeBlockStart(CodeBlockStartAnalysisContext<T> context)
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

                #Disable Warning RS1012
                #Disable Warning RS1013

                <DiagnosticAnalyzer(LanguageNames.CSharp)>
                Class MyAnalyzer(Of T As Structure)
                    Inherits DiagnosticAnalyzer

                    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                        Get
                            Throw New NotImplementedException()
                        End Get
                    End Property

                    Public Overrides Sub Initialize(context As AnalysisContext)
                        context.{|BC30518:RegisterSyntaxNodeAction|}(AddressOf AnalyzeSyntax, Nothing)                  ' Overload resolution failure
                        context.{|BC30521:RegisterSyntaxNodeAction(Of {|BC30002:ErrorType|})|}(AddressOf AnalyzeSyntax, Nothing)    ' Error type argument
                        context.RegisterCodeBlockStartAction(Of T)(AddressOf AnalyzeCodeBlockStart)         ' NYI: Type param as a type argument
                    End Sub

                    Private Shared Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
                    End Sub

                    Private Shared Sub AnalyzeCodeBlockStart(context As CodeBlockStartAnalysisContext(Of T))
                    End Sub
                End Class
                """);

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, string typeArgumentName, string registerMethodName) =>
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(CSharpRegisterActionAnalyzer.InvalidSyntaxKindTypeArgumentRule)
                .WithLocation(line, column)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithArguments(typeArgumentName, DiagnosticWellKnownNames.TLanguageKindEnumName, registerMethodName);

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, string typeArgumentName, string registerMethodName) =>
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyVB.Diagnostic(BasicRegisterActionAnalyzer.InvalidSyntaxKindTypeArgumentRule)
                .WithLocation(line, column)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithArguments(typeArgumentName, DiagnosticWellKnownNames.TLanguageKindEnumName, registerMethodName);
    }
}
