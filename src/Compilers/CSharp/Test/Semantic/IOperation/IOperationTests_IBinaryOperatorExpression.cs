﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
@"
IBinaryOperation (BinaryOperatorKind.Add, IsLifted) (OperationKind.BinaryOperator, Type: System.Int32?) (Syntax: 'x + y')
  Left: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
  Right: 
    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'y')
";

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
@"
IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x + y')
  Left: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
  Right: 
    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
";

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
IBinaryOperation (BinaryOperatorKind.Add, IsLifted, Checked) (OperationKind.BinaryOperator, Type: System.Int32?) (Syntax: 'x + y')
  Left: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
  Right: 
    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'y')
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
IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x + y')
  Left: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
  Right: 
    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
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
@"
IBinaryOperation (BinaryOperatorKind.Add, IsLifted) (OperatorMethod: C C.op_Addition(C c1, C c2)) (OperationKind.BinaryOperator, Type: C?) (Syntax: 'x + y')
  Left: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C?) (Syntax: 'x')
  Right: 
    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: C?) (Syntax: 'y')
";

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
@"
IBinaryOperation (BinaryOperatorKind.Add) (OperatorMethod: C C.op_Addition(C c1, C c2)) (OperationKind.BinaryOperator, Type: C) (Syntax: 'x + y')
  Left: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C) (Syntax: 'x')
  Right: 
    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: C) (Syntax: 'y')
