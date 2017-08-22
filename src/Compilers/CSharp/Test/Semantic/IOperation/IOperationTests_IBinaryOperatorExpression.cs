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
        [Fact]
        public void VerifyLiftedBinaryOperators1()
        {
            var source = @"
class C
{
    void F(int? x, int? y)
    {
        var z = /*<bind>*/x + y/*</bind>*/;
    }
}";

            string expectedOperationTree =
@"IBinaryOperatorExpression (BinaryOperatorKind.Add, IsLifted) (OperationKind.BinaryOperatorExpression, Type: System.Int32?) (Syntax: 'x + y')
  Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32?) (Syntax: 'x')
  Right: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32?) (Syntax: 'y')";

            VerifyOperationTreeForTest<BinaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void VerifyNonLiftedBinaryOperators1()
        {
            var source = @"
class C
{
    void F(int x, int y)
    {
        var z = /*<bind>*/x + y/*</bind>*/;
    }
}";

            string expectedOperationTree =
@"IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + y')
  Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
  Right: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')";

            VerifyOperationTreeForTest<BinaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void VerifyLiftedCheckedBinaryOperators1()
        {
            string source = @"
class C
{
    void F(int? x, int? y)
    {
        checked
        {
            var z = /*<bind>*/x + y/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IBinaryOperatorExpression (BinaryOperatorKind.Add, IsLifted, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32?) (Syntax: 'x + y')
  Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32?) (Syntax: 'x')
  Right: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32?) (Syntax: 'y')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void VerifyNonLiftedCheckedBinaryOperators1()
        {
            string source = @"
class C
{
    void F(int x, int y)
    {
        checked
        {
            var z = /*<bind>*/x + y/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + y')
  Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
  Right: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void VerifyLiftedUserDefinedBinaryOperators1()
        {
            var source = @"
struct C
{
    public static C operator +(C c1, C c2) { }
    void F(C? x, C? y)
    {
        var z = /*<bind>*/x + y/*</bind>*/;
    }
}";

            string expectedOperationTree =
@"IBinaryOperatorExpression (BinaryOperatorKind.Add, IsLifted) (OperatorMethod: C C.op_Addition(C c1, C c2)) (OperationKind.BinaryOperatorExpression, Type: C?) (Syntax: 'x + y')
  Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: C?) (Syntax: 'x')
  Right: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: C?) (Syntax: 'y')";

            VerifyOperationTreeForTest<BinaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void VerifyNonLiftedUserDefinedBinaryOperators1()
        {
            var source = @"
struct C
{
    public static C operator +(C c1, C c2) { }
    void F(C x, C y)
    {
        var z = /*<bind>*/x + y/*</bind>*/;
    }
}";

            string expectedOperationTree =
@"IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperatorMethod: C C.op_Addition(C c1, C c2)) (OperationKind.BinaryOperatorExpression, Type: C) (Syntax: 'x + y')
  Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'x')
  Right: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'y')";

            VerifyOperationTreeForTest<BinaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestBinaryOperators()
        {
            string source = @"
using System;
class C
{
    void M(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j, int k, int l, int m, int n, int o, int p)
    {
        /*<bind>*/Console.WriteLine(
            (a >> 10) + (b << 20) - c * d / e % f & g |
            h ^ (i == (j != ((((k < l ? 1 : 0) > m ? 1 : 0) <= o ? 1 : 0) >= p ? 1 : 0) ? 1 : 0) ? 1 : 0))/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... ) ? 1 : 0))')
  Instance Receiver: null
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '(a >> 10) + ... 0) ? 1 : 0)')
        IBinaryOperatorExpression (BinaryOperatorKind.Or) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '(a >> 10) + ... 0) ? 1 : 0)')
          Left: IBinaryOperatorExpression (BinaryOperatorKind.And) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '(a >> 10) + ... / e % f & g')
              Left: IBinaryOperatorExpression (BinaryOperatorKind.Subtract) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '(a >> 10) + ... * d / e % f')
                  Left: IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '(a >> 10) + (b << 20)')
                      Left: IBinaryOperatorExpression (BinaryOperatorKind.RightShift) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'a >> 10')
                          Left: IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'a')
                          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
                      Right: IBinaryOperatorExpression (BinaryOperatorKind.LeftShift) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'b << 20')
                          Left: IParameterReferenceExpression: b (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'b')
                          Right: ILiteralExpression (Text: 20) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20) (Syntax: '20')
                  Right: IBinaryOperatorExpression (BinaryOperatorKind.Remainder) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'c * d / e % f')
                      Left: IBinaryOperatorExpression (BinaryOperatorKind.Divide) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'c * d / e')
                          Left: IBinaryOperatorExpression (BinaryOperatorKind.Multiply) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'c * d')
                              Left: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'c')
                              Right: IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'd')
                          Right: IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'e')
                      Right: IParameterReferenceExpression: f (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'f')
              Right: IParameterReferenceExpression: g (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'g')
          Right: IBinaryOperatorExpression (BinaryOperatorKind.ExclusiveOr) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'h ^ (i == ( ... 0) ? 1 : 0)')
              Left: IParameterReferenceExpression: h (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'h')
              Right: IConditionalExpression (OperationKind.ConditionalExpression, Type: System.Int32) (Syntax: 'i == (j !=  ...  0) ? 1 : 0')
                  Condition: IBinaryOperatorExpression (BinaryOperatorKind.Equals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i == (j !=  ... 0) ? 1 : 0)')
                      Left: IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
                      Right: IConditionalExpression (OperationKind.ConditionalExpression, Type: System.Int32) (Syntax: 'j != ((((k  ...  0) ? 1 : 0')
                          Condition: IBinaryOperatorExpression (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'j != ((((k  ...  p ? 1 : 0)')
                              Left: IParameterReferenceExpression: j (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'j')
                              Right: IConditionalExpression (OperationKind.ConditionalExpression, Type: System.Int32) (Syntax: '(((k < l ?  ... = p ? 1 : 0')
                                  Condition: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '(((k < l ?  ... 1 : 0) >= p')
                                      Left: IConditionalExpression (OperationKind.ConditionalExpression, Type: System.Int32) (Syntax: '((k < l ? 1 ... = o ? 1 : 0')
                                          Condition: IBinaryOperatorExpression (BinaryOperatorKind.LessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '((k < l ? 1 ... 1 : 0) <= o')
                                              Left: IConditionalExpression (OperationKind.ConditionalExpression, Type: System.Int32) (Syntax: '(k < l ? 1  ... > m ? 1 : 0')
                                                  Condition: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '(k < l ? 1 : 0) > m')
                                                      Left: IConditionalExpression (OperationKind.ConditionalExpression, Type: System.Int32) (Syntax: 'k < l ? 1 : 0')
                                                          Condition: IBinaryOperatorExpression (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'k < l')
                                                              Left: IParameterReferenceExpression: k (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'k')
                                                              Right: IParameterReferenceExpression: l (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'l')
                                                          WhenTrue: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                                                          WhenFalse: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                                                      Right: IParameterReferenceExpression: m (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'm')
                                                  WhenTrue: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                                                  WhenFalse: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                                              Right: IParameterReferenceExpression: o (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'o')
                                          WhenTrue: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                                          WhenFalse: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                                      Right: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
                                  WhenTrue: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                                  WhenFalse: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                          WhenTrue: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                          WhenFalse: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                  WhenTrue: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                  WhenFalse: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestBinaryOperators_Checked()
        {
            string source = @"
using System;
class C
{
    void M(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j, int k, int l, int m, int n, int o, int p)
    {
        checked
        {
            /*<bind>*/Console.WriteLine(
                (a >> 10) + (b << 20) - c * d / e % f & g |
                h ^ (i == (j != ((((k < l ? 1 : 0) > m ? 1 : 0) <= o ? 1 : 0) >= p ? 1 : 0) ? 1 : 0) ? 1 : 0))/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... ) ? 1 : 0))')
  Instance Receiver: null
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '(a >> 10) + ... 0) ? 1 : 0)')
        IBinaryOperatorExpression (BinaryOperatorKind.Or) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '(a >> 10) + ... 0) ? 1 : 0)')
          Left: IBinaryOperatorExpression (BinaryOperatorKind.And) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '(a >> 10) + ... / e % f & g')
              Left: IBinaryOperatorExpression (BinaryOperatorKind.Subtract, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '(a >> 10) + ... * d / e % f')
                  Left: IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '(a >> 10) + (b << 20)')
                      Left: IBinaryOperatorExpression (BinaryOperatorKind.RightShift) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'a >> 10')
                          Left: IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'a')
                          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
                      Right: IBinaryOperatorExpression (BinaryOperatorKind.LeftShift) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'b << 20')
                          Left: IParameterReferenceExpression: b (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'b')
                          Right: ILiteralExpression (Text: 20) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20) (Syntax: '20')
                  Right: IBinaryOperatorExpression (BinaryOperatorKind.Remainder) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'c * d / e % f')
                      Left: IBinaryOperatorExpression (BinaryOperatorKind.Divide, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'c * d / e')
                          Left: IBinaryOperatorExpression (BinaryOperatorKind.Multiply, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'c * d')
                              Left: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'c')
                              Right: IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'd')
                          Right: IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'e')
                      Right: IParameterReferenceExpression: f (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'f')
              Right: IParameterReferenceExpression: g (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'g')
          Right: IBinaryOperatorExpression (BinaryOperatorKind.ExclusiveOr) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'h ^ (i == ( ... 0) ? 1 : 0)')
              Left: IParameterReferenceExpression: h (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'h')
              Right: IConditionalExpression (OperationKind.ConditionalExpression, Type: System.Int32) (Syntax: 'i == (j !=  ...  0) ? 1 : 0')
                  Condition: IBinaryOperatorExpression (BinaryOperatorKind.Equals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i == (j !=  ... 0) ? 1 : 0)')
                      Left: IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
                      Right: IConditionalExpression (OperationKind.ConditionalExpression, Type: System.Int32) (Syntax: 'j != ((((k  ...  0) ? 1 : 0')
                          Condition: IBinaryOperatorExpression (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'j != ((((k  ...  p ? 1 : 0)')
                              Left: IParameterReferenceExpression: j (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'j')
                              Right: IConditionalExpression (OperationKind.ConditionalExpression, Type: System.Int32) (Syntax: '(((k < l ?  ... = p ? 1 : 0')
                                  Condition: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '(((k < l ?  ... 1 : 0) >= p')
                                      Left: IConditionalExpression (OperationKind.ConditionalExpression, Type: System.Int32) (Syntax: '((k < l ? 1 ... = o ? 1 : 0')
                                          Condition: IBinaryOperatorExpression (BinaryOperatorKind.LessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '((k < l ? 1 ... 1 : 0) <= o')
                                              Left: IConditionalExpression (OperationKind.ConditionalExpression, Type: System.Int32) (Syntax: '(k < l ? 1  ... > m ? 1 : 0')
                                                  Condition: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '(k < l ? 1 : 0) > m')
                                                      Left: IConditionalExpression (OperationKind.ConditionalExpression, Type: System.Int32) (Syntax: 'k < l ? 1 : 0')
                                                          Condition: IBinaryOperatorExpression (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'k < l')
                                                              Left: IParameterReferenceExpression: k (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'k')
                                                              Right: IParameterReferenceExpression: l (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'l')
                                                          WhenTrue: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                                                          WhenFalse: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                                                      Right: IParameterReferenceExpression: m (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'm')
                                                  WhenTrue: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                                                  WhenFalse: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                                              Right: IParameterReferenceExpression: o (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'o')
                                          WhenTrue: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                                          WhenFalse: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                                      Right: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
                                  WhenTrue: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                                  WhenFalse: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                          WhenTrue: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                          WhenFalse: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                  WhenTrue: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                  WhenFalse: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
