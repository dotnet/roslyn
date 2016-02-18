// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Analyzers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Analyzers.MetaAnalyzers
{
    public class DoNotStorePerCompilationDataOntoFieldsRuleTests : CodeFixTestBase
    {
        [Fact]
        public void CSharp_VerifyDiagnostic()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using MyNamedType = Microsoft.CodeAnalysis.INamedTypeSymbol;

class MyCompilation : Compilation
{
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly ITypeSymbol x1;
    public static readonly CSharpCompilation x2;
    private readonly List<MyNamedType> x3;
    private static Dictionary<MyCompilation, MyNamedType> x4;

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
}";
            DiagnosticResult[] expected = new[]
            {
                GetCSharpExpectedDiagnostic(17, 29, violatingTypeName: typeof(ITypeSymbol).FullName),
                GetCSharpExpectedDiagnostic(18, 28, violatingTypeName: typeof(CSharpCompilation).FullName),
                GetCSharpExpectedDiagnostic(19, 27, violatingTypeName: typeof(INamedTypeSymbol).FullName),
                GetCSharpExpectedDiagnostic(20, 31, violatingTypeName: "MyCompilation")
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
Imports Microsoft.CodeAnalysis.VisualBasic
Imports MyNamedType = Microsoft.CodeAnalysis.INamedTypeSymbol

Class MyCompilation
    Inherits Compilation
End Class

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Class MyAnalyzer
    Inherits DiagnosticAnalyzer

    Private Shared ReadOnly x1 As ITypeSymbol
    Public Shared ReadOnly x2 As VisualBasicCompilation
    Private ReadOnly x3 As List(Of MyNamedType)
    Private Shared x4 As Dictionary(Of MyCompilation, MyNamedType)

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
    End Sub
End Class
";
            DiagnosticResult[] expected = new[]
            {
                GetBasicExpectedDiagnostic(17, 35, violatingTypeName: typeof(ITypeSymbol).FullName),
                GetBasicExpectedDiagnostic(18, 34, violatingTypeName: typeof(VisualBasicCompilation).FullName),
                GetBasicExpectedDiagnostic(19, 36, violatingTypeName: typeof(INamedTypeSymbol).FullName),
                GetBasicExpectedDiagnostic(20, 40, violatingTypeName: "MyCompilation")
            };

            VerifyBasic(source, expected);
        }

        [Fact]
        public void CSharp_NoDiagnosticCases()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using MyNamedType = Microsoft.CodeAnalysis.INamedTypeSymbol;

class MyCompilation : Compilation
{
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor x1;
    private readonly List<LocalizableResourceString> x2;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public override void Initialize(AnalysisContext context)
    {
        var analyzer = new NestedCompilationAnalyzer();
        context.RegisterCompilationStartAction(analyzer.StartCompilation);
    }

    private class NestedCompilationAnalyzer
    {
        // Ok to store per-compilation data here.
        private readonly Dictionary<MyCompilation, MyNamedType> x;

        internal void StartCompilation(CompilationStartAnalysisContext context)
        {
        }
    }
}

class MyAnalyzerWithoutAttribute : DiagnosticAnalyzer
{
    // Ok to store per-compilation data here.
    private static ITypeSymbol x;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public override void Initialize(AnalysisContext context)
    {
        throw new NotImplementedException();
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
Imports Microsoft.CodeAnalysis.VisualBasic
Imports MyNamedType = Microsoft.CodeAnalysis.INamedTypeSymbol

Class MyCompilation
    Inherits Compilation
End Class

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Class MyAnalyzer
    Inherits DiagnosticAnalyzer

    Private Shared ReadOnly x1 As DiagnosticDescriptor
    Private ReadOnly x2 As List(Of LocalizableResourceString)

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
        Dim compilationAnalyzer = New NestedCompilationAnalyzer
        context.RegisterCompilationStartAction(AddressOf compilationAnalyzer.StartCompilation)
    End Sub

    Class NestedCompilationAnalyzer
        ' Ok to store per-compilation data here.
        Private ReadOnly x As Dictionary(Of MyCompilation, MyNamedType)

        Friend Sub StartCompilation(context As CompilationStartAnalysisContext)
        End Sub
    End Class
End Class

Class MyAnalyzerWithoutAttribute
    Inherits DiagnosticAnalyzer

    ' Ok to store per-compilation data here.
    Private Shared x As ITypeSymbol

    Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
        Throw New NotImplementedException()
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
            return new CSharpDiagnosticAnalyzerFieldsAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicDiagnosticAnalyzerFieldsAnalyzer();
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, string violatingTypeName)
        {
            return GetExpectedDiagnostic(LanguageNames.CSharp, line, column, violatingTypeName);
        }

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, string violatingTypeName)
        {
            return GetExpectedDiagnostic(LanguageNames.VisualBasic, line, column, violatingTypeName);
        }

        private static DiagnosticResult GetExpectedDiagnostic(string language, int line, int column, string violatingTypeName)
        {
            string fileName = language == LanguageNames.CSharp ? "Test0.cs" : "Test0.vb";
            return new DiagnosticResult
            {
                Id = DiagnosticIds.DoNotStorePerCompilationDataOntoFieldsRuleId,
                Message = string.Format(CodeAnalysisDiagnosticsResources.DoNotStorePerCompilationDataOntoFieldsMessage, violatingTypeName),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation(fileName, line, column)
                }
            };
        }
    }
}
