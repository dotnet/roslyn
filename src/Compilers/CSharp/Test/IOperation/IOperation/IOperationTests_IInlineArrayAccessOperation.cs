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
    public class IOperationTests_IInlineArrayAccessOperation : SemanticModelTestBase
    {
        public const string Buffer10Definition =
@"
[System.Runtime.CompilerServices.InlineArray(10)]
public struct Buffer10
{
    private char _element0;
}
";

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ElementAccess()
        {
            string source = @"
class C
{
    public void F(Buffer10 arg, System.Index x)
    {
        var a = /*<bind>*/arg[x]/*</bind>*/;
    }
}
";

            string expectedOperationTree = @"
IInlineArrayAccessOperation (OperationKind.InlineArrayAccess, Type: System.Char) (Syntax: 'arg[x]')
  Instance:
    IParameterReferenceOperation: arg (OperationKind.ParameterReference, Type: Buffer10) (Syntax: 'arg')
  Argument:
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilation(source + Buffer10Definition, targetFramework: TargetFramework.Net80);
            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ElementAccess_NoControlFlow()
        {
            string source = @"
class C
{
    void M(Buffer10 a1, System.Index i1, char result1)
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
                IInlineArrayAccessOperation (OperationKind.InlineArrayAccess, Type: System.Char) (Syntax: 'a1[i1]')
                  Instance:
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: Buffer10) (Syntax: 'a1')
                  Argument:
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i1')
    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilation(source + Buffer10Definition, targetFramework: TargetFramework.Net80);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ElementAccess_ControlFlowInInstance()
        {
            string source = @"
class C
{
    void M(Buffer10? a1, Buffer10 a2, System.Index i1, char result)
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
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: Buffer10?) (Syntax: 'a1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: Buffer10?, IsImplicit) (Syntax: 'a1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IInvocationOperation ( readonly Buffer10 Buffer10?.GetValueOrDefault()) (OperationKind.Invocation, Type: Buffer10, IsImplicit) (Syntax: 'a1')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: Buffer10?, IsImplicit) (Syntax: 'a1')
                      Arguments(0)
            Next (Regular) Block[B5]
                Leaving: {R2}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value:
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: Buffer10) (Syntax: 'a2')
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
                    IInlineArrayAccessOperation (OperationKind.InlineArrayAccess, Type: System.Char) (Syntax: '(a1 ?? a2)[i1]')
                      Instance:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: Buffer10, IsImplicit) (Syntax: 'a1 ?? a2')
                      Argument:
                        IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i1')
        Next (Regular) Block[B6]
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilation(source + Buffer10Definition, targetFramework: TargetFramework.Net80);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ElementAccess_ControlFlowInArgument()
        {
            string source = @"
class C
{
    void M(Buffer10 a, System.Index? i1, System.Index i2, char result)
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
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: Buffer10) (Syntax: 'a')
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
                    IInvocationOperation ( readonly System.Index System.Index?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Index, IsImplicit) (Syntax: 'i1')
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
                    IInlineArrayAccessOperation (OperationKind.InlineArrayAccess, Type: System.Char) (Syntax: 'a[i1 ?? i2]')
                      Instance:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: Buffer10, IsImplicit) (Syntax: 'a')
                      Argument:
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Index, IsImplicit) (Syntax: 'i1 ?? i2')
        Next (Regular) Block[B6]
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilation(source + Buffer10Definition, targetFramework: TargetFramework.Net80);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ElementAccess_ControlFlowInInstanceAndArgument()
        {
            string source = @"
class C
{
    void M(Buffer10? a1, Buffer10 a2, System.Index? i1, System.Index i2, char result)
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
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: Buffer10?) (Syntax: 'a1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: Buffer10?, IsImplicit) (Syntax: 'a1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IInvocationOperation ( readonly Buffer10 Buffer10?.GetValueOrDefault()) (OperationKind.Invocation, Type: Buffer10, IsImplicit) (Syntax: 'a1')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: Buffer10?, IsImplicit) (Syntax: 'a1')
                      Arguments(0)
            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R3}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value:
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: Buffer10) (Syntax: 'a2')
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
                    IInvocationOperation ( readonly System.Index System.Index?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Index, IsImplicit) (Syntax: 'i1')
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
                    IInlineArrayAccessOperation (OperationKind.InlineArrayAccess, Type: System.Char) (Syntax: '(a1 ?? a2)[i1 ?? i2]')
                      Instance:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: Buffer10, IsImplicit) (Syntax: 'a1 ?? a2')
                      Argument:
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Index, IsImplicit) (Syntax: 'i1 ?? i2')
        Next (Regular) Block[B9]
            Leaving: {R1}
}
Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilation(source + Buffer10Definition, targetFramework: TargetFramework.Net80);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Slice()
        {
            string source = @"
class C
{
    public void F(Buffer10 arg, System.Range x)
    {
        var a = /*<bind>*/arg[x]/*</bind>*/;
    }
}
";

            string expectedOperationTree = @"
IInlineArrayAccessOperation (OperationKind.InlineArrayAccess, Type: System.Span<System.Char>) (Syntax: 'arg[x]')
  Instance:
    IParameterReferenceOperation: arg (OperationKind.ParameterReference, Type: Buffer10) (Syntax: 'arg')
  Argument:
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilation(source + Buffer10Definition, targetFramework: TargetFramework.Net80);
            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void Slice_NoControlFlow()
        {
            string source = @"
class C
{
    void M(Buffer10 a1, System.Range i1)
    /*<bind>*/{
        System.Span<char> result1 = a1[i1];
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
    Locals: [System.Span<System.Char> result1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Span<System.Char>, IsImplicit) (Syntax: 'result1 = a1[i1]')
              Left:
                ILocalReferenceOperation: result1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Span<System.Char>, IsImplicit) (Syntax: 'result1 = a1[i1]')
              Right:
                IInlineArrayAccessOperation (OperationKind.InlineArrayAccess, Type: System.Span<System.Char>) (Syntax: 'a1[i1]')
                  Instance:
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: Buffer10) (Syntax: 'a1')
                  Argument:
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'i1')
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilation(source + Buffer10Definition, targetFramework: TargetFramework.Net80);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void Slice_ControlFlowInInstance()
        {
            string source = @"
class C
{
    void M(string a1, string a2, System.Range i1, System.Span<char> result)
    /*<bind>*/{
        result = GetBuffer(a1 ?? a2)[i1];
    }/*</bind>*/

    static ref Buffer10 GetBuffer(string s) => throw null;
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
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Span<System.Char>) (Syntax: 'result')
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
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = Ge ... ?? a2)[i1];')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Span<System.Char>) (Syntax: 'result = Ge ...  ?? a2)[i1]')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Span<System.Char>, IsImplicit) (Syntax: 'result')
                  Right:
                    IInlineArrayAccessOperation (OperationKind.InlineArrayAccess, Type: System.Span<System.Char>) (Syntax: 'GetBuffer(a1 ?? a2)[i1]')
                      Instance:
                        IInvocationOperation (ref Buffer10 C.GetBuffer(System.String s)) (OperationKind.Invocation, Type: Buffer10) (Syntax: 'GetBuffer(a1 ?? a2)')
                          Instance Receiver:
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument, Type: null) (Syntax: 'a1 ?? a2')
                                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a1 ?? a2')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Argument:
                        IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'i1')
        Next (Regular) Block[B6]
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilation(source + Buffer10Definition, targetFramework: TargetFramework.Net80);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void Slice_ControlFlowInArgument()
        {
            string source = @"
class C
{
    void M(Buffer10 a, System.Range? i1, System.Range i2)
    /*<bind>*/{
        System.Span<char> result = a[i1 ?? i2];
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
    Locals: [System.Span<System.Char> result]
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
              Value:
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: Buffer10) (Syntax: 'a')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Range?) (Syntax: 'i1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Range?, IsImplicit) (Syntax: 'i1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IInvocationOperation ( readonly System.Range System.Range?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Range, IsImplicit) (Syntax: 'i1')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Range?, IsImplicit) (Syntax: 'i1')
                      Arguments(0)
            Next (Regular) Block[B5]
                Leaving: {R2}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value:
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'i2')
        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Span<System.Char>, IsImplicit) (Syntax: 'result = a[i1 ?? i2]')
              Left:
                ILocalReferenceOperation: result (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Span<System.Char>, IsImplicit) (Syntax: 'result = a[i1 ?? i2]')
              Right:
                IInlineArrayAccessOperation (OperationKind.InlineArrayAccess, Type: System.Span<System.Char>) (Syntax: 'a[i1 ?? i2]')
                  Instance:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Buffer10, IsImplicit) (Syntax: 'a')
                  Argument:
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Range, IsImplicit) (Syntax: 'i1 ?? i2')
        Next (Regular) Block[B6]
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilation(source + Buffer10Definition, targetFramework: TargetFramework.Net80);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void Slice_ControlFlowInInstanceAndArgument()
        {
            string source = @"
class C
{
    void M(string a1, string a2, System.Range? i1, System.Range i2, System.Span<char> result)
    /*<bind>*/{
        result = GetBuffer(a1 ?? a2)[i1 ?? i2];
    }/*</bind>*/

    static ref Buffer10 GetBuffer(string s) => throw null;
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [3] [5]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value:
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Span<System.Char>) (Syntax: 'result')
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
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
                    Leaving: {R3}
                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                      Value:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a1')
                Next (Regular) Block[B5]
                    Leaving: {R3}
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
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetBuffer(a1 ?? a2)')
                  Value:
                    IInvocationOperation (ref Buffer10 C.GetBuffer(System.String s)) (OperationKind.Invocation, Type: Buffer10) (Syntax: 'GetBuffer(a1 ?? a2)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument, Type: null) (Syntax: 'a1 ?? a2')
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a1 ?? a2')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B6]
                Leaving: {R2}
                Entering: {R4}
    }
    .locals {R4}
    {
        CaptureIds: [4]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Range?) (Syntax: 'i1')
            Jump if True (Regular) to Block[B8]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand:
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Range?, IsImplicit) (Syntax: 'i1')
                Leaving: {R4}
            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B6]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IInvocationOperation ( readonly System.Range System.Range?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Range, IsImplicit) (Syntax: 'i1')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Range?, IsImplicit) (Syntax: 'i1')
                      Arguments(0)
            Next (Regular) Block[B9]
                Leaving: {R4}
    }
    Block[B8] - Block
        Predecessors: [B6]
        Statements (1)
            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value:
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'i2')
        Next (Regular) Block[B9]
    Block[B9] - Block
        Predecessors: [B7] [B8]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = Ge ... [i1 ?? i2];')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Span<System.Char>) (Syntax: 'result = Ge ... )[i1 ?? i2]')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Span<System.Char>, IsImplicit) (Syntax: 'result')
                  Right:
                    IInlineArrayAccessOperation (OperationKind.InlineArrayAccess, Type: System.Span<System.Char>) (Syntax: 'GetBuffer(a ... )[i1 ?? i2]')
                      Instance:
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: Buffer10, IsImplicit) (Syntax: 'GetBuffer(a1 ?? a2)')
                      Argument:
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Range, IsImplicit) (Syntax: 'i1 ?? i2')
        Next (Regular) Block[B10]
            Leaving: {R1}
}
Block[B10] - Exit
    Predecessors: [B9]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilation(source + Buffer10Definition, targetFramework: TargetFramework.Net80);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
