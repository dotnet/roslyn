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
ITupleBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.TupleBinaryOperator, Type: System.Boolean) (Syntax: 'x == y')
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
ITupleBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.TupleBinaryOperator, Type: System.Boolean) (Syntax: 'x == (1, 2)')
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
ITupleBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.TupleBinaryOperator, Type: System.Boolean) (Syntax: '(1, 2) != y')
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
ITupleBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.TupleBinaryOperator, Type: System.Boolean) (Syntax: '(null, (1,  ... l, (3L, 4))')
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
ITupleBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.TupleBinaryOperator, Type: System.Boolean) (Syntax: 'y == default')
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

        [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        public void TupleBinaryOperator_NoControlFlow()
        {
            var source = @"
class C
{
    void F((int, int) x, (int, int) y, bool b)
    /*<bind>*/
    {
        b = x == y;
    }/*</bind>*/
}";

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = x == y;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = x == y')
              Left: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
              Right: 
                ITupleBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.TupleBinaryOperator, Type: System.Boolean) (Syntax: 'x == y')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: (System.Int32, System.Int32)) (Syntax: 'x')
                  Right: 
                    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: (System.Int32, System.Int32)) (Syntax: 'y')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        public void TupleBinaryOperator_ControlFlowInLeftOperand()
        {
            var source = @"
class C
{
    void F((int, int)? x1, (int, int) x2, (int, int) y, bool b)
    /*<bind>*/
    {
        b = (x1 ?? x2) == y;
    }/*</bind>*/
}";

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x1')
          Value: 
            IParameterReferenceOperation: x1 (OperationKind.ParameterReference, Type: (System.Int32, System.Int32)?) (Syntax: 'x1')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x1')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: (System.Int32, System.Int32)?, IsImplicit) (Syntax: 'x1')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x1')
          Value: 
            IInvocationOperation ( (System.Int32, System.Int32) (System.Int32, System.Int32)?.GetValueOrDefault()) (OperationKind.Invocation, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 'x1')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: (System.Int32, System.Int32)?, IsImplicit) (Syntax: 'x1')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x2')
          Value: 
            IParameterReferenceOperation: x2 (OperationKind.ParameterReference, Type: (System.Int32, System.Int32)) (Syntax: 'x2')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = (x1 ?? x2) == y;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = (x1 ?? x2) == y')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'b')
              Right: 
                ITupleBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.TupleBinaryOperator, Type: System.Boolean) (Syntax: '(x1 ?? x2) == y')
                  Left: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 'x1 ?? x2')
                  Right: 
                    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: (System.Int32, System.Int32)) (Syntax: 'y')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        public void TupleBinaryOperator_ControlFlowInRightOperand()
        {
            var source = @"
class C
{
    void F((int, int)? x1, (int, int) x2, (int, int) y, bool b)
    /*<bind>*/
    {
        b = y == (x1 ?? x2);
    }/*</bind>*/
}";

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
          Value: 
            IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: (System.Int32, System.Int32)) (Syntax: 'y')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x1')
          Value: 
            IParameterReferenceOperation: x1 (OperationKind.ParameterReference, Type: (System.Int32, System.Int32)?) (Syntax: 'x1')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x1')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: (System.Int32, System.Int32)?, IsImplicit) (Syntax: 'x1')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x1')
          Value: 
            IInvocationOperation ( (System.Int32, System.Int32) (System.Int32, System.Int32)?.GetValueOrDefault()) (OperationKind.Invocation, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 'x1')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: (System.Int32, System.Int32)?, IsImplicit) (Syntax: 'x1')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x2')
          Value: 
            IParameterReferenceOperation: x2 (OperationKind.ParameterReference, Type: (System.Int32, System.Int32)) (Syntax: 'x2')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = y == (x1 ?? x2);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = y == (x1 ?? x2)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'b')
              Right: 
                ITupleBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.TupleBinaryOperator, Type: System.Boolean) (Syntax: 'y == (x1 ?? x2)')
                  Left: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 'y')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 'x1 ?? x2')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        public void TupleBinaryOperator_ControlFlowInBothOperands()
        {
            var source = @"
class C
{
    void F((int, int)? x1, (int, int) x2, (int, int)? y1, (int, int) y2, bool b)
    /*<bind>*/
    {
        b = (x1 ?? x2) == (y1 ?? y2);
    }/*</bind>*/
}";

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x1')
          Value: 
            IParameterReferenceOperation: x1 (OperationKind.ParameterReference, Type: (System.Int32, System.Int32)?) (Syntax: 'x1')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x1')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: (System.Int32, System.Int32)?, IsImplicit) (Syntax: 'x1')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x1')
          Value: 
            IInvocationOperation ( (System.Int32, System.Int32) (System.Int32, System.Int32)?.GetValueOrDefault()) (OperationKind.Invocation, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 'x1')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: (System.Int32, System.Int32)?, IsImplicit) (Syntax: 'x1')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x2')
          Value: 
            IParameterReferenceOperation: x2 (OperationKind.ParameterReference, Type: (System.Int32, System.Int32)) (Syntax: 'x2')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y1')
          Value: 
            IParameterReferenceOperation: y1 (OperationKind.ParameterReference, Type: (System.Int32, System.Int32)?) (Syntax: 'y1')

    Jump if True (Regular) to Block[B6]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'y1')
          Operand: 
            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: (System.Int32, System.Int32)?, IsImplicit) (Syntax: 'y1')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y1')
          Value: 
            IInvocationOperation ( (System.Int32, System.Int32) (System.Int32, System.Int32)?.GetValueOrDefault()) (OperationKind.Invocation, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 'y1')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: (System.Int32, System.Int32)?, IsImplicit) (Syntax: 'y1')
              Arguments(0)

    Next (Regular) Block[B7]