";

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
IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ) ? 1 : 0))')
  Instance Receiver: 
    null
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '(a >> 10) + ... 0) ? 1 : 0)')
        IBinaryOperation (BinaryOperatorKind.Or) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '(a >> 10) + ... 0) ? 1 : 0)')
          Left: 
            IBinaryOperation (BinaryOperatorKind.And) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '(a >> 10) + ... / e % f & g')
              Left: 
                IBinaryOperation (BinaryOperatorKind.Subtract) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '(a >> 10) + ... * d / e % f')
                  Left: 
                    IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '(a >> 10) + (b << 20)')
                      Left: 
                        IBinaryOperation (BinaryOperatorKind.RightShift) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a >> 10')
                          Left: 
                            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                      Right: 
                        IBinaryOperation (BinaryOperatorKind.LeftShift) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b << 20')
                          Left: 
                            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
                  Right: 
                    IBinaryOperation (BinaryOperatorKind.Remainder) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'c * d / e % f')
                      Left: 
                        IBinaryOperation (BinaryOperatorKind.Divide) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'c * d / e')
                          Left: 
                            IBinaryOperation (BinaryOperatorKind.Multiply) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'c * d')
                              Left: 
                                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')
                              Right: 
                                IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd')
                          Right: 
                            IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'e')
                      Right: 
                        IParameterReferenceOperation: f (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'f')
              Right: 
                IParameterReferenceOperation: g (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'g')
          Right: 
            IBinaryOperation (BinaryOperatorKind.ExclusiveOr) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'h ^ (i == ( ... 0) ? 1 : 0)')
              Left: 
                IParameterReferenceOperation: h (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'h')
              Right: 
                IConditionalOperation (OperationKind.Conditional, Type: System.Int32) (Syntax: 'i == (j !=  ...  0) ? 1 : 0')
                  Condition: 
                    IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i == (j !=  ... 0) ? 1 : 0)')
                      Left: 
                        IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
                      Right: 
                        IConditionalOperation (OperationKind.Conditional, Type: System.Int32) (Syntax: 'j != ((((k  ...  0) ? 1 : 0')
                          Condition: 
                            IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'j != ((((k  ...  p ? 1 : 0)')
                              Left: 
                                IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')
                              Right: 
                                IConditionalOperation (OperationKind.Conditional, Type: System.Int32) (Syntax: '(((k < l ?  ... = p ? 1 : 0')
                                  Condition: 
                                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: '(((k < l ?  ... 1 : 0) >= p')
                                      Left: 
                                        IConditionalOperation (OperationKind.Conditional, Type: System.Int32) (Syntax: '((k < l ? 1 ... = o ? 1 : 0')
                                          Condition: 
                                            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: '((k < l ? 1 ... 1 : 0) <= o')
                                              Left: 
                                                IConditionalOperation (OperationKind.Conditional, Type: System.Int32) (Syntax: '(k < l ? 1  ... > m ? 1 : 0')
                                                  Condition: 
                                                    IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: '(k < l ? 1 : 0) > m')
                                                      Left: 
                                                        IConditionalOperation (OperationKind.Conditional, Type: System.Int32) (Syntax: 'k < l ? 1 : 0')
                                                          Condition: 
                                                            IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'k < l')
                                                              Left: 
                                                                IParameterReferenceOperation: k (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'k')
                                                              Right: 
                                                                IParameterReferenceOperation: l (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'l')
                                                          WhenTrue: 
                                                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                                          WhenFalse: 
                                                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                                      Right: 
                                                        IParameterReferenceOperation: m (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'm')
                                                  WhenTrue: 
                                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                                  WhenFalse: 
                                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                              Right: 
                                                IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'o')
                                          WhenTrue: 
                                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                          WhenFalse: 
                                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                      Right: 
                                        IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')
                                  WhenTrue: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                  WhenFalse: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                          WhenTrue: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                          WhenFalse: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                  WhenTrue: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  WhenFalse: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ) ? 1 : 0))')
  Instance Receiver: 
    null
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '(a >> 10) + ... 0) ? 1 : 0)')
        IBinaryOperation (BinaryOperatorKind.Or) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '(a >> 10) + ... 0) ? 1 : 0)')
          Left: 
            IBinaryOperation (BinaryOperatorKind.And) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '(a >> 10) + ... / e % f & g')
              Left: 
                IBinaryOperation (BinaryOperatorKind.Subtract, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '(a >> 10) + ... * d / e % f')
                  Left: 
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '(a >> 10) + (b << 20)')
                      Left: 
                        IBinaryOperation (BinaryOperatorKind.RightShift) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a >> 10')
                          Left: 
                            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                      Right: 
                        IBinaryOperation (BinaryOperatorKind.LeftShift) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b << 20')
                          Left: 
                            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
                  Right: 
                    IBinaryOperation (BinaryOperatorKind.Remainder) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'c * d / e % f')
                      Left: 
                        IBinaryOperation (BinaryOperatorKind.Divide, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'c * d / e')
                          Left: 
                            IBinaryOperation (BinaryOperatorKind.Multiply, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'c * d')
                              Left: 
                                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')
                              Right: 
                                IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd')
                          Right: 
                            IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'e')
                      Right: 
                        IParameterReferenceOperation: f (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'f')
              Right: 
                IParameterReferenceOperation: g (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'g')
          Right: 
            IBinaryOperation (BinaryOperatorKind.ExclusiveOr) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'h ^ (i == ( ... 0) ? 1 : 0)')
              Left: 
                IParameterReferenceOperation: h (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'h')
              Right: 
                IConditionalOperation (OperationKind.Conditional, Type: System.Int32) (Syntax: 'i == (j !=  ...  0) ? 1 : 0')
                  Condition: 
                    IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i == (j !=  ... 0) ? 1 : 0)')
                      Left: 
                        IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
                      Right: 
                        IConditionalOperation (OperationKind.Conditional, Type: System.Int32) (Syntax: 'j != ((((k  ...  0) ? 1 : 0')
                          Condition: 
                            IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'j != ((((k  ...  p ? 1 : 0)')
                              Left: 
                                IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')
                              Right: 
                                IConditionalOperation (OperationKind.Conditional, Type: System.Int32) (Syntax: '(((k < l ?  ... = p ? 1 : 0')
                                  Condition: 
                                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: '(((k < l ?  ... 1 : 0) >= p')
                                      Left: 
                                        IConditionalOperation (OperationKind.Conditional, Type: System.Int32) (Syntax: '((k < l ? 1 ... = o ? 1 : 0')
                                          Condition: 
                                            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: '((k < l ? 1 ... 1 : 0) <= o')
                                              Left: 
                                                IConditionalOperation (OperationKind.Conditional, Type: System.Int32) (Syntax: '(k < l ? 1  ... > m ? 1 : 0')
                                                  Condition: 
                                                    IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: '(k < l ? 1 : 0) > m')
                                                      Left: 
                                                        IConditionalOperation (OperationKind.Conditional, Type: System.Int32) (Syntax: 'k < l ? 1 : 0')
                                                          Condition: 
                                                            IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'k < l')
                                                              Left: 
                                                                IParameterReferenceOperation: k (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'k')
                                                              Right: 
                                                                IParameterReferenceOperation: l (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'l')
                                                          WhenTrue: 
                                                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                                          WhenFalse: 
                                                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                                      Right: 
                                                        IParameterReferenceOperation: m (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'm')
                                                  WhenTrue: 
                                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                                  WhenFalse: 
                                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                              Right: 
                                                IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'o')
                                          WhenTrue: 
                                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                          WhenFalse: 
                                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                      Right: 
                                        IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')
                                  WhenTrue: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                  WhenFalse: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                          WhenTrue: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                          WhenFalse: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                  WhenTrue: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  WhenFalse: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void LogicalFlow_01()
        {
            string source = @"
class P
{
    void M(bool a, bool b)
/*<bind>*/{
        GetArray()[0] =  a || b;
    }/*</bind>*/

    static bool[] GetArray() => null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()[0]')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Boolean) (Syntax: 'GetArray()[0]')
              Array reference: 
                IInvocationOperation (System.Boolean[] P.GetArray()) (OperationKind.Invocation, Type: System.Boolean[]) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    null
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

    Jump if True (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()[0] =  a || b;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'GetArray()[0] =  a || b')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'GetArray()[0]')
              Right: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'a || b')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void LogicalFlow_02()
        {
            string source = @"
class P
{
    void M(bool a, bool b, bool c)
/*<bind>*/{
        GetArray()[0] =  c && (a || b);
    }/*</bind>*/

    static bool[] GetArray() => null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()[0]')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Boolean) (Syntax: 'GetArray()[0]')
              Array reference: 
                IInvocationOperation (System.Boolean[] P.GetArray()) (OperationKind.Invocation, Type: System.Boolean[]) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    null
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

    Jump if False (Regular) to Block[B5]
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if True (Regular) to Block[B4]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B3]
Block[B3] - Block
    Predecessors: [B2]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B6]
