// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

/// <summary>
/// <see cref="ControlFlowGraph" /> tests for "chained relational comparison"
/// (C# preview feature; spec §11.11.13). The chain's outer link short-circuits
/// on its inner link's bool result, so the CFG must emit the same conditional
/// edges as <c>&amp;&amp;</c>, with the shared middle operand Y evaluated exactly once
/// via a flow capture reused by both links. See
/// <c>ControlFlowGraphBuilder.VisitChainedRelationalComparison</c> in
/// Microsoft.CodeAnalysis for the visitor logic.
///
/// Runtime / binding tests live in <see cref="ChainedRelationalComparisonTests" />;
/// IL-emit pins in <see cref="ChainedRelationalComparisonEmitTests" />;
/// semantic-model API tests in <see cref="ChainedRelationalComparisonSemanticModelTests" />.
/// </summary>
public sealed class ChainedRelationalComparisonControlFlowTests : CSharpTestBase
{
    [Fact]
    public void Chain_AsIfCondition_EmitsShortCircuitEdges()
    {
        // `if (a < b < c)` - the minimal case proving the CFG builder routes
        // chained relational nodes through the short-circuit path. The CFG
        // must show: evaluate a and b, compute a<b, conditional branch on it,
        // and only on the true edge evaluate c and compute Y<c. If the builder
        // instead took the straight-line path, we'd see a single block doing
        // an ill-typed `bool < int` compare.
        string source = """
            class P
            {
                void M(int a, int b, int c)
            /*<bind>*/{
                    if (a < b < c) { }
                }/*</bind>*/
            }
            """;
        // Two nested regions with two flow captures:
        //
        //   {R1} holds the RESULT capture [0] (bool, written by both branches).
        //   {R2} holds the SHARED MIDDLE OPERAND capture [1] (Y=b), alive only on
        //       the true-path where both links reference it.
        //
        // B1 captures b, then conditionally branches on `a < b` (inner link) -
        // if false it leaves {R2} and jumps to B3 (the short-circuit block).
        // B2 is the true path: it stores the outer link `b < c` (using the
        // captured Y) into result capture [0] then leaves {R2} into B4.
        // B3 stores literal false into result capture [0] (Y already out of
        // scope, which is why {R2} was closed before this point).
        // B4 reads the result capture and branches on it for the surrounding
        // `if` statement.
        string expectedGraph = """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1} {R2}

            .locals {R1}
            {
                CaptureIds: [0]
                .locals {R2}
                {
                    CaptureIds: [1]
                    Block[B1] - Block
                        Predecessors: [B0]
                        Statements (1)
                            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                              Value:
                                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')

                        Jump if False (Regular) to Block[B3]
                            IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b')
                              Left:
                                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
                              Right:
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                            Leaving: {R2}

                        Next (Regular) Block[B2]
                    Block[B2] - Block
                        Predecessors: [B1]
                        Statements (1)
                            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a < b < c')
                              Value:
                                IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b < c')
                                  Left:
                                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                                  Right:
                                    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

                        Next (Regular) Block[B4]
                            Leaving: {R2}
                }

                Block[B3] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a < b < c')
                          Value:
                            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'a < b < c')

                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B2] [B3]
                    Statements (0)
                    Jump if False (Regular) to Block[B5]
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'a < b < c')
                        Leaving: {R1}

                    Next (Regular) Block[B5]
                        Leaving: {R1}
            }

            Block[B5] - Exit
                Predecessors: [B4*2]
                Statements (0)
            """;
        var expectedDiagnostics = DiagnosticDescription.None;
        VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
    }

