// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers;
using Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class StartActionWithNoRegisteredActionsRuleTests : DiagnosticAnalyzerTestBase
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
        context.RegisterCompilationStartAction(compilationContext =>
        {
        });

        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
        context.RegisterCodeBlockStartAction<SyntaxKind>(AnalyzeCodeBlockStart);
        context.RegisterOperationBlockStartAction(AnalyzeOperationBlockStart);
    }

    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
    }

    private static void AnalyzeCodeBlockStart(CodeBlockStartAnalysisContext<SyntaxKind> codeBlockContext)
    {
    }

    private static void AnalyzeOperationBlockStart(OperationBlockStartAnalysisContext operationBlockContext)
    {
    }
}";
            DiagnosticResult[] expected = new[]
            {
                GetCSharpExpectedDiagnostic(21, 48, parameterName: "compilationContext", kind: StartActionKind.CompilationStartAction),
                GetCSharpExpectedDiagnostic(34, 47, parameterName: "codeBlockContext", kind: StartActionKind.CodeBlockStartAction),
                GetCSharpExpectedDiagnostic(38, 52, parameterName: "operationBlockContext", kind: StartActionKind.OperationBlockStartAction)
            };

            VerifyCSharp(source, referenceFlags: ReferenceFlags.None, expected: expected);
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

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Class MyAnalyzer
    Inherits DiagnosticAnalyzer
    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
        context.RegisterCompilationStartAction(
            Sub(compilationContext As CompilationStartAnalysisContext)
            End Sub
        )

        context.RegisterSyntaxNodeAction(AddressOf AnalyzeSyntax, SyntaxKind.InvocationExpression)
        context.RegisterCodeBlockStartAction(Of SyntaxKind)(AddressOf AnalyzeCodeBlockStart)
        context.RegisterOperationBlockStartAction(AddressOf AnalyzeOperationBlockStart)
    End Sub

    Private Shared Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
    End Sub

    Private Shared Sub AnalyzeCodeBlockStart(codeBlockContext As CodeBlockStartAnalysisContext(Of SyntaxKind))
    End Sub

    Private Shared Sub AnalyzeOperationBlockStart(operationBlockContext As OperationBlockStartAnalysisContext)
    End Sub
