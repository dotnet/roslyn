﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Analyzers;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Analyzers.MetaAnalyzers
{
    public class InvalidSyntaxKindTypeArgumentRuleTests : CodeFixTestBase
    {
        [Fact]
        public void CSharp_VerifyDiagnostic()
        {
            var source = @"
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
}";
            var expected = new[]
            {
                GetCSharpExpectedDiagnostic(24, 9, typeArgumentName: "Int32", registerMethodName: DiagnosticAnalyzerCorrectnessAnalyzer.RegisterSyntaxNodeActionName),
                GetCSharpExpectedDiagnostic(25, 9, typeArgumentName: "Int32", registerMethodName: DiagnosticAnalyzerCorrectnessAnalyzer.RegisterCodeBlockStartActionName)
            };

            VerifyCSharp(source, expected);
        }

        [Fact]
        public void VisualBasic_VerifyDiagnostic()
        {
            var source = @"
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
";
            var expected = new[]
            {
                GetBasicExpectedDiagnostic(20, 9, typeArgumentName: "Int32", registerMethodName: DiagnosticAnalyzerCorrectnessAnalyzer.RegisterSyntaxNodeActionName),
                GetBasicExpectedDiagnostic(21, 9, typeArgumentName: "Int32", registerMethodName: DiagnosticAnalyzerCorrectnessAnalyzer.RegisterCodeBlockStartActionName)
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
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, null);              // Overload resolution failure
        context.RegisterSyntaxNodeAction<ErrorType>(AnalyzeSyntax, null);   // Error type argument
        context.RegisterCodeBlockStartAction<T>(AnalyzeCodeBlockStart);     // NYI: Type param as a type argument
    }

    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
    }

    private static void AnalyzeCodeBlockStart(CodeBlockStartAnalysisContext<T> context)
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
        context.RegisterSyntaxNodeAction(AddressOf AnalyzeSyntax, Nothing)                  ' Overload resolution failure
        context.RegisterSyntaxNodeAction(Of ErrorType)(AddressOf AnalyzeSyntax, Nothing)    ' Error type argument
        context.RegisterCodeBlockStartAction(Of T)(AddressOf AnalyzeCodeBlockStart)         ' NYI: Type param as a type argument
    End Sub

    Private Shared Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
    End Sub

    Private Shared Sub AnalyzeCodeBlockStart(context As CodeBlockStartAnalysisContext(Of T))
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

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, string typeArgumentName, string registerMethodName)
        {
            return GetExpectedDiagnostic(LanguageNames.CSharp, line, column, typeArgumentName, registerMethodName);
        }

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, string typeArgumentName, string registerMethodName)
        {
            return GetExpectedDiagnostic(LanguageNames.VisualBasic, line, column, typeArgumentName, registerMethodName);
        }

        private static DiagnosticResult GetExpectedDiagnostic(string language, int line, int column, string typeArgumentName, string registerMethodName)
        {
            var fileName = language == LanguageNames.CSharp ? "Test0.cs" : "Test0.vb";
            return new DiagnosticResult
            {
                Id = DiagnosticIds.InvalidSyntaxKindTypeArgumentRuleId,
                Message = string.Format(CodeAnalysisDiagnosticsResources.InvalidSyntaxKindTypeArgumentMessage, typeArgumentName, DiagnosticAnalyzerCorrectnessAnalyzer.TLanguageKindEnumName, registerMethodName),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation(fileName, line, column)
                }
            };
        }
    }
}