    [Fact]
    public void Chain_AsAssignment_CaptureResultWithoutEnclosingCondition()
    {
        // Standalone expression `bool r = a < b < c;` - the surrounding statement
        // isn't a conditional, so the CFG has to capture the chain's result into
        // capture [0] and store it into `r` at the end. This pins that the
        // builder's capture-and-flow shape works outside a consuming conditional.
        string source = """
            class P
            {
                void M(int a, int b, int c)
            /*<bind>*/{
                    bool r = a < b < c;
                }/*</bind>*/
            }
            """;
        string expectedGraph = """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1} {R2}
            .locals {R1}
            {
                Locals: [System.Boolean r]
                CaptureIds: [0]
                .locals {R2}
                {
                    CaptureIds: [1]
                    Block[B1] - Block
                        Predecessors: [B0]
                        Statements (1)
                            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                              Value:
                                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
                        Jump if False (Regular) to Block[B3]
                            IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b')
                              Left:
                                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
                              Right:
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                            Leaving: {R2}
                        Next (Regular) Block[B2]
                    Block[B2] - Block
                        Predecessors: [B1]
                        Statements (1)
                            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a < b < c')
                              Value:
                                IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b < c')
                                  Left:
                                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                                  Right:
                                    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')
                        Next (Regular) Block[B4]
                            Leaving: {R2}
                }
                Block[B3] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a < b < c')
                          Value:
                            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'a < b < c')
                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B2] [B3]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'r = a < b < c')
                          Left:
                            ILocalReferenceOperation: r (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'r = a < b < c')
                          Right:
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'a < b < c')
                    Next (Regular) Block[B5]
                        Leaving: {R1}
            }
            Block[B5] - Exit
                Predecessors: [B4]
                Statements (0)
            """;
        var expectedDiagnostics = DiagnosticDescription.None;
        VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
    }

    [Fact]
    public void Chain_InsideWhileLoop_LoopConditionEmitsShortCircuit()
    {
        // `while (a < b < c)` - the chain is the loop's condition. The loop's
        // back-edge re-enters {R1} {R2} on each iteration (B5 loops back to B1)
        // so Y's capture is freshly established every pass; the short-circuit
        // structure inside the chain is the same as the standalone `if` case.
        string source = """
            class P
            {
                void M(int a, int b, int c)
            /*<bind>*/{
                    while (a < b < c) { }
                }/*</bind>*/
            }
            """;
        string expectedGraph = """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1} {R2}
            .locals {R1}
            {
                CaptureIds: [0]
                .locals {R2}
                {
                    CaptureIds: [1]
                    Block[B1] - Block
                        Predecessors: [B0] [B5]
                        Statements (1)
                            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                              Value:
                                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
                        Jump if False (Regular) to Block[B3]
                            IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b')
                              Left:
                                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
                              Right:
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                            Leaving: {R2}
                        Next (Regular) Block[B2]
                    Block[B2] - Block
                        Predecessors: [B1]
                        Statements (1)
                            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a < b < c')
                              Value:
                                IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b < c')
                                  Left:
                                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                                  Right:
                                    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')
                        Next (Regular) Block[B4]
                            Leaving: {R2}
                }
                Block[B3] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a < b < c')
                          Value:
                            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'a < b < c')
                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B2] [B3]
                    Statements (0)
                    Jump if False (Regular) to Block[B6]
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'a < b < c')
                        Leaving: {R1}
                    Next (Regular) Block[B5]
                        Leaving: {R1}
            }
            Block[B5] - Block
                Predecessors: [B4]
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1} {R2}
            Block[B6] - Exit
                Predecessors: [B4]
                Statements (0)
            """;
        var expectedDiagnostics = DiagnosticDescription.None;
        VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
    }

