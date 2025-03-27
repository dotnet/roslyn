// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis.CSharp.CodeGen
{
    internal class Optimizer
    {
        /// <summary>
        /// Perform IL specific optimizations (mostly reduction of local slots)
        /// </summary>
        /// <param name="src">Method body to optimize</param>
        /// <param name="debugFriendly">
        /// When set, do not perform aggressive optimizations that degrade debugging experience.
        /// In particular we do not do the following:
        ///
        /// 1) Do not elide any user defined locals, even if never read from.
        ///    Example:
        ///      {
        ///        var dummy = Goo();    // should not become just "Goo"
        ///      }
        ///
        ///    User might want to examine dummy in the debugger.
        ///
        /// 2) Do not carry values on the stack between statements
        ///    Example:
        ///      {
        ///        var temp = Goo();
        ///        temp.ToString();       // should not become   Goo().ToString();
        ///      }
        ///
        ///    User might want to examine temp in the debugger.
        ///
        /// </param>
        /// <param name="stackLocals">
        /// Produced list of "ephemeral" locals.
        /// Essentially, these locals do not need to leave the evaluation stack.
        /// As such they do not require an allocation of a local slot and
        /// their load/store operations are implemented trivially.
        /// </param>
        /// <returns></returns>
        public static BoundStatement Optimize(
            BoundStatement src, bool debugFriendly,
            out HashSet<LocalSymbol> stackLocals)
        {
            //TODO: run other optimizing passes here.
            //      stack scheduler must be the last one.

            var locals = PooledDictionary<LocalSymbol, LocalDefUseInfo>.GetInstance();
            src = (BoundStatement)StackOptimizerPass1.Analyze(src, locals, debugFriendly);

            FilterValidStackLocals(locals);

            BoundStatement result;
            if (locals.Count == 0)
            {
                stackLocals = null;
                result = src;
            }
            else
            {
                stackLocals = new HashSet<LocalSymbol>(locals.Keys);
                result = StackOptimizerPass2.Rewrite(src, locals);
            }

            foreach (var info in locals.Values)
            {
                info.Free();
            }

            locals.Free();

            return result;
        }

        private static void FilterValidStackLocals(Dictionary<LocalSymbol, LocalDefUseInfo> info)
        {
            // remove fake dummies and variable that cannot be scheduled
            var dummies = ArrayBuilder<LocalDefUseInfo>.GetInstance();

            foreach (var local in info.Keys.ToArray())
            {
                var locInfo = info[local];

                if (local.SynthesizedKind == SynthesizedLocalKind.OptimizerTemp)
                {
                    dummies.Add(locInfo);
                    info.Remove(local);
                }
                else if (locInfo.CannotSchedule)
                {
                    locInfo.Free();
                    info.Remove(local);
                }
            }

            if (info.Count != 0)
            {
                RemoveIntersectingLocals(info, dummies);
            }

            foreach (var dummy in dummies)
            {
                dummy.Free();
            }
            dummies.Free();
        }

        private static void RemoveIntersectingLocals(Dictionary<LocalSymbol, LocalDefUseInfo> info, ArrayBuilder<LocalDefUseInfo> dummies)
        {
            // Add dummy definitions.
            // Although we do not schedule dummies we intend to guarantee that no
            // local definition span intersects with definition spans of a dummy
            // that will ensure that at any access to dummy is done on same stack state.
            var defs = ArrayBuilder<LocalDefUseSpan>.GetInstance(dummies.Count);
            foreach (var dummy in dummies)
            {
                foreach (var def in dummy.LocalDefs)
                {
                    // not interested in single node definitions
                    if (def.Start != def.End)
                    {
                        defs.Add(def);
                    }
                }
            }

            var dummyCnt = defs.Count;

            //TODO: perf. This can be simplified to not use a query.

            // order definitions by increasing size
            // this will give preference to shorter def-use spans when they intersect
            //
            // also order by usage, giving preference to spans at the beginning of the method
            var ordered = from i in info
                          from d in i.Value.LocalDefs
                          orderby d.End - d.Start, d.End ascending
                          select new { i = i.Key, d = d };

            // collect non-intersecting def-use spans.
            // if span intersects with something already stored, reject corresponding variable.
            //
            // CONSIDER: do we want to remove already added spans of rejected variables?
            //           When I tried it did not improve results much. So I will keep it simple.
            foreach (var pair in ordered)
            {
                if (!info.ContainsKey(pair.i))
                {
                    // this pair belongs to a local that is already rejected
                    // no need to waste time on it
                    continue;
                }

                var newDef = pair.d;
                var cnt = defs.Count;

                bool intersects;

                // 5000 here is just a "sufficiently large number"
                // in practice cnt rarely exceeds 200
                if (cnt > 5000)
                {
                    // too many locals/spans.
                    // This is an n^2 check and optimizing further may become costly.
                    // reject all following definition spans
                    intersects = true;
                }
                else
                {
                    intersects = false;
                    for (int i = 0; i < dummyCnt; i++)
                    {
                        var def = defs[i];

                        if (newDef.ConflictsWithDummy(def))
                        {
                            intersects = true;
                            break;
                        }
                    }

                    if (!intersects)
                    {
                        for (int i = dummyCnt; i < cnt; i++)
                        {
                            var def = defs[i];

                            if (newDef.ConflictsWith(def))
                            {
                                intersects = true;
                                break;
                            }
                        }
                    }
                }

                if (intersects)
                {
                    info[pair.i].LocalDefs.Free();
                    info.Remove(pair.i);
                }
                else
                {
                    defs.Add(newDef);
                }
            }

            defs.Free();
        }
    }

    // represents a local and its Def-Use-Use chain
    //
    // NOTE: stack local reads are destructive to the locals so
    //      if the read is not the last one, it must be immediately followed by
    //      another definition.
    //      For the rewriting purposes it is irrelevant if definition was created by
    //      a write or a subsequent read. These cases are not ambiguous because
    //      when rewriting, definition will match to a single node and
    //      we always know if given node is reading or writing.
    internal class LocalDefUseInfo
    {
        // stack at variable declaration, may be > 0 in sequences.
        public int StackAtDeclaration { get; private set; }
        private readonly ObjectPool<LocalDefUseInfo> _pool;

        // global pool
        private static readonly ObjectPool<LocalDefUseInfo> s_poolInstance = CreatePool();

        // value definitions for this variable.
        private ArrayBuilder<LocalDefUseSpan> _localDefs;
        public ArrayBuilder<LocalDefUseSpan> LocalDefs
        {
            get
            {
                var result = _localDefs;
                if (result == null)
                {
                    _localDefs = result = ArrayBuilder<LocalDefUseSpan>.GetInstance();
                }

                return result;
            }
        }

        // once this goes to true we are no longer interested in this variable.
        public bool CannotSchedule { get; private set; }

        public void ShouldNotSchedule()
        {
            this.CannotSchedule = true;
        }

        private LocalDefUseInfo(ObjectPool<LocalDefUseInfo> pool)
        {
            _pool = pool;
        }

        public void Free()
        {
            if (_localDefs != null)
            {
                _localDefs.Free();
                _localDefs = null;
            }

            _pool?.Free(this);
        }

        // if someone needs to create a pool;
        public static ObjectPool<LocalDefUseInfo> CreatePool()
        {
            ObjectPool<LocalDefUseInfo> pool = null;
            pool = new ObjectPool<LocalDefUseInfo>(() => new LocalDefUseInfo(pool), 128);
            return pool;
        }

        public static LocalDefUseInfo GetInstance(int stackAtDeclaration)
        {
            var instance = s_poolInstance.Allocate();
            Debug.Assert(instance._localDefs == null);
            instance.StackAtDeclaration = stackAtDeclaration;
            instance.CannotSchedule = false;
            return instance;
        }
    }

    // represents a span of a value between definition and use.
    // start/end positions are specified in terms of global node count as visited by
    // StackOptimizer visitors. (i.e. recursive walk not looking into constants)
    internal readonly struct LocalDefUseSpan
    {
        public readonly int Start;
        public readonly int End;

        public LocalDefUseSpan(int start) : this(start, start) { }

        private LocalDefUseSpan(int start, int end)
        {
            this.Start = start;
            this.End = end;
        }

        internal LocalDefUseSpan WithEnd(int end)
        {
            return new LocalDefUseSpan(this.Start, end);
        }

        public override string ToString()
        {
            return "[" + this.Start + " ," + this.End + ")";
        }

        /// <summary>
        /// when current and other use spans are regular spans we can have only 2 conflict cases:
        /// [1, 3) conflicts with [0, 2)
        /// [1, 3) conflicts with [2, 4)
        ///
        /// NOTE: with regular spans, it is not possible for two spans to share an edge point
        /// unless they belong to the same local. (because we cannot access two real locals at the same time)
        ///
        /// specifically:
        /// [1, 3) does not conflict with [0, 1)   since such spans would need to belong to the same local
        /// </summary>
        public bool ConflictsWith(LocalDefUseSpan other)
        {
            return Contains(other.Start) ^ Contains(other.End);
        }

        private bool Contains(int val)
        {
            return this.Start < val && this.End > val;
        }

        /// <summary>
        /// Dummy locals represent implicit control flow
        /// It is not allowed for a regular local span to cross into or
        /// be immediately adjacent to a dummy span.
        ///
        /// specifically:
        /// [1, 3) does conflict with [0, 1)   since that would imply a value flowing into or out of a span surrounded by a branch/label
        ///
        /// </summary>
        public bool ConflictsWithDummy(LocalDefUseSpan dummy)
        {
            return Includes(dummy.Start) ^ Includes(dummy.End);
        }

        private bool Includes(int val)
        {
            return this.Start <= val && this.End >= val;
        }
    }

    // context of expression evaluation.
    // it will affect inference of stack behavior
    // it will also affect when locals can be scheduled to the stack
    // Example:
    //      Goo(x, ref x)     <-- x cannot be a stack local as it is used in different contexts.
    internal enum ExprContext
    {
        None,
        Sideeffects,
        Value,
        Address,
        AssignmentTarget,
        Box
    }

    // Analyzes the tree trying to figure which locals may live on stack.
    // It is a fairly delicate process and must be very familiar with how CodeGen works.
    // It is essentially a part of CodeGen.
    //
    // NOTE: It is always safe to mark a local as not eligible as a stack local
    //       so when situation gets complicated we just refuse to schedule and move on.
    //
    internal sealed class StackOptimizerPass1 : BoundTreeRewriter
    {
        private readonly bool _debugFriendly;
        private readonly ArrayBuilder<(BoundExpression, ExprContext)> _evalStack;

        private int _counter;
        private ExprContext _context;
        private BoundLocal _assignmentLocal;

        private readonly Dictionary<LocalSymbol, LocalDefUseInfo> _locals;

        // we need to guarantee same stack patterns at branches and labels.
        // we do that by placing a fake dummy local at one end of a branch and force that it is accessible at another.
        // if any stack local tries to intervene and misbalance the stack, it will clash with the dummy and will be rejected.
        private readonly SmallDictionary<object, DummyLocal> _dummyVariables =
            new SmallDictionary<object, DummyLocal>(ReferenceEqualityComparer.Instance);

        // fake local that represents the eval stack.
        // when we need to ensure that eval stack is not blocked by stack Locals, we record an access to empty.
        public static readonly DummyLocal empty = new DummyLocal();

        private int _recursionDepth;

        private StackOptimizerPass1(Dictionary<LocalSymbol, LocalDefUseInfo> locals,
            ArrayBuilder<ValueTuple<BoundExpression, ExprContext>> evalStack,
            bool debugFriendly)
        {
            _locals = locals;
            _evalStack = evalStack;
            _debugFriendly = debugFriendly;

            // this is the top of eval stack
            DeclareLocal(empty, 0);
            RecordDummyWrite(empty);
        }

        public static BoundNode Analyze(
            BoundNode node,
            Dictionary<LocalSymbol, LocalDefUseInfo> locals,
            bool debugFriendly)
        {
            var evalStack = ArrayBuilder<ValueTuple<BoundExpression, ExprContext>>.GetInstance();
            var analyzer = new StackOptimizerPass1(locals, evalStack, debugFriendly);
            var rewritten = analyzer.Visit(node);
            evalStack.Free();

            return rewritten;
        }

        public override BoundNode Visit(BoundNode node)
        {
            BoundNode result;

            BoundExpression expr = node as BoundExpression;
            if (expr != null)
            {
                Debug.Assert(expr.Kind != BoundKind.Label);
                result = VisitExpression(expr, ExprContext.Value);
            }
            else
            {
                result = VisitStatement(node);
            }

            return result;
        }

        private BoundExpression VisitExpressionCore(BoundExpression node, ExprContext context)
        {
            var prevContext = _context;
            int prevStack = StackDepth();
            _context = context;

            // Do not recurse into constant expressions. Their children do not push any values.
            var result = node.ConstantValueOpt == null ?
                node = (BoundExpression)base.Visit(node) :
                node;

            _context = prevContext;
            _counter += 1;

            switch (context)
            {
                case ExprContext.Sideeffects:
                    SetStackDepth(prevStack);
                    break;

                case ExprContext.AssignmentTarget:
                    break;

                case ExprContext.Value:
                case ExprContext.Address:
                case ExprContext.Box:
                    SetStackDepth(prevStack);
                    PushEvalStack(node, context);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(context);
            }

            return result;
        }

        private BoundExpression VisitExpression(BoundExpression node, ExprContext context)
        {
            BoundExpression result;
            _recursionDepth++;

            if (_recursionDepth > 1)
            {
                StackGuard.EnsureSufficientExecutionStack(_recursionDepth);

                result = VisitExpressionCore(node, context);
            }
            else
            {
                result = VisitExpressionCoreWithStackGuard(node, context);
            }

            _recursionDepth--;
            return result;
        }

        private BoundExpression VisitExpressionCoreWithStackGuard(BoundExpression node, ExprContext context)
        {
            Debug.Assert(_recursionDepth == 1);

            try
            {
                var result = VisitExpressionCore(node, context);
                Debug.Assert(_recursionDepth == 1);
                return result;
            }
            catch (InsufficientExecutionStackException ex)
            {
                throw new CancelledByStackGuardException(ex, node);
            }
        }

        protected override BoundNode VisitExpressionOrPatternWithoutStackGuard(BoundNode node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        private void PushEvalStack(BoundExpression result, ExprContext context)
        {
            Debug.Assert(result != null || context == ExprContext.None);
            _evalStack.Add((result, context));
        }

        private int StackDepth()
        {
            return _evalStack.Count;
        }

        private bool EvalStackIsEmpty()
        {
            return StackDepth() == 0;
        }

        private void SetStackDepth(int depth)
        {
            _evalStack.Clip(depth);
        }

        private void PopEvalStack()
        {
            SetStackDepth(_evalStack.Count - 1);
        }

        public BoundNode VisitStatement(BoundNode node)
        {
            Debug.Assert(node == null || EvalStackIsEmpty());
            return VisitSideEffect(node);
        }

        public BoundNode VisitSideEffect(BoundNode node)
        {
            var origStack = StackDepth();
            var prevContext = _context;

            var result = base.Visit(node);

            // prevent cross-statement local optimizations
            // when emitting debug-friendly code.
            if (_debugFriendly)
            {
                EnsureOnlyEvalStack();
            }

            _context = prevContext;
            SetStackDepth(origStack);
            _counter += 1;

            return result;
        }

        public override BoundNode VisitConversion(BoundConversion node)
        {
            var context = _context == ExprContext.Sideeffects && !node.ConversionHasSideEffects() ?
                            ExprContext.Sideeffects :
                            ExprContext.Value;

            return node.UpdateOperand(this.VisitExpression(node.Operand, context));
        }

        public override BoundNode VisitPassByCopy(BoundPassByCopy node)
        {
            var context = _context == ExprContext.Sideeffects ?
                            ExprContext.Sideeffects :
                            ExprContext.Value;

            return node.Update(
                this.VisitExpression(node.Expression, context),
                node.Type);
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            Debug.Assert(EvalStackIsEmpty(), "entering blocks when evaluation stack is not empty?");

            if (node.Instrumentation != null)
            {
                foreach (var local in node.Instrumentation.Locals)
                    DeclareLocal(local, stack: 0);
            }

            // normally we would not allow stack locals
            // when evaluation stack is not empty.
            DeclareLocals(node.Locals, stack: 0);

            return base.VisitBlock(node);
        }

        // here we have a case of indirect assignment:  *t1 = expr;
        // normally we would need to push t1 and that will cause spilling of t2
        //
        // TODO: an interesting case arises in unused x[i]++  and ++x[i] :
        //       we have trees that look like:
        //
        //  t1 = &(x[0])
        //  t2 = *t1
        //  *t1 = t2 + 1
        //
        //  t1 = &(x[0])
        //  t2 = *t1 + 1
        //  *t1 = t2
        //
        //  in these cases, we could keep t2 on stack (dev10 does).
        //  we are dealing with exactly 2 locals and access them in strict order
        //  t1, t2, t1, t2  and we are not using t2 after that.
        //  We may consider detecting exactly these cases and pretend that we do not need
        //  to push either t1 or t2 in this case.
        //
        public override BoundNode VisitSequence(BoundSequence node)
        {
            // Normally we can only use stack for local scheduling if stack is not used for evaluation.
            // In a context of a regular block that simply means that eval stack must be empty.
            // Sequences, can be entered on a nonempty evaluation stack
            // Ex:
            //      a.b = Seq{var y, y = 1, y}  // a is on the stack for the duration of the sequence.
            //
            // However evaluation stack at the entry cannot be used inside the sequence, so such stack
            // works as effective "empty" for locals declared in sequence.
            // Therefore sequence locals can be stack scheduled at same stack as at the entry to the sequence.

            // it may seem attractive to relax the stack requirement to be:
            // "all uses must agree on stack depth".
            // The following example illustrates a case where x is safely used at "declarationStack + 1"
            // Ex:
            //      Seq{var x; y.a = Seq{x = 1; x}; y}  // x is used while y is on the eval stack
            //
            // It is, however not safe assumption in general since eval stack may be accessed between usages.
            // Ex:
            //      Seq{var x; y.a = Seq{x = 1; x}; y.z = x; y} // x blocks access to y
            //

            // There is one case where we want to tweak the "use at declaration stack" rule - in the case of
            // compound assignment that involves ByRef operand captures (like:   x[y]++ ) .
            //
            // Those cases produce specific sequences of the shapes:
            //
            //      prefix:  Seq{var temp, ref operand; operand initializers; *operand = Seq{temp = (T)(operand + 1);  temp;}          result: temp}
            //      postfix: Seq{var temp, ref operand; operand initializers; *operand = Seq{temp = operand;        ;  (T)(temp + 1);} result: temp}
            //
            //  1) temp is used as the result of the sequence (and that is the only reason why it is declared in the outer sequence).
            //  2) all side-effects except the last one do not use the temp.
            //  3) last side-effect is an indirect assignment of a sequence (and target does not involve the temp).
            //
            //  Note that in a case of side-effects context, the result value will be ignored and therefore
            //  all usages of the nested temp will be confined to the nested sequence that is executed at +1 stack.
            //
            //  We will detect such case and indicate +1 as the desired stack depth at local accesses.
            //
            var declarationStack = StackDepth();

            var locals = node.Locals;
            if (!locals.IsDefaultOrEmpty)
            {
                if (_context == ExprContext.Sideeffects)
                {
                    foreach (var local in locals)
                    {
                        if (IsNestedLocalOfCompoundOperator(local, node))
                        {
                            // special case
                            DeclareLocal(local, declarationStack + 1);
                        }
                        else
                        {
                            DeclareLocal(local, declarationStack);
                        }
                    }
                }
                else
                {
                    DeclareLocals(locals, declarationStack);
                }
            }

            // rewrite operands

            var origContext = _context;

            var sideeffects = node.SideEffects;
            ArrayBuilder<BoundExpression> rewrittenSideeffects = null;
            if (!sideeffects.IsDefault)
            {
                for (int i = 0; i < sideeffects.Length; i++)
                {
                    var sideeffect = sideeffects[i];
                    var rewrittenSideeffect = this.VisitExpression(sideeffect, ExprContext.Sideeffects);

                    if (rewrittenSideeffects == null && rewrittenSideeffect != sideeffect)
                    {
                        rewrittenSideeffects = ArrayBuilder<BoundExpression>.GetInstance();
                        rewrittenSideeffects.AddRange(sideeffects, i);
                    }

                    if (rewrittenSideeffects != null)
                    {
                        rewrittenSideeffects.Add(rewrittenSideeffect);
                    }
                }
            }

            var value = this.VisitExpression(node.Value, origContext);

            return node.Update(node.Locals,
                                rewrittenSideeffects != null ?
                                    rewrittenSideeffects.ToImmutableAndFree() :
                                    sideeffects,
                                value,
                                node.Type);
        }

        // detect a pattern used in compound operators
        // where a temp is declared in the outer sequence
        // only because it must be returned, otherwise all uses are
        // confined to the nested sequence that is assigned indirectly of to an instance field (and therefore has +1 stack)
        // in such case the desired stack for this local is +1
        private bool IsNestedLocalOfCompoundOperator(LocalSymbol local, BoundSequence node)
        {
            var value = node.Value;

            // local must be used as the value of the sequence.
            if (value != null && value.Kind == BoundKind.Local && ((BoundLocal)value).LocalSymbol == local)
            {
                var sideeffects = node.SideEffects;
                var lastSideeffect = sideeffects.LastOrDefault();

                if (lastSideeffect != null)
                {
                    // last side-effect must be an indirect assignment of a sequence.
                    if (lastSideeffect.Kind == BoundKind.AssignmentOperator)
                    {
                        var assignment = (BoundAssignmentOperator)lastSideeffect;
                        if (IsIndirectOrInstanceFieldAssignment(assignment) &&
                            assignment.Right.Kind == BoundKind.Sequence)
                        {
                            // and no other side-effects should use the variable
                            var localUsedWalker = new LocalUsedWalker(local, _recursionDepth);
                            for (int i = 0; i < sideeffects.Length - 1; i++)
                            {
                                if (localUsedWalker.IsLocalUsedIn(sideeffects[i]))
                                {
                                    return false;
                                }
                            }

                            // and local is not used on the left of the assignment
                            // (extra check, but better be safe)
                            if (localUsedWalker.IsLocalUsedIn(assignment.Left))
                            {
                                return false;
                            }

                            // it should be used somewhere
                            Debug.Assert(localUsedWalker.IsLocalUsedIn(assignment.Right), "who assigns the temp?");

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private sealed class LocalUsedWalker : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
        {
            private readonly LocalSymbol _local;
            private bool _found;

            internal LocalUsedWalker(LocalSymbol local, int recursionDepth)
                : base(recursionDepth)
            {
                _local = local;
            }

            public bool IsLocalUsedIn(BoundNode node)
            {
                _found = false;
                this.Visit(node);

                return _found;
            }

            public override BoundNode Visit(BoundNode node)
            {
                if (!_found)
                {
                    return base.Visit(node);
                }

                return null;
            }

            public override BoundNode VisitLocal(BoundLocal node)
            {
                if (node.LocalSymbol == _local)
                {
                    _found = true;
                }

                return null;
            }
        }

        public override BoundNode VisitExpressionStatement(BoundExpressionStatement node)
        {
            return node.Update(
                this.VisitExpression(node.Expression, ExprContext.Sideeffects));
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            if (node.ConstantValueOpt == null)
            {
                switch (_context)
                {
                    case ExprContext.Address:
                        if (node.LocalSymbol.RefKind != RefKind.None)
                        {
                            RecordVarRead(node.LocalSymbol);
                        }
                        else
                        {
                            RecordVarRef(node.LocalSymbol);
                        }
                        break;

                    case ExprContext.AssignmentTarget:
                        Debug.Assert(_assignmentLocal == null);

                        // actual assignment will happen later, after Right is evaluated
                        // just remember what we are assigning to.
                        _assignmentLocal = node;

                        break;

                    case ExprContext.Sideeffects:
                        if (node.LocalSymbol.RefKind != RefKind.None)
                        {
                            // Reading from a ref has a side effect since the read
                            // may result in a NullReferenceException.
                            RecordVarRead(node.LocalSymbol);
                        }
                        break;

                    case ExprContext.Value:
                    case ExprContext.Box:
                        RecordVarRead(node.LocalSymbol);
                        break;
                }
            }

            return base.VisitLocal(node);
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            var sequence = node.Left as BoundSequence;
            if (sequence != null)
            {
                // assigning to a sequence is uncommon, but could happen in a
                // case if LHS was a declaration expression.
                //
                // Just rewrite {se1, se2, se3, val} = something
                // into ==>     {se1, se2, se3, val = something}
                BoundExpression rewritten = sequence.Update(sequence.Locals,
                                        sequence.SideEffects,
                                        node.Update(sequence.Value, node.Right, node.IsRef, node.Type),
                                        sequence.Type);

                rewritten = (BoundExpression)Visit(rewritten);

                // do not count the assignment twice
                _counter--;

                return rewritten;
            }

            var isIndirectAssignment = IsIndirectAssignment(node);

            var left = VisitExpression(node.Left, isIndirectAssignment ?
                                                    ExprContext.Address :
                                                    ExprContext.AssignmentTarget);

            // must delay recording a write until after RHS is evaluated
            var assignmentLocal = _assignmentLocal;
            _assignmentLocal = null;

            Debug.Assert(_context != ExprContext.AssignmentTarget, "assignment expression cannot be a target of another assignment");

            ExprContext rhsContext;
            if (node.IsRef || _context == ExprContext.Address)
            {
                // we need the address of rhs one way or another so we cannot have it on the stack.
                rhsContext = ExprContext.Address;
            }
            else
            {
                Debug.Assert(_context == ExprContext.Value ||
                             _context == ExprContext.Box ||
                             _context == ExprContext.Sideeffects, "assignment expression cannot be a target of another assignment");
                // we only need a value of rhs, so if otherwise possible it can be a stack value.
                rhsContext = ExprContext.Value;
            }

            // if right is a struct ctor, it may be optimized into in-place call
            // Such call will push the receiver ref before the arguments
            // so we need to ensure that arguments cannot use stack temps
            BoundExpression right = node.Right;
            bool mayPushReceiver = (right.Kind == BoundKind.ObjectCreationExpression &&
                right.Type.IsVerifierValue() &&
                ((BoundObjectCreationExpression)right).Constructor.ParameterCount != 0);

            if (mayPushReceiver)
            {
                // push unknown value just to prevent access to stack locals.
                PushEvalStack(null, ExprContext.None);
            }

            right = VisitExpression(node.Right, rhsContext);

            if (mayPushReceiver)
            {
                PopEvalStack();
            }

            // if assigning to a local, now it is the time to record the Write
            if (assignmentLocal != null)
            {
                // This assert will fire if code relies on implicit CLR coercions
                // - i.e assigns int value to a short local.
                // in that case we should force lhs to be a real local.
                Debug.Assert(
                    node.Left.Type.Equals(node.Right.Type, TypeCompareKind.AllIgnoreOptions) ||
                    IsFixedBufferAssignmentToRefLocal(node.Left, node.Right, node.IsRef),
                    @"type of the assignment value is not the same as the type of assignment target.
                This is not expected by the optimizer and is typically a result of a bug somewhere else.");

                Debug.Assert(!isIndirectAssignment, "indirect assignment is a read, not a write");

                LocalSymbol localSymbol = assignmentLocal.LocalSymbol;

                // If the LHS is a readonly ref and the result is used later we cannot stack
                // schedule since we may be converting a writeable value on the RHS to a readonly
                // one on the LHS.
                if (localSymbol.RefKind is RefKind.RefReadOnly or RefKindExtensions.StrictIn &&
                    (_context == ExprContext.Address || _context == ExprContext.Value))
                {
                    ShouldNotSchedule(localSymbol);
                }

                // Special Case: If the RHS is a pointer conversion, then the assignment functions as
                // a conversion (because the RHS will actually be typed as a native u/int in IL), so
                // we should not optimize away the local (i.e. schedule it on the stack).
                if (CanScheduleToStack(localSymbol) &&
                    assignmentLocal.Type.IsPointerOrFunctionPointer() && right.Kind == BoundKind.Conversion &&
                    ((BoundConversion)right).ConversionKind.IsPointerConversion())
                {
                    ShouldNotSchedule(localSymbol);
                }

                RecordVarWrite(localSymbol);
                assignmentLocal = null;
            }

            return node.Update(left, right, node.IsRef, node.Type);
        }

        /// <summary>
        /// Fixed-sized buffers are lowered as field accesses with pointer type, but
        /// we want to assign them to pinned ref locals before creating the pointer
        /// type, so this results in an assignment with mismatched types (pointer to managed
        /// ref). This is legal according to the CLR, but not how we usually represent things
        /// in lowering.
        /// </summary>
        internal static bool IsFixedBufferAssignmentToRefLocal(BoundExpression left, BoundExpression right, bool isRef)
            => isRef &&
               right is BoundFieldAccess fieldAccess &&
               fieldAccess.FieldSymbol.IsFixedSizeBuffer &&
               left.Type.Equals(((PointerTypeSymbol)right.Type).PointedAtType, TypeCompareKind.AllIgnoreOptions);

        // indirect assignment is assignment to a value referenced indirectly
        // it may only happen if
        //      1) lhs is a reference (must be a parameter or a local)
        //      2) it is not a ref/out assignment where the reference itself would be assigned
        private static bool IsIndirectAssignment(BoundAssignmentOperator node)
        {
            var lhs = node.Left;

            Debug.Assert(!node.IsRef ||
                (lhs.Kind is BoundKind.Local or BoundKind.Parameter or BoundKind.FieldAccess && lhs.GetRefKind() != RefKind.None),
                                "only ref symbols can be a target of a ref assignment");

            switch (lhs.Kind)
            {
                case BoundKind.ThisReference:
                    Debug.Assert(lhs.Type.IsValueType, "'this' is assignable only in structs");
                    return true;

                case BoundKind.Parameter:
                    if (((BoundParameter)lhs).ParameterSymbol.RefKind != RefKind.None)
                    {
                        return !node.IsRef;
                    }

                    return false;

                case BoundKind.Local:
                    if (((BoundLocal)lhs).LocalSymbol.RefKind != RefKind.None)
                    {
                        return !node.IsRef;
                    }

                    return false;

                case BoundKind.Call:
                    Debug.Assert(((BoundCall)lhs).Method.RefKind == RefKind.Ref, "only ref returning methods are assignable");
                    return true;

                case BoundKind.FunctionPointerInvocation:
                    Debug.Assert(((BoundFunctionPointerInvocation)lhs).FunctionPointer.Signature.RefKind == RefKind.Ref, "only ref returning function pointers are assignable");
                    return true;

                case BoundKind.ConditionalOperator:
                    Debug.Assert(((BoundConditionalOperator)lhs).IsRef, "only ref ternaries are assignable");
                    return true;

                case BoundKind.AssignmentOperator:
                    Debug.Assert(((BoundAssignmentOperator)lhs).IsRef, "only ref assignments are assignable");
                    return true;

                case BoundKind.Sequence:
                    Debug.Assert(!IsIndirectAssignment(node.Update(((BoundSequence)node.Left).Value, node.Right, node.IsRef, node.Type)),
                        "indirect assignment to a sequence is unexpected");
                    return false;

                case BoundKind.RefValueOperator:
                case BoundKind.PointerIndirectionOperator:
                case BoundKind.PseudoVariable:
                    return true;

                case BoundKind.ModuleVersionId:
                case BoundKind.InstrumentationPayloadRoot:
                    // these are just stores into special static fields
                    goto case BoundKind.FieldAccess;

                case BoundKind.FieldAccess:
                case BoundKind.ArrayAccess:
                    // always symbolic stores
                    return false;

                default:
                    throw ExceptionUtilities.UnexpectedValue(lhs.Kind);
            }
        }

        private static bool IsIndirectOrInstanceFieldAssignment(BoundAssignmentOperator node)
        {
            var lhs = node.Left;
            if (lhs.Kind == BoundKind.FieldAccess)
            {
                return !((BoundFieldAccess)lhs).FieldSymbol.IsStatic;
            }

            return IsIndirectAssignment(node);
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            if (node.ReceiverOpt is BoundCall receiver)
            {
                int prevStack = StackDepth();
                var calls = ArrayBuilder<BoundCall>.GetInstance();

                calls.Push(node);

                node = receiver;
                while (node.ReceiverOpt is BoundCall receiver2)
                {
                    calls.Push(node);
                    node = receiver2;
                }

                BoundExpression rewrittenReceiver = visitReceiver(node);

                while (true)
                {
                    rewrittenReceiver = visitArgumentsAndUpdateCall(node, rewrittenReceiver);

                    receiver = node;
                    if (!calls.TryPop(out node))
                    {
                        break;
                    }

                    CheckCallReceiver(receiver, node); // VisitCallOrConditionalAccessReceiver does this

                    // VisitExpressionCore does this after visiting each node
                    _counter++;
                    SetStackDepth(prevStack);
                    PushEvalStack(receiver, GetReceiverContext(receiver));
                }

                calls.Free();

                return rewrittenReceiver;
            }
            else
            {
                BoundExpression rewrittenReceiver = visitReceiver(node);
                return visitArgumentsAndUpdateCall(node, rewrittenReceiver);
            }

            BoundExpression visitReceiver(BoundCall node)
            {
                var receiver = node.ReceiverOpt;
                MethodSymbol method = node.Method;

                // matches or a bit stronger than EmitReceiverRef
                // if there are any doubts that receiver is a ref type,
                // assume we will need an address (that will prevent scheduling of receiver).
                if (method.RequiresInstanceReceiver)
                {
                    receiver = VisitCallOrConditionalAccessReceiver(receiver, node);
                }
                else
                {
                    Debug.Assert(method.IsStatic);

                    _counter += 1;

                    if ((method.IsAbstract || method.IsVirtual) && receiver is BoundTypeExpression { Type: { TypeKind: TypeKind.TypeParameter } } typeExpression)
                    {
                        receiver = typeExpression.Update(aliasOpt: null, boundContainingTypeOpt: null, boundDimensionsOpt: ImmutableArray<BoundExpression>.Empty,
                            typeWithAnnotations: typeExpression.TypeWithAnnotations, type: this.VisitType(typeExpression.Type));
                    }
                    else
                    {
                        receiver = null;
                    }
                }

                return receiver;
            }

            BoundCall visitArgumentsAndUpdateCall(BoundCall node, BoundExpression receiver)
            {
                var rewrittenArguments = VisitArguments(node.Arguments, node.Method.Parameters, node.ArgumentRefKindsOpt);
                return node.Update(receiver, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, node.Method, rewrittenArguments);
            }
        }

        private BoundExpression VisitCallOrConditionalAccessReceiver(BoundExpression receiver, BoundCall callOpt)
        {
            var receiverType = receiver.Type;

            if (callOpt is { } call)
            {
                CheckCallReceiver(receiver, call);
            }

            ExprContext context = GetReceiverContext(receiver);

            receiver = VisitExpression(receiver, context);
            return receiver;
        }

        private void CheckCallReceiver(BoundExpression receiver, BoundCall call)
        {
            if (CodeGenerator.IsRef(receiver) &&
                                CodeGenerator.IsPossibleReferenceTypeReceiverOfConstrainedCall(receiver) &&
                                !CodeGenerator.IsSafeToDereferenceReceiverRefAfterEvaluatingArguments(call.Arguments))
            {
                var unwrappedSequence = receiver;

                while (unwrappedSequence is BoundSequence sequence)
                {
                    unwrappedSequence = sequence.Value;
                }

                if (unwrappedSequence is BoundLocal { LocalSymbol: { RefKind: not RefKind.None } localSymbol })
                {
                    ShouldNotSchedule(localSymbol); // Otherwise CodeGenerator is unable to apply proper fixups
                }
            }
        }

        private static ExprContext GetReceiverContext(BoundExpression receiver)
        {
            var receiverType = receiver.Type;
            ExprContext context;

            if (receiverType.IsReferenceType)
            {
                if (receiverType.IsTypeParameter())
                {
                    // type param receiver that we statically know is a reference will be boxed
                    context = ExprContext.Box;
                }
                else
                {
                    // reference receivers will be used as values
                    context = ExprContext.Value;
                }
            }
            else
            {
                // everything else will get an address taken
                context = ExprContext.Address;
            }

            return context;
        }

        private ImmutableArray<BoundExpression> VisitArguments(ImmutableArray<BoundExpression> arguments, ImmutableArray<ParameterSymbol> parameters, ImmutableArray<RefKind> argRefKindsOpt)
        {
            Debug.Assert(!arguments.IsDefault);
            Debug.Assert(!parameters.IsDefault);
            // If this is a varargs method then there will be one additional argument for the __arglist().
            Debug.Assert(arguments.Length == parameters.Length || arguments.Length == parameters.Length + 1);

            ArrayBuilder<BoundExpression> rewrittenArguments = null;
            for (int i = 0; i < arguments.Length; i++)
            {
                RefKind argRefKind = CodeGenerator.GetArgumentRefKind(arguments, parameters, argRefKindsOpt, i);
                VisitArgument(arguments, ref rewrittenArguments, i, argRefKind);
            }

            return rewrittenArguments != null ? rewrittenArguments.ToImmutableAndFree() : arguments;
        }

        private void VisitArgument(ImmutableArray<BoundExpression> arguments, ref ArrayBuilder<BoundExpression> rewrittenArguments, int i, RefKind argRefKind)
        {
            ExprContext context = (argRefKind == RefKind.None) ? ExprContext.Value : ExprContext.Address;

            var arg = arguments[i];
            BoundExpression rewrittenArg = VisitExpression(arg, context);

            if (rewrittenArguments == null && arg != rewrittenArg)
            {
                rewrittenArguments = ArrayBuilder<BoundExpression>.GetInstance();
                rewrittenArguments.AddRange(arguments, i);
            }

            if (rewrittenArguments != null)
            {
                rewrittenArguments.Add(rewrittenArg);
            }
        }

        public override BoundNode VisitArgListOperator(BoundArgListOperator node)
        {

            ArrayBuilder<BoundExpression> rewrittenArguments = null;
            ImmutableArray<BoundExpression> arguments = node.Arguments;
            ImmutableArray<RefKind> argRefKindsOpt = node.ArgumentRefKindsOpt;

            for (int i = 0; i < arguments.Length; i++)
            {
                RefKind refKind = argRefKindsOpt.IsDefaultOrEmpty ? RefKind.None : argRefKindsOpt[i];
                VisitArgument(arguments, ref rewrittenArguments, i, refKind);
            }

            return node.Update(rewrittenArguments?.ToImmutableAndFree() ?? arguments, argRefKindsOpt, node.Type);
        }

        public override BoundNode VisitMakeRefOperator(BoundMakeRefOperator node)
        {
            // The __makeref(x) operator is logically like calling a method
            // static TypedReference MakeReference(ref T x)

            var rewrittenOperand = VisitExpression(node.Operand, ExprContext.Address);
            return node.Update(rewrittenOperand, node.Type);
        }

        public override BoundNode VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            var constructor = node.Constructor;
            var rewrittenArguments = VisitArguments(node.Arguments, constructor.Parameters, node.ArgumentRefKindsOpt);
            Debug.Assert(node.InitializerExpressionOpt == null);

            return node.Update(constructor, rewrittenArguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt,
                node.Expanded, node.ArgsToParamsOpt, node.DefaultArguments, node.ConstantValueOpt, initializerExpressionOpt: null, node.Type);
        }

        public override BoundNode VisitArrayAccess(BoundArrayAccess node)
        {
            // regardless of purpose, array access visits its children as values
            // TODO: do we need to save/restore old context here?
            var oldContext = _context;
            _context = ExprContext.Value;

            var result = base.VisitArrayAccess(node);

            _context = oldContext;
            return result;
        }

        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            var field = node.FieldSymbol;
            var receiver = node.ReceiverOpt;

            // if there are any doubts that receiver is a ref type,
            // assume we will need an address. (that will prevent scheduling of receiver).
            if (!field.IsStatic)
            {
                if (receiver.Type.IsTypeParameter())
                {
                    // type parameters must be boxed to access fields.
                    receiver = VisitExpression(receiver, ExprContext.Box);
                }
                else
                {
                    // need address when assigning to a field and receiver is not a reference
                    //              when accessing a field of a struct unless we only need Value and Value is preferred.
                    if (receiver.Type.IsValueType && (
                            _context == ExprContext.AssignmentTarget ||
                            _context == ExprContext.Address ||
                            CodeGenerator.FieldLoadMustUseRef(receiver)))
                    {
                        receiver = VisitExpression(receiver, ExprContext.Address);
                    }
                    else
                    {
                        receiver = VisitExpression(receiver, ExprContext.Value);
                    }
                }
            }
            else
            {
                // for some reason it could be not null even if field is static...
                //       it seems wrong
                _counter += 1;
                receiver = null;
            }

            return node.Update(receiver, field, node.ConstantValueOpt, node.ResultKind, node.Type);
        }

        public override BoundNode VisitLabelStatement(BoundLabelStatement node)
        {
            RecordLabel(node.Label);
            return base.VisitLabelStatement(node);
        }

        public override BoundNode VisitLabel(BoundLabel node)
        {
            Debug.Assert(false, "we should not have label expressions at this stage");
            return node;
        }

        public override BoundNode VisitIsPatternExpression(BoundIsPatternExpression node)
        {
            Debug.Assert(false, "we should not have is-pattern expressions at this stage");
            return node;
        }

        public override BoundNode VisitGotoStatement(BoundGotoStatement node)
        {
            Debug.Assert(node.CaseExpressionOpt == null, "we should not have label expressions at this stage");

            var result = base.VisitGotoStatement(node);
            RecordBranch(node.Label);

            return result;
        }

        public override BoundNode VisitConditionalGoto(BoundConditionalGoto node)
        {
            var result = base.VisitConditionalGoto(node);
            PopEvalStack();  // condition gets consumed.
            RecordBranch(node.Label);

            return result;
        }

        public override BoundNode VisitSwitchDispatch(BoundSwitchDispatch node)
        {
            Debug.Assert(EvalStackIsEmpty());

            // switch dispatch needs a byval local or a parameter as a key.
            // if this is already a fitting local, let's keep it that way
            BoundExpression boundExpression = node.Expression;
            if (boundExpression.Kind == BoundKind.Local)
            {
                var localSym = ((BoundLocal)boundExpression).LocalSymbol;
                if (localSym.RefKind == RefKind.None)
                {
                    ShouldNotSchedule(localSym);
                }
            }

            boundExpression = (BoundExpression)this.Visit(boundExpression);

            // expression value is consumed by the switch
            PopEvalStack();

            // implicit control flow
            EnsureOnlyEvalStack();

            RecordBranch(node.DefaultLabel);
            foreach ((_, LabelSymbol label) in node.Cases)
            {
                RecordBranch(label);
            }

            return node.Update(boundExpression, node.Cases, node.DefaultLabel, node.LengthBasedStringSwitchDataOpt);
        }

        public override BoundNode VisitConditionalOperator(BoundConditionalOperator node)
        {
            var origStack = StackDepth();
            BoundExpression condition = this.VisitExpression(node.Condition, ExprContext.Value);

            var cookie = GetStackStateCookie();  // implicit goto here

            var context = node.IsRef ? ExprContext.Address : ExprContext.Value;

            SetStackDepth(origStack);  // consequence is evaluated with original stack
            BoundExpression consequence = this.VisitExpression(node.Consequence, context);

            EnsureStackState(cookie);   // implicit label here

            SetStackDepth(origStack);  // alternative is evaluated with original stack
            BoundExpression alternative = this.VisitExpression(node.Alternative, context);

            EnsureStackState(cookie);   // implicit label here

            return node.Update(node.IsRef, condition, consequence, alternative, node.ConstantValueOpt, node.NaturalTypeOpt, node.WasCompilerGenerated, node.Type);
        }

        public override BoundNode VisitBinaryOperator(BoundBinaryOperator node)
        {
            BoundExpression child = node.Left;

            if (child.Kind != BoundKind.BinaryOperator || child.ConstantValueOpt != null)
            {
                return VisitBinaryOperatorSimple(node);
            }

            // Do not blow the stack due to a deep recursion on the left.
            var stack = ArrayBuilder<BoundBinaryOperator>.GetInstance();
            stack.Push(node);

            BoundBinaryOperator binary = (BoundBinaryOperator)child;

            while (true)
            {
                stack.Push(binary);
                child = binary.Left;

                if (child.Kind != BoundKind.BinaryOperator || child.ConstantValueOpt != null)
                {
                    break;
                }

                binary = (BoundBinaryOperator)child;
            }

            var prevContext = _context;
            int prevStack = StackDepth();

            var left = (BoundExpression)this.Visit(child);

            while (true)
            {
                binary = stack.Pop();

                var isLogical = (binary.OperatorKind & BinaryOperatorKind.Logical) != 0;

                object cookie = null;
                if (isLogical)
                {
                    cookie = GetStackStateCookie();     // implicit branch here
                    SetStackDepth(prevStack);  // right is evaluated with original stack
                }

                var right = (BoundExpression)this.Visit(binary.Right);

                if (isLogical)
                {
                    EnsureStackState(cookie);   // implicit label here
                }

                var type = this.VisitType(binary.Type);
                left = binary.Update(binary.OperatorKind, binary.ConstantValueOpt, binary.Method, binary.ConstrainedToType, binary.ResultKind, left, right, type);

                if (stack.Count == 0)
                {
                    break;
                }

                _context = prevContext;
                _counter += 1;
                SetStackDepth(prevStack);
                PushEvalStack(binary, ExprContext.Value);
            }

            Debug.Assert((object)binary == node);
            stack.Free();

            return left;
        }

        private BoundNode VisitBinaryOperatorSimple(BoundBinaryOperator node)
        {
            var isLogical = (node.OperatorKind & BinaryOperatorKind.Logical) != 0;
            if (isLogical)
            {
                var origStack = StackDepth();
                BoundExpression left = (BoundExpression)this.Visit(node.Left);

                var cookie = GetStackStateCookie();     // implicit branch here

                SetStackDepth(origStack);  // right is evaluated with original stack
                BoundExpression right = (BoundExpression)this.Visit(node.Right);

                EnsureStackState(cookie);   // implicit label here

                return node.Update(node.OperatorKind, node.ConstantValueOpt, node.Method, node.ConstrainedToType, node.ResultKind, left, right, node.Type);
            }

            return base.VisitBinaryOperator(node);
        }

        public override BoundNode VisitNullCoalescingOperator(BoundNullCoalescingOperator node)
        {
            Debug.Assert(node.LeftPlaceholder is null);
            Debug.Assert(node.LeftConversion is null);
            var origStack = StackDepth();
            BoundExpression left = (BoundExpression)this.Visit(node.LeftOperand);

            var cookie = GetStackStateCookie();     // implicit branch here

            // right is evaluated with original stack
            // (this is not entirely true, codegen may keep left on the stack as an ephemeral temp, but that is irrelevant here)
            SetStackDepth(origStack);
            BoundExpression right = (BoundExpression)this.Visit(node.RightOperand);

            EnsureStackState(cookie);   // implicit label here

            return node.Update(left, right, node.LeftPlaceholder, node.LeftConversion, node.OperatorResultKind, @checked: node.Checked, node.Type);
        }

        public override BoundNode VisitLoweredConditionalAccess(BoundLoweredConditionalAccess node)
        {
            var origStack = StackDepth();
            BoundExpression receiver = VisitCallOrConditionalAccessReceiver(node.Receiver, callOpt: null);

            var cookie = GetStackStateCookie();     // implicit branch here

            // right is evaluated with original stack
            // (this is not entirely true, codegen will keep receiver on the stack, but that is irrelevant here)
            SetStackDepth(origStack);
            BoundExpression whenNotNull = (BoundExpression)this.Visit(node.WhenNotNull);

            EnsureStackState(cookie);   // implicit label here

            var whenNull = node.WhenNullOpt;
            if (whenNull != null)
            {
                SetStackDepth(origStack);  // whenNull is evaluated with original stack
                whenNull = (BoundExpression)this.Visit(whenNull);
                EnsureStackState(cookie);   // implicit label here
            }
            else
            {
                // compensate for the whenNull that we are not visiting.
                _counter += 1;
            }

            return node.Update(receiver, node.HasValueMethodOpt, whenNotNull, whenNull, node.Id, node.ForceCopyOfNullableValueType, node.Type);
        }

        public override BoundNode VisitComplexConditionalReceiver(BoundComplexConditionalReceiver node)
        {
            EnsureOnlyEvalStack();

            var origStack = StackDepth();

            PushEvalStack(null, ExprContext.None);

            var cookie = GetStackStateCookie(); // implicit goto here

            SetStackDepth(origStack); // consequence is evaluated with original stack
            var valueTypeReceiver = (BoundExpression)this.Visit(node.ValueTypeReceiver);

            EnsureStackState(cookie); // implicit label here

            SetStackDepth(origStack); // alternative is evaluated with original stack 

            var unwrappedSequence = node.ReferenceTypeReceiver;

            while (unwrappedSequence is BoundSequence sequence)
            {
                unwrappedSequence = sequence.Value;
            }

            if (unwrappedSequence is BoundLocal { LocalSymbol: { } localSymbol })
            {
                ShouldNotSchedule(localSymbol);
            }

            var referenceTypeReceiver = (BoundExpression)this.Visit(node.ReferenceTypeReceiver);

            EnsureStackState(cookie); // implicit label here

            return node.Update(valueTypeReceiver, referenceTypeReceiver, node.Type);
        }

        public override BoundNode VisitUnaryOperator(BoundUnaryOperator node)
        {
            // checked(-x) is emitted as "0 - x"
            if (node.OperatorKind.IsChecked() && node.OperatorKind.Operator() == UnaryOperatorKind.UnaryMinus)
            {
                var origStack = StackDepth();
                PushEvalStack(new BoundDefaultExpression(node.Syntax, node.Operand.Type), ExprContext.Value);
                BoundExpression operand = (BoundExpression)this.Visit(node.Operand);
                return node.Update(node.OperatorKind, operand, node.ConstantValueOpt, node.MethodOpt, node.ConstrainedToTypeOpt, node.ResultKind, node.Type);
            }
            else
            {
                return base.VisitUnaryOperator(node);
            }
        }

        public override BoundNode VisitTryStatement(BoundTryStatement node)
        {
            EnsureOnlyEvalStack();
            var tryBlock = (BoundBlock)this.Visit(node.TryBlock);

            var catchBlocks = this.VisitList(node.CatchBlocks);

            EnsureOnlyEvalStack();
            var finallyBlock = (BoundBlock)this.Visit(node.FinallyBlockOpt);

            EnsureOnlyEvalStack();

            return node.Update(tryBlock, catchBlocks, finallyBlock, finallyLabelOpt: node.FinallyLabelOpt, node.PreferFaultHandler);
        }

        public override BoundNode VisitCatchBlock(BoundCatchBlock node)
        {
            EnsureOnlyEvalStack();

            var exceptionSourceOpt = node.ExceptionSourceOpt;
            DeclareLocals(node.Locals, stack: 0);

            if (exceptionSourceOpt != null)
            {
                // runtime pushes the exception object
                PushEvalStack(null, ExprContext.None);
                _counter++;

                // We consume it by writing into the exception source.
                if (exceptionSourceOpt.Kind == BoundKind.Local)
                {
                    RecordVarWrite(((BoundLocal)exceptionSourceOpt).LocalSymbol);
                }
                else
                {
                    int prevStack = StackDepth();
                    exceptionSourceOpt = VisitExpression(exceptionSourceOpt, ExprContext.AssignmentTarget);
                    _assignmentLocal = null; // not using this for exceptionSource
                    SetStackDepth(prevStack);
                }

                PopEvalStack();
                _counter++;
            }

            BoundStatementList filterPrologue;
            if (node.ExceptionFilterPrologueOpt != null)
            {
                EnsureOnlyEvalStack();
                filterPrologue = (BoundStatementList)this.Visit(node.ExceptionFilterPrologueOpt);
            }
            else
            {
                filterPrologue = null;
            }

            BoundExpression boundFilter;
            if (node.ExceptionFilterOpt != null)
            {
                boundFilter = (BoundExpression)this.Visit(node.ExceptionFilterOpt);

                // the value of filter expression is consumed by the VM
                PopEvalStack();
                _counter++;

                // variables allocated on stack in a filter can't be used in the catch handler
                EnsureOnlyEvalStack();
            }
            else
            {
                boundFilter = null;
            }

            var boundBlock = (BoundBlock)this.Visit(node.Body);
            var exceptionTypeOpt = this.VisitType(node.ExceptionTypeOpt);

            return node.Update(node.Locals, exceptionSourceOpt, exceptionTypeOpt, filterPrologue, boundFilter, boundBlock, node.IsSynthesizedAsyncCatchAll);
        }

        public override BoundNode VisitConvertedStackAllocExpression(BoundConvertedStackAllocExpression node)
        {
            // CLI spec section 3.47 requires that the stack be empty when localloc occurs.
            EnsureOnlyEvalStack();
            return base.VisitConvertedStackAllocExpression(node);
        }

        public override BoundNode VisitArrayInitialization(BoundArrayInitialization node)
        {
            // nontrivial construct - may use dups, metadata blob helpers etc..
            EnsureOnlyEvalStack();

            var initializers = node.Initializers;
            ArrayBuilder<BoundExpression> rewrittenInitializers = null;
            if (!initializers.IsDefault)
            {
                for (int i = 0; i < initializers.Length; i++)
                {
                    // array itself will be pushed on the stack here.
                    EnsureOnlyEvalStack();

                    var initializer = initializers[i];
                    var rewrittenInitializer = this.VisitExpression(initializer, ExprContext.Value);

                    if (rewrittenInitializers == null && rewrittenInitializer != initializer)
                    {
                        rewrittenInitializers = ArrayBuilder<BoundExpression>.GetInstance();
                        rewrittenInitializers.AddRange(initializers, i);
                    }

                    if (rewrittenInitializers != null)
                    {
                        rewrittenInitializers.Add(rewrittenInitializer);
                    }
                }
            }

            return node.Update(rewrittenInitializers != null ?
                                    rewrittenInitializers.ToImmutableAndFree() :
                                    initializers);
        }

        public override BoundNode VisitAddressOfOperator(BoundAddressOfOperator node)
        {
            BoundExpression visitedOperand = this.VisitExpression(node.Operand, ExprContext.Address);
            return node.Update(visitedOperand, node.IsManaged, node.Type);
        }

        public override BoundNode VisitReturnStatement(BoundReturnStatement node)
        {
            BoundExpression expressionOpt = (BoundExpression)this.Visit(node.ExpressionOpt);

            // must not have locals on stack when returning
            EnsureOnlyEvalStack();

            return node.Update(node.RefKind, expressionOpt, @checked: node.Checked);
        }

        // Ensures that there are no stack locals.
        // It is done by accessing virtual "empty" local that is at the bottom of all stack locals.
        private void EnsureOnlyEvalStack()
        {
            RecordVarRead(empty);
        }

        private object GetStackStateCookie()
        {
            // create a dummy and start tracing it
            var dummy = new DummyLocal();
            _dummyVariables.Add(dummy, dummy);
            _locals.Add(dummy, LocalDefUseInfo.GetInstance(StackDepth()));
            RecordDummyWrite(dummy);

            return dummy;
        }

        private void EnsureStackState(object cookie)
        {
            RecordVarRead(_dummyVariables[cookie]);
        }

        // called on branches and labels
        private void RecordBranch(LabelSymbol label)
        {
            DummyLocal dummy;
            if (_dummyVariables.TryGetValue(label, out dummy))
            {
                RecordVarRead(dummy);
            }
            else
            {
                // create a dummy and start tracing it
                dummy = new DummyLocal();
                _dummyVariables.Add(label, dummy);
                _locals.Add(dummy, LocalDefUseInfo.GetInstance(StackDepth()));
                RecordDummyWrite(dummy);
            }
        }

        private void RecordLabel(LabelSymbol label)
        {
            DummyLocal dummy;
            if (_dummyVariables.TryGetValue(label, out dummy))
            {
                RecordVarRead(dummy);
            }
            else
            {
                // this is a backwards jump with nontrivial stack requirements.
                // just use empty.
                dummy = empty;
                _dummyVariables.Add(label, dummy);
                RecordVarRead(dummy);
            }
        }

        private void ShouldNotSchedule(LocalSymbol localSymbol)
        {
            LocalDefUseInfo localDefInfo;
            if (_locals.TryGetValue(localSymbol, out localDefInfo))
            {
                localDefInfo.ShouldNotSchedule();
            }
        }

        private void RecordVarRef(LocalSymbol local)
        {
            Debug.Assert(local.RefKind == RefKind.None, "cannot take a ref of a ref");

            if (!CanScheduleToStack(local))
            {
                return;
            }

            // if we ever take a reference of a local, it must be a real local.
            ShouldNotSchedule(local);
        }

        private void RecordVarRead(LocalSymbol local)
        {
            if (!CanScheduleToStack(local))
            {
                return;
            }

            var locInfo = _locals[local];

            if (locInfo.CannotSchedule)
            {
                return;
            }

            var defs = locInfo.LocalDefs;
            if (defs.Count == 0)
            {
                //reading before writing.
                locInfo.ShouldNotSchedule();
                return;
            }

            // if accessing real val, check stack
            if (local.SynthesizedKind != SynthesizedLocalKind.OptimizerTemp)
            {
                if (locInfo.StackAtDeclaration != StackDepth() &&
                    !EvalStackHasLocal(local))
                {
                    //reading at different eval stack.
                    locInfo.ShouldNotSchedule();
                    return;
                }
            }
            else
            {
                // dummy must be accessed on same stack.
                Debug.Assert(local == empty || locInfo.StackAtDeclaration == StackDepth());
            }

            var last = defs.Count - 1;
            defs[last] = defs[last].WithEnd(_counter);

            var nextDef = new LocalDefUseSpan(_counter);
            defs.Add(nextDef);
        }

        private bool EvalStackHasLocal(LocalSymbol local)
        {
            var top = _evalStack.Last();

            return top.Item2 == (local.RefKind == RefKind.None ? ExprContext.Value : ExprContext.Address) &&
                   top.Item1.Kind == BoundKind.Local &&
                   ((BoundLocal)top.Item1).LocalSymbol == local;
        }

        private void RecordDummyWrite(LocalSymbol local)
        {
            Debug.Assert(local.SynthesizedKind == SynthesizedLocalKind.OptimizerTemp);

            var locInfo = _locals[local];

            // dummy must be accessed on same stack.
            Debug.Assert(local == empty || locInfo.StackAtDeclaration == StackDepth());

            var locDef = new LocalDefUseSpan(_counter);
            locInfo.LocalDefs.Add(locDef);
        }

        private void RecordVarWrite(LocalSymbol local)
        {
            Debug.Assert(local.SynthesizedKind != SynthesizedLocalKind.OptimizerTemp);

            if (!CanScheduleToStack(local))
            {
                return;
            }

            var locInfo = _locals[local];
            if (locInfo.CannotSchedule)
            {
                return;
            }

            // check stack
            // -1 because real assignment "consumes, assigns, and then pushes back" the value.
            var evalStack = StackDepth() - 1;

            if (locInfo.StackAtDeclaration != evalStack)
            {
                //writing at different eval stack.
                locInfo.ShouldNotSchedule();
                return;
            }

            var locDef = new LocalDefUseSpan(_counter);
            locInfo.LocalDefs.Add(locDef);
        }

        private bool CanScheduleToStack(LocalSymbol local)
        {
            return local.CanScheduleToStack &&
                (!_debugFriendly || !local.SynthesizedKind.IsLongLived());
        }

        private void DeclareLocals(ImmutableArray<LocalSymbol> locals, int stack)
        {
            foreach (var local in locals)
            {
                DeclareLocal(local, stack);
            }
        }

        private void DeclareLocal(LocalSymbol local, int stack)
        {
            if ((object)local != null)
            {
                if (CanScheduleToStack(local))
                {
                    LocalDefUseInfo info;
                    if (!_locals.TryGetValue(local, out info))
                    {
                        _locals.Add(local, LocalDefUseInfo.GetInstance(stack));
                    }
                    else
                    {
                        Debug.Assert(local.SynthesizedKind == SynthesizedLocalKind.LoweringTemp, "only lowering temps may be sometimes reused");
                        if (info.StackAtDeclaration != stack)
                        {
                            info.ShouldNotSchedule();
                        }
                    }
                }
            }
        }
    }

    // Rewrites the tree to account for destructive nature of stack local reads.
    //
    // Typically, last read stays as-is and local is destroyed by the read.
    // Intermediate reads are rewritten as Dups -
    //
    //              NotLastUse(X_stackLocal) ===> NotLastUse(Dup)
    //              LastUse(X_stackLocal) ===> LastUse(X_stackLocal)
    //
    internal sealed class StackOptimizerPass2 : BoundTreeRewriterWithStackGuard
    {
        private int _nodeCounter;
        private readonly Dictionary<LocalSymbol, LocalDefUseInfo> _info;

        private StackOptimizerPass2(Dictionary<LocalSymbol, LocalDefUseInfo> info)
        {
            _info = info;
        }

        public static BoundStatement Rewrite(BoundStatement src, Dictionary<LocalSymbol, LocalDefUseInfo> info)
        {
            var scheduler = new StackOptimizerPass2(info);
            return (BoundStatement)scheduler.Visit(src);
        }

        public override BoundNode Visit(BoundNode node)
        {
            BoundNode result;

            // rewriting constants may undo constant folding and make thing worse.
            // so we will not go into constant nodes.
            // CodeGen will not do that either.
            var asExpression = node as BoundExpression;
            if (asExpression != null && asExpression.ConstantValueOpt != null)
            {
                result = node;
            }
            else
            {
                result = base.Visit(node);
            }

            _nodeCounter += 1;

            return result;
        }

        public override BoundNode VisitBinaryOperator(BoundBinaryOperator node)
        {
            BoundExpression child = node.Left;

            if (child.Kind != BoundKind.BinaryOperator || child.ConstantValueOpt != null)
            {
                return base.VisitBinaryOperator(node);
            }

            // Do not blow the stack due to a deep recursion on the left.
            var stack = ArrayBuilder<BoundBinaryOperator>.GetInstance();
            stack.Push(node);

            BoundBinaryOperator binary = (BoundBinaryOperator)child;

            while (true)
            {
                stack.Push(binary);
                child = binary.Left;

                if (child.Kind != BoundKind.BinaryOperator || child.ConstantValueOpt != null)
                {
                    break;
                }

                binary = (BoundBinaryOperator)child;
            }

            var left = (BoundExpression)this.Visit(child);

            while (true)
            {
                binary = stack.Pop();
                var right = (BoundExpression)this.Visit(binary.Right);
                var type = this.VisitType(binary.Type);
                left = binary.Update(binary.OperatorKind, binary.ConstantValueOpt, binary.Method, binary.ConstrainedToType, binary.ResultKind, left, right, type);

                if (stack.Count == 0)
                {
                    break;
                }

                _nodeCounter += 1;
            }

            Debug.Assert((object)binary == node);
            stack.Free();

            return left;
        }

        private static bool IsLastAccess(LocalDefUseInfo locInfo, int counter)
        {
            return locInfo.LocalDefs.Any((d) => counter == d.Start && counter == d.End);
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            LocalDefUseInfo locInfo;
            if (!_info.TryGetValue(node.LocalSymbol, out locInfo))
            {
                return base.VisitLocal(node);
            }

            // not the last access, emit Dup.
            if (!IsLastAccess(locInfo, _nodeCounter))
            {
                return new BoundDup(node.Syntax, node.LocalSymbol.RefKind, node.Type);
            }

            // last access - leave the node as is. Emit will do nothing expecting the node on the stack
            return base.VisitLocal(node);
        }

        public override BoundNode VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            ImmutableArray<BoundExpression> arguments = this.VisitList(node.Arguments);
            Debug.Assert(node.InitializerExpressionOpt == null);
            TypeSymbol type = this.VisitType(node.Type);
            return node.Update(node.Constructor, arguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt, node.Expanded, node.ArgsToParamsOpt, node.DefaultArguments, node.ConstantValueOpt, initializerExpressionOpt: null, type);
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            LocalDefUseInfo locInfo;
            var left = node.Left as BoundLocal;

            // store to something that is not special. (operands still could be rewritten)
            if (left == null || !_info.TryGetValue(left.LocalSymbol, out locInfo))
            {
                return base.VisitAssignmentOperator(node);
            }

            // indirect local store is not special. (operands still could be rewritten)
            // NOTE: if lhs is a stack local, it will be handled as a read and possibly duped.
            var isIndirectLocalStore = left.LocalSymbol.RefKind != RefKind.None && !node.IsRef;
            if (isIndirectLocalStore)
            {
                return base.VisitAssignmentOperator(node);
            }

            // ==  here we have a regular write to a stack local
            //
            // we do not need to visit lhs, because we do not read the local,
            // just update the counter to be in sync.
            //
            // if this is the last store, we just push the rhs
            // otherwise record a store.

            // fake visiting of left
            _nodeCounter += 1;

            // visit right
            var right = (BoundExpression)Visit(node.Right);

            // do actual assignment

            Debug.Assert(locInfo.LocalDefs.Any((d) => _nodeCounter == d.Start && _nodeCounter <= d.End));
            var isLast = IsLastAccess(locInfo, _nodeCounter);

            if (isLast)
            {
                if (node.IsRef &&
                    !node.WasCompilerGenerated &&
                    left.LocalSymbol.RefKind == RefKind.Ref &&
                    right is BoundArrayAccess arrayAccess &&
                    // Value types do not need runtime element type checks.
                    !arrayAccess.Type.IsValueType)
                {
                    return new BoundRefArrayAccess(arrayAccess.Syntax, arrayAccess);
                }

                // assigned local is not used later => just emit the Right
                return right;
            }
            else
            {
                // assigned local used later - keep assignment.
                // codegen will keep value on stack when sees assignment "stackLocal = expr"
                return node.Update(left, right, node.IsRef, node.Type);
            }
        }

#nullable enable
        public override BoundNode VisitCall(BoundCall node)
        {
            if (node.ReceiverOpt is BoundCall receiver1)
            {
                var calls = ArrayBuilder<BoundCall>.GetInstance();

                calls.Push(node);

                node = receiver1;
                while (node.ReceiverOpt is BoundCall receiver2)
                {
                    calls.Push(node);
                    node = receiver2;
                }

                BoundExpression? rewrittenReceiver = visitReceiver(node);

                while (true)
                {
                    rewrittenReceiver = visitArgumentsAndUpdateCall(node, rewrittenReceiver);

                    if (!calls.TryPop(out node!))
                    {
                        break;
                    }

                    // Visit does this after visiting each node
                    _nodeCounter++;
                }

                calls.Free();

                return rewrittenReceiver;
            }
            else
            {
                BoundExpression? rewrittenReceiver = visitReceiver(node);
                return visitArgumentsAndUpdateCall(node, rewrittenReceiver);
            }

            BoundExpression? visitReceiver(BoundCall node)
            {
                BoundExpression? receiverOpt = node.ReceiverOpt;

                if (node.Method.RequiresInstanceReceiver)
                {
                    receiverOpt = (BoundExpression?)this.Visit(receiverOpt);
                }
                else
                {
                    _nodeCounter++;

                    if (receiverOpt is BoundTypeExpression { AliasOpt: null, BoundContainingTypeOpt: null, BoundDimensionsOpt: { IsEmpty: true }, Type: { TypeKind: TypeKind.TypeParameter } } typeExpression)
                    {
                        receiverOpt = typeExpression.Update(aliasOpt: null, boundContainingTypeOpt: null, boundDimensionsOpt: ImmutableArray<BoundExpression>.Empty,
                            typeWithAnnotations: typeExpression.TypeWithAnnotations, type: this.VisitType(typeExpression.Type));
                    }
                    else if (receiverOpt is not null)
                    {
                        throw ExceptionUtilities.Unreachable();
                    }
                }

                return receiverOpt;
            }

            BoundExpression visitArgumentsAndUpdateCall(BoundCall node, BoundExpression? receiverOpt)
            {
                ImmutableArray<BoundExpression> arguments = this.VisitList(node.Arguments);
                TypeSymbol? type = this.VisitType(node.Type);
                return node.Update(receiverOpt, node.InitialBindingReceiverIsSubjectToCloning, node.Method, arguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt, node.IsDelegateCall, node.Expanded, node.InvokedAsExtensionMethod, node.ArgsToParamsOpt, node.DefaultArguments, node.ResultKind, node.OriginalMethodsOpt, type);
            }
        }
#nullable disable

        public override BoundNode VisitCatchBlock(BoundCatchBlock node)
        {
            var exceptionSource = node.ExceptionSourceOpt;
            var type = node.ExceptionTypeOpt;
            var filterPrologue = node.ExceptionFilterPrologueOpt;
            var filter = node.ExceptionFilterOpt;
            var body = node.Body;

            if (exceptionSource != null)
            {
                // runtime pushes the exception object
                _nodeCounter++;

                if (exceptionSource.Kind == BoundKind.Local)
                {
                    var sourceLocal = ((BoundLocal)exceptionSource).LocalSymbol;
                    LocalDefUseInfo locInfo;

                    // If catch is the last access, we do not need to store the exception object.
                    if (_info.TryGetValue(sourceLocal, out locInfo) &&
                        IsLastAccess(locInfo, _nodeCounter))
                    {
                        exceptionSource = null;
                    }
                }
                else
                {
                    exceptionSource = (BoundExpression)Visit(exceptionSource);
                }

                // we consume it by writing into the local
                _nodeCounter++;
            }

            filterPrologue = (filterPrologue != null) ? (BoundStatementList)this.Visit(filterPrologue) : null;

            if (filter != null)
            {
                filter = (BoundExpression)this.Visit(filter);

                // the value of filter expression is consumed by the VM
                _nodeCounter++;
            }

            body = (BoundBlock)this.Visit(body);
            type = this.VisitType(type);

            return node.Update(node.Locals, exceptionSource, type, filterPrologue, filter, body, node.IsSynthesizedAsyncCatchAll);
        }
    }

    internal sealed class DummyLocal : LocalSymbol
    {
        internal override bool IsImportedFromMetadata
        {
            get { return false; }
        }

        internal override LocalDeclarationKind DeclarationKind
        {
            get { return LocalDeclarationKind.None; }
        }

        internal override SynthesizedLocalKind SynthesizedKind
        {
            get { return SynthesizedLocalKind.OptimizerTemp; }
        }

        internal override SyntaxNode ScopeDesignatorOpt
        {
            get { return null; }
        }

        internal override LocalSymbol WithSynthesizedLocalKindAndSyntax(
            SynthesizedLocalKind kind, SyntaxNode syntax
#if DEBUG
            ,
            [CallerLineNumber] int createdAtLineNumber = 0,
            [CallerFilePath] string createdAtFilePath = null
#endif
            )
        {
            throw new NotImplementedException();
        }

        internal override SyntaxToken IdentifierToken
        {
            get { return default(SyntaxToken); }
        }

        internal override bool IsPinned
        {
            get { return false; }
        }

        internal override bool IsKnownToReferToTempIfReferenceType
        {
            get { return false; }
        }

        public override Symbol ContainingSymbol
        {
            get { throw new NotImplementedException(); }
        }

        public override TypeWithAnnotations TypeWithAnnotations
        {
            get { throw new NotImplementedException(); }
        }

        public override ImmutableArray<Location> Locations
        {
            get { throw new NotImplementedException(); }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { throw new NotImplementedException(); }
        }

        internal override ConstantValue GetConstantValue(SyntaxNode node, LocalSymbol inProgress, BindingDiagnosticBag diagnostics)
        {
            throw new NotImplementedException();
        }

        internal override bool IsCompilerGenerated
        {
            get { return true; }
        }

        internal override ReadOnlyBindingDiagnostic<AssemblySymbol> GetConstantValueDiagnostics(BoundExpression boundInitValue)
        {
            throw new NotImplementedException();
        }

        internal override SyntaxNode GetDeclaratorSyntax()
        {
            throw new NotImplementedException();
        }

        internal override bool HasSourceLocation => false;

        public override RefKind RefKind
        {
            get { return RefKind.None; }
        }

        internal override ScopedKind Scope => ScopedKind.None;
    }
}
