// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Operations
{
    internal sealed class ControlFlowGraphBuilder : CSharpOperationCloner
    {
        private readonly BasicBlock _entry = new BasicBlock(BasicBlockKind.Entry);
        private readonly BasicBlock _exit = new BasicBlock(BasicBlockKind.Exit);
        private ArrayBuilder<BasicBlock> _blocks;
        private BasicBlock _currentBasicBlock;
        private IOperation _currentStatement;

        private ControlFlowGraphBuilder()
        { }

        public static ImmutableArray<BasicBlock> Create(IBlockOperation body)
        {
            var builder = new ControlFlowGraphBuilder();
            var blocks = ArrayBuilder<BasicBlock>.GetInstance();
            builder._blocks = blocks;
            blocks.Add(builder._entry);

            builder.VisitStatement(body);
            builder.AppendNewBlock(builder._exit);

            // Do a pass to eliminate blocks without statements and only Next set.
            Pack(blocks);

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
             AddStatement(Visit(operation, null));
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

            CurrentBasicBlock.AddStatement(statement);
        }

        private void AppendNewBlock(BasicBlock block)
        {
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

                    var afterIf = new BasicBlock(BasicBlockKind.Block);
                    BasicBlock current = CurrentBasicBlock;
                    _currentBasicBlock = null;

                    LinkBlocks(current, (operation.Condition, false, afterIf));
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

                    BasicBlock conditional = CurrentBasicBlock;
                    _currentBasicBlock = null;

                    VisitStatement(operation.WhenTrue);

                    var afterIf = new BasicBlock(BasicBlockKind.Block);
                    LinkBlocks(CurrentBasicBlock, afterIf);
                    _currentBasicBlock = null;

                    BasicBlock whenFalse = new BasicBlock(BasicBlockKind.Block);
                    LinkBlocks(conditional, (operation.Condition, false, whenFalse));
                    AppendNewBlock(whenFalse);
                    VisitStatement(operation.WhenFalse);

                    AppendNewBlock(afterIf);
                }

                return null;
            }

            return base.VisitConditional(operation, argument);
        }

        private static void LinkBlocks(BasicBlock previous, (IOperation Condition, bool JumpIfTrue, BasicBlock Destination) next)
        {
            Debug.Assert(previous.Conditional.Condition == null);
            next.Destination.AddPredecessor(previous);
            previous.Conditional = next;
        }
    }
}
