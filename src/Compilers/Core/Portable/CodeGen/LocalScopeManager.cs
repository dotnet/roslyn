// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using Cci = Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal partial class ILBuilder
    {
        private sealed class LocalScopeManager
        {
            private readonly LocalScopeInfo _rootScope;
            private readonly Stack<ScopeInfo> _scopes;
            private ExceptionHandlerScope _enclosingExceptionHandler;

            internal LocalScopeManager()
            {
                _rootScope = new LocalScopeInfo();
                _scopes = new Stack<ScopeInfo>(1);
                _scopes.Push(_rootScope);
            }

            private ScopeInfo CurrentScope => _scopes.Peek();

            internal ScopeInfo OpenScope(ScopeType scopeType, Cci.ITypeReference exceptionType)
            {
                var scope = CurrentScope.OpenScope(scopeType, exceptionType, _enclosingExceptionHandler);
                _scopes.Push(scope);

                if (scope.IsExceptionHandler)
                {
                    _enclosingExceptionHandler = (ExceptionHandlerScope)scope;
                }

                Debug.Assert(_enclosingExceptionHandler == GetEnclosingExceptionHandler());
                return scope;
            }

            internal void FinishFilterCondition(ILBuilder builder)
            {
                CurrentScope.FinishFilterCondition(builder);
            }

            internal void ClosingScope(ILBuilder builder)
            {
                CurrentScope.ClosingScope(builder);
            }

            internal void CloseScope(ILBuilder builder)
            {
                var scope = _scopes.Pop();
                scope.CloseScope(builder);

                if (scope.IsExceptionHandler)
                {
                    _enclosingExceptionHandler = GetEnclosingExceptionHandler();
                }

                Debug.Assert(_enclosingExceptionHandler == GetEnclosingExceptionHandler());
            }

            internal ExceptionHandlerScope EnclosingExceptionHandler => _enclosingExceptionHandler;

            private ExceptionHandlerScope GetEnclosingExceptionHandler()
            {
                foreach (var scope in _scopes)
                {
                    switch (scope.Type)
                    {
                        case ScopeType.Try:
                        case ScopeType.Catch:
                        case ScopeType.Filter:
                        case ScopeType.Finally:
                        case ScopeType.Fault:
                            return (ExceptionHandlerScope)scope;
                    }
                }
                return null;
            }

            internal BasicBlock CreateBlock(ILBuilder builder)
            {
                var scope = (LocalScopeInfo)CurrentScope;
                return scope.CreateBlock(builder);
            }

            internal SwitchBlock CreateSwitchBlock(ILBuilder builder)
            {
                var scope = (LocalScopeInfo)CurrentScope;
                return scope.CreateSwitchBlock(builder);
            }

            internal void AddLocal(LocalDefinition variable)
            {
                var scope = (LocalScopeInfo)CurrentScope;
                scope.AddLocal(variable);
            }

            internal void AddLocalConstant(LocalConstantDefinition constant)
            {
                var scope = (LocalScopeInfo)CurrentScope;
                scope.AddLocalConstant(constant);
            }

            /// <summary>
            /// Gets all scopes that contain variables.
            /// </summary>
            internal ImmutableArray<Cci.LocalScope> GetAllScopesWithLocals()
            {
                var result = ArrayBuilder<Cci.LocalScope>.GetInstance();
                ScopeBounds rootBounds = _rootScope.GetLocalScopes(result);

                int expectedRootScopeLength = rootBounds.End - rootBounds.Begin;

                // Add root scope if it was not already added.
                // we add it even if it does not contain any locals
                if (result.Count > 0 && result[result.Count - 1].Length != expectedRootScopeLength)
                {
                    result.Add(new Cci.LocalScope(
                        0,
                        expectedRootScopeLength,
                        ImmutableArray<Cci.ILocalDefinition>.Empty,
                        ImmutableArray<Cci.ILocalDefinition>.Empty));
                }

                // scopes should be sorted by position and size
                result.Sort(ScopeComparer.Instance);

                return result.ToImmutableAndFree();
            }

            /// <summary>
            /// Returns an ExceptionHandlerRegion for each exception handler clause
            /// beneath the root scope. Each ExceptionHandlerRegion indicates the type
            /// of clause (catch or finally) and the bounds of the try block and clause block.
            /// </summary>
            internal ImmutableArray<Cci.ExceptionHandlerRegion> GetExceptionHandlerRegions()
            {
                var result = ArrayBuilder<Cci.ExceptionHandlerRegion>.GetInstance();
                _rootScope.GetExceptionHandlerRegions(result);
                return result.ToImmutableAndFree();
            }

            internal ImmutableArray<StateMachineHoistedLocalScope> GetHoistedLocalScopes()
            {
                var result = ArrayBuilder<StateMachineHoistedLocalScope>.GetInstance();
                _rootScope.GetHoistedLocalScopes(result);
                return result.ToImmutableAndFree();
            }

            internal void AddUserHoistedLocal(int slotIndex)
            {
                var scope = (LocalScopeInfo)CurrentScope;
                scope.AddUserHoistedLocal(slotIndex);
            }

            internal void FreeBasicBlocks()
            {
                _rootScope.FreeBasicBlocks();
            }

            internal bool PossiblyDefinedOutsideOfTry(LocalDefinition local)
            {
                foreach (var s in _scopes)
                {
                    if (s.ContainsLocal(local))
                    {
                        return false;
                    }

                    if (s.Type == ScopeType.Try)
                    {
                        return true;
                    }
                }

                // not recorded in scopes, could be a temp
                // we cannot tell anything.
                return true;
            }
        }

        /// <summary>
        /// Base class for IL scopes where a scope contains IL blocks and other nested
        /// scopes. A scope may represent a scope for variable declarations, an exception
        /// handler clause, or an entire exception handler (multiple clauses).
        /// </summary>
        internal abstract class ScopeInfo
        {
            public abstract ScopeType Type { get; }

            public virtual ScopeInfo OpenScope(ScopeType scopeType,
                Microsoft.Cci.ITypeReference exceptionType,
                ExceptionHandlerScope currentHandler)
            {
                if (scopeType == ScopeType.TryCatchFinally)
                {
                    return new ExceptionHandlerContainerScope(currentHandler);
                }
                else
                {
                    Debug.Assert(scopeType == ScopeType.Variable || scopeType == ScopeType.StateMachineVariable);
                    return new LocalScopeInfo();
                }
            }

            public virtual void ClosingScope(ILBuilder builder)
            {
            }

            public virtual void CloseScope(ILBuilder builder)
            {
            }

            public virtual void FinishFilterCondition(ILBuilder builder)
            {
                throw ExceptionUtilities.Unreachable();
            }

            public bool IsExceptionHandler
            {
                get
                {
                    switch (this.Type)
                    {
                        case ScopeType.Try:
                        case ScopeType.Catch:
                        case ScopeType.Filter:
                        case ScopeType.Finally:
                        case ScopeType.Fault:
                            return true;
                        default:
                            return false;
                    }
                }
            }

            internal abstract void GetExceptionHandlerRegions(ArrayBuilder<Cci.ExceptionHandlerRegion> regions);

            /// <summary>
            /// Recursively calculates the start and end of the given scope.
            /// Only scopes with locals are actually dumped to the list.
            /// </summary>
            internal abstract ScopeBounds GetLocalScopes(ArrayBuilder<Cci.LocalScope> result);

            protected static ScopeBounds GetLocalScopes<TScopeInfo>(ArrayBuilder<Cci.LocalScope> result, ImmutableArray<TScopeInfo>.Builder scopes)
                where TScopeInfo : ScopeInfo
            {
                Debug.Assert(scopes.Count > 0);

                int begin = int.MaxValue;
                int end = 0;

                foreach (var scope in scopes)
                {
                    ScopeBounds bounds = scope.GetLocalScopes(result);
                    begin = Math.Min(begin, bounds.Begin);
                    end = Math.Max(end, bounds.End);
                }

                return new ScopeBounds(begin, end);
            }

            /// <summary>
            /// Recursively calculates the start and end of the given scope.
            /// Only scopes with locals are actually dumped to the list.
            /// </summary>
            internal abstract ScopeBounds GetHoistedLocalScopes(ArrayBuilder<StateMachineHoistedLocalScope> result);

            protected static ScopeBounds GetHoistedLocalScopes<TScopeInfo>(ArrayBuilder<StateMachineHoistedLocalScope> result, ImmutableArray<TScopeInfo>.Builder scopes)
                where TScopeInfo : ScopeInfo
            {
                Debug.Assert(scopes.Count > 0);

                int begin = int.MaxValue;
                int end = 0;

                foreach (var scope in scopes)
                {
                    ScopeBounds bounds = scope.GetHoistedLocalScopes(result);
                    begin = Math.Min(begin, bounds.Begin);
                    end = Math.Max(end, bounds.End);
                }

                return new ScopeBounds(begin, end);
            }

            /// <summary>
            /// Free any basic blocks owned by this scope or sub-scopes.
            /// </summary>
            public abstract void FreeBasicBlocks();

            internal virtual bool ContainsLocal(LocalDefinition local) => false;
        }

        /// <summary>
        /// Class that collects content of the scope (blocks, nested scopes, variables etc).
        /// There is one for every opened scope.
        /// </summary>
        internal class LocalScopeInfo : ScopeInfo
        {
            private ImmutableArray<LocalDefinition>.Builder _localVariables;
            private ImmutableArray<LocalConstantDefinition>.Builder _localConstants;
            private ImmutableArray<int>.Builder _stateMachineUserHoistedLocalSlotIndices;

            // Nested scopes and blocks are not relevant for PDB. 
            // We need these only to figure scope bounds.
            private ImmutableArray<ScopeInfo>.Builder _nestedScopes;
            protected ImmutableArray<BasicBlock>.Builder Blocks;

            public override ScopeType Type => ScopeType.Variable;

            public override ScopeInfo OpenScope(
                ScopeType scopeType,
                Cci.ITypeReference exceptionType,
                ExceptionHandlerScope currentExceptionHandler)
            {
                var scope = base.OpenScope(scopeType, exceptionType, currentExceptionHandler);
                if (_nestedScopes == null)
                {
                    _nestedScopes = ImmutableArray.CreateBuilder<ScopeInfo>(1);
                }
                _nestedScopes.Add(scope);
                return scope;
            }

            internal void AddLocal(LocalDefinition variable)
            {
                if (_localVariables == null)
                {
                    _localVariables = ImmutableArray.CreateBuilder<LocalDefinition>(1);
                }

                Debug.Assert(variable.Name != null);

                _localVariables.Add(variable);
            }

            internal void AddLocalConstant(LocalConstantDefinition constant)
            {
                if (_localConstants == null)
                {
                    _localConstants = ImmutableArray.CreateBuilder<LocalConstantDefinition>(1);
                }

                Debug.Assert(constant.Name != null);

                _localConstants.Add(constant);
            }

            internal void AddUserHoistedLocal(int slotIndex)
            {
                if (_stateMachineUserHoistedLocalSlotIndices == null)
                {
                    _stateMachineUserHoistedLocalSlotIndices = ImmutableArray.CreateBuilder<int>(1);
                }

                Debug.Assert(slotIndex >= 0);
                _stateMachineUserHoistedLocalSlotIndices.Add(slotIndex);
            }

            internal override bool ContainsLocal(LocalDefinition local)
            {
                var locals = _localVariables;
                return locals != null && locals.Contains(local);
            }

            public virtual BasicBlock CreateBlock(ILBuilder builder)
            {
                var enclosingHandler = builder.EnclosingExceptionHandler;
                var block = enclosingHandler == null ?
                    AllocatePooledBlock(builder) :
                    new BasicBlockWithHandlerScope(builder, enclosingHandler);

                AddBlock(block);
                return block;
            }

            private static BasicBlock AllocatePooledBlock(ILBuilder builder)
            {
                var block = BasicBlock.Pool.Allocate();
                block.Initialize(builder);
                return block;
            }

            public SwitchBlock CreateSwitchBlock(ILBuilder builder)
            {
                var block = new SwitchBlock(builder, builder.EnclosingExceptionHandler);
                AddBlock(block);
                return block;
            }

            protected void AddBlock(BasicBlock block)
            {
                if (Blocks == null)
                {
                    Blocks = ImmutableArray.CreateBuilder<BasicBlock>(4);
                }

                Blocks.Add(block);
            }

            internal override void GetExceptionHandlerRegions(ArrayBuilder<Cci.ExceptionHandlerRegion> regions)
            {
                if (_nestedScopes != null)
                {
                    for (int i = 0, cnt = _nestedScopes.Count; i < cnt; i++)
                    {
                        _nestedScopes[i].GetExceptionHandlerRegions(regions);
                    }
                }
            }

            internal override ScopeBounds GetLocalScopes(ArrayBuilder<Cci.LocalScope> result)
            {
                int begin = int.MaxValue;
                int end = 0;

                // It may seem overkill to scan all blocks, 
                // but blocks may be reordered so we cannot be sure which ones are first/last.
                if (Blocks != null)
                {
                    for (int i = 0; i < Blocks.Count; i++)
                    {
                        var block = Blocks[i];

                        if (block.Reachability != Reachability.NotReachable)
                        {
                            begin = Math.Min(begin, block.Start);
                            end = Math.Max(end, block.Start + block.TotalSize);
                        }
                    }
                }

                // if there are nested scopes, dump them too
                // also may need to adjust current scope bounds.
                if (_nestedScopes != null)
                {
                    ScopeBounds nestedBounds = GetLocalScopes(result, _nestedScopes);
                    begin = Math.Min(begin, nestedBounds.Begin);
                    end = Math.Max(end, nestedBounds.End);
                }

                // we are not interested in scopes with no variables or no code in them.
                if ((_localVariables != null || _localConstants != null) && end > begin)
                {
                    var newScope = new Cci.LocalScope(
                        begin,
                        end,
                        _localConstants.AsImmutableOrEmpty<Cci.ILocalDefinition>(),
                        _localVariables.AsImmutableOrEmpty<Cci.ILocalDefinition>());

                    result.Add(newScope);
                }

                return new ScopeBounds(begin, end);
            }

            internal override ScopeBounds GetHoistedLocalScopes(ArrayBuilder<StateMachineHoistedLocalScope> result)
            {
                int begin = int.MaxValue;
                int end = 0;

                // It may seem overkill to scan all blocks, 
                // but blocks may be reordered so we cannot be sure which ones are first/last.
                if (Blocks != null)
                {
                    for (int i = 0; i < Blocks.Count; i++)
                    {
                        var block = Blocks[i];

                        if (block.Reachability != Reachability.NotReachable)
                        {
                            begin = Math.Min(begin, block.Start);
                            end = Math.Max(end, block.Start + block.TotalSize);
                        }
                    }
                }

                // if there are nested scopes, dump them too
                // also may need to adjust current scope bounds.
                if (_nestedScopes != null)
                {
                    ScopeBounds nestedBounds = GetHoistedLocalScopes(result, _nestedScopes);
                    begin = Math.Min(begin, nestedBounds.Begin);
                    end = Math.Max(end, nestedBounds.End);
                }

                // we are not interested in scopes with no variables or no code in them.
                if (_stateMachineUserHoistedLocalSlotIndices != null && end > begin)
                {
                    var newScope = new StateMachineHoistedLocalScope(begin, end);

                    foreach (var slotIndex in _stateMachineUserHoistedLocalSlotIndices)
                    {
                        while (result.Count <= slotIndex)
                        {
                            result.Add(default(StateMachineHoistedLocalScope));
                        }

                        result[slotIndex] = newScope;
                    }
                }

                return new ScopeBounds(begin, end);
            }

            public override void FreeBasicBlocks()
            {
                if (Blocks != null)
                {
                    for (int i = 0, cnt = Blocks.Count; i < cnt; i++)
                    {
                        Blocks[i].Free();
                    }
                }

                if (_nestedScopes != null)
                {
                    for (int i = 0, cnt = _nestedScopes.Count; i < cnt; i++)
                    {
                        _nestedScopes[i].FreeBasicBlocks();
                    }
                }
            }
        }

        /// <summary>
        /// A scope for a single try, catch, or finally clause. If the clause
        /// is a catch clause, ExceptionType will be set.
        /// </summary>
        internal sealed class ExceptionHandlerScope : LocalScopeInfo
        {
            private readonly ExceptionHandlerContainerScope _containingScope;
            private readonly ScopeType _type;
            private readonly Microsoft.Cci.ITypeReference _exceptionType;

            private BasicBlock _lastFilterConditionBlock;

            // branches may become "blocked by finally" if finally does not terminate (throws or contains infinite loop)
            // we cannot guarantee that the original label will be emitted (it might be unreachable).
            // on the other hand, it does not matter what blocked branches target as long as it is still blocked by same finally
            // so we provide this "special" block that is located right after finally that any blocked branch can safely target
            // We do guarantee that special block will be emitted as long as something uses it as a target of a branch.
            private object _blockedByFinallyDestination;

            public ExceptionHandlerScope(ExceptionHandlerContainerScope containingScope, ScopeType type, Microsoft.Cci.ITypeReference exceptionType)
            {
                Debug.Assert((type == ScopeType.Try) || (type == ScopeType.Catch) || (type == ScopeType.Filter) || (type == ScopeType.Finally) || (type == ScopeType.Fault));
                Debug.Assert((type == ScopeType.Catch) == (exceptionType != null));

                _containingScope = containingScope;
                _type = type;
                _exceptionType = exceptionType;
            }

            public ExceptionHandlerContainerScope ContainingExceptionScope => _containingScope;

            public override ScopeType Type => _type;

            public Microsoft.Cci.ITypeReference ExceptionType => _exceptionType;

            // pessimistically sets destination for blocked branches.
            // called when finally block is inserted in the outer TryFinally scope.
            // reachability analysis will clear the label as son as it proves
            // that finally is not blocking.
            public void SetBlockedByFinallyDestination(object label)
            {
                _blockedByFinallyDestination = label;
            }

            // if current finally does not terminate, this is where 
            // branches going through it should be retargeted.
            // Otherwise returns null.
            public object BlockedByFinallyDestination => _blockedByFinallyDestination;

            // Called when finally is determined to be non-blocking
            public void UnblockFinally()
            {
                _blockedByFinallyDestination = null;
            }

            public int FilterHandlerStart
                => _lastFilterConditionBlock.Start + _lastFilterConditionBlock.TotalSize;

            public override void FinishFilterCondition(ILBuilder builder)
            {
                Debug.Assert(_type == ScopeType.Filter);
                Debug.Assert(_lastFilterConditionBlock == null);

                _lastFilterConditionBlock = builder.FinishFilterCondition();
            }

            public BasicBlock LastFilterConditionBlock => _lastFilterConditionBlock;

            public override void ClosingScope(ILBuilder builder)
            {
                switch (_type)
                {
                    case ScopeType.Finally:
                    case ScopeType.Fault:
                        // Emit endfinally|endfault - they are the same opcode.
                        builder.EmitEndFinally();
                        break;

                    default:
                        // Emit branch to label after exception handler.
                        // ("br" will be rewritten as "leave" later by ILBuilder.)
                        var endLabel = _containingScope.EndLabel;
                        Debug.Assert(endLabel != null);

                        builder.EmitBranch(ILOpCode.Br, endLabel);
                        break;
                }
            }

            public override void CloseScope(ILBuilder builder)
            {
                Debug.Assert(LeaderBlock != null);
            }

            public override BasicBlock CreateBlock(ILBuilder builder)
            {
                Debug.Assert(builder.EnclosingExceptionHandler == this);
                var block = (Blocks == null) ?
                    new ExceptionHandlerLeaderBlock(builder, this, this.GetLeaderBlockType()) :
                    new BasicBlockWithHandlerScope(builder, this);

                AddBlock(block);
                return block;
            }

            public ExceptionHandlerLeaderBlock LeaderBlock => (ExceptionHandlerLeaderBlock)Blocks?[0];

            private BlockType GetLeaderBlockType()
            {
                switch (_type)
                {
                    case ScopeType.Try:
                        return BlockType.Try;
                    case ScopeType.Catch:
                        return BlockType.Catch;
                    case ScopeType.Filter:
                        return BlockType.Filter;
                    case ScopeType.Finally:
                        return BlockType.Finally;
                    default:
                        return BlockType.Fault;
                }
            }

            public override void FreeBasicBlocks()
            {
                base.FreeBasicBlocks();
            }
        }

        /// <summary>
        /// A scope for an entire exception handler (a try block with either several
        /// catches or a finally block). Unlike other scopes, this scope contains
        /// nested scopes only, no IL blocks (although nested ExceptionHandlerScopes
        /// for the clauses will contain IL blocks).
        /// </summary>
        internal sealed class ExceptionHandlerContainerScope : ScopeInfo
        {
            private readonly ImmutableArray<ExceptionHandlerScope>.Builder _handlers;
            private readonly object _endLabel;
            private readonly ExceptionHandlerScope _containingHandler;

            public ExceptionHandlerContainerScope(ExceptionHandlerScope containingHandler)
            {
                _handlers = ImmutableArray.CreateBuilder<ExceptionHandlerScope>(2);
                _containingHandler = containingHandler;
                _endLabel = new object();
            }

            public ExceptionHandlerScope ContainingHandler => _containingHandler;

            public object EndLabel => _endLabel;

            public override ScopeType Type => ScopeType.TryCatchFinally;

            public override ScopeInfo OpenScope(ScopeType scopeType,
                Microsoft.Cci.ITypeReference exceptionType,
                ExceptionHandlerScope currentExceptionHandler)
            {
                Debug.Assert(((_handlers.Count == 0) && (scopeType == ScopeType.Try)) ||
                    ((_handlers.Count > 0) && ((scopeType == ScopeType.Catch) || (scopeType == ScopeType.Filter) || (scopeType == ScopeType.Finally) || (scopeType == ScopeType.Fault))));

                Debug.Assert(currentExceptionHandler == _containingHandler);

                var handler = new ExceptionHandlerScope(this, scopeType, exceptionType);
                _handlers.Add(handler);
                return handler;
            }

            public override void CloseScope(ILBuilder builder)
            {
                Debug.Assert(_handlers.Count > 1);

                // Fix up the NextExceptionHandler reference of each leader block.
                var tryScope = _handlers[0];
                var previousBlock = tryScope.LeaderBlock;

                for (int i = 1; i < _handlers.Count; i++)
                {
                    var handlerScope = _handlers[i];
                    var nextBlock = handlerScope.LeaderBlock;

                    previousBlock.NextExceptionHandler = nextBlock;
                    previousBlock = nextBlock;
                }

                // Generate label for try/catch "leave" target.
                builder.MarkLabel(_endLabel);

                // hide the following code, since it could be reached through the label above.
                builder.DefineHiddenSequencePoint();

                Debug.Assert(builder._currentBlock == builder._labelInfos[_endLabel].bb);

                if (_handlers[1].Type == ScopeType.Finally)
                {
                    // Generate "nop" branch to itself. If this block is unreachable
                    // (because the finally block does not complete), the "nop" will be
                    // replaced by Br_s. On the other hand, if this block is reachable,
                    // the "nop" will be skipped so any "leave" instructions jumping
                    // to this block will jump to the next instead.
                    builder.EmitBranch(ILOpCode.Nop, _endLabel);

                    _handlers[1].SetBlockedByFinallyDestination(_endLabel);
                }
            }

            internal override void GetExceptionHandlerRegions(ArrayBuilder<Cci.ExceptionHandlerRegion> regions)
            {
                Debug.Assert(_handlers.Count > 1);

                ExceptionHandlerScope tryScope = null;
                ScopeBounds tryBounds = new ScopeBounds();

                foreach (var handlerScope in _handlers)
                {
                    // Partition I, section 12.4.2.5:
                    // The ordering of the exception clauses in the Exception Handler Table is important. If handlers are nested, 
                    // the most deeply nested try blocks shall come before the try blocks that enclose them.
                    //
                    // so we collect the inner regions first.
                    handlerScope.GetExceptionHandlerRegions(regions);

                    var handlerBounds = GetBounds(handlerScope);

                    if (tryScope == null)
                    {
                        // the first scope that we see should be Try.
                        Debug.Assert(handlerScope.Type == ScopeType.Try);

                        tryScope = handlerScope;
                        tryBounds = handlerBounds;

                        var reachability = tryScope.LeaderBlock.Reachability;
                        Debug.Assert((reachability == Reachability.Reachable) || (reachability == Reachability.NotReachable));

                        // All handler blocks should have same reachability.
                        Debug.Assert(_handlers.All(h => (h.LeaderBlock.Reachability == reachability)));

                        if (reachability != Reachability.Reachable)
                        {
                            return;
                        }
                    }
                    else
                    {
                        Cci.ExceptionHandlerRegion region;
                        switch (handlerScope.Type)
                        {
                            case ScopeType.Finally:
                                region = new Cci.ExceptionHandlerRegionFinally(tryBounds.Begin, tryBounds.End, handlerBounds.Begin, handlerBounds.End);
                                break;

                            case ScopeType.Fault:
                                region = new Cci.ExceptionHandlerRegionFault(tryBounds.Begin, tryBounds.End, handlerBounds.Begin, handlerBounds.End);
                                break;

                            case ScopeType.Catch:
                                region = new Cci.ExceptionHandlerRegionCatch(tryBounds.Begin, tryBounds.End, handlerBounds.Begin, handlerBounds.End, handlerScope.ExceptionType);
                                break;

                            case ScopeType.Filter:
                                region = new Cci.ExceptionHandlerRegionFilter(tryBounds.Begin, tryBounds.End, handlerScope.FilterHandlerStart, handlerBounds.End, handlerBounds.Begin);
                                break;

                            default:
                                throw ExceptionUtilities.UnexpectedValue(handlerScope.Type);
                        }

                        regions.Add(region);
                    }
                }
            }

            internal override ScopeBounds GetLocalScopes(ArrayBuilder<Cci.LocalScope> scopesWithVariables)
                => GetLocalScopes(scopesWithVariables, _handlers);

            internal override ScopeBounds GetHoistedLocalScopes(ArrayBuilder<StateMachineHoistedLocalScope> result)
                => GetHoistedLocalScopes(result, _handlers);

            private static ScopeBounds GetBounds(ExceptionHandlerScope scope)
            {
                var scopes = ArrayBuilder<Cci.LocalScope>.GetInstance();
                var result = scope.GetLocalScopes(scopes);
                scopes.Free();
                return result;
            }

            public override void FreeBasicBlocks()
            {
                // No basic blocks owned directly here.

                foreach (var scope in _handlers)
                {
                    scope.FreeBasicBlocks();
                }
            }

            internal bool FinallyOnly()
            {
                var curScope = this;
                do
                {
                    var handlers = curScope._handlers;
                    // handler[0] is always the try
                    // if we have a finally, then we do not have any catches and 
                    // the finally is as handlers[1]
                    if (handlers.Count != 2 || handlers[1].Type != ScopeType.Finally)
                    {
                        return false;
                    }

                    curScope = curScope._containingHandler?.ContainingExceptionScope;
                }
                while (curScope != null);

                return true;
            }
        }

        internal readonly struct ScopeBounds
        {
            internal readonly int Begin; // inclusive
            internal readonly int End;   // exclusive

            internal ScopeBounds(int begin, int end)
            {
                Debug.Assert(begin >= 0 && end >= 0);
                this.Begin = begin;
                this.End = end;
            }
        }

        /// <summary>
        /// Compares scopes by their start (ascending) and then size (descending).
        /// </summary>
        private sealed class ScopeComparer : IComparer<Cci.LocalScope>
        {
            public static readonly ScopeComparer Instance = new ScopeComparer();

            private ScopeComparer() { }

            public int Compare(Cci.LocalScope x, Cci.LocalScope y)
            {
                var result = x.StartOffset.CompareTo(y.StartOffset);
                return (result == 0) ? y.EndOffset.CompareTo(x.EndOffset) : result;
            }
        }
    }
}
