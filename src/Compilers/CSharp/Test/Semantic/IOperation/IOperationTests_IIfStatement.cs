// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementSimpleIf()
        {
            string source = @"
class P
{
    private void M()
    {
        bool condition = false;
        /*<bind>*/if (true)
        {
            condition = true;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement) (Syntax: 'if (true) ... }')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'condition = true;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean) (Syntax: 'condition = true')
            Left: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'condition')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
  IfFalse: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'condition' is assigned but its value is never used
                //         bool condition = false;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "condition").WithArguments("condition").WithLocation(6, 14)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementSimpleIfWithElse()
        {
            string source = @"
class P
{
    private void M()
    {
        bool condition = false;
        /*<bind>*/if (true)
        {
            condition = true;
        }
        else
        {
            condition = false;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement) (Syntax: 'if (true) ... }')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'condition = true;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean) (Syntax: 'condition = true')
            Left: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'condition')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
  IfFalse: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'condition = false;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean) (Syntax: 'condition = false')
            Left: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'condition')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False) (Syntax: 'false')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0162: Unreachable code detected
                //             condition = false;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "condition").WithLocation(13, 13),
                // CS0219: The variable 'condition' is assigned but its value is never used
                //         bool condition = false;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "condition").WithArguments("condition").WithLocation(6, 14)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementSimpleIfWithConditionEvaluationTrue()
        {
            string source = @"
class P
{
    private void M()
    {
        bool condition = false;
        /*<bind>*/if (1 == 1)
        {
            condition = true;
        }/*</bind>*/

    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement) (Syntax: 'if (1 == 1) ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Constant: True) (Syntax: '1 == 1')
      Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'condition = true;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean) (Syntax: 'condition = true')
            Left: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'condition')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
  IfFalse: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'condition' is assigned but its value is never used
                //         bool condition = false;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "condition").WithArguments("condition").WithLocation(6, 14)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);

        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementSimpleIfNested1()
        {
            string source = @"
using System;
class P
{
    private void M()
    {
        int m = 12;
        int n = 18;
        /*<bind>*/if (m > 10)
        {
            if (n > 20)
                Console.WriteLine(m);
        }
        else
        {
            Console.WriteLine(n);
        }/*</bind>*/

    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement) (Syntax: 'if (m > 10) ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'm > 10')
      Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'm')
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'if (n > 20) ... iteLine(m);')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'n > 20')
            Left: ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'n')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20) (Syntax: '20')
        IfTrue: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(m);')
            Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(m)')
                Instance Receiver: null
                Arguments(1):
                    IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'm')
                      ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'm')
                      InConversion: null
                      OutConversion: null
        IfFalse: null
  IfFalse: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(n);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(n)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'n')
                  ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'n')
                  InConversion: null
                  OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementSimpleIfNested2()
        {
            string source = @"
using System;
class P
{
    private void M()
    {
        int m = 9;
        int n = 7;
        /*<bind>*/if (m > 10)
            if (n > 20)
            {
                Console.WriteLine(m);
            }
            else
            {
                Console.WriteLine(n);
            }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement) (Syntax: 'if (m > 10) ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'm > 10')
      Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'm')
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  IfTrue: IIfStatement (OperationKind.IfStatement) (Syntax: 'if (n > 20) ... }')
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'n > 20')
          Left: ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'n')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20) (Syntax: '20')
      IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
          IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(m);')
            Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(m)')
                Instance Receiver: null
                Arguments(1):
                    IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'm')
                      ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'm')
                      InConversion: null
                      OutConversion: null
      IfFalse: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
          IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(n);')
            Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(n)')
                Instance Receiver: null
                Arguments(1):
                    IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'n')
                      ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'n')
                      InConversion: null
                      OutConversion: null
  IfFalse: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithMultipleCondition()
        {
            string source = @"
using System;
class P
{
    private void M()
    {
        int m = 9;
        int n = 7;
        int p = 5;
        /*<bind>*/if (m >= n && m >= p)
        {
            Console.WriteLine(""Nothing is larger than m."");
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement) (Syntax: 'if (m >= n  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.BooleanConditionalAnd) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'm >= n && m >= p')
      Left: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'm >= n')
          Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'm')
          Right: ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'n')
      Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'm >= p')
          Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'm')
          Right: ILocalReferenceExpression: p (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'p')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ...  than m."");')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... r than m."")')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""Nothing is ... er than m.""')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""Nothing is larger than m."") (Syntax: '""Nothing is ... er than m.""')
                  InConversion: null
                  OutConversion: null
  IfFalse: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithElseIfCondition()
        {
            string source = @"
using System;
class P
{
    private void M()
    {
        int m = 9;
        int n = 7;
        /*<bind>*/if (n > 20)
        {
            Console.WriteLine(""Result1"");
        }
        else if (m > 10)
        {
            Console.WriteLine(""Result2"");
        }
        else
        {
            Console.WriteLine(""Result3"");
        }/*</bind>*/

    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement) (Syntax: 'if (n > 20) ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'n > 20')
      Left: ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'n')
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20) (Syntax: '20')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ""Result1"");')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... (""Result1"")')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""Result1""')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""Result1"") (Syntax: '""Result1""')
                  InConversion: null
                  OutConversion: null
  IfFalse: IIfStatement (OperationKind.IfStatement) (Syntax: 'if (m > 10) ... }')
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'm > 10')
          Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'm')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
      IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
          IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ""Result2"");')
            Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... (""Result2"")')
                Instance Receiver: null
                Arguments(1):
                    IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""Result2""')
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""Result2"") (Syntax: '""Result2""')
                      InConversion: null
                      OutConversion: null
      IfFalse: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
          IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ""Result3"");')
            Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... (""Result3"")')
                Instance Receiver: null
                Arguments(1):
                    IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""Result3""')
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""Result3"") (Syntax: '""Result3""')
                      InConversion: null
                      OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithElseIfConditionOutVar()
        {
            string source = @"
class P
{
    private void M()
    {
        var s = """";
        /*<bind>*/if (int.TryParse(s, out var i))
            System.Console.WriteLine($""i ={i}, s ={s}"");
        else
            System.Console.WriteLine($""i ={i}, s ={s}"");/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement) (Syntax: 'if (int.Try ... , s ={s}"");')
  Condition: IInvocationExpression (System.Boolean System.Int32.TryParse(System.String s, out System.Int32 result)) (OperationKind.InvocationExpression, Type: System.Boolean) (Syntax: 'int.TryPars ...  out var i)')
      Instance Receiver: null
      Arguments(2):
          IArgument (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument) (Syntax: 's')
            ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 's')
            InConversion: null
            OutConversion: null
          IArgument (ArgumentKind.Explicit, Matching Parameter: result) (OperationKind.Argument) (Syntax: 'var i')
            ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'var i')
            InConversion: null
            OutConversion: null
  IfTrue: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... , s ={s}"");')
      Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... }, s ={s}"")')
          Instance Receiver: null
          Arguments(1):
              IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '$""i ={i}, s ={s}""')
                IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""i ={i}, s ={s}""')
                  Parts(4):
                      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'i =')
                        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""i ="") (Syntax: 'i =')
                      IInterpolation (OperationKind.Interpolation) (Syntax: '{i}')
                        Expression: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                        Alignment: null
                        FormatString: null
                      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: ', s =')
                        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "", s ="") (Syntax: ', s =')
                      IInterpolation (OperationKind.Interpolation) (Syntax: '{s}')
                        Expression: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 's')
                        Alignment: null
                        FormatString: null
                InConversion: null
                OutConversion: null
  IfFalse: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... , s ={s}"");')
      Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... }, s ={s}"")')
          Instance Receiver: null
          Arguments(1):
              IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '$""i ={i}, s ={s}""')
                IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""i ={i}, s ={s}""')
                  Parts(4):
                      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'i =')
                        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""i ="") (Syntax: 'i =')
                      IInterpolation (OperationKind.Interpolation) (Syntax: '{i}')
                        Expression: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                        Alignment: null
                        FormatString: null
                      IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: ', s =')
                        Text: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "", s ="") (Syntax: ', s =')
                      IInterpolation (OperationKind.Interpolation) (Syntax: '{s}')
                        Expression: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 's')
                        Alignment: null
                        FormatString: null
                InConversion: null
                OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithOutVar()
        {
            string source = @"
class P
{
    private void M()
    {

        /*<bind>*/if (true)
            System.Console.WriteLine(A());/*</bind>*/
    }
    private int A()
    {
        var s = """";
        if (int.TryParse(s, out var i))
        {
            return i;
        }
        else
        {
            return -1;
        }
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement) (Syntax: 'if (true) ... eLine(A());')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
  IfTrue: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... eLine(A());')
      Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... teLine(A())')
          Instance Receiver: null
          Arguments(1):
              IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'A()')
                IInvocationExpression ( System.Int32 P.A()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'A()')
                  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'A')
                  Arguments(0)
                InConversion: null
                OutConversion: null
  IfFalse: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementExplictEmbeddedOutVar()
        {
            string source = @"
class P
{
    private void M()
    {
        var s = ""data"";
        /*<bind>*/if (true)
        {
            A(int.TryParse(s, out var i));
        }/*</bind>*/
    }
    private void A(bool flag)
    {
        if (flag)
        {
            System.Console.WriteLine(""Result1"");
        }
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement) (Syntax: 'if (true) ... }')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
  IfTrue: IBlockStatement (1 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      Locals: Local_1: System.Int32 i
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'A(int.TryPa ... ut var i));')
        Expression: IInvocationExpression ( void P.A(System.Boolean flag)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'A(int.TryPa ... out var i))')
            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'A')
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: flag) (OperationKind.Argument) (Syntax: 'int.TryPars ...  out var i)')
                  IInvocationExpression (System.Boolean System.Int32.TryParse(System.String s, out System.Int32 result)) (OperationKind.InvocationExpression, Type: System.Boolean) (Syntax: 'int.TryPars ...  out var i)')
                    Instance Receiver: null
                    Arguments(2):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument) (Syntax: 's')
                          ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 's')
                          InConversion: null
                          OutConversion: null
                        IArgument (ArgumentKind.Explicit, Matching Parameter: result) (OperationKind.Argument) (Syntax: 'var i')
                          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'var i')
                          InConversion: null
                          OutConversion: null
                  InConversion: null
                  OutConversion: null
  IfFalse: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementImplicitEmbeddedOutVar()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        object o = 25;
        /*<bind>*/if (true)
            A(o is int i, 1);/*</bind>*/
    }

    private static void A(bool flag, int number)
    {
        if (flag)
        {
            System.Console.WriteLine(new string('*', number));
        }
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement) (Syntax: 'if (true) ...  int i, 1);')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
  IfTrue: IBlockStatement (1 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'A(o is int i, 1);')
      Locals: Local_1: System.Int32 i
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'A(o is int i, 1);')
        Expression: IInvocationExpression (void Program.A(System.Boolean flag, System.Int32 number)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'A(o is int i, 1)')
            Instance Receiver: null
            Arguments(2):
                IArgument (ArgumentKind.Explicit, Matching Parameter: flag) (OperationKind.Argument) (Syntax: 'o is int i')
                  IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean) (Syntax: 'o is int i')
                    Expression: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o')
                    Pattern: IDeclarationPattern (Declared Symbol: System.Int32 i) (OperationKind.DeclarationPattern) (Syntax: 'int i')
                  InConversion: null
                  OutConversion: null
                IArgument (ArgumentKind.Explicit, Matching Parameter: number) (OperationKind.Argument) (Syntax: '1')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: null
                  OutConversion: null
  IfFalse: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithConditionPattern()
        {
            string source = @"
using System;

class P
{
    private void M()
    {
        object obj = ""pattern"";

        /*<bind>*/if (obj is string str)
        {
            Console.WriteLine(str);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement) (Syntax: 'if (obj is  ... }')
  Condition: IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean) (Syntax: 'obj is string str')
      Expression: ILocalReferenceExpression: obj (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'obj')
      Pattern: IDeclarationPattern (Declared Symbol: System.String str) (OperationKind.DeclarationPattern) (Syntax: 'string str')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(str);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(str)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'str')
                  ILocalReferenceExpression: str (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'str')
                  InConversion: null
                  OutConversion: null
  IfFalse: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithPattern()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/if (true)
            A(25);/*</bind>*/
    }

    private static void A(object o)
    {
        if (o is null) return;
        if (!(o is int i)) return;
        System.Console.WriteLine(new string('*', i));
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement) (Syntax: 'if (true) ... A(25);')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
  IfTrue: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'A(25);')
      Expression: IInvocationExpression (void Program.A(System.Object o)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'A(25)')
          Instance Receiver: null
          Arguments(1):
              IArgument (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument) (Syntax: '25')
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: '25')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 25) (Syntax: '25')
                InConversion: null
                OutConversion: null
  IfFalse: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithEmbeddedPattern()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        object o = 25;
        /*<bind>*/if (true)
        {
            A(o is int i, 1);
        }/*</bind>*/
    }

    private static void A(bool flag, int number)
    {
        if (flag)
        {
            System.Console.WriteLine(new string('*', number));
        }
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement) (Syntax: 'if (true) ... }')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
  IfTrue: IBlockStatement (1 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      Locals: Local_1: System.Int32 i
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'A(o is int i, 1);')
        Expression: IInvocationExpression (void Program.A(System.Boolean flag, System.Int32 number)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'A(o is int i, 1)')
            Instance Receiver: null
            Arguments(2):
                IArgument (ArgumentKind.Explicit, Matching Parameter: flag) (OperationKind.Argument) (Syntax: 'o is int i')
                  IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean) (Syntax: 'o is int i')
                    Expression: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o')
                    Pattern: IDeclarationPattern (Declared Symbol: System.Int32 i) (OperationKind.DeclarationPattern) (Syntax: 'int i')
                  InConversion: null
                  OutConversion: null
                IArgument (ArgumentKind.Explicit, Matching Parameter: number) (OperationKind.Argument) (Syntax: '1')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: null
                  OutConversion: null
  IfFalse: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithElseMissing()
        {
            string source = @"
using System;

class P
{
    private void M()
    {
        object obj = ""pattern"";

        /*<bind>*/if (obj is string str)
        {
            Console.WriteLine(str);
        }
        else
/*</bind>*/    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, IsInvalid) (Syntax: 'if (obj is  ... else')
  Condition: IIsPatternExpression (OperationKind.IsPatternExpression, Type: System.Boolean) (Syntax: 'obj is string str')
      Expression: ILocalReferenceExpression: obj (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'obj')
      Pattern: IDeclarationPattern (Declared Symbol: System.String str) (OperationKind.DeclarationPattern) (Syntax: 'string str')
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(str);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(str)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'str')
                  ILocalReferenceExpression: str (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'str')
                  InConversion: null
                  OutConversion: null
  IfFalse: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '')
      Expression: IInvalidExpression (OperationKind.InvalidExpression, Type: ?) (Syntax: '')
          Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term '}'
                //         else
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(14, 13),
                // CS1002: ; expected
                //         else
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(14, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithConditionMissing()
        {
            string source = @"
using System;

class P
{
    private void M()
    {
        int a = 1;
        /*<bind>*/if ()
        {
            a = 2;
        }
        else
        {
            a = 3;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, IsInvalid) (Syntax: 'if () ... }')
  Condition: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid) (Syntax: '')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
          Children(0)
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'a = 2;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'a = 2')
            Left: ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'a')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  IfFalse: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'a = 3;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'a = 3')
            Left: ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'a')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ')'
                //         /*<bind>*/if ()
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(9, 23),
                // CS0219: The variable 'a' is assigned but its value is never used
                //         int a = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "a").WithArguments("a").WithLocation(8, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithStatementMissing()
        {
            string source = @"
using System;

class P
{
    private void M()
    {
        int a = 1;
        
        /*<bind>*/if (a == 1)
        else
/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, IsInvalid) (Syntax: 'if (a == 1) ... else')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'a == 1')
      Left: ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'a')
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  IfTrue: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '')
      Expression: IInvalidExpression (OperationKind.InvalidExpression, Type: ?) (Syntax: '')
          Children(0)
  IfFalse: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '')
      Expression: IInvalidExpression (OperationKind.InvalidExpression, Type: ?) (Syntax: '')
          Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term 'else'
                //         /*<bind>*/if (a == 1)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("else").WithLocation(10, 30),
                // CS1002: ; expected
                //         /*<bind>*/if (a == 1)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(10, 30),
                // CS1525: Invalid expression term '}'
                //         else
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(11, 13),
                // CS1002: ; expected
                //         else
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(11, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithFuncCall()
        {
            string source = @"
using System;

class P
{
    private void M()
    {
        /*<bind>*/if (true)
            A();
        else
            B();/*</bind>*/
    }
    private void A()
    {
        Console.WriteLine(""A"");
    }
    private void B()
    {
        Console.WriteLine(""B"");
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement) (Syntax: 'if (true) ... B();')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
  IfTrue: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'A();')
      Expression: IInvocationExpression ( void P.A()) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'A()')
          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'A')
          Arguments(0)
  IfFalse: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'B();')
      Expression: IInvocationExpression ( void P.B()) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'B()')
          Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'B')
          Arguments(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0162: Unreachable code detected
                //             B();/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreachableCode, "B").WithLocation(11, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithDynamic()
        {
            string source = @"
using System;

class C
{
    public static int F<T>(dynamic d, Type t, T x) where T : struct
    {
        /*<bind>*/if (d.GetType() == t && ((T)d).Equals(x))
        {
            return 1;
        }/*</bind>*/

        return 2;
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement) (Syntax: 'if (d.GetTy ... }')
  Condition: IUnaryOperatorExpression (UnaryOperationKind.DynamicTrue) (OperationKind.UnaryOperatorExpression, Type: System.Boolean) (Syntax: 'd.GetType() ... ).Equals(x)')
      Operand: IBinaryOperatorExpression (BinaryOperationKind.DynamicAnd) (OperationKind.BinaryOperatorExpression, Type: dynamic) (Syntax: 'd.GetType() ... ).Equals(x)')
          Left: IBinaryOperatorExpression (BinaryOperationKind.Invalid) (OperationKind.BinaryOperatorExpression, Type: dynamic) (Syntax: 'd.GetType() == t')
              Left: IOperation:  (OperationKind.None) (Syntax: 'd.GetType()')
                  Children(1):
                      IDynamicMemberReferenceExpression (Member Name: ""GetType"", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic) (Syntax: 'd.GetType')
                        Type Arguments(0)
                        Instance Receiver: IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
              Right: IParameterReferenceExpression: t (OperationKind.ParameterReferenceExpression, Type: System.Type) (Syntax: 't')
          Right: IInvocationExpression (virtual System.Boolean System.ValueType.Equals(System.Object obj)) (OperationKind.InvocationExpression, Type: System.Boolean) (Syntax: '((T)d).Equals(x)')
              Instance Receiver: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: T) (Syntax: '(T)d')
                  Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument) (Syntax: 'x')
                    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'x')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: T) (Syntax: 'x')
                    InConversion: null
                    OutConversion: null
  IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return 1;')
        ReturnedValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  IfFalse: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
