// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal sealed partial class ILBuilder
    {
        private readonly OptimizationLevel _optimizations;
        internal readonly LocalSlotManager LocalSlotManager;
        private readonly LocalScopeManager _scopeManager;

        // internal for testing
        internal readonly ITokenDeferral module;

        //leader block is the entry point of the method body
        internal readonly BasicBlock leaderBlock;

        private EmitState _emitState;
        private BasicBlock _lastCompleteBlock;
        private BasicBlock _currentBlock;

        private SyntaxTree _lastSeqPointTree;

        private readonly SmallDictionary<object, LabelInfo> _labelInfos;
        private int _instructionCountAtLastLabel = -1;

        // This data is only relevant when builder has been realized.
        internal ImmutableArray<byte> RealizedIL;
        internal ImmutableArray<Cci.ExceptionHandlerRegion> RealizedExceptionHandlers;
        internal SequencePointList RealizedSequencePoints;

        // debug sequence points from all blocks, note that each 
        // sequence point references absolute IL offset via IL marker
        public ArrayBuilder<RawSequencePoint> SeqPointsOpt;

        /// <summary> 
        /// In some cases we have to get a final IL offset during emit phase, for example for
        /// proper emitting sequence points. The problem is that before the builder is realized we 
        /// don't know the actual IL offset, but only {block/offset-in-the-block} pair. 
        /// 
        /// Thus, whenever we need to mark some IL position we allocate a new marker id, store it 
        /// in allocatedILMarkers and reference this IL marker in the entity requiring the IL offset.
        /// 
        /// IL markers will be 'materialized' when the builder is realized; the resulting offsets
        /// will be put into allocatedILMarkers array. Note that only markers from reachable blocks 
        /// are materialized, the rest will have offset -1.
        /// </summary>
        private ArrayBuilder<ILMarker> _allocatedILMarkers;

        // Since blocks are created lazily in GetCurrentBlock,
        // pendingBlockCreate is set to true when a block must be
        // created, in particular for leader blocks in exception handlers.
        private bool _pendingBlockCreate;

        internal ILBuilder(ITokenDeferral module, LocalSlotManager localSlotManager, OptimizationLevel optimizations)
        {
            Debug.Assert(BitConverter.IsLittleEndian);

            this.module = module;
            this.LocalSlotManager = localSlotManager;
            _emitState = default(EmitState);
            _scopeManager = new LocalScopeManager();

            leaderBlock = _currentBlock = _scopeManager.CreateBlock(this);

            _labelInfos = new SmallDictionary<object, LabelInfo>(ReferenceEqualityComparer.Instance);
            _optimizations = optimizations;
        }

        private BasicBlock GetCurrentBlock()
        {
            Debug.Assert(!_pendingBlockCreate || (_currentBlock == null));

            if (_currentBlock == null)
            {
                this.CreateBlock();
            }

            return _currentBlock;
        }

        private void CreateBlock()
        {
            Debug.Assert(_currentBlock == null);

            var block = _scopeManager.CreateBlock(this);
            UpdatesForCreatedBlock(block);
        }

        private SwitchBlock CreateSwitchBlock()
        {
            // end the current block
            EndBlock();

            SwitchBlock switchBlock = _scopeManager.CreateSwitchBlock(this);
            UpdatesForCreatedBlock(switchBlock);
            return switchBlock;
        }

        private void UpdatesForCreatedBlock(BasicBlock block)
        {
            _currentBlock = block;
            Debug.Assert(_lastCompleteBlock.NextBlock == null);
            _lastCompleteBlock.NextBlock = block;
            _pendingBlockCreate = false;
            ReconcileTrailingMarkers();
        }

        private void CreateBlockIfPending()
        {
            if (_pendingBlockCreate)
            {
                Debug.Assert(_currentBlock == null);
                this.CreateBlock();

                Debug.Assert(_currentBlock != null);
                Debug.Assert(!_pendingBlockCreate);
            }
        }

        private void EndBlock()
        {
            this.CreateBlockIfPending();

            if (_currentBlock != null)
            {
                _lastCompleteBlock = _currentBlock;
                _currentBlock = null;
            }
        }

        private void ReconcileTrailingMarkers()
        {
            //  there is a chance that 'lastCompleteBlock' may have an IL marker
            //  placed at the end of it such that block offset of the marker points
            //  to the next byte *after* the block is closed. In this case the marker 
            //  should be moved to the next block
            if (_lastCompleteBlock != null &&
                _lastCompleteBlock.BranchCode == ILOpCode.Nop &&
                _lastCompleteBlock.LastILMarker >= 0 &&
                _allocatedILMarkers[_lastCompleteBlock.LastILMarker].BlockOffset == _lastCompleteBlock.RegularInstructionsLength)
            {
                int startMarker = -1;
                int endMarker = -1;

                while (_lastCompleteBlock.LastILMarker >= 0 &&
                      _allocatedILMarkers[_lastCompleteBlock.LastILMarker].BlockOffset == _lastCompleteBlock.RegularInstructionsLength)
                {
                    Debug.Assert((startMarker < 0) || (startMarker == (_lastCompleteBlock.LastILMarker + 1)));
                    startMarker = _lastCompleteBlock.LastILMarker;
                    if (endMarker < 0)
                    {
                        endMarker = _lastCompleteBlock.LastILMarker;
                    }
                    _lastCompleteBlock.RemoveTailILMarker(_lastCompleteBlock.LastILMarker);
                }

                BasicBlock current = this.GetCurrentBlock();
                for (int marker = startMarker; marker <= endMarker; marker++)
                {
                    current.AddILMarker(marker);
                    _allocatedILMarkers[marker] = new ILMarker() { BlockOffset = (int)current.RegularInstructionsLength, AbsoluteOffset = -1 };
                }
            }
        }

        private ExceptionHandlerScope EnclosingExceptionHandler => _scopeManager.EnclosingExceptionHandler;

        internal bool InExceptionHandler => this.EnclosingExceptionHandler != null;

        /// <summary>
        /// Realizes method body.
        /// No more data can be added to the builder after this call.
        /// </summary>
        internal void Realize()
        {
            if (this.RealizedIL.IsDefault)
            {
                this.RealizeBlocks();

                // no more new data.
                _currentBlock = null;
                _lastCompleteBlock = null;
            }
        }

        /// <summary>
        /// Gets all scopes that contain variables.
        /// </summary>
        internal ImmutableArray<Cci.LocalScope> GetAllScopes() => _scopeManager.GetAllScopesWithLocals();

        /// <summary>
        /// Gets all scopes that contain variables.
        /// </summary>
        internal ImmutableArray<Cci.StateMachineHoistedLocalScope> GetHoistedLocalScopes()
        {
            // The hoisted local scopes are enumerated and returned here, sorted by variable "index",
            // which is a number appearing after the "__" at the end of the field's name.  The index should
            // correspond to the location in the returned sequence.  Indices are 1-based, which means that the
            // "first" element at the resulting list (i.e. index 0) corresponds to the variable whose name ends
            // with "__1".
            return _scopeManager.GetHoistedLocalScopes();
        }

        internal void FreeBasicBlocks()
        {
            _scopeManager.FreeBasicBlocks();

            if (this.SeqPointsOpt != null)
            {
                this.SeqPointsOpt.Free();
                this.SeqPointsOpt = null;
            }

            if (_allocatedILMarkers != null)
            {
                _allocatedILMarkers.Free();
                _allocatedILMarkers = null;
            }
        }

        internal ushort MaxStack => (ushort)_emitState.MaxStack;

        /// <summary>
        /// IL opcodes emitted by this builder.
        /// This includes branch instructions that end blocks except if they are fall-through NOPs.
        /// 
        /// This count allows compilers to see if emitting a particular statement/expression 
        /// actually produced any instructions.
        /// 
        /// Example: a label will not result in any code so when emitting debugging information 
        ///          an extra NOP may be needed if we want to decorate the label with sequence point. 
        /// </summary>
        internal int InstructionsEmitted => _emitState.InstructionsEmitted;

        /// <summary>
        /// Marks blocks that are reachable.
        /// </summary>
        private void MarkReachableBlocks()
        {
            Debug.Assert(AllBlocks(block => (block.Reachability == Reachability.NotReachable)));

            ArrayBuilder<BasicBlock> reachableBlocks = ArrayBuilder<BasicBlock>.GetInstance();
            MarkReachableFrom(reachableBlocks, leaderBlock);

            while (reachableBlocks.Count != 0)
            {
                MarkReachableFrom(reachableBlocks, reachableBlocks.Pop());
            }
            reachableBlocks.Free();
        }

        private static void PushReachableBlockToProcess(ArrayBuilder<BasicBlock> reachableBlocks, BasicBlock block)
        {
            if (block.Reachability == Reachability.NotReachable)
            {
                reachableBlocks.Push(block);
            }
        }

        /// <summary>
        /// Marks blocks that are recursively reachable from the given block.
        /// </summary>
        private static void MarkReachableFrom(ArrayBuilder<BasicBlock> reachableBlocks, BasicBlock block)
        {
        tryAgain:

            if (block != null && block.Reachability == Reachability.NotReachable)
            {
                block.Reachability = Reachability.Reachable;

                var branchCode = block.BranchCode;
                if (branchCode == ILOpCode.Nop && block.Type == BlockType.Normal)
                {
                    block = block.NextBlock;
                    goto tryAgain;
                }

                if (branchCode.CanFallThrough())
                {
                    PushReachableBlockToProcess(reachableBlocks, block.NextBlock);
                }
                else
                {
                    // If this block is an "endfinally" block, then clear
                    // the reachability of the following special block.
                    if (branchCode == ILOpCode.Endfinally)
                    {
                        var enclosingFinally = block.EnclosingHandler;
                        enclosingFinally?.UnblockFinally();
                    }
                }

                switch (block.Type)
                {
                    case BlockType.Switch:
                        MarkReachableFromSwitch(reachableBlocks, block);
                        break;

                    case BlockType.Try:
                        MarkReachableFromTry(reachableBlocks, block);
                        break;

                    default:
                        MarkReachableFromBranch(reachableBlocks, block);
                        break;
                }
            }
        }

        private static void MarkReachableFromBranch(ArrayBuilder<BasicBlock> reachableBlocks, BasicBlock block)
        {
            var branchBlock = block.BranchBlock;

            if (branchBlock != null)
            {
                // if branch is blocked by a finally, then should branch to corresponding 
                // BlockedBranchDestination instead. Original label may not be reachable.
                // if there are no blocking finally blocks, then BlockedBranchDestination returns null
                // and we just visit the target.
                var blockedDest = BlockedBranchDestination(block, branchBlock);
                if (blockedDest == null)
                {
                    PushReachableBlockToProcess(reachableBlocks, branchBlock);
                }
                else
                {
                    // just redirect. No need to visit blocking destination
                    // it is a single block infinite loop.
                    RedirectBranchToBlockedDestination(block, blockedDest);
                }
            }
        }

        private static void RedirectBranchToBlockedDestination(BasicBlock block, object blockedDest)
        {
            // replace destination, keep opcode
            block.SetBranch(blockedDest, block.BranchCode);

            // blockedDest is no longer "unreachable".
            // Something branches to it so it should be at least BlockedByFinally.
            var newBranchBlock = block.BranchBlock;
            if (newBranchBlock.Reachability == Reachability.NotReachable)
            {
                block.BranchBlock.Reachability = Reachability.BlockedByFinally;
            }
        }

        // if branch is blocked by a nonterminating finally,
        // returns label for a landing block used as a target of blocked branches
        // Otherwise returns null
        private static object BlockedBranchDestination(BasicBlock src, BasicBlock dest)
        {
            var srcHandler = src.EnclosingHandler;

            // most common case - we are not in an exception handler.
            if (srcHandler == null)
            {
                return null;
            }

            return BlockedBranchDestinationSlow(dest.EnclosingHandler, srcHandler);
        }

        private static object BlockedBranchDestinationSlow(ExceptionHandlerScope destHandler, ExceptionHandlerScope srcHandler)
        {
            ScopeInfo destHandlerScope = null;
            if (destHandler != null)
            {
                destHandlerScope = destHandler.ContainingExceptionScope;
            }

            // go from the source out until no longer crossing any finally blocks
            // between source and destination
            // if any finally blocks found in the process, check if they are blocking
            while (srcHandler != destHandler)
            {
                // branches within same ContainingExceptionScope do not go through finally.
                // only checking that source and destination are within the same handler would miss the case
                // of branching from catch into corresponding try.
                if (srcHandler.ContainingExceptionScope == destHandlerScope)
                {
                    break;
                }

                if (srcHandler.Type == ScopeType.Try)
                {
                    var handlerBlock = srcHandler.LeaderBlock.NextExceptionHandler;
                    if (handlerBlock.Type == BlockType.Finally)
                    {
                        var blockedDest = handlerBlock.EnclosingHandler.BlockedByFinallyDestination;
                        if (blockedDest != null)
                        {
                            return blockedDest;
                        }
                    }
                }

                srcHandler = srcHandler.ContainingExceptionScope.ContainingHandler;
            }

            return null;
        }

        private static void MarkReachableFromTry(ArrayBuilder<BasicBlock> reachableBlocks, BasicBlock block)
        {
            // Since the try block is reachable, associated
            // catch and finally blocks are also reachable.
            var handlerBlock = ((ExceptionHandlerLeaderBlock)block).NextExceptionHandler;
            Debug.Assert(handlerBlock != null);

            // Subsequent handlers are either one or more catch
            // blocks or a single finally block, but not both.
            if (handlerBlock.Type == BlockType.Finally)
            {
                Debug.Assert(handlerBlock.NextExceptionHandler == null);

                // Walk the finally block before walking the try block since we
                // need to determine whether the finally makes any code after
                // the try/finally unreachable (if the finally throws an exception).
                // (Note, the try block is walked in the outer loop.)
                if (handlerBlock.Reachability != Reachability.Reachable)
                {
                    // we have not processed Finally yet, reschedule block for processing
                    // but process the handler first.
                    block.Reachability = Reachability.NotReachable;
                    PushReachableBlockToProcess(reachableBlocks, block);
                    PushReachableBlockToProcess(reachableBlocks, handlerBlock);
                    return;
                }
            }
            else
            {
                // The order the try and handler blocks are walked is not important.
                // Here, we push the handler blocks, then the try block.
                while (handlerBlock != null)
                {
                    Debug.Assert(handlerBlock.Type == BlockType.Catch || handlerBlock.Type == BlockType.Fault || handlerBlock.Type == BlockType.Filter);
                    PushReachableBlockToProcess(reachableBlocks, handlerBlock);
                    handlerBlock = handlerBlock.NextExceptionHandler;
                }
            }

            MarkReachableFromBranch(reachableBlocks, block);
        }

        private static void MarkReachableFromSwitch(ArrayBuilder<BasicBlock> reachableBlocks, BasicBlock block)
        {
            var switchBlock = (SwitchBlock)block;
            var blockBuilder = ArrayBuilder<BasicBlock>.GetInstance();
            switchBlock.GetBranchBlocks(blockBuilder);

            foreach (var targetBlock in blockBuilder)
            {
                PushReachableBlockToProcess(reachableBlocks, targetBlock);
            }

            blockBuilder.Free();
        }

        /// <summary>
        /// If a label points to a block that does nothing other than passing to block X,
        /// replaces target label's block with block X.
        /// </summary>
        /// 
        private bool OptimizeLabels()
        {
            // since unconditional labels can move outside try blocks, but conditional cannot,
            // forwarding unconditional labels via leaving before forwarding unconditional ones
            // may yield slightly different results and we want results to be deterministic.
            return ForwardLabelsNoLeaving() | ForwardLabelsAllowLeaving();
        }

        private bool ForwardLabelsNoLeaving()
        {
            bool madeChanges = false;

            var labels = _labelInfos.Keys;

            bool done;
            do
            {
                done = true;

                foreach (object label in labels)
                {
                    var labelInfo = _labelInfos[label];
                    var targetBlock = labelInfo.bb;

                    Debug.Assert(!IsSpecialEndHandlerBlock(targetBlock));

                    if (targetBlock.HasNoRegularInstructions)
                    {
                        BasicBlock targetsTarget = null;
                        switch (targetBlock.BranchCode)
                        {
                            case ILOpCode.Br:
                                targetsTarget = targetBlock.BranchBlock;
                                break;

                            case ILOpCode.Nop:
                                targetsTarget = targetBlock.NextBlock;
                                break;
                        }

                        if ((targetsTarget != null) && (targetsTarget != targetBlock))
                        {
                            var currentHandler = targetBlock.EnclosingHandler;
                            var newHandler = targetsTarget.EnclosingHandler;

                            // forward the label if can be done without leaving current handler
                            if (currentHandler == newHandler)
                            {
                                _labelInfos[label] = labelInfo.WithNewTarget(targetsTarget);
                                madeChanges = true;

                                // since we modified at least one label we want to try again.
                                done = false;
                            }
                        }
                    }
                }
            } while (!done);

            return madeChanges;
        }

        private bool ForwardLabelsAllowLeaving()
        {
            bool madeChanges = false;

            var labels = _labelInfos.Keys;

            bool done;
            do
            {
                done = true;

                foreach (object label in labels)
                {
                    var labelInfo = _labelInfos[label];
                    if (labelInfo.targetOfConditionalBranches)
                    {
                        // only unconditional labels can be forwarded as a leave
                        continue;
                    }

                    var targetBlock = labelInfo.bb;

                    Debug.Assert(!IsSpecialEndHandlerBlock(targetBlock));

                    if (targetBlock.HasNoRegularInstructions)
                    {
                        BasicBlock targetsTarget = null;
                        switch (targetBlock.BranchCode)
                        {
                            case ILOpCode.Br:
                                targetsTarget = targetBlock.BranchBlock;
                                break;

                            case ILOpCode.Nop:
                                targetsTarget = targetBlock.NextBlock;
                                break;
                        }

                        if ((targetsTarget != null) && (targetsTarget != targetBlock))
                        {
                            var currentHandler = targetBlock.EnclosingHandler;
                            var newHandler = targetsTarget.EnclosingHandler;

                            // can skip the jump if it is in the same try scope
                            if (CanMoveLabelToAnotherHandler(currentHandler, newHandler))
                            {
                                _labelInfos[label] = labelInfo.WithNewTarget(targetsTarget);
                                madeChanges = true;

                                // since we modified at least one label we want to try again.
                                done = false;
                            }
                        }
                    }
                }
            } while (!done);

            return madeChanges;
        }

        private static bool CanMoveLabelToAnotherHandler(ExceptionHandlerScope currentHandler,
                                                 ExceptionHandlerScope newHandler)
        {
            // Generally, assuming already valid code that contains "LABEL1: goto LABEL2" 
            // we can substitute LABEL1 for LABEL2 so that the branches go directly to 
            // the final destination.
            // Technically we can allow "moving" a label to any scope that contains the current one
            // However we should be careful with the cases when current label is protected by a 
            // catch clause.
            // 
            // [COMPAT]
            // If we move a label out of catch-protected try clause, we could be forcing JIT to inject 
            // it back since, in the case of Thread.Abort, the re-throwing of the exception needs 
            // to happen around this leave instruction which we would be removing.
            // In addition to just extra work on the JIT side, handling of this case appears to be
            // very delicate and there are known cases where JITs did not handle this particular 
            // scenario correctly resulting in various violations of Thread.Abort behavior.
            // We cannot rely on these JIT issues being fixed in the end user environment.
            //
            // Considering that we are only winning a single LEAVE here, it seems reasonable to 
            // just disallow labels to move outside of a catch-protected regions.

            // no handler means outermost scope (method level)
            if (newHandler == null && currentHandler.ContainingExceptionScope.FinallyOnly())
            {
                return true;
            }

            // check if the target handler contains current handler.
            do
            {
                if (currentHandler == newHandler)
                {
                    return true;
                }

                var containerScope = currentHandler.ContainingExceptionScope;
                if (!containerScope.FinallyOnly())
                {
                    // this may move the label outside of catch-protected region
                    // we will disallow that.
                    return false;
                }

                currentHandler = containerScope.ContainingHandler;
            } while (currentHandler != null);

            return false;
        }

        /// <summary>
        /// Drops blocks that are not reachable
        /// Returns true if any blocks were dropped
        /// </summary>
        private bool DropUnreachableBlocks()
        {
            bool dropped = false;

            //sweep unreachable
            var current = leaderBlock;
            while (current.NextBlock != null)
            {
                if (current.NextBlock.Reachability == Reachability.NotReachable)
                {
                    current.NextBlock = current.NextBlock.NextBlock;
                    dropped = true;
                }
                else
                {
                    current = current.NextBlock;
                }
            }

            // All blocks should be reachable or, if not reachable, then blocked by finally.
            Debug.Assert(AllBlocks(block => (block.Reachability == Reachability.Reachable) || (block.Reachability == Reachability.BlockedByFinally)));

            return dropped;
        }

        /// <summary>
        /// Marks all blocks unreachable.
        /// </summary>
        private void MarkAllBlocksUnreachable()
        {
            //sweep unreachable
            var current = leaderBlock;
            while (current != null)
            {
                current.Reachability = Reachability.NotReachable;
                current = current.NextBlock;
            }
        }

        private void ComputeOffsets()
        {
            var current = leaderBlock;
            while (current.NextBlock != null)
            {
                current.NextBlock.Start = current.Start + current.TotalSize;
                current = current.NextBlock;
            }
        }

        /// <summary>
        /// Rewrite any block marked as BlockedByFinally as an "infinite loop".
        /// </summary>
        /// <remarks>
        /// Matches the code generated by the native compiler in
        /// ILGENREC::AdjustBlockedLeaveTargets.
        /// </remarks>
        private void RewriteSpecialBlocks()
        {
            var current = leaderBlock;

            while (current != null)
            {
                // The only blocks that should be marked as BlockedByFinally
                // are the special blocks inserted at the end of exception handlers.
                Debug.Assert(current.Reachability != Reachability.BlockedByFinally ||
                    IsSpecialEndHandlerBlock(current));

                if (IsSpecialEndHandlerBlock(current))
                {
                    if (current.Reachability == Reachability.BlockedByFinally)
                    {
                        // BranchLabel points to the same block, so the BranchCode
                        // is changed from Nop to Br_s.
                        current.SetBranchCode(ILOpCode.Br_s);
                    }
                    else
                    {
                        // special block becomes a true nop
                        current.SetBranch(null, ILOpCode.Nop);
                    }
                }
                current = current.NextBlock;
            }

            // Now that the branch code has changed, the block is no longer special.
            Debug.Assert(AllBlocks(block => !IsSpecialEndHandlerBlock(block)));
        }

        /// <summary>
        /// Returns true if the block has the signature of the special
        /// labeled block that follows a complete try/catch or try/finally.
        /// </summary>
        private static bool IsSpecialEndHandlerBlock(BasicBlock block)
        {
            if ((block.BranchCode != ILOpCode.Nop) || (block.BranchLabel == null))
            {
                return false;
            }

            // Should branch back to itself.
            Debug.Assert(block.BranchBlock == block);
            return true;
        }

        private void RewriteBranchesAcrossExceptionHandlers()
        {
            var current = leaderBlock;
            while (current != null)
            {
                current.RewriteBranchesAcrossExceptionHandlers();
                current = current.NextBlock;
            }
        }

        /// <summary>
        /// Returns true if any branches were optimized (that does not include shortening)
        /// We need this because optimizing a branch may result in unreachable code that needs to be eliminated.
        /// 
        /// === Example:
        /// 
        /// x = 1;
        /// 
        /// if (blah)
        /// {
        ///     global = 1;
        /// }
        /// else
        /// {
        ///     throw null;
        /// }
        /// 
        /// return x;
        /// 
        /// === rewrites into
        /// 
        /// push 1;
        /// 
        /// if (blah)
        /// {
        ///     global = 1;
        ///     ret; 
        /// }
        /// else
        /// {
        ///     throw null;
        /// }
        /// 
        /// // this ret unreachable now! 
        /// // even worse - empty stack is assumed thus the ret is illegal.
        /// ret;    
        /// 
        /// </summary>
        private bool ComputeOffsetsAndAdjustBranches()
        {
            ComputeOffsets();

            bool branchesOptimized = false;
            int delta;
            do
            {
                delta = 0;
                var current = leaderBlock;
                while (current != null)
                {
                    current.AdjustForDelta(delta);

                    if (_optimizations == OptimizationLevel.Release)
                    {
                        branchesOptimized |= current.OptimizeBranches(ref delta);
                    }

                    current.ShortenBranches(ref delta);
                    current = current.NextBlock;
                }
            } while (delta < 0); // shortening some branches may enable more branches for shortening.

            return branchesOptimized;
        }

        private void RealizeBlocks()
        {
            // drop dead code.
            // We do not want to deal with unreachable code even when not optimizing.
            // sometimes dead code may have subtle verification violations
            // for example illegal fall-through in unreachable code is still illegal, 
            // but compiler will not enforce returning from dead code.
            // it is easier to just remove dead code than make sure it is all valid
            MarkReachableBlocks();
            RewriteSpecialBlocks();
            DropUnreachableBlocks();

            if (_optimizations == OptimizationLevel.Release && OptimizeLabels())
            {
                // redo unreachable code elimination if some labels were optimized
                // as that could result in more dead code. 
                MarkAllBlocksUnreachable();
                MarkReachableBlocks();
                DropUnreachableBlocks();
            }

            // some gotos must become leaves
            RewriteBranchesAcrossExceptionHandlers();

            // now we can compute block offsets and adjust branches
            while (ComputeOffsetsAndAdjustBranches())
            {
                // if branches were optimized, we may have more unreachable code
                // redo unreachable code elimination and if anything was dropped redo adjusting.
                MarkAllBlocksUnreachable();
                MarkReachableBlocks();
                if (!DropUnreachableBlocks())
                {
                    // nothing was dropped, we are done adjusting
                    break;
                }
            }

            // Now linearize everything with computed offsets.
            var writer = Cci.PooledBlobBuilder.GetInstance();

            for (var block = leaderBlock; block != null; block = block.NextBlock)
            {
                // If the block has any IL markers, we can calculate their real IL offsets now
                int blockFirstMarker = block.FirstILMarker;
                if (blockFirstMarker >= 0)
                {
                    int blockLastMarker = block.LastILMarker;
                    Debug.Assert(blockLastMarker >= blockFirstMarker);
                    for (int i = blockFirstMarker; i <= blockLastMarker; i++)
                    {
                        int blockOffset = _allocatedILMarkers[i].BlockOffset;
                        int absoluteOffset = writer.Position + blockOffset;
                        _allocatedILMarkers[i] = new ILMarker() { BlockOffset = blockOffset, AbsoluteOffset = absoluteOffset };
                    }
                }

                block.RegularInstructions?.WriteContentTo(writer);

                switch (block.BranchCode)
                {
                    case ILOpCode.Nop:
                        break;

                    case ILOpCode.Switch:
                        // switch (N, t1, t2... tN)
                        //  IL ==> ILOpCode.Switch < unsigned int32 > < int32 >... < int32 >

                        WriteOpCode(writer, ILOpCode.Switch);

                        var switchBlock = (SwitchBlock)block;
                        writer.WriteUInt32(switchBlock.BranchesCount);

                        int switchBlockEnd = switchBlock.Start + switchBlock.TotalSize;

                        var blockBuilder = ArrayBuilder<BasicBlock>.GetInstance();
                        switchBlock.GetBranchBlocks(blockBuilder);

                        foreach (var branchBlock in blockBuilder)
                        {
                            writer.WriteInt32(branchBlock.Start - switchBlockEnd);
                        }

                        blockBuilder.Free();

                        break;

                    default:
                        WriteOpCode(writer, block.BranchCode);

                        if (block.BranchLabel != null)
                        {
                            int target = block.BranchBlock.Start;
                            int curBlockEnd = block.Start + block.TotalSize;
                            int offset = target - curBlockEnd;

                            if (block.BranchCode.BranchOperandSize() == 1)
                            {
                                sbyte btOffset = (sbyte)offset;
                                Debug.Assert(btOffset == offset);
                                writer.WriteSByte(btOffset);
                            }
                            else
                            {
                                writer.WriteInt32(offset);
                            }
                        }

                        break;
                }
            }

            this.RealizedIL = writer.ToImmutableArray();
            writer.Free();

            RealizeSequencePoints();

            this.RealizedExceptionHandlers = _scopeManager.GetExceptionHandlerRegions();
        }

        private void RealizeSequencePoints()
        {
            if (this.SeqPointsOpt != null)
            {
                // we keep track of the latest sequence point location to make sure 
                // we don't emit multiple sequence points for the same location
                int lastOffset = -1;

                ArrayBuilder<RawSequencePoint> seqPoints = ArrayBuilder<RawSequencePoint>.GetInstance();
                foreach (var seqPoint in this.SeqPointsOpt)
                {
                    int offset = this.GetILOffsetFromMarker(seqPoint.ILMarker);
                    if (offset >= 0)
                    {
                        // valid IL offset
                        if (lastOffset != offset)
                        {
                            Debug.Assert(lastOffset < offset);
                            // if there are any sequence points, there must
                            // be a sequence point at offset 0.
                            Debug.Assert((lastOffset >= 0) || (offset == 0));
                            // the first sequence point on tree/offset location
                            lastOffset = offset;
                            seqPoints.Add(seqPoint);
                        }
                        else
                        {
                            // override previous sequence point at the same location
                            seqPoints[seqPoints.Count - 1] = seqPoint;
                        }
                    }
                }

                if (seqPoints.Count > 0)
                {
                    this.RealizedSequencePoints = SequencePointList.Create(seqPoints, this);
                }

                seqPoints.Free();
            }
        }

        /// <summary>
        /// Define a sequence point with the given syntax tree and span within it.
        /// </summary>
        internal void DefineSequencePoint(SyntaxTree syntaxTree, TextSpan span)
        {
            var curBlock = GetCurrentBlock();
            _lastSeqPointTree = syntaxTree;

            if (this.SeqPointsOpt == null)
            {
                this.SeqPointsOpt = ArrayBuilder<RawSequencePoint>.GetInstance();
            }

            // Add an initial hidden sequence point if needed.
            if (_initialHiddenSequencePointMarker >= 0)
            {
                Debug.Assert(this.SeqPointsOpt.Count == 0);
                this.SeqPointsOpt.Add(new RawSequencePoint(syntaxTree, _initialHiddenSequencePointMarker, RawSequencePoint.HiddenSequencePointSpan));
                _initialHiddenSequencePointMarker = -1;
            }

            this.SeqPointsOpt.Add(new RawSequencePoint(syntaxTree, this.AllocateILMarker(), span));
        }

        private int _initialHiddenSequencePointMarker = -1;

        /// <summary>
        /// Defines a hidden sequence point.
        /// The effect of this is that debugger will not associate following code 
        /// with any source (until it sees a lexically following sequence point).
        /// 
        /// This is used for synthetic code that is reachable through labels.
        /// 
        /// If such code is not separated from previous sequence point by the means of a hidden sequence point
        /// It looks as a part of the statement that previous sequence point specifies.
        /// As a result, when user steps through the code and goes through a jump to such label,
        /// it will appear as if the jump landed at the beginning of the previous statement.
        /// 
        /// NOTE: Also inserted as the first statement of a method that would not otherwise have a leading
        /// sequence point so that step-into will find the method body.
        /// </summary>
        internal void DefineHiddenSequencePoint()
        {
            var lastDebugDocument = _lastSeqPointTree;

            // if no document is known for this code, do not bother emitting a sequence point
            // CCI will not emit it anyways.
            if (lastDebugDocument != null)
            {
                this.DefineSequencePoint(lastDebugDocument, RawSequencePoint.HiddenSequencePointSpan);
            }
        }

        /// <summary>
        /// Define a hidden sequence point at the first statement of
        /// the method so that step-into will find the method body.
        /// </summary>
        internal void DefineInitialHiddenSequencePoint()
        {
            Debug.Assert(_initialHiddenSequencePointMarker < 0);
            // Create a marker for the sequence point. The actual sequence point
            // is created when the first non-hidden sequence is created since we
            // won't know the syntax tree before then.
            _initialHiddenSequencePointMarker = this.AllocateILMarker();
            Debug.Assert(_initialHiddenSequencePointMarker == 0);
        }

        /// <summary>
        /// This is called when starting emitting a method for which there is some source.
        /// It is done in case the first sequence point is a hidden point.
        /// Even though hidden points do not have syntax, they need to associate with some document.
        /// </summary>
        internal void SetInitialDebugDocument(SyntaxTree initialSequencePointTree)
        {
            _lastSeqPointTree = initialSequencePointTree;
        }

        [Conditional("DEBUG")]
        internal void AssertStackEmpty()
        {
            Debug.Assert(_emitState.CurStack == 0);
        }

        // true if there may have been a label generated with no subsequent code
        internal bool IsJustPastLabel()
        {
            Debug.Assert(_emitState.InstructionsEmitted >= _instructionCountAtLastLabel);
            return _emitState.InstructionsEmitted == _instructionCountAtLastLabel;
        }

        internal void OpenLocalScope(ScopeType scopeType = ScopeType.Variable, Cci.ITypeReference exceptionType = null)
        {
            if (scopeType == ScopeType.TryCatchFinally && IsJustPastLabel())
            {
                DefineHiddenSequencePoint();
                EmitOpCode(ILOpCode.Nop);
            }

            if (scopeType == ScopeType.Finally)
            {
                // WORKAROUND:
                // This is a workaround to an unexpected consequence of a CLR update that causes try nested in finally not verify
                // if there is no code before try. ( DevDiv: 563799 )
                // If we will treat finally as a label, the code above will force a nop before try starts.
                _instructionCountAtLastLabel = _emitState.InstructionsEmitted;
            }

            EndBlock();  //blocks should not cross scope boundaries.
            var scope = _scopeManager.OpenScope(scopeType, exceptionType);

            // Exception handler scopes must have a leader block, even
            // if the exception handler is empty, and created before any
            // other block (before nested scope blocks in particular).
            switch (scopeType)
            {
                case ScopeType.Try:
                    Debug.Assert(!_pendingBlockCreate);
                    _pendingBlockCreate = true;
                    break;

                case ScopeType.Catch:
                case ScopeType.Filter:
                case ScopeType.Finally:
                case ScopeType.Fault:
                    Debug.Assert(!_pendingBlockCreate);
                    _pendingBlockCreate = true;

                    // this is the actual start of the handler.
                    // since it is reachable by an implicit jump (via exception handling) 
                    // we need to put a hidden point to ensure that debugger does not associate
                    // this location with some previous sequence point
                    DefineHiddenSequencePoint();

                    break;
                case ScopeType.Variable:
                case ScopeType.TryCatchFinally:
                case ScopeType.StateMachineVariable:
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(scopeType);
            }
        }

        internal bool PossiblyDefinedOutsideOfTry(LocalDefinition local)
            => _scopeManager.PossiblyDefinedOutsideOfTry(local);

        /// <summary>
        /// Marks the end of filter condition and start of the actual filter handler.
        /// </summary>
        internal void MarkFilterConditionEnd()
        {
            _scopeManager.FinishFilterCondition(this);

            // this is the actual start of the handler.
            // since it is reachable by an implicit jump (via exception handling) 
            // we need to put a hidden point to ensure that debugger does not associate
            // this location with some previous sequence point 
            DefineHiddenSequencePoint();
        }

        internal void CloseLocalScope()
        {
            _scopeManager.ClosingScope(this);
            EndBlock();  //blocks should not cross scope boundaries.
            _scopeManager.CloseScope(this);
        }

        internal void OpenStateMachineScope()
        {
            OpenLocalScope(ScopeType.StateMachineVariable);
        }

        internal void DefineUserDefinedStateMachineHoistedLocal(int slotIndex)
        {
            // Add user-defined local into the current scope.
            // We emit custom debug information for these locals that is used by the EE to reconstruct their scopes.
            _scopeManager.AddUserHoistedLocal(slotIndex);
        }

        internal void CloseStateMachineScope()
        {
            _scopeManager.ClosingScope(this);
            EndBlock(); // blocks should not cross scope boundaries.
            _scopeManager.CloseScope(this);
        }

        /// <summary>
        /// Puts local variable into current scope.
        /// </summary>
        internal void AddLocalToScope(LocalDefinition local)
        {
            HasDynamicLocal |= local.IsDynamic;
            _scopeManager.AddLocal(local);
        }

        /// <summary>
        /// Puts local constant into current scope.
        /// </summary>
        internal void AddLocalConstantToScope(LocalConstantDefinition localConstant)
        {
            HasDynamicLocal |= localConstant.IsDynamic;
            _scopeManager.AddLocalConstant(localConstant);
        }

        internal bool HasDynamicLocal { get; private set; }

        // We have no mechanism for tracking the remapping of tokens when metadata is written.
        // In order to visualize the realized IL for testing, we need to be able to capture
        // a snapshot of the builder with the original (fake) token values.  
        internal ILBuilder GetSnapshot()
        {
            var snapshot = (ILBuilder)this.MemberwiseClone();
            snapshot.RealizedIL = RealizedIL;
            return snapshot;
        }

        private bool AllBlocks(Func<BasicBlock, bool> predicate)
        {
            var current = leaderBlock;
            while (current != null)
            {
                if (!predicate(current))
                {
                    return false;
                }
                current = current.NextBlock;
            }
            return true;
        }

        internal int AllocateILMarker()
        {
            Debug.Assert(this.RealizedIL.IsDefault, "Too late to allocate a new IL marker");
            if (_allocatedILMarkers == null)
            {
                _allocatedILMarkers = ArrayBuilder<ILMarker>.GetInstance();
            }

            BasicBlock curBlock = GetCurrentBlock();
            Debug.Assert(curBlock != null);

            int marker = _allocatedILMarkers.Count;
            curBlock.AddILMarker(marker);

            _allocatedILMarkers.Add(
                new ILMarker()
                {
                    BlockOffset = (int)curBlock.RegularInstructionsLength,
                    AbsoluteOffset = -1
                }
            );

            return marker;
        }

        public int GetILOffsetFromMarker(int ilMarker)
        {
            Debug.Assert(!RealizedIL.IsDefault, "Builder must be realized to perform this operation");
            Debug.Assert(_allocatedILMarkers != null, "There are not markers in this builder");
            Debug.Assert(ilMarker >= 0 && ilMarker < _allocatedILMarkers.Count, "Wrong builder?");
            return _allocatedILMarkers[ilMarker].AbsoluteOffset;
        }

        private string GetDebuggerDisplay()
        {
#if DEBUG
            var visType = Type.GetType("Roslyn.Test.Utilities.ILBuilderVisualizer, Roslyn.Test.Utilities", false);
            if (visType != null)
            {
                var method = visType.GetTypeInfo().GetDeclaredMethod("ILBuilderToString");
                return (string)method.Invoke(null, new object[] { this, null, null });
            }
#endif

            return "";
        }

        private struct ILMarker
        {
            public int BlockOffset;
            public int AbsoluteOffset;
        }
    }
}
