// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Operations
{
    internal sealed class ControlFlowGraphBuilder : OperationCloner
    {
        // PROTOTYPE(dataflow): does it have to be a field?
        private readonly BasicBlock _entry = new BasicBlock(BasicBlockKind.Entry);

        // PROTOTYPE(dataflow): does it have to be a field?
        private readonly BasicBlock _exit = new BasicBlock(BasicBlockKind.Exit);

        private ArrayBuilder<BasicBlock> _blocks;
        private BasicBlock _currentBasicBlock;
        private IOperation _currentStatement;
        private ArrayBuilder<IOperation> _evalStack;

        // PROTOTYPE(dataflow): does the public API IFlowCaptureOperation.Id specify how identifiers are created or assigned?
        // Should we use uint to exclude negative integers? Should we randomize them in any way to avoid dependencies 
        // being taken?
        private int _availableCaptureId = 0;

        private ControlFlowGraphBuilder()
        { }

        public static ImmutableArray<BasicBlock> Create(IBlockOperation body)
        {
            var builder = new ControlFlowGraphBuilder();
            var blocks = ArrayBuilder<BasicBlock>.GetInstance();
            builder._blocks = blocks;
            builder._evalStack = ArrayBuilder<IOperation>.GetInstance();
            blocks.Add(builder._entry);

            builder.VisitStatement(body);
            builder.AppendNewBlock(builder._exit);

            // Do a pass to eliminate blocks without statements and only Next set.
            Pack(blocks);

            Debug.Assert(builder._evalStack.Count == 0);
            builder._evalStack.Free();

            return blocks.ToImmutableAndFree();
        }

        private static void Pack(ArrayBuilder<BasicBlock> blocks)
        {
            int count = blocks.Count - 1;
            for (int i = 1; i < count; i++)
            {
                BasicBlock block = blocks[i];
                Debug.Assert(block.Next != null);
                if (block.Statements.IsEmpty && block.Conditional.Condition == null)
                {
                    BasicBlock next = block.Next;
                    foreach (BasicBlock predecessor in block.Predecessors)
                    {
                        if (predecessor.Next == block)
                        {
                            predecessor.Next = next;
                            next.AddPredecessor(predecessor);
                        }

                        (IOperation condition, bool jumpIfTrue, BasicBlock destination) = predecessor.Conditional;
                        if (destination == block)
                        {
                            predecessor.Conditional = (condition, jumpIfTrue, next);
                            next.AddPredecessor(predecessor);
                        }
                    }

                    next.RemovePredecessor(block);
                    blocks.RemoveAt(i);
                    i--;
                    count--;
                }
            }
        }

        private void VisitStatement(IOperation operation)
        {
            IOperation saveCurrentStatement = _currentStatement;
            _currentStatement = operation;
            Debug.Assert(_evalStack.Count == 0);

            // PROTOTYPE(dataflow): Ensure that statement's parent is null at this point.
            AddStatement(Visit(operation, null));
            Debug.Assert(_evalStack.Count == 0);
            _currentStatement = saveCurrentStatement;
        }

        private BasicBlock CurrentBasicBlock
        {
            get
            {
                if (_currentBasicBlock == null)
                {
                    AppendNewBlock(new BasicBlock(BasicBlockKind.Block));
                }

                return _currentBasicBlock;
            }
        }

        private void AddStatement(IOperation statement)
        {
            if (statement == null)
            {
                return;
            }

            // PROTOTYPE(dataflow): Assert that statement's parent is null at this point.
            CurrentBasicBlock.AddStatement(statement);
        }

        private void AppendNewBlock(BasicBlock block)
        {
            Debug.Assert(block != null);
            BasicBlock prevBlock = _blocks.Last();

            if (prevBlock.Next == null)
            {
                LinkBlocks(prevBlock, block);
            }

            _blocks.Add(block);
            _currentBasicBlock = block;
        }

        private static void LinkBlocks(BasicBlock prevBlock, BasicBlock nextBlock)
        {
            Debug.Assert(prevBlock.Next == null);
            prevBlock.Next = nextBlock;
            nextBlock.AddPredecessor(prevBlock);
        }

        public override IOperation VisitBlock(IBlockOperation operation, object argument)
        {
            foreach (var statement in operation.Operations)
            {
                VisitStatement(statement);
            }

            return null;
        }

        public override IOperation VisitConditional(IConditionalOperation operation, object argument)
        {
            if (operation == _currentStatement)
            {
                if (operation.WhenFalse == null)
                {
                    // if (condition) 
                    //   consequence;  
                    //
                    // becomes
                    //
                    // GotoIfFalse condition afterif;
                    // consequence;
                    // afterif:

                    BasicBlock afterIf = null;
                    VisitConditionalBranch(operation.Condition, ref afterIf, sense: false);
                    VisitStatement(operation.WhenTrue);
                    AppendNewBlock(afterIf);
                }
                else
                {
                    // if (condition)
                    //     consequence;
                    // else 
                    //     alternative
                    //
                    // becomes
                    //
                    // GotoIfFalse condition alt;
                    // consequence
                    // goto afterif;
                    // alt:
                    // alternative;
                    // afterif:

                    BasicBlock whenFalse = null;
                    VisitConditionalBranch(operation.Condition, ref whenFalse, sense: false);

                    VisitStatement(operation.WhenTrue);

                    var afterIf = new BasicBlock(BasicBlockKind.Block);
                    LinkBlocks(CurrentBasicBlock, afterIf);
                    _currentBasicBlock = null;

                    AppendNewBlock(whenFalse);
                    VisitStatement(operation.WhenFalse);

                    AppendNewBlock(afterIf);
                }

                return null;
            }
            else
            {
                // condition ? consequence : alternative
                //
                // becomes
                //
                // GotoIfFalse condition alt;
                // capture = consequence
                // goto afterif;
                // alt:
                // capture = alternative;
                // afterif:
                // result - capture

                SpillEvalStack();
                int captureId = _availableCaptureId++;

                BasicBlock whenFalse = null;
                VisitConditionalBranch(operation.Condition, ref whenFalse, sense: false);

                AddStatement(new SimpleAssignmentExpression(new FlowCapture(captureId, operation.Syntax,
                                                                            isInitialization: true, operation.Type,
                                                                            default(Optional<object>)),
                                                            operation.IsRef,
                                                            Visit(operation.WhenTrue),
                                                            semanticModel: null,
                                                            operation.WhenTrue.Syntax,
                                                            operation.Type,
                                                            default(Optional<object>),
                                                            isImplicit: true));

                var afterIf = new BasicBlock(BasicBlockKind.Block);
                LinkBlocks(CurrentBasicBlock, afterIf);
                _currentBasicBlock = null;

                AppendNewBlock(whenFalse);

                AddStatement(new SimpleAssignmentExpression(new FlowCapture(captureId, operation.Syntax,
                                                                            isInitialization: true, operation.Type,
                                                                            default(Optional<object>)),
                                                            operation.IsRef,
                                                            Visit(operation.WhenFalse),
                                                            semanticModel: null,
                                                            operation.WhenFalse.Syntax,
                                                            operation.Type,
                                                            default(Optional<object>),
                                                            isImplicit: true));

                AppendNewBlock(afterIf);

                return new FlowCapture(captureId, operation.Syntax, isInitialization: false, operation.Type, operation.ConstantValue);
            }
        }

        private void SpillEvalStack()
        {
            for (int i = 0; i < _evalStack.Count; i++)
            {
                IOperation operation = _evalStack[i];
                if (operation.Kind != OperationKind.FlowCapture)
                {
                    int captureId = _availableCaptureId++;

                    AddStatement(new SimpleAssignmentExpression(new FlowCapture(captureId, operation.Syntax,
                                                                                isInitialization: true, operation.Type,
                                                                                default(Optional<object>)),
                                                                isRef: false, // PROTOTYPE(dataflow): Is 'false' always the right value?
                                                                operation,
                                                                semanticModel: null,
                                                                operation.Syntax,
                                                                operation.Type,
                                                                default(Optional<object>),
                                                                isImplicit: true));

                    _evalStack[i] = new FlowCapture(captureId, operation.Syntax, isInitialization: false, operation.Type, operation.ConstantValue);
                }
            }
        }

        public override IOperation VisitSimpleAssignment(ISimpleAssignmentOperation operation, object argument)
        {
            _evalStack.Push(Visit(operation.Target));
            IOperation value = Visit(operation.Value);
            return new SimpleAssignmentExpression(_evalStack.Pop(), operation.IsRef, value, null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        // PROTOTYPE(dataflow):
        //public override IOperation VisitArrayElementReference(IArrayElementReferenceOperation operation, object argument)
        //{
        //    _evalStack.Push(Visit(operation.ArrayReference));
        //    foreach (var index in operation.Indices)
        //    return new ArrayElementReferenceExpression(Visit(operation.ArrayReference), VisitArray(operation.Indices), ((Operation)operation).SemanticModel, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        //}

        private static bool IsConditional(IBinaryOperation operation)
        {
            switch (operation.OperatorKind)
            {
                case BinaryOperatorKind.ConditionalOr:
                case BinaryOperatorKind.ConditionalAnd:
                    return true;
            }

            return false;
        }

        public override IOperation VisitBinaryOperator(IBinaryOperation operation, object argument)
        {
            if (IsConditional(operation))
            {
                return VisitBinaryConditionalOperator(operation, sense: true);
            }

            return base.VisitBinaryOperator(operation, argument);
        }

        public override IOperation VisitUnaryOperator(IUnaryOperation operation, object argument)
        {
            // PROTOTYPE(dataflow): ensure we properly detect logical Not
            if (operation.OperatorKind == UnaryOperatorKind.Not)
            {
                return VisitConditionalExpression(operation.Operand, sense: false);
            }

            return base.VisitUnaryOperator(operation, argument);
        }

        private IOperation VisitBinaryConditionalOperator(IBinaryOperation binOp, bool sense)
        {
            bool andOrSense = sense;

            switch (binOp.OperatorKind)
            {
                case BinaryOperatorKind.ConditionalOr:
                    Debug.Assert(binOp.LeftOperand.Type.SpecialType == SpecialType.System_Boolean);
                    Debug.Assert(binOp.RightOperand.Type.SpecialType == SpecialType.System_Boolean);

                    // Rewrite (a || b) as ~(~a && ~b)
                    andOrSense = !andOrSense;
                    // Fall through
                    goto case BinaryOperatorKind.ConditionalAnd;

                case BinaryOperatorKind.ConditionalAnd:
                    Debug.Assert(binOp.LeftOperand.Type.SpecialType == SpecialType.System_Boolean);
                    Debug.Assert(binOp.RightOperand.Type.SpecialType == SpecialType.System_Boolean);

                    // ~(a && b) is equivalent to (~a || ~b)
                    if (!andOrSense)
                    {
                        // generate (~a || ~b)
                        return VisitShortCircuitingOperator(binOp, sense: sense, stopSense: sense, stopValue: true);
                    }
                    else
                    {
                        // generate (a && b)
                        return VisitShortCircuitingOperator(binOp, sense: sense, stopSense: !sense, stopValue: false);
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(binOp.OperatorKind);
            }
        }

        private IOperation VisitShortCircuitingOperator(IBinaryOperation condition, bool sense, bool stopSense, bool stopValue)
        {
            // we generate:
            //
            // gotoif (a == stopSense) fallThrough
            // b == sense
            // goto labEnd
            // fallThrough:
            // stopValue
            // labEnd:
            //                 AND       OR
            //            +-  ------    -----
            // stopSense  |   !sense    sense
            // stopValue  |     0         1

            SpillEvalStack();

            BasicBlock lazyFallThrough = null;

            VisitConditionalBranch(condition.LeftOperand, ref lazyFallThrough, stopSense);
            IOperation resultFromRight = VisitConditionalExpression(condition.RightOperand, sense);

            int captureId;

            if (resultFromRight.Kind == OperationKind.FlowCapture)
            {
                captureId = ((IFlowCaptureOperation)resultFromRight).Id;
            }
            else
            {
                captureId = _availableCaptureId++;
                SyntaxNode rightSyntax = condition.RightOperand.Syntax;
                AddStatement(new SimpleAssignmentExpression(new FlowCapture(captureId, rightSyntax,
                                                                            isInitialization: true, condition.Type,
                                                                            default(Optional<object>)),
                                                            isRef: false,
                                                            resultFromRight,
                                                            semanticModel: null,
                                                            rightSyntax,
                                                            condition.Type,
                                                            default(Optional<object>),
                                                            isImplicit: true));
            }

            var labEnd = new BasicBlock(BasicBlockKind.Block);
            LinkBlocks(CurrentBasicBlock, labEnd);
            _currentBasicBlock = null;

            AppendNewBlock(lazyFallThrough);

            var constantValue = new Optional<object>(stopValue);
            SyntaxNode leftSyntax = condition.LeftOperand.Syntax;
            AddStatement(new SimpleAssignmentExpression(new FlowCapture(captureId, leftSyntax,
                                                                        isInitialization: true, condition.Type,
                                                                        default(Optional<object>)),
                                                        isRef: false,
                                                        new LiteralExpression(semanticModel: null, leftSyntax, condition.Type, constantValue, isImplicit: true),
                                                        semanticModel: null,
                                                        leftSyntax,
                                                        condition.Type,
                                                        constantValue,
                                                        isImplicit: true));

            AppendNewBlock(labEnd);

            return new FlowCapture(captureId, condition.Syntax, isInitialization: false, condition.Type, condition.ConstantValue);
        }

        private IOperation VisitConditionalExpression(IOperation condition, bool sense)
        {
            // PROTOTYPE(dataflow): Do not erase UnaryOperatorKind.Not if ProduceIsSense below will have to add it back.
            while (condition.Kind == OperationKind.UnaryOperator)
            {
                var unOp = (IUnaryOperation)condition;
                // PROTOTYPE(dataflow): ensure we properly detect logical Not
                if (unOp.OperatorKind != UnaryOperatorKind.Not)
                {
                    break;
                }
                condition = unOp.Operand;
                sense = !sense;
            }

            Debug.Assert(condition.Type.SpecialType == SpecialType.System_Boolean);

            if (condition.Kind == OperationKind.BinaryOperator)
            {
                var binOp = (IBinaryOperation)condition;
                if (IsConditional(binOp))
                {
                    return VisitBinaryConditionalOperator(binOp, sense);
                }
            }

            return ProduceIsSense(Visit(condition), sense);
        }

        private IOperation ProduceIsSense(IOperation condition, bool sense)
        {
            if (!sense)
            {
                return new UnaryOperatorExpression(UnaryOperatorKind.Not,
                                                   condition,
                                                   isLifted: false, // PROTOTYPE(dataflow): Deal with nullable
                                                   isChecked: false,
                                                   operatorMethod: null,
                                                   semanticModel: null,
                                                   condition.Syntax,
                                                   condition.Type,
                                                   constantValue: default, // revert constant value if we have one.
                                                   isImplicit: true);
            }

            return condition;
        }

        private void VisitConditionalBranch(IOperation condition, ref BasicBlock dest, bool sense)
        {
oneMoreTime:

            switch (condition.Kind)
            {
                case OperationKind.BinaryOperator:
                    var binOp = (IBinaryOperation)condition;
                    bool testBothArgs = sense;

                    switch (binOp.OperatorKind)
                    {
                        case BinaryOperatorKind.ConditionalOr:
                            testBothArgs = !testBothArgs;
                            // Fall through
                            goto case BinaryOperatorKind.ConditionalAnd;

                        case BinaryOperatorKind.ConditionalAnd:
                            if (testBothArgs)
                            {
                                // gotoif(a != sense) fallThrough
                                // gotoif(b == sense) dest
                                // fallThrough:

                                BasicBlock fallThrough = null;

                                VisitConditionalBranch(binOp.LeftOperand, ref fallThrough, !sense);
                                VisitConditionalBranch(binOp.RightOperand, ref dest, sense);

                                if (fallThrough != null)
                                {
                                    AppendNewBlock(fallThrough);
                                }
                            }
                            else
                            {
                                // gotoif(a == sense) labDest
                                // gotoif(b == sense) labDest

                                VisitConditionalBranch(binOp.LeftOperand, ref dest, sense);
                                condition = binOp.RightOperand;
                                goto oneMoreTime;
                            }
                            return;
                    }

                    // none of above. 
                    // then it is regular binary expression - Or, And, Xor ...
                    goto default;

                case OperationKind.UnaryOperator:
                    var unOp = (IUnaryOperation)condition;
                    if (unOp.OperatorKind == UnaryOperatorKind.Not && unOp.Operand.Type.SpecialType == SpecialType.System_Boolean)
                    {
                        sense = !sense;
                        condition = unOp.Operand;
                        goto oneMoreTime;
                    }
                    goto default;

                default:
                    condition = Visit(condition);
                    dest = dest ?? new BasicBlock(BasicBlockKind.Block);
                    LinkBlocks(CurrentBasicBlock, (condition, sense, dest));
                    _currentBasicBlock = null;
                    return;
            }
        }

        private static void LinkBlocks(BasicBlock previous, (IOperation Condition, bool JumpIfTrue, BasicBlock Destination) next)
        {
            Debug.Assert(previous.Conditional.Condition == null);
            next.Destination.AddPredecessor(previous);
            previous.Conditional = next;
        }
    }
}
