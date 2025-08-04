// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.DiagnosticAnalyzerAttributeAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.DiagnosticAnalyzerAttributeAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class AddLanguageSupportToAnalyzerRuleTests
    {
        [Fact]
        public async Task CSharp_VerifyDiagnosticAsync()
        {
            var source = """
                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer(LanguageNames.CSharp, "MyLanguage")]
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
                    }
                }
                """;
            DiagnosticResult expected = GetCSharpExpectedDiagnostic(6, 2, "MyAnalyzer", missingLanguageName: LanguageNames.VisualBasic);

            // Verify diagnostic if analyzer assembly doesn't reference C# code analysis assembly.
            await new VerifyCS.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithoutRoslynSymbols,
                TestState =
                {
                    Sources = { source },
                    ExpectedDiagnostics = { expected },
                },
            }.RunAsync();

            // Verify no diagnostic if analyzer assembly references C# code analysis assembly.
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task VisualBasic_VerifyDiagnosticAsync()
        {
            var source = """
                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer(LanguageNames.VisualBasic, "MyLanguage")>
                Class MyAnalyzer
                	Inherits DiagnosticAnalyzer
                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Throw New NotImplementedException()
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class
                """;
            DiagnosticResult expected = GetBasicExpectedDiagnostic(6, 2, "MyAnalyzer", missingLanguageName: LanguageNames.CSharp);

            // Verify diagnostic if analyzer assembly doesn't reference VB code analysis assembly.
            await new VerifyVB.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithoutRoslynSymbols,
                TestState =
                {
                    Sources = { source },
                    ExpectedDiagnostics = { expected },
                },
            }.RunAsync();

            // Verify no diagnostic if analyzer assembly references VB code analysis assembly.
            await VerifyVB.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task CSharp_NoDiagnosticCasesAsync()
        {
            var source = """
                using System;
                using System.Collections.Immutable;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Diagnostics;

                [DiagnosticAnalyzer("MyLanguage")]
                class MyAnalyzerWithCustomLanguageAttribute : DiagnosticAnalyzer
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
                    }
                }

                [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
                class MyAnalyzerWithBothLanguages : DiagnosticAnalyzer
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
                    }
                }

                [DiagnosticAnalyzer(LanguageNames.CSharp)]
                public abstract class MyAbstractAnalyzer : DiagnosticAnalyzer
                {
                }
                """;
            await new VerifyCS.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithoutRoslynSymbols,
                TestState = { Sources = { source } },
            }.RunAsync();

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task VisualBasic_NoDiagnosticCasesAsync()
        {
            var source = """
                Imports System
                Imports System.Collections.Immutable
                Imports Microsoft.CodeAnalysis
                Imports Microsoft.CodeAnalysis.Diagnostics

                <DiagnosticAnalyzer("MyLanguage")>
                Class MyAnalyzerWithCustomLanguageAttribute
                	Inherits DiagnosticAnalyzer
                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Throw New NotImplementedException()
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                <DiagnosticAnalyzer(LanguageNames.VisualBasic, LanguageNames.CSharp)>
                Class MyAnalyzerWithBothLanguages
                	Inherits DiagnosticAnalyzer
                	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                		Get
                			Throw New NotImplementedException()
                		End Get
                	End Property

                	Public Overrides Sub Initialize(context As AnalysisContext)
                	End Sub
                End Class

                <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
                Public MustInherit Class MyAbstractAnalyzer
                	Inherits DiagnosticAnalyzer
                End Class
                """;
            await new VerifyVB.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithoutRoslynSymbols,
                TestState = { Sources = { source } },
            }.RunAsync();

            await VerifyVB.VerifyAnalyzerAsync(source);
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, string analyzerTypeName, string missingLanguageName) =>
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DiagnosticAnalyzerAttributeAnalyzer.AddLanguageSupportToAnalyzerRule)
                .WithLocation(line, column)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithArguments(analyzerTypeName, missingLanguageName);

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, string analyzerTypeName, string missingLanguageName) =>
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyVB.Diagnostic(DiagnosticAnalyzerAttributeAnalyzer.AddLanguageSupportToAnalyzerRule)
                .WithLocation(line, column)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithArguments(analyzerTypeName, missingLanguageName);
    }
}
