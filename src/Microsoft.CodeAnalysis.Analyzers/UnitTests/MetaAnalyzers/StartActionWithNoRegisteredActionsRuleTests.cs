// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.CSharpRegisterActionAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers.BasicRegisterActionAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class StartActionWithNoRegisteredActionsRuleTests
    {
        [Fact]
        public async Task CSharp_VerifyDiagnostic()
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

            await VerifyCS.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task VisualBasic_VerifyDiagnostic()
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

            await VerifyVB.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task CSharp_NoDiagnosticCases()
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

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task CSharp_NoDiagnosticCases_2()
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

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task CSharp_NoDiagnosticCases_OperationAnalyzerRegistration()
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
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Default,
                TestState =
                {
                    Sources = { source },
                    ExpectedDiagnostics =
                    {
                        // Test0.cs(3,26): error CS0234: The type or namespace name 'Immutable' does not exist in the namespace 'System.Collections' (are you missing an assembly reference?)
                        DiagnosticResult.CompilerError("CS0234").WithSpan(3, 26, 3, 35).WithArguments("Immutable", "System.Collections"),
                        // Test0.cs(4,17): error CS0234: The type or namespace name 'CodeAnalysis' does not exist in the namespace 'Microsoft' (are you missing an assembly reference?)
                        DiagnosticResult.CompilerError("CS0234").WithSpan(4, 17, 4, 29).WithArguments("CodeAnalysis", "Microsoft"),
                        // Test0.cs(5,17): error CS0234: The type or namespace name 'CodeAnalysis' does not exist in the namespace 'Microsoft' (are you missing an assembly reference?)
                        DiagnosticResult.CompilerError("CS0234").WithSpan(5, 17, 5, 29).WithArguments("CodeAnalysis", "Microsoft"),
                        // Test0.cs(7,2): error CS0246: The type or namespace name 'DiagnosticAnalyzer' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(7, 2, 7, 20).WithArguments("DiagnosticAnalyzer"),
                        // Test0.cs(7,2): error CS0246: The type or namespace name 'DiagnosticAnalyzerAttribute' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(7, 2, 7, 20).WithArguments("DiagnosticAnalyzerAttribute"),
                        // Test0.cs(7,21): error CS0103: The name 'LanguageNames' does not exist in the current context
                        DiagnosticResult.CompilerError("CS0103").WithSpan(7, 21, 7, 34).WithArguments("LanguageNames"),
                        // Test0.cs(8,20): error CS0246: The type or namespace name 'DiagnosticAnalyzer' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(8, 20, 8, 38).WithArguments("DiagnosticAnalyzer"),
                        // Test0.cs(10,21): error CS0246: The type or namespace name 'ImmutableArray<>' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(10, 21, 10, 57).WithArguments("ImmutableArray<>"),
                        // Test0.cs(10,36): error CS0246: The type or namespace name 'DiagnosticDescriptor' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(10, 36, 10, 56).WithArguments("DiagnosticDescriptor"),
                        // Test0.cs(10,58): error CS0115: 'MyAnalyzer.SupportedDiagnostics': no suitable method found to override
                        DiagnosticResult.CompilerError("CS0115").WithSpan(10, 58, 10, 78).WithArguments("MyAnalyzer.SupportedDiagnostics"),
                        // Test0.cs(18,37): error CS0246: The type or namespace name 'AnalysisContext' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(18, 37, 18, 52).WithArguments("AnalysisContext"),
                        // Test0.cs(20,59): error CS0103: The name 'OperationKind' does not exist in the current context
                        DiagnosticResult.CompilerError("CS0103").WithSpan(20, 59, 20, 72).WithArguments("OperationKind"),
                        // Test0.cs(23,42): error CS0246: The type or namespace name 'OperationAnalysisContext' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(23, 42, 23, 66).WithArguments("OperationAnalysisContext"),
                        // Test0.cs(28,2): error CS0246: The type or namespace name 'DiagnosticAnalyzer' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(28, 2, 28, 20).WithArguments("DiagnosticAnalyzer"),
                        // Test0.cs(28,2): error CS0246: The type or namespace name 'DiagnosticAnalyzerAttribute' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(28, 2, 28, 20).WithArguments("DiagnosticAnalyzerAttribute"),
                        // Test0.cs(28,21): error CS0103: The name 'LanguageNames' does not exist in the current context
                        DiagnosticResult.CompilerError("CS0103").WithSpan(28, 21, 28, 34).WithArguments("LanguageNames"),
                        // Test0.cs(29,21): error CS0246: The type or namespace name 'DiagnosticAnalyzer' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(29, 21, 29, 39).WithArguments("DiagnosticAnalyzer"),
                        // Test0.cs(31,21): error CS0246: The type or namespace name 'ImmutableArray<>' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(31, 21, 31, 57).WithArguments("ImmutableArray<>"),
                        // Test0.cs(31,36): error CS0246: The type or namespace name 'DiagnosticDescriptor' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(31, 36, 31, 56).WithArguments("DiagnosticDescriptor"),
                        // Test0.cs(31,58): error CS0115: 'MyAnalyzer2.SupportedDiagnostics': no suitable method found to override
                        DiagnosticResult.CompilerError("CS0115").WithSpan(31, 58, 31, 78).WithArguments("MyAnalyzer2.SupportedDiagnostics"),
                        // Test0.cs(39,37): error CS0246: The type or namespace name 'AnalysisContext' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(39, 37, 39, 52).WithArguments("AnalysisContext"),
                        // Test0.cs(44,47): error CS0246: The type or namespace name 'OperationBlockAnalysisContext' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(44, 47, 44, 76).WithArguments("OperationBlockAnalysisContext")
                    },
                }
            }.RunAsync();
        }

        [Fact]
        public async Task CSharp_NoDiagnosticCases_NestedOperationAnalyzerRegistration()
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

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Default,
                TestState =
                {
                    Sources = { source },
                    ExpectedDiagnostics =
                    {
                        // Test0.cs(3,26): error CS0234: The type or namespace name 'Immutable' does not exist in the namespace 'System.Collections' (are you missing an assembly reference?)
                        DiagnosticResult.CompilerError("CS0234").WithSpan(3, 26, 3, 35).WithArguments("Immutable", "System.Collections"),
                        // Test0.cs(4,17): error CS0234: The type or namespace name 'CodeAnalysis' does not exist in the namespace 'Microsoft' (are you missing an assembly reference?)
                        DiagnosticResult.CompilerError("CS0234").WithSpan(4, 17, 4, 29).WithArguments("CodeAnalysis", "Microsoft"),
                        // Test0.cs(5,17): error CS0234: The type or namespace name 'CodeAnalysis' does not exist in the namespace 'Microsoft' (are you missing an assembly reference?)
                        DiagnosticResult.CompilerError("CS0234").WithSpan(5, 17, 5, 29).WithArguments("CodeAnalysis", "Microsoft"),
                        // Test0.cs(7,2): error CS0246: The type or namespace name 'DiagnosticAnalyzer' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(7, 2, 7, 20).WithArguments("DiagnosticAnalyzer"),
                        // Test0.cs(7,2): error CS0246: The type or namespace name 'DiagnosticAnalyzerAttribute' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(7, 2, 7, 20).WithArguments("DiagnosticAnalyzerAttribute"),
                        // Test0.cs(7,21): error CS0103: The name 'LanguageNames' does not exist in the current context
                        DiagnosticResult.CompilerError("CS0103").WithSpan(7, 21, 7, 34).WithArguments("LanguageNames"),
                        // Test0.cs(8,20): error CS0246: The type or namespace name 'DiagnosticAnalyzer' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(8, 20, 8, 38).WithArguments("DiagnosticAnalyzer"),
                        // Test0.cs(10,21): error CS0246: The type or namespace name 'ImmutableArray<>' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(10, 21, 10, 57).WithArguments("ImmutableArray<>"),
                        // Test0.cs(10,36): error CS0246: The type or namespace name 'DiagnosticDescriptor' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(10, 36, 10, 56).WithArguments("DiagnosticDescriptor"),
                        // Test0.cs(10,58): error CS0115: 'MyAnalyzer.SupportedDiagnostics': no suitable method found to override
                        DiagnosticResult.CompilerError("CS0115").WithSpan(10, 58, 10, 78).WithArguments("MyAnalyzer.SupportedDiagnostics"),
                        // Test0.cs(18,37): error CS0246: The type or namespace name 'AnalysisContext' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(18, 37, 18, 52).WithArguments("AnalysisContext"),
                        // Test0.cs(39,42): error CS0246: The type or namespace name 'OperationAnalysisContext' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(39, 42, 39, 66).WithArguments("OperationAnalysisContext"),
                        // Test0.cs(43,47): error CS0246: The type or namespace name 'OperationBlockAnalysisContext' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(43, 47, 43, 76).WithArguments("OperationBlockAnalysisContext"),
                        // Test0.cs(47,52): error CS0246: The type or namespace name 'OperationBlockStartAnalysisContext' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(47, 52, 47, 86).WithArguments("OperationBlockStartAnalysisContext"),
                        // Test0.cs(49,59): error CS0103: The name 'OperationKind' does not exist in the current context
                        DiagnosticResult.CompilerError("CS0103").WithSpan(49, 59, 49, 72).WithArguments("OperationKind"),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task VisualBasic_NoDiagnosticCases()
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

            await VerifyVB.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task VisualBasic_NoDiagnosticCases_2()
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

            await VerifyVB.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task VisualBasic_NoDiagnosticCases_OperationAnalyzerRegistration()
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
            await new VerifyVB.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Default,
                TestState =
                {
                    Sources = { source },
                    ExpectedDiagnostics =
                    {
                        // Test0.vb(7) : error BC30002: Type 'DiagnosticAnalyzer' is not defined.
                        DiagnosticResult.CompilerError("BC30002").WithSpan(7, 2, 7, 20).WithArguments("DiagnosticAnalyzer"),
                        // Test0.vb(7) : error BC30451: 'LanguageNames' is not declared. It may be inaccessible due to its protection level.
                        DiagnosticResult.CompilerError("BC30451").WithSpan(7, 21, 7, 34).WithArguments("LanguageNames"),
                        // Test0.vb(9) : error BC30002: Type 'DiagnosticAnalyzer' is not defined.
                        DiagnosticResult.CompilerError("BC30002").WithSpan(9, 11, 9, 29).WithArguments("DiagnosticAnalyzer"),
                        // Test0.vb(10) : error BC30284: property 'SupportedDiagnostics' cannot be declared 'Overrides' because it does not override a property in a base class.
                        DiagnosticResult.CompilerError("BC30284").WithSpan(10, 37, 10, 57).WithArguments("property", "SupportedDiagnostics"),
                        // Test0.vb(10) : error BC30002: Type 'ImmutableArray' is not defined.
                        DiagnosticResult.CompilerError("BC30002").WithSpan(10, 63, 10, 102).WithArguments("ImmutableArray"),
                        // Test0.vb(10) : error BC30002: Type 'DiagnosticDescriptor' is not defined.
                        DiagnosticResult.CompilerError("BC30002").WithSpan(10, 81, 10, 101).WithArguments("DiagnosticDescriptor"),
                        // Test0.vb(16) : error BC30284: sub 'Initialize' cannot be declared 'Overrides' because it does not override a sub in a base class.
                        DiagnosticResult.CompilerError("BC30284").WithSpan(16, 23, 16, 33).WithArguments("sub", "Initialize"),
                        // Test0.vb(16) : error BC30002: Type 'AnalysisContext' is not defined.
                        DiagnosticResult.CompilerError("BC30002").WithSpan(16, 45, 16, 60).WithArguments("AnalysisContext"),
                        // Test0.vb(17) : error BC30451: 'OperationKind' is not declared. It may be inaccessible due to its protection level.
                        DiagnosticResult.CompilerError("BC30451").WithSpan(17, 63, 17, 76).WithArguments("OperationKind"),
                        // Test0.vb(20) : error BC30002: Type 'OperationAnalysisContext' is not defined.
                        DiagnosticResult.CompilerError("BC30002").WithSpan(20, 49, 20, 73).WithArguments("OperationAnalysisContext"),
                        // Test0.vb(24) : error BC30002: Type 'DiagnosticAnalyzer' is not defined.
                        DiagnosticResult.CompilerError("BC30002").WithSpan(24, 2, 24, 20).WithArguments("DiagnosticAnalyzer"),
                        // Test0.vb(24) : error BC30451: 'LanguageNames' is not declared. It may be inaccessible due to its protection level.
                        DiagnosticResult.CompilerError("BC30451").WithSpan(24, 21, 24, 34).WithArguments("LanguageNames"),
                        // Test0.vb(26) : error BC30002: Type 'DiagnosticAnalyzer' is not defined.
                        DiagnosticResult.CompilerError("BC30002").WithSpan(26, 11, 26, 29).WithArguments("DiagnosticAnalyzer"),
                        // Test0.vb(27) : error BC30284: property 'SupportedDiagnostics' cannot be declared 'Overrides' because it does not override a property in a base class.
                        DiagnosticResult.CompilerError("BC30284").WithSpan(27, 37, 27, 57).WithArguments("property", "SupportedDiagnostics"),
                        // Test0.vb(27) : error BC30002: Type 'ImmutableArray' is not defined.
                        DiagnosticResult.CompilerError("BC30002").WithSpan(27, 63, 27, 102).WithArguments("ImmutableArray"),
                        // Test0.vb(27) : error BC30002: Type 'DiagnosticDescriptor' is not defined.
                        DiagnosticResult.CompilerError("BC30002").WithSpan(27, 81, 27, 101).WithArguments("DiagnosticDescriptor"),
                        // Test0.vb(33) : error BC30284: sub 'Initialize' cannot be declared 'Overrides' because it does not override a sub in a base class.
                        DiagnosticResult.CompilerError("BC30284").WithSpan(33, 23, 33, 33).WithArguments("sub", "Initialize"),
                        // Test0.vb(33) : error BC30002: Type 'AnalysisContext' is not defined.
                        DiagnosticResult.CompilerError("BC30002").WithSpan(33, 45, 33, 60).WithArguments("AnalysisContext"),
                        // Test0.vb(37) : error BC30002: Type 'OperationBlockAnalysisContext' is not defined.
                        DiagnosticResult.CompilerError("BC30002").WithSpan(37, 54, 37, 83).WithArguments("OperationBlockAnalysisContext")
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task VisualBasic_NoDiagnosticCases_NestedOperationAnalyzerRegistration()
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

            await new VerifyVB.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Default,
                TestState =
                {
                    Sources = { source },
                    ExpectedDiagnostics =
                    {
                        // Test0.vb(7) : error BC30002: Type 'DiagnosticAnalyzer' is not defined.
                        DiagnosticResult.CompilerError("BC30002").WithSpan(7, 2, 7, 20).WithArguments("DiagnosticAnalyzer"),
                        // Test0.vb(7) : error BC30451: 'LanguageNames' is not declared. It may be inaccessible due to its protection level.
                        DiagnosticResult.CompilerError("BC30451").WithSpan(7, 21, 7, 34).WithArguments("LanguageNames"),
                        // Test0.vb(9) : error BC30002: Type 'DiagnosticAnalyzer' is not defined.
                        DiagnosticResult.CompilerError("BC30002").WithSpan(9, 11, 9, 29).WithArguments("DiagnosticAnalyzer"),
                        // Test0.vb(10) : error BC30284: property 'SupportedDiagnostics' cannot be declared 'Overrides' because it does not override a property in a base class.
                        DiagnosticResult.CompilerError("BC30284").WithSpan(10, 37, 10, 57).WithArguments("property", "SupportedDiagnostics"),
                        // Test0.vb(10) : error BC30002: Type 'ImmutableArray' is not defined.
                        DiagnosticResult.CompilerError("BC30002").WithSpan(10, 63, 10, 102).WithArguments("ImmutableArray"),
                        // Test0.vb(10) : error BC30002: Type 'DiagnosticDescriptor' is not defined.
                        DiagnosticResult.CompilerError("BC30002").WithSpan(10, 81, 10, 101).WithArguments("DiagnosticDescriptor"),
                        // Test0.vb(16) : error BC30284: sub 'Initialize' cannot be declared 'Overrides' because it does not override a sub in a base class.
                        DiagnosticResult.CompilerError("BC30284").WithSpan(16, 23, 16, 33).WithArguments("sub", "Initialize"),
                        // Test0.vb(16) : error BC30002: Type 'AnalysisContext' is not defined.
                        DiagnosticResult.CompilerError("BC30002").WithSpan(16, 45, 16, 60).WithArguments("AnalysisContext"),
                        // Test0.vb(32) : error BC30002: Type 'OperationAnalysisContext' is not defined.
                        DiagnosticResult.CompilerError("BC30002").WithSpan(32, 49, 32, 73).WithArguments("OperationAnalysisContext"),
                        // Test0.vb(35) : error BC30002: Type 'OperationBlockAnalysisContext' is not defined.
                        DiagnosticResult.CompilerError("BC30002").WithSpan(35, 54, 35, 83).WithArguments("OperationBlockAnalysisContext"),
                        // Test0.vb(38) : error BC30002: Type 'OperationBlockStartAnalysisContext' is not defined.
                        DiagnosticResult.CompilerError("BC30002").WithSpan(38, 59, 38, 93).WithArguments("OperationBlockStartAnalysisContext"),
                        // Test0.vb(39) : error BC30451: 'OperationKind' is not declared. It may be inaccessible due to its protection level.
                        DiagnosticResult.CompilerError("BC30451").WithSpan(39, 63, 39, 76).WithArguments("OperationKind")
                    },
                }
            }.RunAsync();
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column, string parameterName, StartActionKind kind)
        {
            return GetExpectedDiagnostic(line, column, parameterName, kind);
        }

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column, string parameterName, StartActionKind kind)
        {
            return GetExpectedDiagnostic(line, column, parameterName, kind);
        }

        private static DiagnosticResult GetExpectedDiagnostic(int line, int column, string parameterName, StartActionKind kind)
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

            return new DiagnosticResult(DiagnosticIds.StartActionWithNoRegisteredActionsRuleId, DiagnosticHelpers.DefaultDiagnosticSeverity)
                .WithLocation(line, column)
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