Block[B4] - Block
    Predecessors: [B2]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B6]
Block[B5] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'c')

    Next (Regular) Block[B6]
Block[B6] - Block
    Predecessors: [B3] [B4] [B5]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()[ ... & (a || b);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'GetArray()[ ... && (a || b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'GetArray()[0]')
              Right: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'c && (a || b)')

    Next (Regular) Block[B7]
Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void LogicalFlow_03()
        {
            string source = @"
class P
{
    void M(bool a, bool b, bool c)
/*<bind>*/{
        GetArray()[0] =  c && (a && b);
    }/*</bind>*/

    static bool[] GetArray() => null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()[0]')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Boolean) (Syntax: 'GetArray()[0]')
              Array reference: 
                IInvocationOperation (System.Boolean[] P.GetArray()) (OperationKind.Invocation, Type: System.Boolean[]) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    null
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

    Jump if False (Regular) to Block[B4]
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B3]
Block[B3] - Block
    Predecessors: [B2]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B5]
Block[B4] - Block
    Predecessors: [B1] [B2]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c && (a && b)')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'c && (a && b)')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B3] [B4]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()[ ... & (a && b);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'GetArray()[ ... && (a && b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'GetArray()[0]')
              Right: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'c && (a && b)')

    Next (Regular) Block[B6]
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void LogicalFlow_04()
        {
            string source = @"
class P
{
    void M(bool a, bool b, bool c)
/*<bind>*/{
        GetArray()[0] =  c || (a || b);
    }/*</bind>*/

    static bool[] GetArray() => null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()[0]')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Boolean) (Syntax: 'GetArray()[0]')
              Array reference: 
                IInvocationOperation (System.Boolean[] P.GetArray()) (OperationKind.Invocation, Type: System.Boolean[]) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    null
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

    Jump if True (Regular) to Block[B4]
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if True (Regular) to Block[B4]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B3]
Block[B3] - Block
    Predecessors: [B2]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B5]
