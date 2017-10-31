// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Semantics;
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
IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32) (Syntax: '(a + b)')
  Operand: 
    IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'a + b')
      Left: 
        IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'a')
      Right: 
        IParameterReferenceExpression: b (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'b')
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
IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'a + b')
  Left: 
    IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'a')
  Right: 
    IParameterReferenceExpression: b (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'b')
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
IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return (a + b);')
  ReturnedValue: 
    IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32) (Syntax: '(a + b)')
      Operand: 
        IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'a + b')
          Left: 
            IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'a')
          Right: 
            IParameterReferenceExpression: b (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'b')
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
IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32) (Syntax: '((a + b))')
  Operand: 
    IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32) (Syntax: '(a + b)')
      Operand: 
        IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'a + b')
          Left: 
            IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'a')
          Right: 
            IParameterReferenceExpression: b (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'b')
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
IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32) (Syntax: '((a + b) * c)')
  Operand: 
    IBinaryOperatorExpression (BinaryOperatorKind.Multiply) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '(a + b) * c')
      Left: 
        IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32) (Syntax: '(a + b)')
          Operand: 
            IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'a + b')
              Left: 
                IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'a')
              Right: 
                IParameterReferenceExpression: b (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'b')
      Right: 
        IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'c')
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
IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32) (Syntax: '(((a + b)))')
  Operand: 
    IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32) (Syntax: '((a + b))')
      Operand: 
        IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32) (Syntax: '(a + b)')
          Operand: 
            IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'a + b')
              Left: 
                IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'a')
              Right: 
                IParameterReferenceExpression: b (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'b')
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
IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32) (Syntax: '(a + b)')
  Operand: 
    IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'a + b')
      Left: 
        IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'a')
      Right: 
        IParameterReferenceExpression: b (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'b')
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
IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'a + b')
  Left: 
    IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'a')
  Right: 
    IParameterReferenceExpression: b (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'b')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedConversion()
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
IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32) (Syntax: '(a + b)')
  Operand: 
    IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'a + b')
      Left: 
        IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'a')
      Right: 
        IParameterReferenceExpression: b (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'b')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ParenthesizedExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedConversionParent()
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
IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return (a + b);')
  ReturnedValue: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64, IsImplicit) (Syntax: 'a + b')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32) (Syntax: '(a + b)')
          Operand: 
            IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'a + b')
              Left: 
                IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'a')
              Right: 
                IParameterReferenceExpression: b (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'b')
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
IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32, Constant: 5) (Syntax: '(5)')
  Operand: 
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
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
IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Int32) (Syntax: '(-r)')
  Operand: 
    IUnaryOperatorExpression (UnaryOperatorKind.Minus) (OperationKind.UnaryOperatorExpression, Type: System.Int32) (Syntax: '-r')
      Operand: 
        IOperation:  (OperationKind.None) (Syntax: 'r')
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
IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: ?, IsInvalid) (Syntax: '(a)')
  Operand: 
    IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'a')
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