    [Fact]
    public void Chain_WithSideEffectingMiddleOperand_YCapturedOncePerIteration()
    {
        // `a < Middle() < c` where Middle() has side effects. The IInvocation
        // for Middle() appears EXACTLY ONCE in the CFG (captured into capture
        // [1] in B1, then referenced twice: once as the right operand of the
        // inner compare, once as the left operand of the outer compare via a
        // IFlowCaptureReferenceOperation). If the builder took the straight-line
        // path or naively re-visited innerOp.RightOperand, Middle() would
        // appear TWICE - the single-evaluation CFG invariant is what this test
        // guards.
        string source = """
            class P
            {
                static int Middle() => 5;
                void M(int a, int c)
            /*<bind>*/{
                    bool r = a < Middle() < c;
                }/*</bind>*/
            }
            """;
        string expectedGraph = """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1} {R2}
            .locals {R1}
            {
                Locals: [System.Boolean r]
                CaptureIds: [0]
                .locals {R2}
                {
                    CaptureIds: [1]
                    Block[B1] - Block
                        Predecessors: [B0]
                        Statements (1)
                            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Middle()')
                              Value:
                                IInvocationOperation (System.Int32 P.Middle()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Middle()')
                                  Instance Receiver:
                                    null
                                  Arguments(0)
                        Jump if False (Regular) to Block[B3]
                            IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < Middle()')
                              Left:
                                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
                              Right:
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'Middle()')
                            Leaving: {R2}
                        Next (Regular) Block[B2]
                    Block[B2] - Block
                        Predecessors: [B1]
                        Statements (1)
                            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a < Middle() < c')
                              Value:
                                IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < Middle() < c')
                                  Left:
                                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'Middle()')
                                  Right:
                                    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')
                        Next (Regular) Block[B4]
                            Leaving: {R2}
                }
                Block[B3] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a < Middle() < c')
                          Value:
                            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'a < Middle() < c')
                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B2] [B3]
                    Statements (1)
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'r = a < Middle() < c')
                          Left:
                            ILocalReferenceOperation: r (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'r = a < Middle() < c')
                          Right:
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'a < Middle() < c')
                    Next (Regular) Block[B5]
                        Leaving: {R1}
            }
            Block[B5] - Exit
                Predecessors: [B4]
                Statements (0)
            """;
        var expectedDiagnostics = DiagnosticDescription.None;
        VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
    }

    [Fact]
    public void Chain_NAry_FourOperands_TwoNestedShortCircuits()
    {
        // `a < b < c < d` - spec §11.11.13 expands to `(a<b) && (b<c) && (c<d)`
        // with b and c each evaluated exactly once. Both intermediate chained
        // nodes (`a<b<c` and `a<b<c<d`) carry IsChainedRelationalComparison=true,
        // and the CFG builder walks the chain's spine emitting one short-circuit
        // per inner link plus one final result capture for the outermost check.
        //
        // The generated CFG has:
        //   - Two nested sub-regions {R2} and {R3} holding captures [1] (= b)
        //     and [2] (= c) respectively, both nested inside the result region
        //     {R1} which holds capture [0] (the chain's overall bool result).
        //   - Two short-circuits (B1's Jump-if-False and B2's Jump-if-False),
        //     each targeting the shared fall-through block that stores literal
        //     false into the result capture.
        //   - B3 is the all-true path: it captures `c < d` (using the captured
        //     `c` from {R3}) as the final result.
        //
        // The `b` capture is declared in {R2} (outer sub-region) and referenced
        // from blocks inside {R3} for the `b < c` check - a cross-region
        // reference that the CFG verifier accepts via the
        // isChainedRelationalMiddleOperandReference carve-out matching the
        // syntactic shape of a chained shared middle operand.
        string source = """
            class P
            {
                void M(int a, int b, int c, int d)
            /*<bind>*/{
                    if (a < b < c < d) { }
                }/*</bind>*/
            }
            """;
        string expectedGraph = """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1} {R2}
            .locals {R1}
            {
                CaptureIds: [0]
                .locals {R2}
                {
                    CaptureIds: [1]
                    Block[B1] - Block
                        Predecessors: [B0]
                        Statements (1)
                            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                              Value:
                                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
                        Jump if False (Regular) to Block[B4]
                            IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b')
                              Left:
                                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
                              Right:
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                            Leaving: {R2}
                        Next (Regular) Block[B2]
                            Entering: {R3}
                    .locals {R3}
                    {
                        CaptureIds: [2]
                        Block[B2] - Block
                            Predecessors: [B1]
                            Statements (1)
                                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
                                  Value:
                                    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')
                            Jump if False (Regular) to Block[B4]
                                IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b < c < d')
                                  Left:
                                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                                  Right:
                                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'c')
                                Leaving: {R3} {R2}
                            Next (Regular) Block[B3]
                        Block[B3] - Block
                            Predecessors: [B2]
                            Statements (1)
                                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a < b < c < d')
                                  Value:
                                    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b < c < d')
                                      Left:
                                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'c')
                                      Right:
                                        IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd')
                            Next (Regular) Block[B5]
                                Leaving: {R3} {R2}
                    }
                }
                Block[B4] - Block
                    Predecessors: [B1] [B2]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a < b < c < d')
                          Value:
                            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'a < b < c < d')
                    Next (Regular) Block[B5]
                Block[B5] - Block
                    Predecessors: [B3] [B4]
                    Statements (0)
                    Jump if False (Regular) to Block[B6]
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'a < b < c < d')
                        Leaving: {R1}
                    Next (Regular) Block[B6]
                        Leaving: {R1}
            }
            Block[B6] - Exit
                Predecessors: [B5*2]
                Statements (0)
            """;
        var expectedDiagnostics = DiagnosticDescription.None;
        VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
    }