End Class
";
            DiagnosticResult[] expected = new[]
            {
                GetBasicExpectedDiagnostic(19, 17, parameterName: "compilationContext", kind: StartActionKind.CompilationStartAction),
                GetBasicExpectedDiagnostic(31, 46, parameterName: "codeBlockContext", kind: StartActionKind.CodeBlockStartAction),
                GetBasicExpectedDiagnostic(34, 51, parameterName: "operationBlockContext", kind: StartActionKind.OperationBlockStartAction)
            };

            VerifyBasic(source, referenceFlags: ReferenceFlags.None, expected: expected);
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
        context.RegisterCompilationStartAction(compilationContext =>
        {
            compilationContext.RegisterCodeBlockStartAction<SyntaxKind>(AnalyzeCodeBlockStart);
        });
    }

    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
    }

    private static void AnalyzeCodeBlockStart(CodeBlockStartAnalysisContext<SyntaxKind> context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
    }
}";

            VerifyCSharp(source, referenceFlags: ReferenceFlags.None);
        }

        [Fact]
        public void CSharp_NoDiagnosticCases_2()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

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
        context.RegisterCompilationStartAction(compilationContext =>
        {
            compilationContext.RegisterCodeBlockStartAction<SyntaxKind>(codeBlockContext =>
            {
                AnalyzeCodeBlockStart(codeBlockContext);
            });
        });
    }

    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
    }

    private static void AnalyzeCodeBlockStart(CodeBlockStartAnalysisContext<SyntaxKind> context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
    }
}";

            VerifyCSharp(source, referenceFlags: ReferenceFlags.None);
        }

        [Fact]
        public void CSharp_NoDiagnosticCases_OperationAnalyzerRegistration()
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
        context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
    }

    private static void AnalyzeOperation(OperationAnalysisContext context)
    {
    }
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
class MyAnalyzer2 : DiagnosticAnalyzer
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
        context.RegisterOperationBlockAction(AnalyzeOperationBlock);
    }

    private static void AnalyzeOperationBlock(OperationBlockAnalysisContext context)
    {
    }
}";
            VerifyCSharp(source, referenceFlags: ReferenceFlags.RemoveCodeAnalysis);
        }

        [Fact]
        public void CSharp_NoDiagnosticCases_NestedOperationAnalyzerRegistration()
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
        context.RegisterCompilationStartAction(compilationContext =>
        {
            compilationContext.RegisterOperationBlockStartAction(operationBlockContext =>
            {
                AnalyzeOperationBlockStart(operationBlockContext);
            });
        });

        context.RegisterCompilationStartAction(compilationContext =>
        {
            compilationContext.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
        });

        context.RegisterCompilationStartAction(compilationContext =>
        {
            compilationContext.RegisterOperationBlockAction(AnalyzeOperationBlock);
        });
    }

    private static void AnalyzeOperation(OperationAnalysisContext context)
    {
    }

    private static void AnalyzeOperationBlock(OperationBlockAnalysisContext context)
    {
    }

    private static void AnalyzeOperationBlockStart(OperationBlockStartAnalysisContext context)
    {
        context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
    }
}";

            VerifyCSharp(source, referenceFlags: ReferenceFlags.RemoveCodeAnalysis);
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

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Class MyAnalyzer(Of T As Structure)
    Inherits DiagnosticAnalyzer

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
        context.RegisterCompilationStartAction(
            Sub(compilationContext As CompilationStartAnalysisContext)
                compilationContext.RegisterCodeBlockStartAction(Of SyntaxKind)(AddressOf AnalyzeCodeBlockStart)
            End Sub
        )
    End Sub

    Private Shared Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
    End Sub

    Private Shared Sub AnalyzeCodeBlockStart(context As CodeBlockStartAnalysisContext(Of SyntaxKind))
        context.RegisterSyntaxNodeAction(AddressOf AnalyzeSyntax, SyntaxKind.InvocationExpression)
    End Sub
End Class
";

            VerifyBasic(source, referenceFlags: ReferenceFlags.None);
        }

        [Fact]
        public void VisualBasic_NoDiagnosticCases_2()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Class MyAnalyzer(Of T As Structure)
    Inherits DiagnosticAnalyzer

    Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
        context.RegisterCompilationStartAction(
            Sub(compilationContext As CompilationStartAnalysisContext)
                compilationContext.RegisterCodeBlockStartAction(Of SyntaxKind)(
                    Sub(codeBlockContext As CodeBlockStartAnalysisContext(Of SyntaxKind))
                        AnalyzeCodeBlockStart(codeBlockContext)
                    End Sub
                )
            End Sub
        )
    End Sub

    Private Shared Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
    End Sub

    Private Shared Sub AnalyzeCodeBlockStart(context As CodeBlockStartAnalysisContext(Of SyntaxKind))
        context.RegisterSyntaxNodeAction(AddressOf AnalyzeSyntax, SyntaxKind.InvocationExpression)
    End Sub
End Class
";

            VerifyBasic(source, referenceFlags: ReferenceFlags.None);
        }

        [Fact]
        public void VisualBasic_NoDiagnosticCases_OperationAnalyzerRegistration()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

<DiagnosticAnalyzer(LanguageNames.VisualBasic)> _
Class MyAnalyzer
	Inherits DiagnosticAnalyzer
	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
		Get
			Throw New NotImplementedException()
		End Get
	End Property

	Public Overrides Sub Initialize(context As AnalysisContext)
		context.RegisterOperationAction(AddressOf AnalyzeOperation, OperationKind.Invocation)
	End Sub

	Private Shared Sub AnalyzeOperation(context As OperationAnalysisContext)
	End Sub
End Class

