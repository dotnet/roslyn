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
    }
}