    [Fact]
    public void Chain_NAry_FiveOperands_ThreeNestedShortCircuits()
    {
        // `a < b < c < d < e` - pins that the spine-walking loop scales to
        // arbitrary n (not just 4). For 5 operands we get three shared middles
        // (b, c, d) and four checks (a<b, b<c, c<d, d<e); the CFG should have
        // three nested Y sub-regions (R2, R3, R4), three short-circuits, and
        // one final result capture for the outermost `d<e` check. Block count:
        // 7 blocks (B1..B4 for the four checks, B5 for the false-literal capture,
        // B6 for the if-branch, B7 = exit) plus the entry B0 = 8 total.
        //
        // Guards against an "n=3 works, n=4 works by accident" regression: the
        // 3-operand case degenerates to a single-level structure, the 4-operand
        // case exercises the spine loop with 2 iterations (still might hide off-
        // by-one bugs), and THIS test forces 3 iterations to prove the loop is
        // truly n-ary. Also pins the cross-region capture reference pattern at
        // greater depth: capture [1] (b) is declared in {R2} (outermost Y sub-
        // region) and referenced deep inside {R4} (innermost, for the b<c
        // check) - exercising the `isChainedRelationalMiddleOperandReference`
        // verifier carve-out across TWO enclosing-region layers.
        string source = """
            class P
            {
                void M(int a, int b, int c, int d, int e)
            /*<bind>*/{
                    if (a < b < c < d < e) { }
                }/*</bind>*/
            }
            """;
        string expectedGraph = """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1} {R2}
            .locals {R1}
            {
                CaptureIds: [0]
                .locals {R2}
                {
                    CaptureIds: [1]
                    Block[B1] - Block
                        Predecessors: [B0]
                        Statements (1)
                            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                              Value:
                                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
                        Jump if False (Regular) to Block[B5]
                            IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b')
                              Left:
                                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
                              Right:
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                            Leaving: {R2}
                        Next (Regular) Block[B2]
                            Entering: {R3}
                    .locals {R3}
                    {
                        CaptureIds: [2]
                        Block[B2] - Block
                            Predecessors: [B1]
                            Statements (1)
                                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
                                  Value:
                                    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')
                            Jump if False (Regular) to Block[B5]
                                IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b < c < d')
                                  Left:
                                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                                  Right:
                                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'c')
                                Leaving: {R3} {R2}
                            Next (Regular) Block[B3]
                                Entering: {R4}
                        .locals {R4}
                        {
                            CaptureIds: [3]
                            Block[B3] - Block
                                Predecessors: [B2]
                                Statements (1)
                                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
                                      Value:
                                        IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'd')
                                Jump if False (Regular) to Block[B5]
                                    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b < c < d < e')
                                      Left:
                                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'c')
                                      Right:
                                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'd')
                                    Leaving: {R4} {R3} {R2}
                                Next (Regular) Block[B4]
                            Block[B4] - Block
                                Predecessors: [B3]
                                Statements (1)
                                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a < b < c < d < e')
                                      Value:
                                        IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b < c < d < e')
                                          Left:
                                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'd')
                                          Right:
                                            IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'e')
                                Next (Regular) Block[B6]
                                    Leaving: {R4} {R3} {R2}
                        }
                    }
                }
                Block[B5] - Block
                    Predecessors: [B1] [B2] [B3]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a < b < c < d < e')
                          Value:
                            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'a < b < c < d < e')
                    Next (Regular) Block[B6]
                Block[B6] - Block
                    Predecessors: [B4] [B5]
                    Statements (0)
                    Jump if False (Regular) to Block[B7]
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'a < b < c < d < e')
                        Leaving: {R1}
                    Next (Regular) Block[B7]
                        Leaving: {R1}
            }
            Block[B7] - Exit
                Predecessors: [B6*2]
                Statements (0)
            """;
        var expectedDiagnostics = DiagnosticDescription.None;
        VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
    }