Block[B6] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y2')
          Value: 
            IParameterReferenceOperation: y2 (OperationKind.ParameterReference, Type: (System.Int32, System.Int32)) (Syntax: 'y2')

    Next (Regular) Block[B7]
Block[B7] - Block
    Predecessors: [B5] [B6]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = (x1 ??  ... (y1 ?? y2);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = (x1 ??  ...  (y1 ?? y2)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'b')
              Right: 
                ITupleBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.TupleBinaryOperator, Type: System.Boolean) (Syntax: '(x1 ?? x2) == (y1 ?? y2)')
                  Left: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 'x1 ?? x2')
                  Right: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 'y1 ?? y2')

    Next (Regular) Block[B8]
Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        public void TupleBinaryOperator_TupleExpressions_NoControlFlow()
        {
            var source = @"
class C
{
    void F(int i1, int i2, int i3, int i4, bool b)
    /*<bind>*/
    {
        b = (i1, i2) == (i3, i4);
    }/*</bind>*/
}";

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = (i1, i2 ... = (i3, i4);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = (i1, i2) == (i3, i4)')
              Left: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
              Right: 
                ITupleBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.TupleBinaryOperator, Type: System.Boolean) (Syntax: '(i1, i2) == (i3, i4)')
                  Left: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(i1, i2)')
                      NaturalType: (System.Int32 i1, System.Int32 i2)
                      Elements(2):
                          IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')
                          IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')
                  Right: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i3, System.Int32 i4)) (Syntax: '(i3, i4)')
                      NaturalType: (System.Int32 i3, System.Int32 i4)
                      Elements(2):
                          IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i3')
                          IParameterReferenceOperation: i4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i4')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        public void TupleBinaryOperator_TupleExpressions_ControlFlowInLeftOperand()
        {
            var source = @"
class C
{
    void F(int i1, int? i2, int i3, int i4, int i5, bool b)
    /*<bind>*/
    {
        b = (i1, i2 ?? i5) == (i3, i4);
    }/*</bind>*/
}";

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (3)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
          Value: 
            IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
          Value: 
            IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i2')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i2')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i2')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i2')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i2')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i5')
          Value: 
            IParameterReferenceOperation: i5 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i5')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = (i1, i2 ... = (i3, i4);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = (i1, i2 ... == (i3, i4)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'b')
              Right: 
                ITupleBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.TupleBinaryOperator, Type: System.Boolean) (Syntax: '(i1, i2 ??  ... == (i3, i4)')
                  Left: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32)) (Syntax: '(i1, i2 ?? i5)')
                      NaturalType: (System.Int32 i1, System.Int32)
                      Elements(2):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                          IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i2 ?? i5')
                  Right: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i3, System.Int32 i4)) (Syntax: '(i3, i4)')
                      NaturalType: (System.Int32 i3, System.Int32 i4)
                      Elements(2):
                          IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i3')
                          IParameterReferenceOperation: i4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i4')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        public void TupleBinaryOperator_TupleExpressions_ControlFlowInRightOperand()
        {
            var source = @"
class C
{
    void F(int i1, int i2, int i3, int? i4, int i5, bool b)
    /*<bind>*/
    {
        b = (i1, i2) == (i3, i4 ?? i5);
    }/*</bind>*/
}";

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (5)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
          Value: 
            IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
          Value: 
            IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i3')
          Value: 
            IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i3')

        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i4')
          Value: 
            IParameterReferenceOperation: i4 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i4')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i4')
          Operand: 
            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i4')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i4')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i4')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i4')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i5')
          Value: 
            IParameterReferenceOperation: i5 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i5')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = (i1, i2 ...  i4 ?? i5);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = (i1, i2 ... , i4 ?? i5)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'b')
              Right: 
                ITupleBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.TupleBinaryOperator, Type: System.Boolean) (Syntax: '(i1, i2) == ... , i4 ?? i5)')
                  Left: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(i1, i2)')
                      NaturalType: (System.Int32 i1, System.Int32 i2)
                      Elements(2):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i2')
                  Right: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i3, System.Int32)) (Syntax: '(i3, i4 ?? i5)')
                      NaturalType: (System.Int32 i3, System.Int32)
                      Elements(2):
                          IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i3')
                          IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i4 ?? i5')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        public void TupleBinaryOperator_TupleExpressions_ControlFlowInBothOperands()
        {
            var source = @"
class C
{
    void F(int? i1, int i2, int i3, int? i4, int i5, int i6, bool b)
    /*<bind>*/
    {
        b = (i1 ?? i5, i2) == (i3, i4 ?? i6);
    }/*</bind>*/
}";

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
          Value: 
            IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i1')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i1')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i5')
          Value: 
            IParameterReferenceOperation: i5 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i5')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (3)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
          Value: 
            IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i3')
          Value: 
            IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i3')

        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i4')
          Value: 
            IParameterReferenceOperation: i4 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i4')

    Jump if True (Regular) to Block[B6]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i4')
          Operand: 
            IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i4')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i4')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i4')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i4')
              Arguments(0)

    Next (Regular) Block[B7]
Block[B6] - Block
    Predecessors: [B4]
    Statements (1)
        IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i6')
          Value: 
            IParameterReferenceOperation: i6 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i6')

    Next (Regular) Block[B7]
Block[B7] - Block
    Predecessors: [B5] [B6]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = (i1 ??  ...  i4 ?? i6);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = (i1 ??  ... , i4 ?? i6)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'b')
              Right: 
                ITupleBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.TupleBinaryOperator, Type: System.Boolean) (Syntax: '(i1 ?? i5,  ... , i4 ?? i6)')
                  Left: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32 i2)) (Syntax: '(i1 ?? i5, i2)')
                      NaturalType: (System.Int32, System.Int32 i2)
                      Elements(2):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1 ?? i5')
                          IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i2')
                  Right: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i3, System.Int32)) (Syntax: '(i3, i4 ?? i6)')
                      NaturalType: (System.Int32 i3, System.Int32)
                      Elements(2):
                          IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i3')
                          IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i4 ?? i6')

    Next (Regular) Block[B8]
Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