Block[B4] - Block
    Predecessors: [B1] [B2]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c || (a || b)')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'c || (a || b)')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B3] [B4]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()[ ... | (a || b);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'GetArray()[ ... || (a || b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'GetArray()[0]')
              Right: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'c || (a || b)')

    Next (Regular) Block[B6]
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void LogicalFlow_05()
        {
            string source = @"
class P
{
    void M(bool a, bool b, bool c, bool d, bool e)
/*<bind>*/{
        GetArray()[0] =  a && (b || (c && (d || e)));
    }/*</bind>*/

    static bool[] GetArray() => null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()[0]')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Boolean) (Syntax: 'GetArray()[0]')
              Array reference: 
                IInvocationOperation (System.Boolean[] P.GetArray()) (OperationKind.Invocation, Type: System.Boolean[]) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    null
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

    Jump if False (Regular) to Block[B7]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if True (Regular) to Block[B6]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B3]
Block[B3] - Block
    Predecessors: [B2]
    Statements (0)
    Jump if False (Regular) to Block[B7]
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B3]
    Statements (0)
    Jump if True (Regular) to Block[B6]
        IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'd')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
          Value: 
            IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'e')

    Next (Regular) Block[B8]
Block[B6] - Block
    Predecessors: [B2] [B4]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b || (c && (d || e))')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'b || (c && (d || e))')

    Next (Regular) Block[B8]
Block[B7] - Block
    Predecessors: [B1] [B3]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a && (b ||  ...  (d || e)))')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'a && (b ||  ...  (d || e)))')

    Next (Regular) Block[B8]
Block[B8] - Block
    Predecessors: [B5] [B6] [B7]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()[ ... (d || e)));')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'GetArray()[ ...  (d || e)))')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'GetArray()[0]')
              Right: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'a && (b ||  ...  (d || e)))')

    Next (Regular) Block[B9]
Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NonLogicalFlow_01()
        {
            string source = @"
using System;
class C
{
    void M(int? a, int b)
    /*<bind>*/{
        GetArray()[0] = (a ?? b) + b;
    }/*</bind>*/

    static int[] GetArray() => null;
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()[0]')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()[0]')
              Array reference: 
                IInvocationOperation (System.Int32[] C.GetArray()) (OperationKind.Invocation, Type: System.Int32[]) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    null
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()[ ...  ?? b) + b;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'GetArray()[ ... a ?? b) + b')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()[0]')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '(a ?? b) + b')
                  Left: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ?? b')
                  Right: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NonLogicalFlow_02()
        {
            string source = @"
using System;
class C
{
    void M(int? a, int b)
    /*<bind>*/{
        GetArray()[0] = b + (a ?? b);
    }/*</bind>*/

    static int[] GetArray() => null;
}
";
            var expectedDiagnostics = DiagnosticDescription.None;
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()[0]')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()[0]')
              Array reference: 
                IInvocationOperation (System.Int32[] C.GetArray()) (OperationKind.Invocation, Type: System.Int32[]) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    null
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()[ ... + (a ?? b);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'GetArray()[ ...  + (a ?? b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()[0]')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b + (a ?? b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ?? b')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NonLogicalFlow_03()
        {
            string source = @"
using System;
class C
{
    void M(int? a, int b)
    /*<bind>*/{
        GetArray()[0] = (a ?? b) + (a ?? b);
    }/*</bind>*/

    static int[] GetArray() => null;
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()[0]')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()[0]')
              Array reference: 
                IInvocationOperation (System.Int32[] C.GetArray()) (OperationKind.Invocation, Type: System.Int32[]) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    null
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'a')

    Jump if True (Regular) to Block[B6]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next (Regular) Block[B7]
Block[B6] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next (Regular) Block[B7]
Block[B7] - Block
    Predecessors: [B5] [B6]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()[ ... + (a ?? b);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'GetArray()[ ...  + (a ?? b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()[0]')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '(a ?? b) + (a ?? b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ?? b')
                  Right: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ?? b')

    Next (Regular) Block[B8]
Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NonLogicalFlow_04()
        {
            string source = @"
using System;
class C
{
    void M(C a, C b)
    /*<bind>*/{
        GetArray()[0] = (a ?? b) + (a ?? b);
    }/*</bind>*/

    static int[] GetArray() => null;

    public static int operator +(C c1, C c2) => 0;
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()[0]')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()[0]')
              Array reference: 
                IInvocationOperation (System.Int32[] C.GetArray()) (OperationKind.Invocation, Type: System.Int32[]) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    null
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: C) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: C) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: C) (Syntax: 'a')

    Jump if True (Regular) to Block[B6]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B7]
