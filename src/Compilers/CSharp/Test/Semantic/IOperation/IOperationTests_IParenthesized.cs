// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesized()
        {
            string source = @"
class P
{
    static int M1(int a, int b)
    {
        return /*<bind>*/(a + b)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(a + b)')
  Operand: 
    IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
      Left: 
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
      Right: 
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ParenthesizedExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedChild()
        {
            string source = @"
class P
{
    static int M1(int a, int b)
    {
        return (/*<bind>*/a + b/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
  Left: 
    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
  Right: 
    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedParent()
        {
            string source = @"
class P
{
    static int M1(int a, int b)
    {
        /*<bind>*/return (a + b);/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return (a + b);')
  ReturnedValue: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(a + b)')
      Operand: 
        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
          Left: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
          Right: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ReturnStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedMultipleNesting()
        {
            string source = @"
class P
{
    static int M1(int a, int b)
    {
        return (/*<bind>*/((a + b))/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '((a + b))')
  Operand: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(a + b)')
      Operand: 
        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
          Left: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
          Right: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ParenthesizedExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedMultipleNesting02()
        {
            string source = @"
class P
{
    static int M1(int a, int b, int c)
    {
        return (/*<bind>*/((a + b) * c)/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '((a + b) * c)')
  Operand: 
    IBinaryOperation (BinaryOperatorKind.Multiply) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '(a + b) * c')
      Left: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(a + b)')
          Operand: 
            IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
              Left: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
              Right: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
      Right: 
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ParenthesizedExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedMultipleNesting03()
        {
            string source = @"
class P
{
    static int M1(int a, int b)
    {
        return /*<bind>*/(((a + b)))/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(((a + b)))')
  Operand: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '((a + b))')
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(a + b)')
          Operand: 
            IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
              Left: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
              Right: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ParenthesizedExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedMultipleNesting04()
        {
            string source = @"
class P
{
    static int M1(int a, int b)
    {
        return ((/*<bind>*/(a + b)/*</bind>*/));
    }
}
";
            string expectedOperationTree = @"
IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(a + b)')
  Operand: 
    IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
      Left: 
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
      Right: 
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ParenthesizedExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedMultipleNesting05()
        {
            string source = @"
class P
{
    static int M1(int a, int b)
    {
        return (((/*<bind>*/a + b/*</bind>*/)));
    }
}
";
            string expectedOperationTree = @"
IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
  Left: 
    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
  Right: 
    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedImplicitConversion()
        {
            string source = @"
class P
{
    static long M1(int a, int b)
    {
        return /*<bind>*/(a + b)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int64) (Syntax: '(a + b)')
  Operand: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'a + b')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
          Left: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
          Right: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ParenthesizedExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedImplicitConversionParent()
        {
            string source = @"
class P
{
    static long M1(int a, int b)
    {
        /*<bind>*/return (a + b);/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return (a + b);')
  ReturnedValue: 
    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int64) (Syntax: '(a + b)')
      Operand: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'a + b')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
              Left: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
              Right: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ReturnStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedExplicitConversion()
        {
            string source = @"
class P
{
    static double M1(int a, int b)
    {
        return /*<bind>*/(double)(a + b)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(a + b)')
  Operand: 
    IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
      Left: 
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
      Right: 
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ParenthesizedExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedExplicitConversionParent()
        {
            string source = @"
class P
{
    static double M1(int a, int b)
    {
        /*<bind>*/return (double)(a + b);/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return (double)(a + b);')
  ReturnedValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double) (Syntax: '(double)(a + b)')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(a + b)')
          Operand: 
            IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
              Left: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
              Right: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ReturnStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedConstantValue()
        {
            string source = @"
class P
{
    static int M1()
    {
        return /*<bind>*/(5)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32, Constant: 5) (Syntax: '(5)')
  Operand: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ParenthesizedExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedQueryClause()
        {
            string source = @"
using System.Linq;

class P
{
    static object M1(int[] a)
    {
        return from r in a select /*<bind>*/(-r)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(-r)')
  Operand: 
    IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, Type: System.Int32) (Syntax: '-r')
      Operand: 
        IOperation:  (OperationKind.None, Type: null) (Syntax: 'r')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ParenthesizedExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedErrorOperand()
        {
            string source = @"
class P
{
    static int M1()
    {
        return /*<bind>*/(a)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IParenthesizedOperation (OperationKind.Parenthesized, Type: ?, IsInvalid) (Syntax: '(a)')
  Operand: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'a')
      Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'a' does not exist in the current context
                //         return /*<bind>*/(a)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ParenthesizedExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
