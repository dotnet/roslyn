// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IFromEndIndexOperation_IRangeOperation : SemanticModelTestBase
    {
        // The tests in this file right now are just to verify that we do not assert in the CFG builder. These need to be expanded.
        // https://github.com/dotnet/roslyn/issues/31545

        [Fact]
        public void PatternIndexAndRangeIndexer()
        {
            var src = @"
class C
{
    public int Length => 0;
    public int this[int i] => i;
    public int Slice(int i, int j) => i;
    public void M()
    /*<bind*/{
        _ = this[^0];
        _ = this[0..];
    }/*</bind>*/
}";
            var comp = CreateCompilationWithIndexAndRange(src);
            const string expectedOperationTree = @"
IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '_ = this[^0];')
    Expression:
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '_ = this[^0]')
        Left:
          IDiscardOperation (Symbol: System.Int32 _) (OperationKind.Discard, Type: System.Int32) (Syntax: '_')
        Right:
          IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Int32) (Syntax: 'this[^0]')
            Instance:
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
            Argument:
              IUnaryOperation (UnaryOperatorKind.Hat) (OperationKind.Unary, Type: System.Index) (Syntax: '^0')
                Operand:
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            LengthSymbol: System.Int32 C.Length { get; }
            IndexerSymbol: System.Int32 C.this[System.Int32 i] { get; }
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '_ = this[0..];')
    Expression:
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '_ = this[0..]')
        Left:
          IDiscardOperation (Symbol: System.Int32 _) (OperationKind.Discard, Type: System.Int32) (Syntax: '_')
        Right:
          IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Int32) (Syntax: 'this[0..]')
            Instance:
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
            Argument:
              IRangeOperation (OperationKind.Range, Type: System.Range) (Syntax: '0..')
                LeftOperand:
                  IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.Index System.Index.op_Implicit(System.Int32 value)) (OperationKind.Conversion, Type: System.Index, IsImplicit) (Syntax: '0')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Index System.Index.op_Implicit(System.Int32 value))
                    Operand:
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                RightOperand:
                  null
            LengthSymbol: System.Int32 C.Length { get; }
            IndexerSymbol: System.Int32 C.Slice(System.Int32 i, System.Int32 j)
