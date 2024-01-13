// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IImplicitIndexerReferenceExpression : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitIndexIndexer_String()
        {
            string source = @"
class C
{
    public void F(string args, System.Index x)
    {
        var a = /*<bind>*/args[x]/*</bind>*/;
    }
}
";

            string expectedOperationTree = @"
IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Char) (Syntax: 'args[x]')
  Instance:
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String) (Syntax: 'args')
  Argument:
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'x')
  LengthSymbol: System.Int32 System.String.Length { get; }
  IndexerSymbol: System.Char System.String.this[System.Int32 index] { get; }
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(source);
            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ImplicitIndexIndexer_NoControlFlow_String()
        {
            string source = @"
class C
{
    void M(string a1, System.Index i1, char result1)
    /*<bind>*/{
        result1 = a1[i1];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result1 = a1[i1];')
          Expression:
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Char) (Syntax: 'result1 = a1[i1]')
              Left:
                IParameterReferenceOperation: result1 (OperationKind.ParameterReference, Type: System.Char) (Syntax: 'result1')
              Right:
                IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Char) (Syntax: 'a1[i1]')
                  Instance:
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'a1')
                  Argument:
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i1')
                  LengthSymbol: System.Int32 System.String.Length { get; }
                  IndexerSymbol: System.Char System.String.this[System.Int32 index] { get; }
    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(source);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ImplicitIndexIndexer_ControlFlowInInstance_String()
        {
            string source = @"
class C
{
    void M(string a1, string a2, System.Index i1, char result)
    /*<bind>*/{
        result = (a1 ?? a2)[i1];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value:
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Char) (Syntax: 'result')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'a1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a1')
            Next (Regular) Block[B5]
                Leaving: {R2}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value:
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'a2')
        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (a1 ?? a2)[i1];')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Char) (Syntax: 'result = (a1 ?? a2)[i1]')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Char, IsImplicit) (Syntax: 'result')
                  Right:
                    IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Char) (Syntax: '(a1 ?? a2)[i1]')
                      Instance:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a1 ?? a2')
                      Argument:
                        IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i1')
                      LengthSymbol: System.Int32 System.String.Length { get; }
                      IndexerSymbol: System.Char System.String.this[System.Int32 index] { get; }
        Next (Regular) Block[B6]
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(source);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ImplicitIndexIndexer_ControlFlowInArgument_String()
        {
            string source = @"
class C
{
    void M(string a, System.Index? i1, System.Index i2, char result)
    /*<bind>*/{
        result = a[i1 ?? i2];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [1] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value:
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Char) (Syntax: 'result')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
              Value:
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.String) (Syntax: 'a')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Index?) (Syntax: 'i1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand:
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Index?, IsImplicit) (Syntax: 'i1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IInvocationOperation ( System.Index System.Index?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Index, IsImplicit) (Syntax: 'i1')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Index?, IsImplicit) (Syntax: 'i1')
                      Arguments(0)
            Next (Regular) Block[B5]
                Leaving: {R2}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value:
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i2')
        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = a[i1 ?? i2];')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Char) (Syntax: 'result = a[i1 ?? i2]')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Char, IsImplicit) (Syntax: 'result')
                  Right:
                    IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Char) (Syntax: 'a[i1 ?? i2]')
                      Instance:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a')
                      Argument:
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Index, IsImplicit) (Syntax: 'i1 ?? i2')
                      LengthSymbol: System.Int32 System.String.Length { get; }
                      IndexerSymbol: System.Char System.String.this[System.Int32 index] { get; }
        Next (Regular) Block[B6]
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(source);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ImplicitIndexIndexer_ControlFlowInInstanceAndArgument_String()
        {
            string source = @"
class C
{
    void M(string a1, string a2, System.Index? i1, System.Index i2, char result)
    /*<bind>*/{
        result = (a1 ?? a2)[i1 ?? i2];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [2] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value:
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Char) (Syntax: 'result')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'a1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a1')
            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R3}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value:
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'a2')
        Next (Regular) Block[B5]
            Entering: {R3}
    .locals {R3}
    {
        CaptureIds: [3]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Index?) (Syntax: 'i1')
            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand:
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Index?, IsImplicit) (Syntax: 'i1')
                Leaving: {R3}
            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IInvocationOperation ( System.Index System.Index?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Index, IsImplicit) (Syntax: 'i1')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Index?, IsImplicit) (Syntax: 'i1')
                      Arguments(0)
            Next (Regular) Block[B8]
                Leaving: {R3}
    }
    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value:
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i2')
        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (a ... [i1 ?? i2];')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Char) (Syntax: 'result = (a ... )[i1 ?? i2]')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Char, IsImplicit) (Syntax: 'result')
                  Right:
                    IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Char) (Syntax: '(a1 ?? a2)[i1 ?? i2]')
                      Instance:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a1 ?? a2')
                      Argument:
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Index, IsImplicit) (Syntax: 'i1 ?? i2')
                      LengthSymbol: System.Int32 System.String.Length { get; }
                      IndexerSymbol: System.Char System.String.this[System.Int32 index] { get; }
        Next (Regular) Block[B9]
            Leaving: {R1}
}
Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(source);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitRangeIndexer_String()
        {
            string source = @"
class C
{
    public void F(string args, System.Range x)
    {
        var a = /*<bind>*/args[x]/*</bind>*/;
    }
}
";

            string expectedOperationTree = @"
IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.String) (Syntax: 'args[x]')
  Instance:
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String) (Syntax: 'args')
  Argument:
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'x')
  LengthSymbol: System.Int32 System.String.Length { get; }
  IndexerSymbol: System.String System.String.Substring(System.Int32 startIndex, System.Int32 length)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(source);
            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ImplicitRangeIndexer_NoControlFlow_String()
        {
            string source = @"
class C
{
    void M(string a1, System.Range i1, string result1)
    /*<bind>*/{
        result1 = a1[i1];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result1 = a1[i1];')
          Expression:
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'result1 = a1[i1]')
              Left:
                IParameterReferenceOperation: result1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'result1')
              Right:
                IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.String) (Syntax: 'a1[i1]')
                  Instance:
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'a1')
                  Argument:
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'i1')
                  LengthSymbol: System.Int32 System.String.Length { get; }
                  IndexerSymbol: System.String System.String.Substring(System.Int32 startIndex, System.Int32 length)
    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(source);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ImplicitRangeIndexer_ControlFlowInInstance_String()
        {
            string source = @"
class C
{
    void M(string a1, string a2, System.Range i1, string result)
    /*<bind>*/{
        result = (a1 ?? a2)[i1];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value:
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.String) (Syntax: 'result')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'a1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a1')
            Next (Regular) Block[B5]
                Leaving: {R2}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value:
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'a2')
        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (a1 ?? a2)[i1];')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'result = (a1 ?? a2)[i1]')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'result')
                  Right:
                    IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.String) (Syntax: '(a1 ?? a2)[i1]')
                      Instance:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a1 ?? a2')
                      Argument:
                        IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'i1')
                      LengthSymbol: System.Int32 System.String.Length { get; }
                      IndexerSymbol: System.String System.String.Substring(System.Int32 startIndex, System.Int32 length)
        Next (Regular) Block[B6]
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(source);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ImplicitRangeIndexer_ControlFlowInArgument_String()
        {
            string source = @"
class C
{
    void M(string a, System.Range? i1, System.Range i2, string result)
    /*<bind>*/{
        result = a[i1 ?? i2];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [1] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value:
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.String) (Syntax: 'result')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
              Value:
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.String) (Syntax: 'a')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Range?) (Syntax: 'i1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand:
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Range?, IsImplicit) (Syntax: 'i1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IInvocationOperation ( System.Range System.Range?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Range, IsImplicit) (Syntax: 'i1')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Range?, IsImplicit) (Syntax: 'i1')
                      Arguments(0)
            Next (Regular) Block[B5]
                Leaving: {R2}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value:
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'i2')
        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = a[i1 ?? i2];')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'result = a[i1 ?? i2]')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'result')
                  Right:
                    IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.String) (Syntax: 'a[i1 ?? i2]')
                      Instance:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a')
                      Argument:
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Range, IsImplicit) (Syntax: 'i1 ?? i2')
                      LengthSymbol: System.Int32 System.String.Length { get; }
                      IndexerSymbol: System.String System.String.Substring(System.Int32 startIndex, System.Int32 length)
        Next (Regular) Block[B6]
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(source);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ImplicitRangeIndexer_ControlFlowInInstanceAndArgument_String()
        {
            string source = @"
class C
{
    void M(string a1, string a2, System.Range? i1, System.Range i2, string result)
    /*<bind>*/{
        result = (a1 ?? a2)[i1 ?? i2];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [2] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value:
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.String) (Syntax: 'result')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'a1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a1')
            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R3}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value:
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'a2')
        Next (Regular) Block[B5]
            Entering: {R3}
    .locals {R3}
    {
        CaptureIds: [3]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Range?) (Syntax: 'i1')
            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand:
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Range?, IsImplicit) (Syntax: 'i1')
                Leaving: {R3}
            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IInvocationOperation ( System.Range System.Range?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Range, IsImplicit) (Syntax: 'i1')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Range?, IsImplicit) (Syntax: 'i1')
                      Arguments(0)
            Next (Regular) Block[B8]
                Leaving: {R3}
    }
    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value:
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'i2')
        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (a ... [i1 ?? i2];')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'result = (a ... )[i1 ?? i2]')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'result')
                  Right:
                    IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.String) (Syntax: '(a1 ?? a2)[i1 ?? i2]')
                      Instance:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a1 ?? a2')
                      Argument:
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Range, IsImplicit) (Syntax: 'i1 ?? i2')
                      LengthSymbol: System.Int32 System.String.Length { get; }
                      IndexerSymbol: System.String System.String.Substring(System.Int32 startIndex, System.Int32 length)
        Next (Regular) Block[B9]
            Leaving: {R1}
}
Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(source);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        private string IndexableAndSliceable => @"
partial class C
{
    public int Length => 0;
    public int this[int i] => i;
    public C Slice(int i, int j) => throw null;
}
";

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitIndexIndexer()
        {
            string source = @"
partial class C
{
    public void F(C args, System.Index x)
    {
        var a = /*<bind>*/args[x]/*</bind>*/;
    }
}
";

            string expectedOperationTree = @"
IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Int32) (Syntax: 'args[x]')
  Instance:
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: C) (Syntax: 'args')
  Argument:
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'x')
  LengthSymbol: System.Int32 C.Length { get; }
  IndexerSymbol: System.Int32 C.this[System.Int32 i] { get; }
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(new[] { source, IndexableAndSliceable });
            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ImplicitIndexIndexer_NoControlFlow()
        {
            string source = @"
partial class C
{
    void M(C a1, System.Index i1, int result1)
    /*<bind>*/{
        result1 = a1[i1];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result1 = a1[i1];')
          Expression:
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result1 = a1[i1]')
              Left:
                IParameterReferenceOperation: result1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result1')
              Right:
                IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Int32) (Syntax: 'a1[i1]')
                  Instance:
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: C) (Syntax: 'a1')
                  Argument:
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i1')
                  LengthSymbol: System.Int32 C.Length { get; }
                  IndexerSymbol: System.Int32 C.this[System.Int32 i] { get; }
    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(new[] { source, IndexableAndSliceable });
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ImplicitIndexIndexer_ControlFlowInInstance()
        {
            string source = @"
partial class C
{
    void M(C a1, C a2, System.Index i1, int result)
    /*<bind>*/{
        result = (a1 ?? a2)[i1];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value:
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: C) (Syntax: 'a1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a1')
            Next (Regular) Block[B5]
                Leaving: {R2}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value:
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: C) (Syntax: 'a2')
        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (a1 ?? a2)[i1];')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = (a1 ?? a2)[i1]')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'result')
                  Right:
                    IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Int32) (Syntax: '(a1 ?? a2)[i1]')
                      Instance:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a1 ?? a2')
                      Argument:
                        IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i1')
                      LengthSymbol: System.Int32 C.Length { get; }
                      IndexerSymbol: System.Int32 C.this[System.Int32 i] { get; }
        Next (Regular) Block[B6]
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(new[] { source, IndexableAndSliceable });
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ImplicitIndexIndexer_ControlFlowInArgument()
        {
            string source = @"
partial class C
{
    void M(C a, System.Index? i1, System.Index i2, int result)
    /*<bind>*/{
        result = a[i1 ?? i2];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [1] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value:
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
              Value:
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: C) (Syntax: 'a')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Index?) (Syntax: 'i1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand:
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Index?, IsImplicit) (Syntax: 'i1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IInvocationOperation ( System.Index System.Index?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Index, IsImplicit) (Syntax: 'i1')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Index?, IsImplicit) (Syntax: 'i1')
                      Arguments(0)
            Next (Regular) Block[B5]
                Leaving: {R2}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value:
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i2')
        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = a[i1 ?? i2];')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = a[i1 ?? i2]')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'result')
                  Right:
                    IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Int32) (Syntax: 'a[i1 ?? i2]')
                      Instance:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a')
                      Argument:
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Index, IsImplicit) (Syntax: 'i1 ?? i2')
                      LengthSymbol: System.Int32 C.Length { get; }
                      IndexerSymbol: System.Int32 C.this[System.Int32 i] { get; }
        Next (Regular) Block[B6]
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(new[] { source, IndexableAndSliceable });
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ImplicitIndexIndexer_ControlFlowInInstanceAndArgument()
        {
            string source = @"
partial class C
{
    void M(C a1, C a2, System.Index? i1, System.Index i2, int result)
    /*<bind>*/{
        result = (a1 ?? a2)[i1 ?? i2];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [2] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value:
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: C) (Syntax: 'a1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a1')
            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R3}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value:
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: C) (Syntax: 'a2')
        Next (Regular) Block[B5]
            Entering: {R3}
    .locals {R3}
    {
        CaptureIds: [3]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Index?) (Syntax: 'i1')
            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand:
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Index?, IsImplicit) (Syntax: 'i1')
                Leaving: {R3}
            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IInvocationOperation ( System.Index System.Index?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Index, IsImplicit) (Syntax: 'i1')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Index?, IsImplicit) (Syntax: 'i1')
                      Arguments(0)
            Next (Regular) Block[B8]
                Leaving: {R3}
    }
    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value:
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i2')
        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (a ... [i1 ?? i2];')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = (a ... )[i1 ?? i2]')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'result')
                  Right:
                    IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Int32) (Syntax: '(a1 ?? a2)[i1 ?? i2]')
                      Instance:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a1 ?? a2')
                      Argument:
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Index, IsImplicit) (Syntax: 'i1 ?? i2')
                      LengthSymbol: System.Int32 C.Length { get; }
                      IndexerSymbol: System.Int32 C.this[System.Int32 i] { get; }
        Next (Regular) Block[B9]
            Leaving: {R1}
}
Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(new[] { source, IndexableAndSliceable });
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ImplicitRangeIndexer()
        {
            string source = @"
partial class C
{
    public void F(C args, System.Range x)
    {
        var a = /*<bind>*/args[x]/*</bind>*/;
    }
}
";

            string expectedOperationTree = @"
IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: C) (Syntax: 'args[x]')
  Instance:
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: C) (Syntax: 'args')
  Argument:
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'x')
  LengthSymbol: System.Int32 C.Length { get; }
  IndexerSymbol: C C.Slice(System.Int32 i, System.Int32 j)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(new[] { source, IndexableAndSliceable });
            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ImplicitRangeIndexer_NoControlFlow()
        {
            string source = @"
partial class C
{
    void M(C a1, System.Range i1, C result1)
    /*<bind>*/{
        result1 = a1[i1];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result1 = a1[i1];')
          Expression:
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: 'result1 = a1[i1]')
              Left:
                IParameterReferenceOperation: result1 (OperationKind.ParameterReference, Type: C) (Syntax: 'result1')
              Right:
                IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: C) (Syntax: 'a1[i1]')
                  Instance:
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: C) (Syntax: 'a1')
                  Argument:
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'i1')
                  LengthSymbol: System.Int32 C.Length { get; }
                  IndexerSymbol: C C.Slice(System.Int32 i, System.Int32 j)
    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(new[] { source, IndexableAndSliceable });
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ImplicitRangeIndexer_ControlFlowInInstance()
        {
            string source = @"
partial class C
{
    void M(C a1, C a2, System.Range i1, C result)
    /*<bind>*/{
        result = (a1 ?? a2)[i1];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value:
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: C) (Syntax: 'result')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: C) (Syntax: 'a1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a1')
            Next (Regular) Block[B5]
                Leaving: {R2}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value:
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: C) (Syntax: 'a2')
        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (a1 ?? a2)[i1];')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: 'result = (a1 ?? a2)[i1]')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'result')
                  Right:
                    IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: C) (Syntax: '(a1 ?? a2)[i1]')
                      Instance:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a1 ?? a2')
                      Argument:
                        IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'i1')
                      LengthSymbol: System.Int32 C.Length { get; }
                      IndexerSymbol: C C.Slice(System.Int32 i, System.Int32 j)
        Next (Regular) Block[B6]
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(new[] { source, IndexableAndSliceable });
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ImplicitRangeIndexer_ControlFlowInArgument()
        {
            string source = @"
partial class C
{
    void M(C a, System.Range? i1, System.Range i2, C result)
    /*<bind>*/{
        result = a[i1 ?? i2];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [1] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value:
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: C) (Syntax: 'result')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
              Value:
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: C) (Syntax: 'a')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Range?) (Syntax: 'i1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand:
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Range?, IsImplicit) (Syntax: 'i1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IInvocationOperation ( System.Range System.Range?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Range, IsImplicit) (Syntax: 'i1')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Range?, IsImplicit) (Syntax: 'i1')
                      Arguments(0)
            Next (Regular) Block[B5]
                Leaving: {R2}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value:
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'i2')
        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = a[i1 ?? i2];')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: 'result = a[i1 ?? i2]')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'result')
                  Right:
                    IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: C) (Syntax: 'a[i1 ?? i2]')
                      Instance:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a')
                      Argument:
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Range, IsImplicit) (Syntax: 'i1 ?? i2')
                      LengthSymbol: System.Int32 C.Length { get; }
                      IndexerSymbol: C C.Slice(System.Int32 i, System.Int32 j)
        Next (Regular) Block[B6]
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(new[] { source, IndexableAndSliceable });
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ImplicitRangeIndexer_ControlFlowInInstanceAndArgument()
        {
            string source = @"
partial class C
{
    void M(C a1, C a2, System.Range? i1, System.Range i2, C result)
    /*<bind>*/{
        result = (a1 ?? a2)[i1 ?? i2];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [2] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value:
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: C) (Syntax: 'result')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: C) (Syntax: 'a1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a1')
            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R3}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value:
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: C) (Syntax: 'a2')
        Next (Regular) Block[B5]
            Entering: {R3}
    .locals {R3}
    {
        CaptureIds: [3]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Range?) (Syntax: 'i1')
            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand:
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Range?, IsImplicit) (Syntax: 'i1')
                Leaving: {R3}
            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IInvocationOperation ( System.Range System.Range?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Range, IsImplicit) (Syntax: 'i1')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Range?, IsImplicit) (Syntax: 'i1')
                      Arguments(0)
            Next (Regular) Block[B8]
                Leaving: {R3}
    }
    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value:
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'i2')
        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (a ... [i1 ?? i2];')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: 'result = (a ... )[i1 ?? i2]')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'result')
                  Right:
                    IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: C) (Syntax: '(a1 ?? a2)[i1 ?? i2]')
                      Instance:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'a1 ?? a2')
                      Argument:
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Range, IsImplicit) (Syntax: 'i1 ?? i2')
                      LengthSymbol: System.Int32 C.Length { get; }
                      IndexerSymbol: C C.Slice(System.Int32 i, System.Int32 j)
        Next (Regular) Block[B9]
            Leaving: {R1}
}
Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(new[] { source, IndexableAndSliceable });
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
