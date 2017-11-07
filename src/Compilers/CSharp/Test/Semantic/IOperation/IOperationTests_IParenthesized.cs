// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
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
            // GetOperation returns null for ParenthesizedExpressionSyntax
            Assert.Null(GetOperationTreeForTest<ParenthesizedExpressionSyntax>(source));
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
            // GetOperation returns null for ParenthesizedExpressionSyntax
            Assert.Null(GetOperationTreeForTest<ParenthesizedExpressionSyntax>(source));
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedMultipleNestingParent()
        {
            string source = @"
class P
{
    static int M1(int a, int b)
    {
        /*<bind>*/return (((a + b)));/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return (((a + b)));')
  ReturnedValue: 
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
            // GetOperation returns null for ParenthesizedExpressionSyntax
            Assert.Null(GetOperationTreeForTest<ParenthesizedExpressionSyntax>(source));
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedMultipleNesting02Parent()
        {
            string source = @"
class P
{
    static int M1(int a, int b, int c)
    {
        /*<bind>*/return (((a + b) * c));/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return (((a + b) * c));')
  ReturnedValue: 
    IBinaryOperation (BinaryOperatorKind.Multiply) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '(a + b) * c')
      Left: 
        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'a + b')
          Left: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
          Right: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
      Right: 
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ReturnStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
            // GetOperation returns null for ParenthesizedExpressionSyntax
            Assert.Null(GetOperationTreeForTest<ParenthesizedExpressionSyntax>(source));
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedMultipleNesting03Parent()
        {
            string source = @"
class P
{
    static int M1(int a, int b)
    {
        /*<bind>*/return (((a + b)));/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return (((a + b)));')
  ReturnedValue: 
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
            // GetOperation returns null for ParenthesizedExpressionSyntax
            Assert.Null(GetOperationTreeForTest<ParenthesizedExpressionSyntax>(source));
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
            // GetOperation returns null for ParenthesizedExpressionSyntax
            Assert.Null(GetOperationTreeForTest<ParenthesizedExpressionSyntax>(source));
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
            // GetOperation returns null for ParenthesizedExpressionSyntax
            Assert.Null(GetOperationTreeForTest<ParenthesizedExpressionSyntax>(source));
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
            // GetOperation returns null for ParenthesizedExpressionSyntax
            Assert.Null(GetOperationTreeForTest<ParenthesizedExpressionSyntax>(source));
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
            // GetOperation returns null for ParenthesizedExpressionSyntax
            Assert.Null(GetOperationTreeForTest<ParenthesizedExpressionSyntax>(source));
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedConstantValueParent()
        {
            string source = @"
class P
{
    static int M1()
    {
        /*<bind>*/return (5);/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return (5);')
  ReturnedValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ReturnStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
            // GetOperation returns null for ParenthesizedExpressionSyntax
            Assert.Null(GetOperationTreeForTest<ParenthesizedExpressionSyntax>(source));
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedQueryClauseParent()
        {
            string source = @"
using System.Linq;

class P
{
    static object M1(int[] a)
    {
        /*<bind>*/return from r in a select (-r);/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return from ... elect (-r);')
  ReturnedValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'from r in a select (-r)')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'from r in a select (-r)')
          Expression: 
            IInvocationOperation (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Select<System.Int32, System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> source, System.Func<System.Int32, System.Int32> selector)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'select (-r)')
              Instance Receiver: 
                null
              Arguments(2):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'from r in a')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from r in a')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '(-r)')
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.Int32, System.Int32>, IsImplicit) (Syntax: '(-r)')
                      Target: 
                        IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: '(-r)')
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: '(-r)')
                            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '(-r)')
                              ReturnedValue: 
                                IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, Type: System.Int32) (Syntax: '-r')
                                  Operand: 
                                    IOperation:  (OperationKind.None, Type: null) (Syntax: 'r')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ReturnStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
            // GetOperation returns null for ParenthesizedExpressionSyntax
            Assert.Null(GetOperationTreeForTest<ParenthesizedExpressionSyntax>(source));
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParenthesizedErrorOperandParent()
        {
            string source = @"
class P
{
    static int M1()
    {
        /*<bind>*/return (a);/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.Return, Type: null, IsInvalid) (Syntax: 'return (a);')
  ReturnedValue: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'a')
      Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'a' does not exist in the current context
                //         return /*<bind>*/(a)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ReturnStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
