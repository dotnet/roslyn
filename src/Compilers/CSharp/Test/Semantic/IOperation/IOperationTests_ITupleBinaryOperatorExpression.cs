// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.IOperation)]
    public partial class IOperationTests : SemanticModelTestBase
    {
        [Fact]
        public void VerifyTupleEqualityBinaryOperator()
        {
            var source = @"
class C
{
    bool F((int, int) x, (int, int) y)
    {
        return /*<bind>*/x == y/*</bind>*/;
    }
}";

            string expectedOperationTree =
@"
ITupleBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x == y')
  Left: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: (System.Int32, System.Int32)) (Syntax: 'x')
  Right: 
    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: (System.Int32, System.Int32)) (Syntax: 'y')
";

            VerifyOperationTreeForTest<BinaryExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact]
        public void VerifyTupleEqualityBinaryOperator_WithTupleLiteral()
        {
            var source = @"
class C
{
    bool F((int, int) x)
    {
        return /*<bind>*/x == (1, 2)/*</bind>*/;
    }
}";

            string expectedOperationTree =
@"
ITupleBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'x == (1, 2)')
  Left: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: (System.Int32, System.Int32)) (Syntax: 'x')
  Right: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
      NaturalType: (System.Int32, System.Int32)
      Elements(2):
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";

            VerifyOperationTreeForTest<BinaryExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact]
        public void VerifyTupleEqualityBinaryOperator_WithNotEquals()
        {
            var source = @"
class C
{
    bool F((long, byte) y)
    {
        return /*<bind>*/(1, 2) != y/*</bind>*/;
    }
}";

            string expectedOperationTree =
@"
ITupleBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: '(1, 2) != y')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int64, System.Int32)) (Syntax: '(1, 2)')
      NaturalType: (System.Int32, System.Int32)
      Elements(2):
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 1, IsImplicit) (Syntax: '1')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
  Right: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int64, System.Int32), IsImplicit) (Syntax: 'y')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: (System.Int64, System.Byte)) (Syntax: 'y')
";

            VerifyOperationTreeForTest<BinaryExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact]
        public void VerifyTupleEqualityBinaryOperator_WithNullsAndConversions()
        {
            var source = @"
class C
{
    bool F()
    {
        return /*<bind>*/(null, (1, 2L)) == (null, (3L, 4))/*</bind>*/;
    }
}";

            string expectedOperationTree =
@"
ITupleBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: '(null, (1,  ... l, (3L, 4))')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: null) (Syntax: '(null, (1, 2L))')
      NaturalType: null
      Elements(2):
          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
          ITupleOperation (OperationKind.Tuple, Type: (System.Int64, System.Int64)) (Syntax: '(1, 2L)')
            NaturalType: (System.Int32, System.Int64)
            Elements(2):
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 1, IsImplicit) (Syntax: '1')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ILiteralOperation (OperationKind.Literal, Type: System.Int64, Constant: 2) (Syntax: '2L')
  Right: 
    ITupleOperation (OperationKind.Tuple, Type: null) (Syntax: '(null, (3L, 4))')
      NaturalType: null
      Elements(2):
          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
          ITupleOperation (OperationKind.Tuple, Type: (System.Int64, System.Int64)) (Syntax: '(3L, 4)')
            NaturalType: (System.Int64, System.Int32)
            Elements(2):
                ILiteralOperation (OperationKind.Literal, Type: System.Int64, Constant: 3) (Syntax: '3L')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 4, IsImplicit) (Syntax: '4')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
";

            VerifyOperationTreeForTest<BinaryExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact]
        public void VerifyTupleEqualityBinaryOperator_WithDefault()
        {
            var source = @"
class C
{
    bool F((int, string) y)
    {
        return /*<bind>*/y == default/*</bind>*/;
    }
}";

            string expectedOperationTree =
@"
ITupleBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'y == default')
  Left: 
    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: (System.Int32, System.String)) (Syntax: 'y')
  Right: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int32, System.String), IsImplicit) (Syntax: 'default')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IDefaultValueOperation (OperationKind.DefaultValue, Type: (System.Int32, System.String)) (Syntax: 'default')
";

            VerifyOperationTreeForTest<BinaryExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact]
        public void VerifyTupleEqualityBinaryOperator_VerifyChildren()
        {
            string source = @"
class C
{
    void M1((int, int) t1, long l)
    {
        _ = /*<bind>*/t1 == (l, l)/*</bind>*/;
    }
}
";
            var compilation = CreateEmptyCompilation(source);
            (var operation, _) = GetOperationAndSyntaxForTest<BinaryExpressionSyntax>(compilation);
            var equals = (ITupleBinaryOperation)operation;
            Assert.Equal(OperationKind.SimpleAssignment, equals.Parent.Kind);
            Assert.Equal(2, equals.Children.Count());

            var left = equals.Children.ElementAt(0);
            Assert.Equal(OperationKind.Conversion, left.Kind);
            Assert.Equal(OperationKind.ParameterReference, left.Children.Single().Kind);

            var right = equals.Children.ElementAt(1);
            Assert.Equal(OperationKind.Tuple, right.Kind);
        }
    }
}