    [Fact]
    public void Chain_AsConditionalExpressionCondition_CapturesFlowIntoBothBranches()
    {
        // `(a < b < c) ? x : y` - three nested regions: {R1} for the conditional's
        // result (capture [2] = x or y), {R2} for the chain's result capture [0],
        // and {R3} for Y (capture [1] = b). After the chain's capture [0] is
        // decided, {R2} and {R3} are left; the remaining branch on capture [0]
        // picks x (B5) or y (B6) into the result capture [2], which is then
        // returned. This pins that the chain's capture structure nests cleanly
        // inside an enclosing conditional expression's own capture region.
        string source = """
            class P
            {
                int M(int a, int b, int c, int x, int y)
            /*<bind>*/{
                    return (a < b < c) ? x : y;
                }/*</bind>*/
            }
            """;
        string expectedGraph = """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1} {R2} {R3}
            .locals {R1}
            {
                CaptureIds: [2]
                .locals {R2}
                {
                    CaptureIds: [0]
                    .locals {R3}
                    {
                        CaptureIds: [1]
                        Block[B1] - Block
                            Predecessors: [B0]
                            Statements (1)
                                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                                  Value:
                                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'b')
                            Jump if False (Regular) to Block[B3]
                                IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b')
                                  Left:
                                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')
                                  Right:
                                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                                Leaving: {R3}
                            Next (Regular) Block[B2]
                        Block[B2] - Block
                            Predecessors: [B1]
                            Statements (1)
                                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a < b < c')
                                  Value:
                                    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b < c')
                                      Left:
                                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                                      Right:
                                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')
                            Next (Regular) Block[B4]
                                Leaving: {R3}
                    }
                    Block[B3] - Block
                        Predecessors: [B1]
                        Statements (1)
                            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a < b < c')
                              Value:
                                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'a < b < c')
                        Next (Regular) Block[B4]
                    Block[B4] - Block
                        Predecessors: [B2] [B3]
                        Statements (0)
                        Jump if False (Regular) to Block[B6]
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'a < b < c')
                            Leaving: {R2}
                        Next (Regular) Block[B5]
                            Leaving: {R2}
                }
                Block[B5] - Block
                    Predecessors: [B4]
                    Statements (1)
                        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                          Value:
                            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                    Next (Regular) Block[B7]
                Block[B6] - Block
                    Predecessors: [B4]
                    Statements (1)
                        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
                          Value:
                            IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
                    Next (Regular) Block[B7]
                Block[B7] - Block
                    Predecessors: [B5] [B6]
                    Statements (0)
                    Next (Return) Block[B8]
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: '(a < b < c) ? x : y')
                        Leaving: {R1}
            }
            Block[B8] - Exit
                Predecessors: [B7]
                Statements (0)
            """;
        var expectedDiagnostics = DiagnosticDescription.None;
        VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
    }

