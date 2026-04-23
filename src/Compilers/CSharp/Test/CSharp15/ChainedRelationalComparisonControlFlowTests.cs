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
/// Control-flow-graph tests for "chained relational comparison" (C# preview feature;
/// spec §11.11.13). The CFG must emit the same short-circuit edges as a hand-written
/// <c>X op' Y &amp;&amp; Y op Z</c>, with Y evaluated once via a flow capture shared by
/// both links.
/// </summary>
public sealed class ChainedRelationalComparisonControlFlowTests : CSharpTestBase
{
    [Fact]
    public void Chain_AsIfCondition_EmitsShortCircuitEdges()
    {
        string source = """
            class P
            {
                void M(int a, int b, int c)
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
        // Chain as a standalone expression (no consuming conditional).
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
        // Chain as a `while` condition: the loop's back-edge re-enters Y's capture region.
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
        // Side-effecting middle operand: the invocation appears once, referenced twice via capture.
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
        // `a < b < c < d`: two nested Y sub-regions, two short-circuit branches.
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
                                IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b < c')
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
        // `a < b < c < d < e`: three nested Y sub-regions, three short-circuit branches.
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
                                IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b < c')
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
                                    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a < b < c < d')
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
    public void Chain_NAry_MixedOperators_EachLinkKeepsItsOwnOperatorKind()
    {
        // `a <= b < c <= d`: each link keeps its own relational operator.
        string source = """
            class P
            {
                void M(int a, int b, int c, int d)
            /*<bind>*/{
                    if (a <= b < c <= d) { }
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
                            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a <= b')
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
                                IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a <= b < c')
                                  Left:
                                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b')
                                  Right:
                                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'c')
                                Leaving: {R3} {R2}
                            Next (Regular) Block[B3]
                        Block[B3] - Block
                            Predecessors: [B2]
                            Statements (1)
                                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a <= b < c <= d')
                                  Value:
                                    IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'a <= b < c <= d')
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
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a <= b < c <= d')
                          Value:
                            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'a <= b < c <= d')
                    Next (Regular) Block[B5]
                Block[B5] - Block
                    Predecessors: [B3] [B4]
                    Statements (0)
                    Jump if False (Regular) to Block[B6]
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'a <= b < c <= d')
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
    public void Chain_AsConditionalExpressionCondition_CapturesFlowIntoBothBranches()
    {
        // Chain nested inside an enclosing ternary's capture region.
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
        // User-defined `operator <` on a struct: same CFG shape as the int case.
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
        // Lifted `int? a, b, c => a < b < c`: captured Y is the full `int?`.
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