Block[B6] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: C) (Syntax: 'b')

    Next (Regular) Block[B7]
Block[B7] - Block
    Predecessors: [B5] [B6]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()[ ... + (a ?? b);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'GetArray()[ ...  + (a ?? b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()[0]')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add) (OperatorMethod: System.Int32 C.op_Addition(C c1, C c2)) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '(a ?? b) + (a ?? b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a ?? b')
                  Right: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a ?? b')

    Next (Regular) Block[B8]
Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NonLogicalFlow_05()
        {
            string source = @"
using System;
class C
{
    void M(int? a, int b)
    /*<bind>*/{
        GetArray()[0] = b - (a ?? b);
    }/*</bind>*/

    static int[] GetArray() => null;
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()[0]')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()[0]')
              Array reference: 
                IInvocationOperation (System.Int32[] C.GetArray()) (OperationKind.Invocation, Type: System.Int32[]) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    null
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()[ ... - (a ?? b);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'GetArray()[ ...  - (a ?? b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()[0]')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Subtract) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b - (a ?? b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ?? b')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NonLogicalFlow_06()
        {
            string source = @"
using System;
class C
{
    void M(int? a, int b)
    /*<bind>*/{
        GetArray()[0] = b << (a ?? b);
    }/*</bind>*/

    static int[] GetArray() => null;
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()[0]')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()[0]')
              Array reference: 
                IInvocationOperation (System.Int32[] C.GetArray()) (OperationKind.Invocation, Type: System.Int32[]) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    null
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()[ ... < (a ?? b);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'GetArray()[ ... << (a ?? b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()[0]')
              Right: 
                IBinaryOperation (BinaryOperatorKind.LeftShift) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b << (a ?? b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ?? b')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NonLogicalFlow_07()
        {
            string source = @"
using System;
class C
{
    void M(int? a, int b)
    /*<bind>*/{
        GetArray()[0] = b * (a ?? b);
    }/*</bind>*/

    static int[] GetArray() => null;
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()[0]')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()[0]')
              Array reference: 
                IInvocationOperation (System.Int32[] C.GetArray()) (OperationKind.Invocation, Type: System.Int32[]) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    null
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()[ ... * (a ?? b);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'GetArray()[ ...  * (a ?? b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()[0]')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Multiply) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b * (a ?? b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ?? b')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NonLogicalFlow_08()
        {
            string source = @"
using System;
class C
{
    void M(int? a, int b)
    /*<bind>*/{
        GetArray()[0] = b / (a ?? b);
    }/*</bind>*/

    static int[] GetArray() => null;
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()[0]')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()[0]')
              Array reference: 
                IInvocationOperation (System.Int32[] C.GetArray()) (OperationKind.Invocation, Type: System.Int32[]) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    null
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()[ ... / (a ?? b);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'GetArray()[ ...  / (a ?? b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()[0]')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Divide) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b / (a ?? b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ?? b')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NonLogicalFlow_09()
        {
            string source = @"
using System;
class C
{
    void M(int? a, int b)
    /*<bind>*/{
        GetArray()[0] = b % (a ?? b);
    }/*</bind>*/

    static int[] GetArray() => null;
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetArray()[0]')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()[0]')
              Array reference: 
                IInvocationOperation (System.Int32[] C.GetArray()) (OperationKind.Invocation, Type: System.Int32[]) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    null
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()[ ... % (a ?? b);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'GetArray()[ ...  % (a ?? b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()[0]')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Remainder) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b % (a ?? b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ?? b')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NonLogicalFlow_10()
        {
            string source = @"
using System;
class C
{
    void M(int? a, int b, bool c)
    /*<bind>*/{
        c = b < (a ?? b);
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = b < (a ?? b);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'c = b < (a ?? b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'c')
              Right: 
                IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'b < (a ?? b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ?? b')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NonLogicalFlow_12()
        {
            string source = @"
using System;
class C
{
    void M(int? a, int b, bool c)
    /*<bind>*/{
        c = b > (a ?? b);
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = b > (a ?? b);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'c = b > (a ?? b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'c')
              Right: 
                IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'b > (a ?? b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ?? b')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NonLogicalFlow_13()
        {
            string source = @"
using System;
class C
{
    void M(int? a, int b, bool c)
    /*<bind>*/{
        c = b <= (a ?? b);
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = b <= (a ?? b);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'c = b <= (a ?? b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'c')
              Right: 
                IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'b <= (a ?? b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ?? b')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NonLogicalFlow_14()
        {
            string source = @"
using System;
class C
{
    void M(int? a, int b, bool c)
    /*<bind>*/{
        c = b >= (a ?? b);
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = b >= (a ?? b);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'c = b >= (a ?? b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'c')
              Right: 
                IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'b >= (a ?? b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ?? b')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NonLogicalFlow_15()
        {
            string source = @"
using System;
class C
{
    void M(int? a, int b, bool c)
    /*<bind>*/{
        c = b == (a ?? b);
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = b == (a ?? b);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'c = b == (a ?? b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'c')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'b == (a ?? b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ?? b')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NonLogicalFlow_16()
        {
            string source = @"
using System;
class C
{
    void M(int? a, int b, bool c)
    /*<bind>*/{
        c = b != (a ?? b);
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = b != (a ?? b);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'c = b != (a ?? b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'c')
              Right: 
                IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'b != (a ?? b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ?? b')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NonLogicalFlow_17()
        {
            string source = @"
using System;
class C
{
    void M(int? a, int b, int c)
    /*<bind>*/{
        c = b & (a ?? b);
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = b & (a ?? b);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'c = b & (a ?? b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'c')
              Right: 
                IBinaryOperation (BinaryOperatorKind.And) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b & (a ?? b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ?? b')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NonLogicalFlow_18()
        {
            string source = @"
using System;
class C
{
    void M(int? a, int b, int c)
    /*<bind>*/{
        c = b | (a ?? b);
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = b | (a ?? b);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'c = b | (a ?? b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'c')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Or) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b | (a ?? b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ?? b')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NonLogicalFlow_19()
        {
            string source = @"
using System;
class C
{
    void M(int? a, int b, int c)
    /*<bind>*/{
        c = b ^ (a ?? b);
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
          Value: 
            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'a')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'a')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = b ^ (a ?? b);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'c = b ^ (a ?? b)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'c')
              Right: 
                IBinaryOperation (BinaryOperatorKind.ExclusiveOr) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'b ^ (a ?? b)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a ?? b')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
