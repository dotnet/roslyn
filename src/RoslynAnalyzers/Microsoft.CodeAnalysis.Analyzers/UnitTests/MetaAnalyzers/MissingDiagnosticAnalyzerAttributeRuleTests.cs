// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable CA1305

using System.Threading.Tasks;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.Fixers;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers.CodeFixes;
using Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers;

using VerifyCS = CSharpCodeFixVerifier<DiagnosticAnalyzerAttributeAnalyzer, CSharpApplyDiagnosticAnalyzerAttributeFix>;
using VerifyVB = VisualBasicCodeFixVerifier<DiagnosticAnalyzerAttributeAnalyzer, BasicApplyDiagnosticAnalyzerAttributeFix>;

public sealed class MissingDiagnosticAnalyzerAttributeRuleTests
{
    [Fact]
    public async Task CSharp_VerifyDiagnosticAndFixesAsync()
    {
        var source = """
            using System;
            using System.Collections.Immutable;
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.Diagnostics;

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
#pragma warning disable RS0030 // Do not use banned APIs
        DiagnosticResult expected = VerifyCS.Diagnostic(DiagnosticAnalyzerAttributeAnalyzer.MissingDiagnosticAnalyzerAttributeRule).WithLocation(6, 7).WithArguments(WellKnownTypeNames.MicrosoftCodeAnalysisDiagnosticsDiagnosticAnalyzerAttribute);
#pragma warning restore RS0030 // Do not use banned APIs
        await VerifyCS.VerifyAnalyzerAsync(source, expected);

        var fixedCode_WithCSharpAttribute = """
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
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { source },
                ExpectedDiagnostics = { expected },
            },
            FixedState = { Sources = { fixedCode_WithCSharpAttribute } },
            CodeActionEquivalenceKey = string.Format(CodeAnalysisDiagnosticsResources.ApplyDiagnosticAnalyzerAttribute_1, LanguageNames.CSharp),
        }.RunAsync();

        var fixedCode_WithCSharpAndVBAttributes = """
            using System;
            using System.Collections.Immutable;
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.Diagnostics;

            [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
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

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { source },
                ExpectedDiagnostics = { expected },
            },
            FixedState = { Sources = { fixedCode_WithCSharpAndVBAttributes } },
            CodeActionIndex = 2,
            CodeActionEquivalenceKey = string.Format(CodeAnalysisDiagnosticsResources.ApplyDiagnosticAnalyzerAttribute_2, LanguageNames.CSharp, LanguageNames.VisualBasic),
        }.RunAsync();
    }

    [Fact]
    public async Task VisualBasic_VerifyDiagnosticAndFixesAsync()
    {
        var source = """
            Imports System
            Imports System.Collections.Immutable
            Imports Microsoft.CodeAnalysis
            Imports Microsoft.CodeAnalysis.Diagnostics

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
#pragma warning disable RS0030 // Do not use banned APIs
        DiagnosticResult expected = VerifyVB.Diagnostic(DiagnosticAnalyzerAttributeAnalyzer.MissingDiagnosticAnalyzerAttributeRule).WithLocation(6, 7).WithArguments(WellKnownTypeNames.MicrosoftCodeAnalysisDiagnosticsDiagnosticAnalyzerAttribute);
#pragma warning restore RS0030 // Do not use banned APIs
        await VerifyVB.VerifyAnalyzerAsync(source, expected);

        var fixedCode_WithVBAttribute = """
            Imports System
            Imports System.Collections.Immutable
            Imports Microsoft.CodeAnalysis
            Imports Microsoft.CodeAnalysis.Diagnostics

            <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
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

        await new VerifyVB.Test
        {
            TestState =
            {
                Sources = { source },
                ExpectedDiagnostics = { expected },
            },
            FixedState = { Sources = { fixedCode_WithVBAttribute } },
            CodeActionIndex = 1,
            CodeActionEquivalenceKey = string.Format(CodeAnalysisDiagnosticsResources.ApplyDiagnosticAnalyzerAttribute_1, LanguageNames.VisualBasic),
        }.RunAsync();

        var fixedCode_WithCSharpAndVBAttributes = """
            Imports System
            Imports System.Collections.Immutable
            Imports Microsoft.CodeAnalysis
            Imports Microsoft.CodeAnalysis.Diagnostics

            <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
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

        await new VerifyVB.Test
        {
            TestState =
            {
                Sources = { source },
                ExpectedDiagnostics = { expected },
            },
            FixedState = { Sources = { fixedCode_WithCSharpAndVBAttributes } },
            CodeActionIndex = 2,
            CodeActionEquivalenceKey = string.Format(CodeAnalysisDiagnosticsResources.ApplyDiagnosticAnalyzerAttribute_2, LanguageNames.CSharp, LanguageNames.VisualBasic),
        }.RunAsync();
    }

    [Fact]
    public Task CSharp_NoDiagnosticCasesAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using System.Collections.Immutable;
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.Diagnostics;

            [DiagnosticAnalyzer(LanguageNames.CSharp)]
            class MyAnalyzerWithLanguageSpecificAttribute : DiagnosticAnalyzer
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

            public abstract class MyAbstractAnalyzerWithoutAttribute : DiagnosticAnalyzer
            {
            }
            """);

    [Fact]
    public Task VisualBasic_NoDiagnosticCasesAsync()
        => VerifyVB.VerifyAnalyzerAsync("""
            Imports System
            Imports System.Collections.Immutable
            Imports Microsoft.CodeAnalysis
            Imports Microsoft.CodeAnalysis.Diagnostics

            <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
            Class MyAnalyzerWithLanguageSpecificAttribute
            	Inherits DiagnosticAnalyzer
            	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
            		Get
            			Throw New NotImplementedException()
            		End Get
            	End Property

            	Public Overrides Sub Initialize(context As AnalysisContext)
            	End Sub
            End Class

            Public MustInherit Class MyAbstractAnalyzerWithoutAttribute
            	Inherits DiagnosticAnalyzer
            End Class
            """);
}
