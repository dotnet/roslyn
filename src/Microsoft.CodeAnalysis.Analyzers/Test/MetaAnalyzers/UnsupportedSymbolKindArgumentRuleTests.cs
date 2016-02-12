// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Analyzers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Analyzers.MetaAnalyzers
{
    public class UnsupportedSymbolKindArgumentRuleTests : CodeFixTestBase
    {
        [Fact]
        public void CSharp_VerifyDiagnostic()
        {
            var source = @"
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
            SymbolKind.DynamicType,
            SymbolKind.ErrorType,
            SymbolKind.Label,
            SymbolKind.Local,
            SymbolKind.NetModule,
            SymbolKind.Parameter,
            SymbolKind.PointerType,
            SymbolKind.Preprocessing,
            SymbolKind.RangeVariable,
            SymbolKind.TypeParameter);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
    }

    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
    }
}";
            DiagnosticResult[] expected = new[]
            {
                GetCSharpExpectedDiagnostic(21, 13, unsupportedSymbolKind: SymbolKind.Alias),
                GetCSharpExpectedDiagnostic(22, 13, unsupportedSymbolKind: SymbolKind.ArrayType),
                GetCSharpExpectedDiagnostic(23, 13, unsupportedSymbolKind: SymbolKind.Assembly),
                GetCSharpExpectedDiagnostic(24, 13, unsupportedSymbolKind: SymbolKind.DynamicType),
                GetCSharpExpectedDiagnostic(25, 13, unsupportedSymbolKind: SymbolKind.ErrorType),
                GetCSharpExpectedDiagnostic(26, 13, unsupportedSymbolKind: SymbolKind.Label),
                GetCSharpExpectedDiagnostic(27, 13, unsupportedSymbolKind: SymbolKind.Local),
                GetCSharpExpectedDiagnostic(28, 13, unsupportedSymbolKind: SymbolKind.NetModule),
                GetCSharpExpectedDiagnostic(29, 13, unsupportedSymbolKind: SymbolKind.Parameter),
                GetCSharpExpectedDiagnostic(30, 13, unsupportedSymbolKind: SymbolKind.PointerType),
                GetCSharpExpectedDiagnostic(31, 13, unsupportedSymbolKind: SymbolKind.Preprocessing),
                GetCSharpExpectedDiagnostic(32, 13, unsupportedSymbolKind: SymbolKind.RangeVariable),
                GetCSharpExpectedDiagnostic(33, 13, unsupportedSymbolKind: SymbolKind.TypeParameter),
            };

            VerifyCSharp(source, expected);
        }

        [Fact]
        public void VisualBasic_VerifyRegisterSymbolActionDiagnostic()
        {
            var source = @"
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
            SymbolKind.DynamicType,
            SymbolKind.ErrorType,
            SymbolKind.Label,
            SymbolKind.Local,
            SymbolKind.NetModule,
            SymbolKind.Parameter,
            SymbolKind.PointerType,
            SymbolKind.Preprocessing,
            SymbolKind.RangeVariable,
            SymbolKind.TypeParameter)
    End Sub

    Private Shared Sub AnalyzeSymbol(context As SymbolAnalysisContext)
    End Sub

    Private Shared Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
    End Sub
End Class
";
            DiagnosticResult[] expected = new[]
            {
                GetBasicExpectedDiagnostic(18, 13, unsupportedSymbolKind: SymbolKind.Alias),
                GetBasicExpectedDiagnostic(19, 13, unsupportedSymbolKind: SymbolKind.ArrayType),
                GetBasicExpectedDiagnostic(20, 13, unsupportedSymbolKind: SymbolKind.Assembly),
                GetBasicExpectedDiagnostic(21, 13, unsupportedSymbolKind: SymbolKind.DynamicType),
                GetBasicExpectedDiagnostic(22, 13, unsupportedSymbolKind: SymbolKind.ErrorType),
                GetBasicExpectedDiagnostic(23, 13, unsupportedSymbolKind: SymbolKind.Label),
                GetBasicExpectedDiagnostic(24, 13, unsupportedSymbolKind: SymbolKind.Local),
                GetBasicExpectedDiagnostic(25, 13, unsupportedSymbolKind: SymbolKind.NetModule),
                GetBasicExpectedDiagnostic(26, 13, unsupportedSymbolKind: SymbolKind.Parameter),
                GetBasicExpectedDiagnostic(27, 13, unsupportedSymbolKind: SymbolKind.PointerType),
                GetBasicExpectedDiagnostic(28, 13, unsupportedSymbolKind: SymbolKind.Preprocessing),
                GetBasicExpectedDiagnostic(29, 13, unsupportedSymbolKind: SymbolKind.RangeVariable),
                GetBasicExpectedDiagnostic(30, 13, unsupportedSymbolKind: SymbolKind.TypeParameter),
            };

            VerifyBasic(source, expected);
        }

        [Fact]
        public void CSharp_NoDiagnosticCases()
        {
            var source = @"
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
        context.RegisterSymbolAction(AnalyzeSyntax,
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
}";

            VerifyCSharp(source);
        }

        [Fact]
        public void VisualBasic_NoDiagnosticCases()
        {
            var source = @"
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
        context.RegisterSymbolAction(AddressOf AnalyzeSyntax,
            SymbolKind.Alias)
    End Sub

    Private Shared Sub AnalyzeSymbol(context As SymbolAnalysisContext)
    End Sub

    Private Shared Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
    End Sub
End Class
";

            VerifyBasic(source);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return null;
        }

        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return null;
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpRegisterActionAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicRegisterActionAnalyzer();
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, SymbolKind unsupportedSymbolKind)
        {
            return GetExpectedDiagnostic(LanguageNames.CSharp, line, column, unsupportedSymbolKind);
        }

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, SymbolKind unsupportedSymbolKind)
        {
            return GetExpectedDiagnostic(LanguageNames.VisualBasic, line, column, unsupportedSymbolKind);
        }

        private static DiagnosticResult GetExpectedDiagnostic(string language, int line, int column, SymbolKind unsupportedSymbolKind)
        {
            string fileName = language == LanguageNames.CSharp ? "Test0.cs" : "Test0.vb";
            return new DiagnosticResult
            {
                Id = DiagnosticIds.UnsupportedSymbolKindArgumentRuleId,
                Message = string.Format(CodeAnalysisDiagnosticsResources.UnsupportedSymbolKindArgumentToRegisterActionMessage, unsupportedSymbolKind.ToString()),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation(fileName, line, column)
                }
            };
        }
    }
}
