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
    public class IOperationTests_IAddressOfOperation : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void AddressOfFlow_01()
        {
            string source = @"
class C
{
    unsafe void M(int i)
    /*<bind>*/{
        int* p = &i;
    }/*</bind>*/
}";

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32* p]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32*, IsImplicit) (Syntax: 'p = &i')
              Left: 
                ILocalReferenceOperation: p (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32*, IsImplicit) (Syntax: 'p = &i')
              Right: 
                IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*) (Syntax: '&i')
                  Reference: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics, compilationOptions: TestOptions.UnsafeDebugDll);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void AddressOfFlow_02()
        {
            string source = @"
struct S2
{
    unsafe void M(bool x, S2* p1, S2* p2, int* p3)
    /*<bind>*/
    {
        p3 = &(x ? p1 : p2)->i;
    }/*</bind>*/
    public int i;
}";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p3')
              Value: 
                IParameterReferenceOperation: p3 (OperationKind.ParameterReference, Type: System.Int32*) (Syntax: 'p3')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p1')
              Value: 
                IParameterReferenceOperation: p1 (OperationKind.ParameterReference, Type: S2*) (Syntax: 'p1')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p2')
              Value: 
                IParameterReferenceOperation: p2 (OperationKind.ParameterReference, Type: S2*) (Syntax: 'p2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p3 = &(x ? p1 : p2)->i;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32*) (Syntax: 'p3 = &(x ? p1 : p2)->i')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32*, IsImplicit) (Syntax: 'p3')
                  Right: 
                    IAddressOfOperation (OperationKind.AddressOf, Type: System.Int32*) (Syntax: '&(x ? p1 : p2)->i')
                      Reference: 
                        IFieldReferenceOperation: System.Int32 S2.i (OperationKind.FieldReference, Type: System.Int32) (Syntax: '(x ? p1 : p2)->i')
                          Instance Receiver: 
                            IOperation:  (OperationKind.None, Type: S2, IsImplicit) (Syntax: '(x ? p1 : p2)')
                              Children(1):
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: S2*, IsImplicit) (Syntax: 'x ? p1 : p2')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics, TestOptions.UnsafeDebugDll);
        }
    }
}
