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
        [Fact]
        public void ConditionalExpression_01()
        {
            string source = @"
class P
{
    private void M()
    {
        int i = 0;
        int j = 2;
        var z = (/*<bind>*/true ? i : j/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: System.Int32) (Syntax: 'true ? i : j')
  Condition: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
  WhenTrue: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
  WhenFalse: 
    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ConditionalExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConditionalExpression_02()
        {
            string source = @"
class P
{
    private void M()
    {
        int i = 0;
        int j = 2;
        (/*<bind>*/true ? ref i : ref j/*</bind>*/) = 4;
    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (IsRef) (OperationKind.Conditional, Type: System.Int32) (Syntax: 'true ? ref i : ref j')
  Condition: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
  WhenTrue: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
  WhenFalse: 
    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ConditionalExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ConditionalExpressionFlow_01()
        {
            string source = @"
class P
{
    void M(bool a, bool b)
/*<bind>*/{
        GetArray()[0] =  a && b ? 1 : 2;
    }/*</bind>*/

    static int[] GetArray() => null;
}
";
            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()[0]')
          Left: 
            IFlowCaptureOperation: 0 (IsInitialization: True) (OperationKind.FlowCapture, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()[0]')
          Right: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'GetArray()[0]')
              Array reference: 
                IInvocationOperation (System.Int32[] P.GetArray()) (OperationKind.Invocation, Type: System.Int32[]) (Syntax: 'GetArray()')
                  Instance Receiver: 
                    null
                  Arguments(0)
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

    Jump if False to Block[4]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
    Statements (0)
    Jump if False to Block[4]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next Block[3]
Block[3] - Block
    Predecessors (1)
        [2]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '1')
          Left: 
            IFlowCaptureOperation: 1 (IsInitialization: True) (OperationKind.FlowCapture, Type: System.Int32, IsImplicit) (Syntax: 'a && b ? 1 : 2')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Next Block[5]
Block[4] - Block
    Predecessors (2)
        [1]
        [2]
    Statements (1)
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '2')
          Left: 
            IFlowCaptureOperation: 1 (IsInitialization: True) (OperationKind.FlowCapture, Type: System.Int32, IsImplicit) (Syntax: 'a && b ? 1 : 2')
          Right: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

    Next Block[5]
Block[5] - Block
    Predecessors (2)
        [3]
        [4]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'GetArray()[ ...  b ? 1 : 2;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'GetArray()[ ... & b ? 1 : 2')
              Left: 
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: System.Int32, IsImplicit) (Syntax: 'GetArray()[0]')
              Right: 
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: System.Int32, IsImplicit) (Syntax: 'a && b ? 1 : 2')

    Next Block[6]
Block[6] - Exit
    Predecessors (1)
        [5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }
    }
}
