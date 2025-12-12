// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IIncrementOrDecrementOperation : SemanticModelTestBase
    {

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IncrementOrDecrementFlow_01()
        {
            string source = @"
class C
{
    void M(int i, int j)
    /*<bind>*/{
        j = --i;
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
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = --i;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = --i')
              Left: 
                IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')
              Right: 
                IIncrementOrDecrementOperation (Prefix) (OperationKind.Decrement, Type: System.Int32) (Syntax: '--i')
                  Target: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IncrementOrDecrementFlow_02()
        {
            string source = @"
class C
{
    void M(D x, D y)
    /*<bind>*/{
        (x ?? y).i++;
    }/*</bind>*/
    public class D { public int i; }
}

";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                  Value: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C.D) (Syntax: 'x')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.D, IsImplicit) (Syntax: 'x')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.D, IsImplicit) (Syntax: 'x')

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
              Value: 
                IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: C.D) (Syntax: 'y')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '(x ?? y).i++;')
              Expression: 
                IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: '(x ?? y).i++')
                  Target: 
                    IFieldReferenceOperation: System.Int32 C.D.i (OperationKind.FieldReference, Type: System.Int32) (Syntax: '(x ?? y).i')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C.D, IsImplicit) (Syntax: 'x ?? y')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IncrementOrDecrementFlow_03()
        {
            string source = @"
class C
{
    void M(D x, D y)
    /*<bind>*/{
        (x ?? y).i++;
    }/*</bind>*/
    public class D { public S1 i; }
}

struct S1
{
    public void operator ++() {}
}
" + CompilerFeatureRequiredAttribute;

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                  Value:
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C.D) (Syntax: 'x')
            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Operand:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.D, IsImplicit) (Syntax: 'x')
                Leaving: {R2}
            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                  Value:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.D, IsImplicit) (Syntax: 'x')
            Next (Regular) Block[B4]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
              Value:
                IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: C.D) (Syntax: 'y')
        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '(x ?? y).i++;')
              Expression:
                IIncrementOrDecrementOperation (Postfix) (OperatorMethod: void S1.op_IncrementAssignment()) (OperationKind.Increment, Type: System.Void) (Syntax: '(x ?? y).i++')
                  Target:
                    IFieldReferenceOperation: S1 C.D.i (OperationKind.FieldReference, Type: S1) (Syntax: '(x ?? y).i')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C.D, IsImplicit) (Syntax: 'x ?? y')
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Extensions_01()
        {
            string source = @"
class A
{
    CustomType Method(CustomType x)
    {
        return /*<bind>*/--x/*</bind>*/;
    }
}
public struct CustomType
{
}

static class Extensions
{
    extension(CustomType)
    {
        public static CustomType operator --(CustomType x)
        {
            return x;
        }
    }
}
";
            string expectedOperationTree = @"
IIncrementOrDecrementOperation (Prefix) (OperatorMethod: CustomType Extensions.<G>$9F7826FAF592F1266BEA2CA4AC24ECDD.op_Decrement(CustomType x)) (OperationKind.Decrement, Type: CustomType) (Syntax: '--x')
  Target:
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: CustomType) (Syntax: 'x')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Extensions_02()
        {
            string source = @"
class A
{
    CustomType Method(CustomType x)
    {
        return /*<bind>*/--x/*</bind>*/;
    }
}
public struct CustomType
{
}

static class Extensions
{
    extension(ref CustomType x)
    {
        public void operator --()
        {
            return x;
        }
    }
}

" + CompilerFeatureRequiredAttribute;

            string expectedOperationTree = @"
IIncrementOrDecrementOperation (Prefix) (OperatorMethod: void Extensions.<G>$9F7826FAF592F1266BEA2CA4AC24ECDD.op_DecrementAssignment()) (OperationKind.Decrement, Type: CustomType) (Syntax: '--x')
  Target:
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: CustomType) (Syntax: 'x')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Extensions_03()
        {
            string source = @"
class A
{
    CustomType Method(CustomType x)
    {
        return /*<bind>*/--x/*</bind>*/;
    }
}
public class CustomType
{
}

static class Extensions
{
    extension(object)
    {
        public void operator --()
        {
            return x;
        }
    }
}

" + CompilerFeatureRequiredAttribute;

            string expectedOperationTree = @"
IIncrementOrDecrementOperation (Prefix) (OperatorMethod: void Extensions.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.op_DecrementAssignment()) (OperationKind.Decrement, Type: CustomType) (Syntax: '--x')
  Target:
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: CustomType) (Syntax: 'x')
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void Extensions_04_Flow()
        {
            string source = @"
class C
{
    void M(D x, D y)
    /*<bind>*/{
        (x ?? y).i++;
    }/*</bind>*/
    public class D { public S1 i; }
}

struct S1
{
}

static class Extensions
{
    extension(S1)
    {
        public static S1 operator ++(S1 x)
        {
            return x;
        }
    }
}
" + CompilerFeatureRequiredAttribute;

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                  Value:
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C.D) (Syntax: 'x')
            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Operand:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.D, IsImplicit) (Syntax: 'x')
                Leaving: {R2}
            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                  Value:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.D, IsImplicit) (Syntax: 'x')
            Next (Regular) Block[B4]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
              Value:
                IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: C.D) (Syntax: 'y')
        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '(x ?? y).i++;')
              Expression:
                IIncrementOrDecrementOperation (Postfix) (OperatorMethod: S1 Extensions.<G>$78CFE6F93D970DBBE44B05C24FFEB91E.op_Increment(S1 x)) (OperationKind.Increment, Type: S1) (Syntax: '(x ?? y).i++')
                  Target:
                    IFieldReferenceOperation: S1 C.D.i (OperationKind.FieldReference, Type: S1) (Syntax: '(x ?? y).i')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C.D, IsImplicit) (Syntax: 'x ?? y')
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void Extensions_05_Flow()
        {
            string source = @"
class C
{
    void M(D x, D y)
    /*<bind>*/{
        (x ?? y).i++;
    }/*</bind>*/
    public class D { public S1 i; }
}

struct S1
{
}

static class Extensions
{
    extension(ref S1 x)
    {
        public void operator ++() {}
    }
}
" + CompilerFeatureRequiredAttribute;

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                  Value:
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C.D) (Syntax: 'x')
            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Operand:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.D, IsImplicit) (Syntax: 'x')
                Leaving: {R2}
            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                  Value:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.D, IsImplicit) (Syntax: 'x')
            Next (Regular) Block[B4]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
              Value:
                IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: C.D) (Syntax: 'y')
        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '(x ?? y).i++;')
              Expression:
                IIncrementOrDecrementOperation (Postfix) (OperatorMethod: void Extensions.<G>$78CFE6F93D970DBBE44B05C24FFEB91E.op_IncrementAssignment()) (OperationKind.Increment, Type: System.Void) (Syntax: '(x ?? y).i++')
                  Target:
                    IFieldReferenceOperation: S1 C.D.i (OperationKind.FieldReference, Type: S1) (Syntax: '(x ?? y).i')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C.D, IsImplicit) (Syntax: 'x ?? y')
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
