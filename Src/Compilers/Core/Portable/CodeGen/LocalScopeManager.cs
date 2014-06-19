// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;
using Cci = Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CodeGen
{
    partial class ILBuilder
    {
        private sealed class LocalScopeManager
        {
            private readonly LocalScopeInfo rootScope;
            private readonly Stack<ScopeInfo> scopes;
            private ExceptionHandlerScope enclosingExceptionHandler;

            internal LocalScopeManager()
            {
                rootScope = new LocalScopeInfo();
                scopes = new Stack<ScopeInfo>(1);
                scopes.Push(rootScope);
            }

            private ScopeInfo CurrentScope
            {
                get
                {
                    return scopes.Peek();
                }
            }

            internal ScopeInfo OpenScope(ScopeType scopeType, Microsoft.Cci.ITypeReference exceptionType)
            {
                var scope = CurrentScope.OpenScope(scopeType, exceptionType, this.enclosingExceptionHandler);
                scopes.Push(scope);

                if (scope.IsExceptionHandler)
                {
                    this.enclosingExceptionHandler = (ExceptionHandlerScope)scope;
                }

                Debug.Assert(this.enclosingExceptionHandler == GetEnclosingExceptionHandler());
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
                var scope = scopes.Pop();
                scope.CloseScope(builder);

                if (scope.IsExceptionHandler)
                {
                    this.enclosingExceptionHandler = GetEnclosingExceptionHandler();
                }

                Debug.Assert(this.enclosingExceptionHandler == GetEnclosingExceptionHandler());
            }

            internal ExceptionHandlerScope EnclosingExceptionHandler
            {
                get { return this.enclosingExceptionHandler; }
            }

            private ExceptionHandlerScope GetEnclosingExceptionHandler()
            {
                foreach (var scope in scopes)
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
            /// <param name="edgeInclusive">Specifies whether scope spans should be reported as edge inclusive
            /// (position at "start + length" is IN the scope). VB EE expects that.</param>
            /// <returns></returns>
            /// <remarks>
            /// NOTE that edgeInclusive affects only how results are _reported_. 
            /// All internal representation is EDGE EXCLUSIVE.
            /// </remarks>
            internal ImmutableArray<Cci.LocalScope> GetAllScopesWithLocals(bool edgeInclusive = false)
            {
                var result = ArrayBuilder<Cci.LocalScope>.GetInstance();
                ScopeBounds rootBounds = rootScope.GetScopesWithLocals(result, edgeInclusive);

                uint expectedRootScopeLength = rootBounds.End - rootBounds.Begin;
                if (edgeInclusive)
                {
                    expectedRootScopeLength--;
                }

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

                //scopes should be sorted by position and size
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
                rootScope.GetExceptionHandlerRegions(result);
                return result.ToImmutableAndFree();
            }

            internal ImmutableArray<Cci.LocalScope> GetIteratorScopes(bool edgeInclusive)
            {
                var result = ArrayBuilder<Cci.LocalScope>.GetInstance();
                rootScope.GetIteratorScopes(result, edgeInclusive);
                return result.ToImmutableAndFree();
            }

            internal void AddIteratorVariable(int index)
            {
                var scope = (LocalScopeInfo)CurrentScope;
                scope.AddIteratorVariable(index);
            }

            internal void FreeBasicBlocks()
            {
                rootScope.FreeBasicBlocks();
            }

            internal bool PossiblyDefinedOutsideOfTry(LocalDefinition local)
            {
                foreach (var s in this.scopes)
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

                // not recoreded in scopes, could be a temp
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
                    Debug.Assert(scopeType == ScopeType.Variable || scopeType == ScopeType.IteratorVariable);
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
                throw ExceptionUtilities.Unreachable;
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
            internal abstract ScopeBounds GetScopesWithLocals(ArrayBuilder<Cci.LocalScope> scopesWithVariables, bool edgeInclusive);

            protected static ScopeBounds GetScopesWithLocals<TScopeInfo>(ArrayBuilder<Cci.LocalScope> scopesWithVariables, ImmutableArray<TScopeInfo>.Builder scopes, bool edgeInclusive)
                where TScopeInfo : ScopeInfo
            {
                Debug.Assert(scopes.Count > 0);

                uint begin = uint.MaxValue;
                uint end = 0;

                foreach (var scope in scopes)
                {
                    ScopeBounds bounds = scope.GetScopesWithLocals(scopesWithVariables, edgeInclusive);
                    begin = Math.Min(begin, bounds.Begin);
                    end = Math.Max(end, bounds.End);
                }

                return new ScopeBounds(begin, end);
            }

            /// <summary>
            /// Recursively calculates the start and end of the given scope.
            /// Only scopes with locals are actually dumped to the list.
            /// </summary>
            internal abstract ScopeBounds GetIteratorScopes(ArrayBuilder<Cci.LocalScope> scopesWithIteratorLocals, bool edgeInclusive);

            protected static ScopeBounds GetIteratorScopes<TScopeInfo>(ArrayBuilder<Cci.LocalScope> scopesWithIteratorLocals, ImmutableArray<TScopeInfo>.Builder scopes, bool edgeInclusive)
                where TScopeInfo : ScopeInfo
            {
                Debug.Assert(scopes.Count > 0);

                uint begin = uint.MaxValue;
                uint end = 0;

                foreach (var scope in scopes)
                {
                    ScopeBounds bounds = scope.GetIteratorScopes(scopesWithIteratorLocals, edgeInclusive);
                    begin = Math.Min(begin, bounds.Begin);
                    end = Math.Max(end, bounds.End);
                }

                return new ScopeBounds(begin, end);
            }

            /// <summary>
            /// Free any basic blocks owned by this scope or sub-scopes.
            /// </summary>
            public abstract void FreeBasicBlocks();

            internal virtual bool ContainsLocal(LocalDefinition local)
            {
                return false;
            }
        }

        /// <summary>
        /// Class that collects content of the scope (blocks, nested scopes, variables etc).
        /// There is one for every opened scope.
        /// </summary>
        internal class LocalScopeInfo : ScopeInfo
        {
            private ImmutableArray<LocalDefinition>.Builder LocalVariables;
            private ImmutableArray<LocalConstantDefinition>.Builder LocalConstants;
            private ImmutableArray<int>.Builder IteratorVariables;

            // Nested scopes and blocks are not relevant for PDB. 
            // We need these only to figure scope bounds.
            private ImmutableArray<ScopeInfo>.Builder NestedScopes;
            protected ImmutableArray<BasicBlock>.Builder Blocks;

            public override ScopeType Type
            {
                get { return ScopeType.Variable; }
            }

            public override ScopeInfo OpenScope(
                ScopeType scopeType,
                Microsoft.Cci.ITypeReference exceptionType,
                ExceptionHandlerScope currentExceptionHandler)
            {
                var scope = base.OpenScope(scopeType, exceptionType, currentExceptionHandler);
                if (NestedScopes == null)
                {
                    NestedScopes = ImmutableArray.CreateBuilder<ScopeInfo>(1);
                }
                NestedScopes.Add(scope);
                return scope;
            }

            internal void AddLocal(LocalDefinition variable)
            {
                if (LocalVariables == null)
                {
                    LocalVariables = ImmutableArray.CreateBuilder<LocalDefinition>(1);
                }

                LocalVariables.Add(variable);
            }

            internal void AddLocalConstant(LocalConstantDefinition constant)
            {
                if (LocalConstants == null)
                {
                    LocalConstants = ImmutableArray.CreateBuilder<LocalConstantDefinition>(1);
                }

                LocalConstants.Add(constant);
            }

            internal void AddIteratorVariable(int index)
            {
                if (IteratorVariables == null)
                {
                    IteratorVariables = ImmutableArray.CreateBuilder<int>(1);
                }

                IteratorVariables.Add(index);
            }

            internal override bool ContainsLocal(LocalDefinition local)
            {
                var locals = this.LocalVariables;
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
                if (NestedScopes != null)
                {
                    for (int i = 0, cnt = NestedScopes.Count; i < cnt; i++)
                    {
                        NestedScopes[i].GetExceptionHandlerRegions(regions);
                    }
                }
            }

            internal override ScopeBounds GetScopesWithLocals(ArrayBuilder<Cci.LocalScope> scopesWithVariables, bool edgeInclusive)
            {
                uint begin = uint.MaxValue;
                uint end = 0;

                // It may seem overkill to scan all blocks, 
                // but blocks may be reordered so we cannot be sure which ones are first/last.
                if (Blocks != null)
                {
                    for (int i = 0; i < Blocks.Count; i++)
                    {
                        var block = Blocks[i];

                        if (block.Reachability != Reachability.NotReachable)
                        {
                            begin = Math.Min(begin, (uint)block.Start);
                            end = Math.Max(end, (uint)(block.Start + block.TotalSize));
                        }
                    }
                }

                // if there are nested scopes, dump them too
                // also may need to adjust current scope bounds.
                if (NestedScopes != null)
                {
                    ScopeBounds nestedBounds = GetScopesWithLocals(scopesWithVariables, NestedScopes, edgeInclusive);
                    begin = Math.Min(begin, nestedBounds.Begin);
                    end = Math.Max(end, nestedBounds.End);
                }

                // we are not interested in scopes with no variables or no code in them.
                if ((this.LocalVariables != null || this.LocalConstants != null) && end > begin)
                {
                    uint endAdjusted = edgeInclusive ? end - 1 : end;

                    var newScope = new Cci.LocalScope(
                        begin, 
                        endAdjusted - begin, 
                        this.LocalConstants.AsImmutableOrEmpty<Cci.ILocalDefinition>(),
                        this.LocalVariables.AsImmutableOrEmpty<Cci.ILocalDefinition>());

                    scopesWithVariables.Add(newScope);
                }

                return new ScopeBounds(begin, end);
            }

            internal override ScopeBounds GetIteratorScopes(ArrayBuilder<Cci.LocalScope> scopesWithIteratorLocals, bool edgeInclusive)
            {
                uint begin = uint.MaxValue;
                uint end = 0;

                // It may seem overkill to scan all blocks, 
                // but blocks may be reordered so we cannot be sure which ones are first/last.
                if (Blocks != null)
                {
                    for (int i = 0; i < Blocks.Count; i++)
                    {
                        var block = Blocks[i];

                        if (block.Reachability != Reachability.NotReachable)
                        {
                            begin = Math.Min(begin, (uint)block.Start);
                            end = Math.Max(end, (uint)(block.Start + block.TotalSize));
                        }
                    }
                }

                // if there are nested scopes, dump them too
                // also may need to adjust current scope bounds.
                if (NestedScopes != null)
                {
                    ScopeBounds nestedBounds = GetIteratorScopes(scopesWithIteratorLocals, NestedScopes, edgeInclusive);
                    begin = Math.Min(begin, nestedBounds.Begin);
                    end = Math.Max(end, nestedBounds.End);
                }

                // we are not interested in scopes with no variables or no code in them.
                if (this.IteratorVariables != null && end > begin)
                {
                    uint endAdjusted = edgeInclusive ? end - 1 : end;

                    var newScope = new Cci.LocalScope(
                        begin, 
                        endAdjusted - begin,
                        ImmutableArray<Cci.ILocalDefinition>.Empty,
                        ImmutableArray<Cci.ILocalDefinition>.Empty);

                    foreach (var iv in this.IteratorVariables)
                    {
                        while (scopesWithIteratorLocals.Count <= iv)
                        {
                            scopesWithIteratorLocals.Add(default(Cci.LocalScope));
                        }

                        scopesWithIteratorLocals[iv] = newScope;
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

                if (NestedScopes != null)
                {
                    for (int i = 0, cnt = NestedScopes.Count; i < cnt; i++)
                    {
                        NestedScopes[i].FreeBasicBlocks();
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
            private readonly ExceptionHandlerContainerScope containingScope;
            private readonly ScopeType type;
            private readonly Microsoft.Cci.ITypeReference exceptionType;

            private BasicBlock lastFilterConditionBlock;

            // branches mey become "blocked by finally" if finally does not terminate (throws or contains infinite loop)
            // we cannot guarantee that the original lable will be emitted (it might be unreachable).
            // on the other hand, it does not matter what blocked branches target as long as it is still blocked by same finally
            // so we provide this "special" block that is located right after finally that any blocked branch can safely target
            // We do guarantee that special block will be emitted as long as something uses it as a target of a branch.
            private object blockedByFinallyDestination;

            public ExceptionHandlerScope(ExceptionHandlerContainerScope containingScope, ScopeType type, Microsoft.Cci.ITypeReference exceptionType)
            {
                Debug.Assert((type == ScopeType.Try) || (type == ScopeType.Catch) || (type == ScopeType.Filter) || (type == ScopeType.Finally) || (type == ScopeType.Fault));
                Debug.Assert((type == ScopeType.Catch) == (exceptionType != null));

                this.containingScope = containingScope;
                this.type = type;
                this.exceptionType = exceptionType;
            }

            public ExceptionHandlerContainerScope ContainingExceptionScope
            {
                get
                {
                    return containingScope;
                }
            }

            public override ScopeType Type
            {
                get { return this.type; }
            }

            public Microsoft.Cci.ITypeReference ExceptionType
            {
                get { return this.exceptionType; }
            }

            // pessimistically sets destination for blocked branches.
            // called when finally block is inserted in the outer TryFinally scope.
            // reachability analysis will clear the label as son as it proves
            // that finally is not blocking.
            public void SetBlockedByFinallyDestination(object label)
            {
                this.blockedByFinallyDestination = label;
            }

            // if current finally does not terminate, this is where 
            // branches going through it should be retargeted.
            // Otherwise returns null.
            public object BlockedByFinallyDestination
            {
                get { return this.blockedByFinallyDestination; }
            }

            // Called when finally is determined to be non-blocking
            public void UnblockFinally()
            {
                this.blockedByFinallyDestination = null;
            }

            public uint FilterHandlerStart
            {
                get
                {
                    return (uint)(lastFilterConditionBlock.Start + lastFilterConditionBlock.TotalSize);
                }
            }

            public override void FinishFilterCondition(ILBuilder builder)
            {
                Debug.Assert(this.type == ScopeType.Filter);
                Debug.Assert(this.lastFilterConditionBlock == null);

                this.lastFilterConditionBlock = builder.FinishFilterCondition();
            }

            public override void ClosingScope(ILBuilder builder)
            {
                switch (this.type)
                {
                    case ScopeType.Finally:
                    case ScopeType.Fault:
                        // Emit endfinally|endfault - they are the same opcode.
                        builder.EmitEndFinally();
                        break;

                    default:
                        // Emit branch to label after exception handler.
                        // ("br" will be rewritten as "leave" later by ILBuilder.)
                        var endLabel = this.containingScope.EndLabel;
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

            public ExceptionHandlerLeaderBlock LeaderBlock
            {
                get { return (Blocks == null) ? null : (ExceptionHandlerLeaderBlock)Blocks[0]; }
            }

            private BlockType GetLeaderBlockType()
            {
                switch (this.type)
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
            private readonly ImmutableArray<ExceptionHandlerScope>.Builder handlers;
            private readonly object endLabel;
            private readonly ExceptionHandlerScope containingHandler;

            public ExceptionHandlerContainerScope(ExceptionHandlerScope containingHandler)
            {
                this.handlers = ImmutableArray.CreateBuilder<ExceptionHandlerScope>(2);
                this.containingHandler = containingHandler;
                this.endLabel = new object();
            }

            public ExceptionHandlerScope ContainingHandler
            {
                get
                {
                    return containingHandler;
                }
            }

            public object EndLabel
            {
                get { return this.endLabel; }
            }

            public override ScopeType Type
            {
                get { return ScopeType.TryCatchFinally; }
            }

            public override ScopeInfo OpenScope(ScopeType scopeType,
                Microsoft.Cci.ITypeReference exceptionType,
                ExceptionHandlerScope currentExceptionHandler)
            {
                Debug.Assert(((this.handlers.Count == 0) && (scopeType == ScopeType.Try)) ||
                    ((this.handlers.Count > 0) && ((scopeType == ScopeType.Catch) || (scopeType == ScopeType.Filter) || (scopeType == ScopeType.Finally) || (scopeType == ScopeType.Fault))));

                Debug.Assert(currentExceptionHandler == this.containingHandler);

                var handler = new ExceptionHandlerScope(this, scopeType, exceptionType);
                this.handlers.Add(handler);
                return handler;
            }

            public override void CloseScope(ILBuilder builder)
            {
                Debug.Assert(this.handlers.Count > 1);

                // Fix up the NextExceptionHandler reference of each leader block.
                var tryScope = this.handlers[0];
                var previousBlock = tryScope.LeaderBlock;

                for (int i = 1; i < this.handlers.Count; i++)
                {
                    var handlerScope = this.handlers[i];
                    var nextBlock = handlerScope.LeaderBlock;

                    previousBlock.NextExceptionHandler = nextBlock;
                    previousBlock = nextBlock;
                }

                // Generate label for try/catch "leave" target.
                builder.MarkLabel(this.endLabel);

                // hide the following code, since it could be reached through the label above.
                builder.DefineHiddenSeqPoint();

                Debug.Assert(builder.currentBlock == builder.labelInfos[this.endLabel].bb);

                if (this.handlers[1].Type == ScopeType.Finally)
                {
                    // Generate "nop" branch to itself. If this block is unreachable
                    // (because the finally block does not complete), the "nop" will be
                    // replaced by Br_s. On the other hand, if this block is reachable,
                    // the "nop" will be skipped so any "leave" instructions jumping
                    // to this block will jump to the next instead.
                    builder.EmitBranch(ILOpCode.Nop, this.endLabel);

                    this.handlers[1].SetBlockedByFinallyDestination(this.endLabel);
                }
            }

            internal override void GetExceptionHandlerRegions(ArrayBuilder<Cci.ExceptionHandlerRegion> regions)
            {
                Debug.Assert(this.handlers.Count > 1);

                ExceptionHandlerScope tryScope = null;
                ScopeBounds tryBounds = new ScopeBounds();

                foreach (var handlerScope in this.handlers)
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
                        Debug.Assert(this.handlers.All(h => (h.LeaderBlock.Reachability == reachability)));

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

            internal override ScopeBounds GetScopesWithLocals(ArrayBuilder<Cci.LocalScope> scopesWithVariables, bool edgeInclusive)
            {
                return GetScopesWithLocals(scopesWithVariables, this.handlers, edgeInclusive);
            }

            internal override ScopeBounds GetIteratorScopes(ArrayBuilder<Cci.LocalScope> scopesWithIteratorVariables, bool edgeInclusive)
            {
                return GetIteratorScopes(scopesWithIteratorVariables, this.handlers, edgeInclusive);
            }

            private static ScopeBounds GetBounds(ExceptionHandlerScope scope)
            {
                var scopes = ArrayBuilder<Cci.LocalScope>.GetInstance();
                var result = scope.GetScopesWithLocals(scopes, edgeInclusive: false);
                scopes.Free();
                return result;
            }

            public override void FreeBasicBlocks()
            {
                // No basic blocks owned directly here.

                foreach (var scope in this.handlers)
                {
                    scope.FreeBasicBlocks();
                }
            }
        }

        internal struct ScopeBounds
        {
            internal readonly uint Begin;
            internal readonly uint End;

            internal ScopeBounds(uint begin, uint end)
            {
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
                var res = x.Offset.CompareTo(y.Offset);
                if (res == 0)
                {
                    res = y.Length.CompareTo(x.Length);
                }

                return res;
            }
        }
    }
}