    [Fact]
    public void Chain_WithUserDefinedOperator_ShortCircuitsWithOperatorMethodOnEachLink()
    {
        // Chain with user-defined `operator <` on a struct. Both the inner and
        // outer IBinaryOperation carry OperatorMethod = S.op_LessThan(S, S).
        // The CFG builder's chained-relational dispatch is independent of
        // whether the operator is intrinsic or user-defined - it only cares
        // about IsChainedRelationalComparison. So the short-circuit shape is
        // the same as the int case, just with OperatorMethod populated.
        string source = """
            struct S
            {
                public int V;
                public static bool operator <(S a, S b) => a.V < b.V;
                public static bool operator >(S a, S b) => a.V > b.V;
            }

            class P
            {
                void M(S a, S b, S c)
            /*<bind>*/{
                    if (a < b < c) { }
                }/*</bind>*/
            }
            """;
        string expectedGraph = """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1} {R2}
            .locals {R1}
            {
                CaptureIds: [0]
                .locals {R2}
                {
                    CaptureIds: [1]
                    Block[B1] - Block
                        Predecessors: [B0]
                        Statements (1)
                            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                              Value:
                                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: S) (Syntax: 'b')
                        Jump if False (Regular) to Block[B3]
                            IBinaryOperation (BinaryOperatorKind.LessThan) (OperatorMethod: System.Boolean S.op_LessThan(S a, S b)) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b')
                              Left:
                                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: S) (Syntax: 'a')
                              Right:
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: S, IsImplicit) (Syntax: 'b')
                            Leaving: {R2}
                        Next (Regular) Block[B2]
                    Block[B2] - Block
                        Predecessors: [B1]
                        Statements (1)
                            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a < b < c')
                              Value:
                                IBinaryOperation (BinaryOperatorKind.LessThan) (OperatorMethod: System.Boolean S.op_LessThan(S a, S b)) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b < c')
                                  Left:
                                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: S, IsImplicit) (Syntax: 'b')
                                  Right:
                                    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: S) (Syntax: 'c')
                        Next (Regular) Block[B4]
                            Leaving: {R2}
                }
                Block[B3] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a < b < c')
                          Value:
                            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'a < b < c')
                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B2] [B3]
                    Statements (0)
                    Jump if False (Regular) to Block[B5]
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'a < b < c')
                        Leaving: {R1}
                    Next (Regular) Block[B5]
                        Leaving: {R1}
            }
            Block[B5] - Exit
                Predecessors: [B4*2]
                Statements (0)
            """;
        var expectedDiagnostics = new[]
        {
            // (3,16): warning CS0649: Field 'S.V' is never assigned to, and will always have its default value 0
            //     public int V;
            Diagnostic(ErrorCode.WRN_UnassignedInternalField, "V").WithArguments("S.V", "0").WithLocation(3, 16)
        };
        VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
    }

    [Fact]
    public void Chain_LiftedNullable_CapturesNullableValueTypeOperand()
    {
        // `int? a, b, c => a < b < c` - spec §11.4.8 lifted relational composed
        // with chained short-circuit. The captured Y is the full `int?` value
        // (type `System.Int32?`), not its underlying `int` - both links' lifted
        // comparisons need the whole Nullable<int> to do their HasValue check.
        // The `IsLifted` flag on each IBinaryOperation carries the lifted
        // semantics forward; the CFG shape is otherwise identical to the
        // non-nullable case.
        string source = """
            class P
            {
                void M(int? a, int? b, int? c)
            /*<bind>*/{
                    if (a < b < c) { }
                }/*</bind>*/
            }
            """;
        string expectedGraph = """
            Block[B0] - Entry
                Statements (0)
                Next (Regular) Block[B1]
                    Entering: {R1} {R2}
            .locals {R1}
            {
                CaptureIds: [0]
                .locals {R2}
                {
                    CaptureIds: [1]
                    Block[B1] - Block
                        Predecessors: [B0]
                        Statements (1)
                            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                              Value:
                                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'b')
                        Jump if False (Regular) to Block[B3]
                            IBinaryOperation (BinaryOperatorKind.LessThan, IsLifted) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b')
                              Left:
                                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'a')
                              Right:
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                            Leaving: {R2}
                        Next (Regular) Block[B2]
                    Block[B2] - Block
                        Predecessors: [B1]
                        Statements (1)
                            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a < b < c')
                              Value:
                                IBinaryOperation (BinaryOperatorKind.LessThan, IsLifted) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b < c')
                                  Left:
                                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                                  Right:
                                    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'c')
                        Next (Regular) Block[B4]
                            Leaving: {R2}
                }
                Block[B3] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a < b < c')
                          Value:
                            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'a < b < c')
                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B2] [B3]
                    Statements (0)
                    Jump if False (Regular) to Block[B5]
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'a < b < c')
                        Leaving: {R1}
                    Next (Regular) Block[B5]
                        Leaving: {R1}
            }
            Block[B5] - Exit
                Predecessors: [B4*2]
                Statements (0)
            """;
        var expectedDiagnostics = DiagnosticDescription.None;
        VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
    }
}
