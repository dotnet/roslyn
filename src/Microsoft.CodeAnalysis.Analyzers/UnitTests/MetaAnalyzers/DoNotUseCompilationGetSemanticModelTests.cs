// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public async Task CallInInitialize()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
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
}",
                GetCSharpExpectedDiagnostic(14, 13));

            await VerifyVB.VerifyAnalyzerAsync(@"
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
End Class",
                GetBasicExpectedDiagnostic(18, 17));
        }

        [Fact]
        public async Task CallInSeparateMethod()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
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
}",
                GetCSharpExpectedDiagnostic(19, 33));

            await VerifyVB.VerifyAnalyzerAsync(@"
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
End Class",
                GetBasicExpectedDiagnostic(22, 37));
        }

        [Fact]
        public async Task CastedCall()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
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
}",
                GetCSharpExpectedDiagnostic(16, 13));

            await VerifyVB.VerifyAnalyzerAsync(@"
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
End Class",
                GetBasicExpectedDiagnostic(20, 17));
        }

        [Fact]
        public async Task CallInNonDiagnosticAnalyzerClass()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
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
}");

            await VerifyVB.VerifyAnalyzerAsync(@"
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
End Class");
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column) =>
            VerifyCS.Diagnostic().WithLocation(line, column);

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column) =>
            VerifyVB.Diagnostic().WithLocation(line, column);
    }
}
