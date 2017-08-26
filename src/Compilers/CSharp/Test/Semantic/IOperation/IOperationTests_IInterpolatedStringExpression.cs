// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_Empty()
        {
            string source = @"
using System;

internal class Class
{
    public void M()
    {
        Console.WriteLine(/*<bind>*/$""""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""""')
  Parts(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_OnlyTextPart()
        {
            string source = @"
using System;

internal class Class
{
    public void M()
    {
        Console.WriteLine(/*<bind>*/$""Only text part""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""Only text part""')
  Parts(1):
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'Only text part')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""Only text part"") (Syntax: 'Only text part')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_OnlyInterpolationPart()
        {
            string source = @"
using System;

internal class Class
{
    public void M()
    {
        Console.WriteLine(/*<bind>*/$""{1}""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""{1}""')
  Parts(1):
      IInterpolation (OperationKind.Interpolation) (Syntax: '{1}')
        Expression: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: null
        FormatString: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_EmptyInterpolationPart()
        {
            string source = @"
using System;

internal class Class
{
    public void M()
    {
        Console.WriteLine(/*<bind>*/$""{}""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String, IsInvalid) (Syntax: '$""{}""')
  Parts(1):
      IInterpolation (OperationKind.Interpolation, IsInvalid) (Syntax: '{}')
        Expression: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
            Children(0)
        Alignment: null
        FormatString: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1733: Expected expression
                //         Console.WriteLine(/*<bind>*/$"{}"/*</bind>*/);
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(8, 40)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_TextAndInterpolationParts()
        {
            string source = @"
using System;

internal class Class
{
    public void M(int x)
    {
        Console.WriteLine(/*<bind>*/$""String {x} and constant {1}""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""String {x ... nstant {1}""')
  Parts(4):
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'String ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""String "") (Syntax: 'String ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{x}')
        Expression: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Alignment: null
        FormatString: null
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: ' and constant ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "" and constant "") (Syntax: ' and constant ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{1}')
        Expression: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: null
        FormatString: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_FormatAndAlignment()
        {
            string source = @"
using System;

internal class Class
{
    private string x = string.Empty;
    private int y = 0;

    public void M()
    {
        Console.WriteLine(/*<bind>*/$""String {x,20} and {y:D3} and constant {1}""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""String {x ... nstant {1}""')
  Parts(6):
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'String ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""String "") (Syntax: 'String ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{x,20}')
        Expression: IFieldReferenceExpression: System.String Class.x (OperationKind.FieldReferenceExpression, Type: System.String) (Syntax: 'x')
            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Class) (Syntax: 'x')
        Alignment: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20) (Syntax: '20')
        FormatString: null
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: ' and ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "" and "") (Syntax: ' and ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{y:D3}')
        Expression: IFieldReferenceExpression: System.Int32 Class.y (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'y')
            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Class) (Syntax: 'y')
        Alignment: null
        FormatString: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""D3"") (Syntax: ':D3')
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: ' and constant ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "" and constant "") (Syntax: ' and constant ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{1}')
        Expression: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: null
        FormatString: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_InterpolationAndFormatAndAlignment()
        {
            string source = @"
using System;

internal class Class
{
    private string x = string.Empty;
    private const int y = 0;

    public void M()
    {
        Console.WriteLine(/*<bind>*/$""String {x,y:D3}""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""String {x,y:D3}""')
  Parts(2):
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'String ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""String "") (Syntax: 'String ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{x,y:D3}')
        Expression: IFieldReferenceExpression: System.String Class.x (OperationKind.FieldReferenceExpression, Type: System.String) (Syntax: 'x')
            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Class) (Syntax: 'x')
        Alignment: IFieldReferenceExpression: System.Int32 Class.y (Static) (OperationKind.FieldReferenceExpression, Type: System.Int32, Constant: 0) (Syntax: 'y')
            Instance Receiver: null
        FormatString: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""D3"") (Syntax: ':D3')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_InvocationInInterpolation()
        {
            string source = @"
using System;

internal class Class
{
    public void M()
    {
        string x = string.Empty;
        int y = 0;
        Console.WriteLine(/*<bind>*/$""String {x} and {M2(y)} and constant {1}""/*</bind>*/);
    }

    private string M2(int z) => z.ToString();
}
";
            string expectedOperationTree = @"
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""String {x ... nstant {1}""')
  Parts(6):
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'String ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""String "") (Syntax: 'String ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{x}')
        Expression: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'x')
        Alignment: null
        FormatString: null
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: ' and ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "" and "") (Syntax: ' and ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{M2(y)}')
        Expression: IInvocationExpression ( System.String Class.M2(System.Int32 z)) (OperationKind.InvocationExpression, Type: System.String) (Syntax: 'M2(y)')
            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Class) (Syntax: 'M2')
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument) (Syntax: 'y')
                  ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
        Alignment: null
        FormatString: null
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: ' and constant ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "" and constant "") (Syntax: ' and constant ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{1}')
        Expression: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: null
        FormatString: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_NestedInterpolation()
        {
            string source = @"
using System;

internal class Class
{
    public void M()
    {
        string x = string.Empty;
        int y = 0;
        Console.WriteLine(/*<bind>*/$""String {M2($""{y}"")}""/*</bind>*/);
    }

    private int M2(string z) => Int32.Parse(z);
}
";
            string expectedOperationTree = @"
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""String {M2($""{y}"")}""')
  Parts(2):
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'String ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""String "") (Syntax: 'String ')
      IInterpolation (OperationKind.Interpolation) (Syntax: '{M2($""{y}"")}')
        Expression: IInvocationExpression ( System.Int32 Class.M2(System.String z)) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'M2($""{y}"")')
            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Class) (Syntax: 'M2')
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument) (Syntax: '$""{y}""')
                  IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""{y}""')
                    Parts(1):
                        IInterpolation (OperationKind.Interpolation) (Syntax: '{y}')
                          Expression: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
                          Alignment: null
                          FormatString: null
        Alignment: null
        FormatString: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_InvalidExpressionInInterpolation()
        {
            string source = @"
using System;

internal class Class
{
    public void M(int x)
    {
        Console.WriteLine(/*<bind>*/$""String {x1} and constant {Class}""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String, IsInvalid) (Syntax: '$""String {x ... nt {Class}""')
  Parts(4):
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'String ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""String "") (Syntax: 'String ')
      IInterpolation (OperationKind.Interpolation, IsInvalid) (Syntax: '{x1}')
        Expression: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'x1')
            Children(0)
        Alignment: null
        FormatString: null
      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: ' and constant ')
        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "" and constant "") (Syntax: ' and constant ')
      IInterpolation (OperationKind.Interpolation, IsInvalid) (Syntax: '{Class}')
        Expression: IInvalidExpression (OperationKind.InvalidExpression, Type: Class, IsInvalid) (Syntax: 'Class')
            Children(1):
                IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'Class')
        Alignment: null
        FormatString: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'x1' does not exist in the current context
                //         Console.WriteLine(/*<bind>*/$"String {x1} and constant {Class}"/*</bind>*/);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(8, 47),
                // CS0119: 'Class' is a type, which is not valid in the given context
                //         Console.WriteLine(/*<bind>*/$"String {x1} and constant {Class}"/*</bind>*/);
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Class").WithArguments("Class", "type").WithLocation(8, 65)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