";
            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, expectedOperationTree, DiagnosticDescription.None);

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, expectedOperationTree, DiagnosticDescription.None);

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '_ = this[^0];')
          Expression:
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '_ = this[^0]')
              Left:
                IDiscardOperation (Symbol: System.Int32 _) (OperationKind.Discard, Type: System.Int32) (Syntax: '_')
              Right:
                IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Int32) (Syntax: 'this[^0]')
                  Instance:
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
                  Argument:
                    IUnaryOperation (UnaryOperatorKind.Hat) (OperationKind.Unary, Type: System.Index) (Syntax: '^0')
                      Operand:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                  LengthSymbol: System.Int32 C.Length { get; }
                  IndexerSymbol: System.Int32 C.this[System.Int32 i] { get; }
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '_ = this[0..];')
          Expression:
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '_ = this[0..]')
              Left:
                IDiscardOperation (Symbol: System.Int32 _) (OperationKind.Discard, Type: System.Int32) (Syntax: '_')
              Right:
                IImplicitIndexerReferenceOperation (OperationKind.ImplicitIndexerReference, Type: System.Int32) (Syntax: 'this[0..]')
                  Instance:
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
                  Argument:
                    IRangeOperation (OperationKind.Range, Type: System.Range) (Syntax: '0..')
                      LeftOperand:
                        IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.Index System.Index.op_Implicit(System.Int32 value)) (OperationKind.Conversion, Type: System.Index, IsImplicit) (Syntax: '0')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Index System.Index.op_Implicit(System.Int32 value))
                            (ImplicitUserDefined)
                          Operand:
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                      RightOperand:
                        null
                  LengthSymbol: System.Int32 C.Length { get; }
                  IndexerSymbol: System.Int32 C.Slice(System.Int32 i, System.Int32 j)
    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";

            VerifyFlowGraphForTest<BlockSyntax>(comp, expectedFlowGraph);
        }

        [Fact]
        public void FromEndIndexFlow_01()
        {
            var source = @"
class Test
{
    void M(int arg)
    /*<bind>*/{
        var x = ^arg;
    }/*</bind>*/
}";

            var compilation = CreateCompilationWithIndex(source);

            var expectedOperationTree = @"
IBlockOperation (1 statements, 1 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Locals: Local_1: System.Index x
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var x = ^arg;')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var x = ^arg')
      Declarators:
          IVariableDeclaratorOperation (Symbol: System.Index x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x = ^arg')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= ^arg')
                IUnaryOperation (UnaryOperatorKind.Hat) (OperationKind.Unary, Type: System.Index) (Syntax: '^arg')
                  Operand: 
                    IParameterReferenceOperation: arg (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'arg')
      Initializer: 
        null";

            var diagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(compilation, expectedOperationTree, diagnostics);

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Index x]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Index, IsImplicit) (Syntax: 'x = ^arg')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Index, IsImplicit) (Syntax: 'x = ^arg')
              Right: 
                IUnaryOperation (UnaryOperatorKind.Hat) (OperationKind.Unary, Type: System.Index) (Syntax: '^arg')
                  Operand: 
                    IParameterReferenceOperation: arg (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'arg')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)";

            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedFlowGraph);
        }

        [Fact]
        public void RangeFlow_01()
        {
            var source = @"
using System;
class Test
{
    void M(Index start, Index end)
    /*<bind>*/{
        var a = start..end;
        var b = start..;
        var c = ..end;
        var d = ..;
    }/*</bind>*/
}";

            var compilation = CreateCompilationWithIndexAndRange(source);

            var expectedOperationTree = @"
IBlockOperation (4 statements, 4 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  Locals: Local_1: System.Range a
    Local_2: System.Range b
    Local_3: System.Range c
    Local_4: System.Range d
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var a = start..end;')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var a = start..end')
      Declarators:
          IVariableDeclaratorOperation (Symbol: System.Range a) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'a = start..end')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= start..end')
                IRangeOperation (OperationKind.Range, Type: System.Range) (Syntax: 'start..end')
                  LeftOperand: 
                    IParameterReferenceOperation: start (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'start')
                  RightOperand: 
                    IParameterReferenceOperation: end (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'end')
      Initializer: 
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var b = start..;')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var b = start..')
      Declarators:
          IVariableDeclaratorOperation (Symbol: System.Range b) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'b = start..')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= start..')
                IRangeOperation (OperationKind.Range, Type: System.Range) (Syntax: 'start..')
                  LeftOperand: 
                    IParameterReferenceOperation: start (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'start')
                  RightOperand: 
                    null
      Initializer: 
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var c = ..end;')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var c = ..end')
      Declarators:
          IVariableDeclaratorOperation (Symbol: System.Range c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = ..end')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= ..end')
                IRangeOperation (OperationKind.Range, Type: System.Range) (Syntax: '..end')
                  LeftOperand: 
                    null
                  RightOperand: 
                    IParameterReferenceOperation: end (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'end')
      Initializer: 
        null
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var d = ..;')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var d = ..')
      Declarators:
          IVariableDeclaratorOperation (Symbol: System.Range d) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'd = ..')
            Initializer: 
              IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= ..')
                IRangeOperation (OperationKind.Range, Type: System.Range) (Syntax: '..')
                  LeftOperand: 
                    null
                  RightOperand: 
                    null
      Initializer: 
        null
";

            var diagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(compilation, expectedOperationTree, diagnostics);

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Range a] [System.Range b] [System.Range c] [System.Range d]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Range, IsImplicit) (Syntax: 'a = start..end')
              Left: 
                ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Range, IsImplicit) (Syntax: 'a = start..end')
              Right: 
                IRangeOperation (OperationKind.Range, Type: System.Range) (Syntax: 'start..end')
                  LeftOperand: 
                    IParameterReferenceOperation: start (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'start')
                  RightOperand: 
                    IParameterReferenceOperation: end (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'end')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Range, IsImplicit) (Syntax: 'b = start..')
              Left: 
                ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Range, IsImplicit) (Syntax: 'b = start..')
              Right: 
                IRangeOperation (OperationKind.Range, Type: System.Range) (Syntax: 'start..')
                  LeftOperand: 
                    IParameterReferenceOperation: start (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'start')
                  RightOperand: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Range, IsImplicit) (Syntax: 'c = ..end')
              Left: 
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Range, IsImplicit) (Syntax: 'c = ..end')
              Right: 
                IRangeOperation (OperationKind.Range, Type: System.Range) (Syntax: '..end')
                  LeftOperand: 
                    null
                  RightOperand: 
                    IParameterReferenceOperation: end (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'end')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Range, IsImplicit) (Syntax: 'd = ..')
              Left: 
                ILocalReferenceOperation: d (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Range, IsImplicit) (Syntax: 'd = ..')
              Right: 
                IRangeOperation (OperationKind.Range, Type: System.Range) (Syntax: '..')
                  LeftOperand: 
                    null
                  RightOperand: 
                    null

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";

            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedFlowGraph);
        }
    }
}
