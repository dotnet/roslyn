// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IDeconstructionAssignmentOperation : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void DeconstructionFlow_01()
        {
            string source = @"
class C
{
    void M(bool b, int i1, int i2, int i3, int i4, int i5, int i6)
    /*<bind>*/{
        (i1, i2) = b ? (i3, i4) : (i5, i6);
    }/*</bind>*/
}

";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
              Value: 
                IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(i3, i4)')
              Value: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: '(i3, i4)')
                  Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (Identity)
                  Operand: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i3, System.Int32 i4)) (Syntax: '(i3, i4)')
                      NaturalType: (System.Int32 i3, System.Int32 i4)
                      Elements(2):
                          IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i3')
                          IParameterReferenceOperation: i4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i4')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(i5, i6)')
              Value: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: '(i5, i6)')
                  Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (Identity)
                  Operand: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i5, System.Int32 i6)) (Syntax: '(i5, i6)')
                      NaturalType: (System.Int32 i5, System.Int32 i6)
                      Elements(2):
                          IParameterReferenceOperation: i5 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i5')
                          IParameterReferenceOperation: i6 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i6')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '(i1, i2) =  ... : (i5, i6);')
              Expression: 
                IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(i1, i2) =  ...  : (i5, i6)')
                  Left: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(i1, i2)')
                      NaturalType: (System.Int32 i1, System.Int32 i2)
                      Elements(2):
                          IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i2')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 'b ? (i3, i4) : (i5, i6)')

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
        public void DeconstructionFlow_02()
        {
            string source = @"
class C
{
    void M(bool b)
    /*<bind>*/{
        (var i1, var i2) = b ? (1, 2) : (3, 4);
    }/*</bind>*/
}

";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 i1] [System.Int32 i2]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(1, 2)')
              Value: 
                ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
                  NaturalType: (System.Int32, System.Int32)
                  Elements(2):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(3, 4)')
              Value: 
                ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(3, 4)')
                  NaturalType: (System.Int32, System.Int32)
                  Elements(2):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '(var i1, va ... ) : (3, 4);')
              Expression: 
                IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(var i1, va ... 2) : (3, 4)')
                  Left: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(var i1, var i2)')
                      NaturalType: (System.Int32 i1, System.Int32 i2)
                      Elements(2):
                          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i1')
                            ILocalReferenceOperation: i1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i1')
                          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i2')
                            ILocalReferenceOperation: i2 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i2')
                  Right: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 'b ? (1, 2) : (3, 4)')

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
        public void DeconstructionFlow_03()
        {
            string source = @"
class C
{
    void M(bool b)
    /*<bind>*/{
        var (i1, i2) = b ? (1, 2) : (3, 4);
    }/*</bind>*/
}

";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 i1] [System.Int32 i2]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(1, 2)')
              Value: 
                ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
                  NaturalType: (System.Int32, System.Int32)
                  Elements(2):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(3, 4)')
              Value: 
                ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(3, 4)')
                  NaturalType: (System.Int32, System.Int32)
                  Elements(2):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'var (i1, i2 ... ) : (3, 4);')
              Expression: 
                IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: 'var (i1, i2 ... 2) : (3, 4)')
                  Left: 
                    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: 'var (i1, i2)')
                      ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(i1, i2)')
                        NaturalType: (System.Int32 i1, System.Int32 i2)
                        Elements(2):
                            ILocalReferenceOperation: i1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i1')
                            ILocalReferenceOperation: i2 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i2')
                  Right: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 'b ? (1, 2) : (3, 4)')

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
        public void DeconstructionFlow_04()
        {
            string source = @"
class C
{
    void M(bool b, C c1)
    /*<bind>*/{
        var (i1, i2) = b ? this : c1;
    }/*</bind>*/

    public void Deconstruct(out int i1, out int i2)
    {
        i1 = 1;
        i2 = 2;
    }
}

";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 i1] [System.Int32 i2]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'this')
              Value: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
              Value: 
                IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'var (i1, i2 ...  this : c1;')
              Expression: 
                IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: 'var (i1, i2 ... ? this : c1')
                  Left: 
                    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: 'var (i1, i2)')
                      ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(i1, i2)')
                        NaturalType: (System.Int32 i1, System.Int32 i2)
                        Elements(2):
                            ILocalReferenceOperation: i1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i1')
                            ILocalReferenceOperation: i2 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i2')
                  Right: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'b ? this : c1')

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
        public void MixedDeconstruction()
        {
            string source = @"
class C
{
    void M(bool b)
    /*<bind>*/{
        int i2;
        (int i1, i2) = b ? (1, 2) : (3, 4);
    }/*</bind>*/
}";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IBlockOperation (2 statements, 2 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Locals: Local_1: System.Int32 i2
    Local_2: System.Int32 i1
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int i2;')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i2')
      Declarators:
          IVariableDeclaratorOperation (Symbol: System.Int32 i2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i2')
            Initializer: 
              null
      Initializer: 
        null
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '(int i1, i2 ... ) : (3, 4);')
    Expression: 
      IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(int i1, i2 ... 2) : (3, 4)')
        Left: 
          ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(int i1, i2)')
            NaturalType: (System.Int32 i1, System.Int32 i2)
            Elements(2):
                IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int i1')
                  ILocalReferenceOperation: i1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i1')
                ILocalReferenceOperation: i2 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i2')
        Right: 
          IConditionalOperation (OperationKind.Conditional, Type: (System.Int32, System.Int32)) (Syntax: 'b ? (1, 2) : (3, 4)')
            Condition: 
              IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
            WhenTrue: 
              ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
                NaturalType: (System.Int32, System.Int32)
                Elements(2):
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
            WhenFalse: 
              ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(3, 4)')
                NaturalType: (System.Int32, System.Int32)
                Elements(2):
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')";
            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [System.Int32 i2] [System.Int32 i1]
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value: 
                ILocalReferenceOperation: i2 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i2')
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(1, 2)')
              Value: 
                ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
                  NaturalType: (System.Int32, System.Int32)
                  Elements(2):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(3, 4)')
              Value: 
                ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(3, 4)')
                  NaturalType: (System.Int32, System.Int32)
                  Elements(2):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '(int i1, i2 ... ) : (3, 4);')
              Expression: 
                IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(int i1, i2 ... 2) : (3, 4)')
                  Left: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(int i1, i2)')
                      NaturalType: (System.Int32 i1, System.Int32 i2)
                      Elements(2):
                          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int i1')
                            ILocalReferenceOperation: i1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i1')
                          IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i2')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 'b ? (1, 2) : (3, 4)')
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void MixedNestedDeconstruction()
        {
            string source = @"
class C
{
    void M(bool b)
    /*<bind>*/{
        int i2;
        (int i1, (i2, int i3)) = b ? (1, (2, 3)) : (4, (5, 6));
    }/*</bind>*/
}";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IBlockOperation (2 statements, 3 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Locals: Local_1: System.Int32 i2
    Local_2: System.Int32 i1
    Local_3: System.Int32 i3
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'int i2;')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i2')
      Declarators:
          IVariableDeclaratorOperation (Symbol: System.Int32 i2) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i2')
            Initializer: 
              null
      Initializer: 
        null
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '(int i1, (i ... 4, (5, 6));')
    Expression: 
      IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 i1, (System.Int32 i2, System.Int32 i3))) (Syntax: '(int i1, (i ... (4, (5, 6))')
        Left: 
          ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, (System.Int32 i2, System.Int32 i3))) (Syntax: '(int i1, (i2, int i3))')
            NaturalType: (System.Int32 i1, (System.Int32 i2, System.Int32 i3))
            Elements(2):
                IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int i1')
                  ILocalReferenceOperation: i1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i1')
                ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i2, System.Int32 i3)) (Syntax: '(i2, int i3)')
                  NaturalType: (System.Int32 i2, System.Int32 i3)
                  Elements(2):
                      ILocalReferenceOperation: i2 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i2')
                      IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int i3')
                        ILocalReferenceOperation: i3 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i3')
        Right: 
          IConditionalOperation (OperationKind.Conditional, Type: (System.Int32, (System.Int32, System.Int32))) (Syntax: 'b ? (1, (2, ... (4, (5, 6))')
            Condition: 
              IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
            WhenTrue: 
              ITupleOperation (OperationKind.Tuple, Type: (System.Int32, (System.Int32, System.Int32))) (Syntax: '(1, (2, 3))')
                NaturalType: (System.Int32, (System.Int32, System.Int32))
                Elements(2):
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(2, 3)')
                      NaturalType: (System.Int32, System.Int32)
                      Elements(2):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            WhenFalse: 
              ITupleOperation (OperationKind.Tuple, Type: (System.Int32, (System.Int32, System.Int32))) (Syntax: '(4, (5, 6))')
                NaturalType: (System.Int32, (System.Int32, System.Int32))
                Elements(2):
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(5, 6)')
                      NaturalType: (System.Int32, System.Int32)
                      Elements(2):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6) (Syntax: '6')";
            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [System.Int32 i2] [System.Int32 i1] [System.Int32 i3]
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value: 
                ILocalReferenceOperation: i2 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i2')
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(1, (2, 3))')
              Value: 
                ITupleOperation (OperationKind.Tuple, Type: (System.Int32, (System.Int32, System.Int32))) (Syntax: '(1, (2, 3))')
                  NaturalType: (System.Int32, (System.Int32, System.Int32))
                  Elements(2):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                      ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(2, 3)')
                        NaturalType: (System.Int32, System.Int32)
                        Elements(2):
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(4, (5, 6))')
              Value: 
                ITupleOperation (OperationKind.Tuple, Type: (System.Int32, (System.Int32, System.Int32))) (Syntax: '(4, (5, 6))')
                  NaturalType: (System.Int32, (System.Int32, System.Int32))
                  Elements(2):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                      ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(5, 6)')
                        NaturalType: (System.Int32, System.Int32)
                        Elements(2):
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6) (Syntax: '6')
        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '(int i1, (i ... 4, (5, 6));')
              Expression: 
                IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 i1, (System.Int32 i2, System.Int32 i3))) (Syntax: '(int i1, (i ... (4, (5, 6))')
                  Left: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, (System.Int32 i2, System.Int32 i3))) (Syntax: '(int i1, (i2, int i3))')
                      NaturalType: (System.Int32 i1, (System.Int32 i2, System.Int32 i3))
                      Elements(2):
                          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int i1')
                            ILocalReferenceOperation: i1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i1')
                          ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i2, System.Int32 i3)) (Syntax: '(i2, int i3)')
                            NaturalType: (System.Int32 i2, System.Int32 i3)
                            Elements(2):
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i2')
                                IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int i3')
                                  ILocalReferenceOperation: i3 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i3')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: (System.Int32, (System.Int32, System.Int32)), IsImplicit) (Syntax: 'b ? (1, (2, ... (4, (5, 6))')
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void DeconstructionFlow_06()
        {
            string source = @"
class C
{
    int fI1 = 0;
    void M(bool b, C c1, int i1)
    /*<bind>*/{
        (c1?.fI1, i1) = b ? (1, 2) : (3, 4);
    }/*</bind>*/
}

";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         (c1?.fI1, i1) = b ? (1, 2) : (3, 4);
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "c1?.fI1").WithLocation(7, 10),
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3}

.locals {R1}
{
    CaptureIds: [2] [3] [4]
    .locals {R2}
    {
        CaptureIds: [0]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B1] - Block
                Predecessors: [B0]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c1')

                Jump if True (Regular) to Block[B3]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'c1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'c1')
                    Leaving: {R3}

                Next (Regular) Block[B2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '.fI1')
                      Value: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: '.fI1')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitNullable)
                          Operand: 
                            IFieldReferenceOperation: System.Int32 C.fI1 (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: '.fI1')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'c1')

                Next (Regular) Block[B4]
                    Leaving: {R3}
        }

        Block[B3] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'c1')
                  Value: 
                    IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'c1')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'c1?.fI1')
                  Value: 
                    IInvalidOperation (OperationKind.Invalid, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'c1?.fI1')
                      Children(1):
                          IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'c1?.fI1')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B5] - Block
        Predecessors: [B4]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
              Value: 
                IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')

        Jump if False (Regular) to Block[B7]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B6]
    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(1, 2)')
              Value: 
                ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
                  NaturalType: (System.Int32, System.Int32)
                  Elements(2):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B8]
    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(3, 4)')
              Value: 
                ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(3, 4)')
                  NaturalType: (System.Int32, System.Int32)
                  Elements(2):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '(c1?.fI1, i ... ) : (3, 4);')
              Expression: 
                IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32? fI1, System.Int32 i1), IsInvalid) (Syntax: '(c1?.fI1, i ... 2) : (3, 4)')
                  Left: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32? fI1, System.Int32 i1), IsInvalid) (Syntax: '(c1?.fI1, i1)')
                      NaturalType: (System.Int32? fI1, System.Int32 i1)
                      Elements(2):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'c1?.fI1')
                          IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                  Right: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: (System.Int32, System.Int32), IsImplicit) (Syntax: 'b ? (1, 2) : (3, 4)')

        Next (Regular) Block[B9]
            Leaving: {R1}
}

Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void DeconstructionFlow_07()
        {
            string source = @"
class C
{
    void M(bool b, int i1, int i2, int i3)
    /*<bind>*/{
        (i1, (i2, i3)) = b ? (1, (2, 3)) : (4, (5, 6));
    }/*</bind>*/
}

";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [2] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
              Value: 
                IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i3')
              Value: 
                IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i3')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(1, (2, 3))')
              Value: 
                ITupleOperation (OperationKind.Tuple, Type: (System.Int32, (System.Int32, System.Int32))) (Syntax: '(1, (2, 3))')
                  NaturalType: (System.Int32, (System.Int32, System.Int32))
                  Elements(2):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                      ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(2, 3)')
                        NaturalType: (System.Int32, System.Int32)
                        Elements(2):
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(4, (5, 6))')
              Value: 
                ITupleOperation (OperationKind.Tuple, Type: (System.Int32, (System.Int32, System.Int32))) (Syntax: '(4, (5, 6))')
                  NaturalType: (System.Int32, (System.Int32, System.Int32))
                  Elements(2):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                      ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(5, 6)')
                        NaturalType: (System.Int32, System.Int32)
                        Elements(2):
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6) (Syntax: '6')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '(i1, (i2, i ... 4, (5, 6));')
              Expression: 
                IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 i1, (System.Int32 i2, System.Int32 i3))) (Syntax: '(i1, (i2, i ... (4, (5, 6))')
                  Left: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, (System.Int32 i2, System.Int32 i3))) (Syntax: '(i1, (i2, i3))')
                      NaturalType: (System.Int32 i1, (System.Int32 i2, System.Int32 i3))
                      Elements(2):
                          IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                          ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i2, System.Int32 i3)) (Syntax: '(i2, i3)')
                            NaturalType: (System.Int32 i2, System.Int32 i3)
                            Elements(2):
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i2')
                                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i3')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: (System.Int32, (System.Int32, System.Int32)), IsImplicit) (Syntax: 'b ? (1, (2, ... (4, (5, 6))')

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
        public void DeconstructionFlow_08()
        {
            string source = @"
class C
{
    void M(int i1, int i2, int i3, int i4, int i5, int? i6, int i7)
    /*<bind>*/
    {
        ((i1, i2), i3) = ((i4, i5), i6 ?? i7);
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [2] [3] [4] [6]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (5)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
              Value: 
                IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i3')
              Value: 
                IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i3')

            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i4')
              Value: 
                IParameterReferenceOperation: i4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i4')

            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i5')
              Value: 
                IParameterReferenceOperation: i5 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i5')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [5]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i6')
                  Value: 
                    IParameterReferenceOperation: i6 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i6')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i6')
                  Operand: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i6')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i6')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i6')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i6')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i7')
              Value: 
                IParameterReferenceOperation: i7 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i7')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '((i1, i2),  ...  i6 ?? i7);')
              Expression: 
                IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ((System.Int32 i1, System.Int32 i2), System.Int32 i3)) (Syntax: '((i1, i2),  ... , i6 ?? i7)')
                  Left: 
                    ITupleOperation (OperationKind.Tuple, Type: ((System.Int32 i1, System.Int32 i2), System.Int32 i3)) (Syntax: '((i1, i2), i3)')
                      NaturalType: ((System.Int32 i1, System.Int32 i2), System.Int32 i3)
                      Elements(2):
                          ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(i1, i2)')
                            NaturalType: (System.Int32 i1, System.Int32 i2)
                            Elements(2):
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i2')
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i3')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ((System.Int32, System.Int32), System.Int32), IsImplicit) (Syntax: '((i4, i5), i6 ?? i7)')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      Operand: 
                        ITupleOperation (OperationKind.Tuple, Type: ((System.Int32 i4, System.Int32 i5), System.Int32)) (Syntax: '((i4, i5), i6 ?? i7)')
                          NaturalType: ((System.Int32 i4, System.Int32 i5), System.Int32)
                          Elements(2):
                              ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i4, System.Int32 i5)) (Syntax: '(i4, i5)')
                                NaturalType: (System.Int32 i4, System.Int32 i5)
                                Elements(2):
                                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i4')
                                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i5')
                              IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i6 ?? i7')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