<DiagnosticAnalyzer(LanguageNames.VisualBasic)> _
Class MyAnalyzer2
	Inherits DiagnosticAnalyzer
	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
		Get
			Throw New NotImplementedException()
		End Get
	End Property

	Public Overrides Sub Initialize(context As AnalysisContext)
		context.RegisterOperationBlockAction(AddressOf AnalyzeOperationBlock)
	End Sub

	Private Shared Sub AnalyzeOperationBlock(context As OperationBlockAnalysisContext)
	End Sub
End Class
";
            VerifyBasic(source, referenceFlags: ReferenceFlags.RemoveCodeAnalysis);
        }

        [Fact]
        public void VisualBasic_NoDiagnosticCases_NestedOperationAnalyzerRegistration()
        {
            var source = @"
Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

<DiagnosticAnalyzer(LanguageNames.VisualBasic)> _
MustInherit Class MyAnalyzer
	Inherits DiagnosticAnalyzer
	Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
		Get
			Throw New NotImplementedException()
		End Get
	End Property

	Public Overrides Sub Initialize(context As AnalysisContext)
		context.RegisterCompilationStartAction(Function(compilationContext) 
      		                                        compilationContext.RegisterOperationBlockStartAction(Function(operationBlockContext) 
		                                                                                                    AnalyzeOperationBlockStart(operationBlockContext)
                                                                                                         End Function)
                                               End Function)

		context.RegisterCompilationStartAction(Function(compilationContext) 
		                                         compilationContext.RegisterOperationAction(AddressOf AnalyzeOperation, OperationKind.Invocation)
                                               End Function)

		context.RegisterCompilationStartAction(Function(compilationContext) 
		                                            compilationContext.RegisterOperationBlockAction(AddressOf AnalyzeOperationBlock)
                                               End Function)
	End Sub

	Private Shared Sub AnalyzeOperation(context As OperationAnalysisContext)
	End Sub

	Private Shared Sub AnalyzeOperationBlock(context As OperationBlockAnalysisContext)
	End Sub

	Private Shared Sub AnalyzeOperationBlockStart(context As OperationBlockStartAnalysisContext)
		context.RegisterOperationAction(AddressOf AnalyzeOperation, OperationKind.Invocation)
	End Sub
End Class
";

            VerifyBasic(source, referenceFlags: ReferenceFlags.RemoveCodeAnalysis);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpRegisterActionAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicRegisterActionAnalyzer();
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, string parameterName, StartActionKind kind)
        {
            return GetExpectedDiagnostic(LanguageNames.CSharp, line, column, parameterName, kind);
        }

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, string parameterName, StartActionKind kind)
        {
            return GetExpectedDiagnostic(LanguageNames.VisualBasic, line, column, parameterName, kind);
        }

        private static DiagnosticResult GetExpectedDiagnostic(string language, int line, int column, string parameterName, StartActionKind kind)
        {
            string arg2;
            switch (kind)
            {
                case StartActionKind.CompilationStartAction:
                    arg2 = "Initialize";
                    break;

                case StartActionKind.CodeBlockStartAction:
                case StartActionKind.OperationBlockStartAction:
                    arg2 = "Initialize, CompilationStartAction";
                    break;

                default:
                    throw new ArgumentException("Unsupported action kind", nameof(kind));
            }

            string message = string.Format(CultureInfo.CurrentCulture, CodeAnalysisDiagnosticsResources.StartActionWithNoRegisteredActionsMessage, parameterName, arg2);

            string fileName = language == LanguageNames.CSharp ? "Test0.cs" : "Test0.vb";
            return new DiagnosticResult(DiagnosticIds.StartActionWithNoRegisteredActionsRuleId, DiagnosticHelpers.DefaultDiagnosticSeverity)
                .WithLocation(fileName, line, column)
                .WithMessageFormat(message);
        }

        private enum StartActionKind
        {
            CompilationStartAction,
            CodeBlockStartAction,
            OperationBlockStartAction
        }
    }
}
