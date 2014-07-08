// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The purpose of this rewriter is to replace await-containing catch and finally handlers
    /// with surrogate replacements that keep actual handler code in regular code blocks.
    /// That allows these constructs to be further lowered at the async lowering pass.
    /// </summary>
    internal sealed class AsyncHandlerRewriter : BoundTreeRewriter
    {
        private readonly bool generateDebugInfo;
        private readonly CSharpCompilation compilation;
        private readonly SyntheticBoundNodeFactory F;
        private readonly DiagnosticBag diagnostics;
        private readonly AwaitInFinallyAnalysis analysis;

        AwaitCatchFrame currentAwaitCatchFrame = null;
        AwaitFinallyFrame currentAwaitFinallyFrame = new AwaitFinallyFrame(null, null);

        private AsyncHandlerRewriter(
            bool generateDebugInfo,
            MethodSymbol containingMethod,
            NamedTypeSymbol containingType,
            SyntheticBoundNodeFactory factory,
            CSharpCompilation compilation,
            DiagnosticBag diagnostics,
            AwaitInFinallyAnalysis analysis)
        {
            this.generateDebugInfo = generateDebugInfo && containingMethod.GenerateDebugInfo;
            this.compilation = compilation;
            this.F = factory;
            this.F.CurrentMethod = containingMethod;
            Debug.Assert(factory.CurrentClass == (containingType ?? containingMethod.ContainingType));
            this.diagnostics = diagnostics;
            this.analysis = analysis;
        }

        /// <summary>
        /// Lower a block of code by performing local rewritings. 
        /// The goal is to not have exception handlers that contain awaits in them.
        /// 
        /// 1) Await containing finallies:
        ///     The general strategy is to rewrite await containing handlers into synthetic handlers.
        ///     Synthetic handlers are not handlers in IL sense so it is ok to have awaits in them.
        ///     Since synthetic handlers are just blocks, we have to deal with pending exception/branch/return manually
        ///     (this is the hard part of the rewrite).
        ///
        ///     try{
        ///        code;
        ///     }finally{
        ///        handler;
        ///     }
        ///
        /// Into ===>
        ///
        ///     Exception ex = null;
        ///     int pendingBranch = 0;
        ///
        ///     try{
        ///         code;  // any gotos/returns are rewritten to code that pends the necessary info and goes to finallyLabel
        ///         goto finallyLabel;
        ///     }catch (ex){  // essentially pend the currently active exception
        ///     };
        ///
        ///     finallyLabel:
        ///     {
        ///        handler;
        ///        if (ex != null) throw ex;     // unpend the exception
        ///        unpend branches/return
        ///     }
        /// 
        /// 2) Await containing catches:
        ///     try{
        ///         code;
        ///     }catch (Exeption ex){
        ///         handler;
        ///         throw;
        ///     }
        /// 
        /// 
        /// Into ===>
        ///
        ///     Object pendingException;
        ///     int pendingCatch = 0;
        ///
        ///     try{
        ///         code; 
        ///     }catch (Exception temp){  // essentially pend the currently active exception
        ///         pendingException = temp;
        ///         pendingCatch = 1;
        ///     };
        ///
        ///     switch(pendingCatch):
        ///     {
        ///        case 1:
        ///         {
        ///             Exception ex = (Exception)pendingException;
        ///             handler;
        ///             throw pendingException
        ///         }
        ///     }
        /// </summary>
        public static BoundStatement Rewrite(
            bool generateDebugInfo,
            MethodSymbol containingSymbol,
            NamedTypeSymbol containingType,
            BoundStatement statement,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(containingSymbol != null);
            Debug.Assert(containingType != null);
            Debug.Assert(statement != null);
            Debug.Assert(compilationState != null);
            Debug.Assert(diagnostics != null);

            var analysis = new AwaitInFinallyAnalysis(statement);
            if (!analysis.ContainsAwaitInHandlers())
            {
                return statement;
            }

            var compilation = containingType.DeclaringCompilation;
            var factory = new SyntheticBoundNodeFactory(containingSymbol, statement.Syntax, compilationState, diagnostics);
            var rewriter = new AsyncHandlerRewriter(generateDebugInfo, containingSymbol, containingType, factory, compilation, diagnostics, analysis);
            var loweredStatement = (BoundStatement)rewriter.Visit(statement);

            return loweredStatement;
        }

        public override BoundNode VisitTryStatement(BoundTryStatement node)
        {
            BoundStatement finalizedRegion;
            BoundBlock rewrittenFinally;

            var finallyContainsAwaits = analysis.FinallyContainsAwaits(node);
            if (!finallyContainsAwaits)
            {
                finalizedRegion = RewriteFinalizedRegion(node);
                rewrittenFinally = (BoundBlock)this.Visit(node.FinallyBlockOpt);

                if (rewrittenFinally == null)
                {
                    return finalizedRegion;
                }

                var asTry = finalizedRegion as BoundTryStatement;
                if (asTry != null)
                {
                    // since finalized region is a try we can just attach finally to it
                    Debug.Assert(asTry.FinallyBlockOpt == null);
                    return asTry.Update(asTry.TryBlock, asTry.CatchBlocks, rewrittenFinally, asTry.PreferFaultHandler);
                }
                else
                {
                    // wrap finalizedRegion into a Try with a finally.
                    return F.Try((BoundBlock)finalizedRegion, ImmutableArray<BoundCatchBlock>.Empty, rewrittenFinally);
                }
            }

            // rewrite finalized region (try and catches) in the current frame
            var frame = PushFrame(node);
            finalizedRegion = RewriteFinalizedRegion(node);
            rewrittenFinally = (BoundBlock)this.VisitBlock(node.FinallyBlockOpt);
            PopFrame();

            var exceptionType = F.SpecialType(SpecialType.System_Object);
            var pendingExceptionLocal = F.SynthesizedLocal(exceptionType, kind: SynthesizedLocalKind.TryAwaitPendingException);
            var finallyLabel = F.GenerateLabel("finallyLabel");
            var pendingBranchVar = F.SynthesizedLocal(F.SpecialType(SpecialType.System_Int32), kind: SynthesizedLocalKind.TryAwaitPendingBranch);

            var catchAll = F.Catch(F.Local(pendingExceptionLocal), F.Block());

            var catchAndPendException = F.Try(
                F.Block(
                    finalizedRegion,
                    F.HiddenSequencePoint(),
                    F.Goto(finallyLabel),
                    PendBranches(frame, pendingBranchVar, finallyLabel)),
                ImmutableArray.Create(catchAll));

            var syntheticFinally = F.Block(
                F.HiddenSequencePoint(),
                F.Label(finallyLabel),
                rewrittenFinally,
                F.HiddenSequencePoint(),
                UnpendException(pendingExceptionLocal),
                UnpendBranches(
                    frame,
                    pendingBranchVar,
                    pendingExceptionLocal));


            var locals = ArrayBuilder<LocalSymbol>.GetInstance();
            var statements = ArrayBuilder<BoundStatement>.GetInstance();

            statements.Add(F.HiddenSequencePoint());

            locals.Add(pendingExceptionLocal);
            statements.Add(F.Assignment(F.Local(pendingExceptionLocal), F.Default(pendingExceptionLocal.Type)));
            locals.Add(pendingBranchVar);
            statements.Add(F.Assignment(F.Local(pendingBranchVar), F.Default(pendingBranchVar.Type)));

            LocalSymbol returnLocal = frame.returnValue;
            if (returnLocal != null)
            {
                locals.Add(returnLocal);
            }

            statements.Add(catchAndPendException);
            statements.Add(syntheticFinally);

            var completeTry = F.Block(
                locals.ToImmutableAndFree(),
                statements.ToImmutableAndFree());

            return completeTry;
        }

        private BoundBlock PendBranches(
            AwaitFinallyFrame frame,
            LocalSymbol pendingBranchVar,
            LabelSymbol finallyLabel)
        {
            var bodyStatements = ArrayBuilder<BoundStatement>.GetInstance();

            // handle proxy labels if have any
            var proxiedLabels = frame.proxiedLabels;
            var proxyLabels = frame.proxyLabels;

            // skip 0 - it means we took no explicit branches
            int i = 1;
            if (proxiedLabels != null)
            {
                for (int cnt = proxiedLabels.Count; i <= cnt; i++)
                {
                    var proxied = proxiedLabels[i - 1];
                    var proxy = proxyLabels[proxied];

                    PendBranch(bodyStatements, proxy, i, pendingBranchVar, finallyLabel);
                }
            }

            var returnProxy = frame.returnProxy;
            if (returnProxy != null)
            {
                PendBranch(bodyStatements, returnProxy, i, pendingBranchVar, finallyLabel);
            }

            return F.Block(bodyStatements.ToImmutableAndFree());
        }

        private void PendBranch(
            ArrayBuilder<BoundStatement> bodyStatements,
            LabelSymbol proxy,
            int i,
            LocalSymbol pendingBranchVar,
            LabelSymbol finallyLabel)
        {
            // branch lands here
            bodyStatements.Add(F.Label(proxy));

            // pend the branch
            bodyStatements.Add(F.Assignment(F.Local(pendingBranchVar), F.Literal(i)));

            // skip other proxies
            bodyStatements.Add(F.Goto(finallyLabel));
        }

        private BoundStatement UnpendBranches(
            AwaitFinallyFrame frame,
            LocalSymbol pendingBranchVar,
            LocalSymbol pendingException)
        {
            var parent = frame.parent;

            // handle proxy labels if have any
            var proxiedLabels = frame.proxiedLabels;
            var proxyLabels = frame.proxyLabels;

            // skip 0 - it means we took no explicit branches
            int i = 1;
            var cases = ArrayBuilder<BoundSwitchSection>.GetInstance();

            if (proxiedLabels != null)
            {
                for (int cnt = proxiedLabels.Count; i <= cnt; i++)
                {
                    var target = proxiedLabels[i - 1];
                    var parentProxy = parent.ProxyLabelIfNeeded(target);
                    var caseStatement = F.SwitchSection(i, F.Goto(parentProxy));
                    cases.Add(caseStatement);
                }
            }

            if (frame.returnProxy != null)
            {
                BoundLocal pendingValue = null;
                if (frame.returnValue != null)
                {
                    pendingValue = F.Local(frame.returnValue);
                }

                LocalSymbol returnValue;
                BoundStatement unpendReturn;

                var returnLabel = parent.ProxyReturnIfNeeded(F.CurrentMethod, pendingValue, out returnValue);

                if (returnLabel == null)
                {
                    unpendReturn = new BoundReturnStatement(F.Syntax, pendingValue);
                }
                else
                {
                    if (pendingValue == null)
                    {
                        unpendReturn = F.Goto(returnLabel);
                    }
                    else
                    {
                        unpendReturn = F.Block(
                            F.Assignment(
                                F.Local(returnValue),
                                pendingValue),
                            F.Goto(returnLabel));
                    }
                }

                var caseStatement = F.SwitchSection(i, unpendReturn);
                cases.Add(caseStatement);
            }

            return F.Switch(F.Local(pendingBranchVar), cases.ToImmutableAndFree());
        }

        public override BoundNode VisitGotoStatement(BoundGotoStatement node)
        {
            BoundExpression caseExpressionOpt = (BoundExpression)this.Visit(node.CaseExpressionOpt);
            BoundLabel labelExpressionOpt = (BoundLabel)this.Visit(node.LabelExpressionOpt);
            var proxyLabel = currentAwaitFinallyFrame.ProxyLabelIfNeeded(node.Label);
            return node.Update(proxyLabel, caseExpressionOpt, labelExpressionOpt);
        }

        public override BoundNode VisitConditionalGoto(BoundConditionalGoto node)
        {
            Debug.Assert(node.Label == currentAwaitFinallyFrame.ProxyLabelIfNeeded(node.Label), "conditional leave?");
            return base.VisitConditionalGoto(node);
        }

        public override BoundNode VisitReturnStatement(BoundReturnStatement node)
        {
            LocalSymbol returnValue;
            var returnLabel = currentAwaitFinallyFrame.ProxyReturnIfNeeded(
                F.CurrentMethod,
                node.ExpressionOpt,
                out returnValue);

            if (returnLabel == null)
            {
                return base.VisitReturnStatement(node);
            }

            var returnExpr = (BoundExpression)(this.Visit(node.ExpressionOpt));
            if (returnExpr != null)
            {
                return F.Block(
                        F.Assignment(
                            F.Local(returnValue),
                            returnExpr),
                        F.Goto(
                            returnLabel));
            }
            else
            {
                return F.Goto(returnLabel);
            }
        }

        private BoundStatement UnpendException(LocalSymbol pendingExceptionLocal)
        {
            // create a temp. 
            // pendingExceptionLocal will certainly be captured, no need to access it over and over.
            var obj = F.SynthesizedLocal(F.SpecialType(SpecialType.System_Object));
            var objInit = F.Assignment(F.Local(obj), F.Local(pendingExceptionLocal));

            // throw pendingExceptionLocal;
            BoundStatement rethrow = Rethrow(obj);

            return F.Block(
                    ImmutableArray.Create(obj),
                    objInit,
                    F.If(
                        F.ObjectNotEqual(
                            F.Local(obj),
                            F.Null(obj.Type)),
                        rethrow));
        }

        private BoundStatement Rethrow(LocalSymbol obj)
        {
            // conservative rethrow 
            BoundStatement rethrow = F.Throw(F.Local(obj));

            var exceptionDispatchInfoCapture = F.WellKnownMethod(WellKnownMember.System_Runtime_ExceptionServices_ExceptionDispatchInfo__Capture, isOptional: true);
            var exceptionDispatchInfoThrow = F.WellKnownMethod(WellKnownMember.System_Runtime_ExceptionServices_ExceptionDispatchInfo__Throw, isOptional: true);

            // if these helpers are available, we can rethrow with original stack info
            // as long as it derives from Exception
            if (exceptionDispatchInfoCapture != null && exceptionDispatchInfoThrow != null)
            {
                var ex = F.SynthesizedLocal(F.WellKnownType(WellKnownType.System_Exception));
                var assignment = F.Assignment(
                    F.Local(ex),
                    F.As(F.Local(obj), ex.Type));

                // better rethrow 
                rethrow = F.Block(
                    ImmutableArray.Create(ex),
                    assignment,
                    F.If(F.ObjectEqual(F.Local(ex), F.Null(ex.Type)), rethrow),
                    // ExceptionDispatchInfo.Capture(pendingExceptionLocal).Throw();
                    F.ExpressionStatement(
                        F.Call(
                            F.StaticCall(
                                exceptionDispatchInfoCapture.ContainingType,
                                exceptionDispatchInfoCapture,
                                F.Local(ex)),
                            exceptionDispatchInfoThrow)));
            }

            return rethrow;
        }

        /// <summary>
        /// Rewrites Try/Catch part of the Try/Catch/Finally
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private BoundStatement RewriteFinalizedRegion(BoundTryStatement node)
        {
            var rewrittenTry = (BoundBlock)this.VisitBlock(node.TryBlock);

            var catches = node.CatchBlocks;
            if (catches.IsDefaultOrEmpty)
            {
                return rewrittenTry;
            }

            var origAwaitCatchFrame = this.currentAwaitCatchFrame;
            this.currentAwaitCatchFrame = null;

            var rewrittenCatches = this.VisitList(node.CatchBlocks);
            BoundStatement tryWithCatches = F.Try(rewrittenTry, rewrittenCatches);

            var currentAwaitCatchFrame = this.currentAwaitCatchFrame;
            if (currentAwaitCatchFrame != null)
            {
                var handledLabel = F.GenerateLabel("handled");
                var handlersList = currentAwaitCatchFrame.handlers;
                var handlers = ArrayBuilder<BoundSwitchSection>.GetInstance(handlersList.Count);
                for (int i = 0, l = handlersList.Count; i < l; i++)
                {
                    handlers.Add(F.SwitchSection(
                        i + 1,
                        F.Block(
                            handlersList[i],
                            F.Goto(handledLabel))));
                }

                tryWithCatches = F.Block(
                    ImmutableArray.Create(
                        currentAwaitCatchFrame.pendingCaughtException,
                        currentAwaitCatchFrame.pendingCatch).
                        AddRange(currentAwaitCatchFrame.hoistedLocals.Values.ToImmutableArray()),
                    F.HiddenSequencePoint(),
                    F.Assignment(
                        F.Local(currentAwaitCatchFrame.pendingCatch),
                        F.Default(currentAwaitCatchFrame.pendingCatch.Type)),
                    tryWithCatches,
                    F.HiddenSequencePoint(),
                    F.Switch(
                        F.Local(currentAwaitCatchFrame.pendingCatch),
                        handlers.ToImmutableAndFree()),
                    F.HiddenSequencePoint(),
                    F.Label(handledLabel));
            }

            this.currentAwaitCatchFrame = origAwaitCatchFrame;

            return tryWithCatches;
        }

        public override BoundNode VisitCatchBlock(BoundCatchBlock node)
        {
            if (!analysis.CatchContainsAwait(node))
            {
                return (BoundCatchBlock)base.VisitCatchBlock(node);
            }

            var currentAwaitCatchFrame = this.currentAwaitCatchFrame;
            if (currentAwaitCatchFrame == null)
            {
                currentAwaitCatchFrame = this.currentAwaitCatchFrame = new AwaitCatchFrame(F);
            }

            var catchType = node.ExceptionTypeOpt ?? F.SpecialType(SpecialType.System_Object);
            var catchTemp = F.SynthesizedLocal(catchType);

            var storePending = F.AssignmentExpression(
                        F.Local(currentAwaitCatchFrame.pendingCaughtException),
                        F.Convert(currentAwaitCatchFrame.pendingCaughtException.Type,
                            F.Local(catchTemp)));

            var setPendingCatchNum = F.Assignment(
                            F.Local(currentAwaitCatchFrame.pendingCatch),
                            F.Literal(currentAwaitCatchFrame.handlers.Count + 1));

            //  catch (ExType exTemp)
            //  {
            //      pendingCaughtException = exTemp;
            //      catchNo = X;
            //  }
            BoundCatchBlock catchAndPend;
            ImmutableArray<LocalSymbol> handlerLocals;

            var filterOpt = node.ExceptionFilterOpt;
            if (filterOpt == null)
            {
                // store pending exception 
                // as the first statement in a catch
                catchAndPend = node.Update(
                    ImmutableArray.Create(catchTemp),
                    F.Local(catchTemp),
                    catchType,
                    exceptionFilterOpt: null,
                    body: F.Block(
                        F.HiddenSequencePoint(),
                        F.ExpressionStatement(storePending),
                        setPendingCatchNum));

                // catch locals live on the synthetic catch handler block
                handlerLocals = node.Locals;
            }
            else
            {
                // catch locals move up into hoisted locals
                // since we might need to access them from both the filter and the catch
                handlerLocals = ImmutableArray<LocalSymbol>.Empty;
                foreach (var local in node.Locals)
                {
                    currentAwaitCatchFrame.HoistLocal(local, F);
                }

                // store pending exception 
                // as the first expression in a filter
                var sourceOpt = node.ExceptionSourceOpt;
                var rewrittenFilter = (BoundExpression)this.Visit(filterOpt);
                var newFilter = sourceOpt == null ?
                                F.Sequence(
                                    storePending,
                                    rewrittenFilter) :
                                F.Sequence(
                                    storePending,
                                    AssignCatchSource((BoundExpression)this.Visit(sourceOpt), currentAwaitCatchFrame),
                                    rewrittenFilter);

                catchAndPend = node.Update(
                    ImmutableArray.Create(catchTemp),
                    F.Local(catchTemp),
                    catchType,
                    exceptionFilterOpt: newFilter,
                    body: F.Block(
                        F.HiddenSequencePoint(),
                        setPendingCatchNum));
            }

            var handlerStatements = ArrayBuilder<BoundStatement>.GetInstance();

            handlerStatements.Add(F.HiddenSequencePoint());

            if (filterOpt == null)
            {
                var sourceOpt = node.ExceptionSourceOpt;
                if (sourceOpt != null)
                {
                    BoundExpression assignSource = AssignCatchSource((BoundExpression)this.Visit(sourceOpt), currentAwaitCatchFrame);
                    handlerStatements.Add(F.ExpressionStatement(assignSource));
                }
            }

            handlerStatements.Add((BoundStatement)this.Visit(node.Body));

            var handler = F.Block(
                    handlerLocals,
                    handlerStatements.ToImmutableAndFree()
                );

            currentAwaitCatchFrame.handlers.Add(handler);

            return catchAndPend;
        }

        private BoundExpression AssignCatchSource(BoundExpression rewrittenSource, AwaitCatchFrame currentAwaitCatchFrame)
        {
            BoundExpression assignSource = null;
            if (rewrittenSource != null)
            {
                // exceptionSource = (exceptionSourceType)pendingCaughtException;
                assignSource = F.AssignmentExpression(
                                    rewrittenSource,
                                    F.Convert(
                                        rewrittenSource.Type,
                                        F.Local(currentAwaitCatchFrame.pendingCaughtException)));
            }

            return assignSource;
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            var catchFrame = this.currentAwaitCatchFrame;
            if (catchFrame == null || !catchFrame.hoistedLocals.ContainsKey(node.LocalSymbol))
            {
                return base.VisitLocal(node);
            }

            var newLocal = catchFrame.hoistedLocals[node.LocalSymbol];
            return node.Update(newLocal, node.ConstantValueOpt, newLocal.Type);
        }

        public override BoundNode VisitThrowStatement(BoundThrowStatement node)
        {
            if (node.ExpressionOpt != null || this.currentAwaitCatchFrame == null)
            {
                return base.VisitThrowStatement(node);
            }

            return Rethrow(this.currentAwaitCatchFrame.pendingCaughtException);
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            var oldContainingSymbol = this.F.CurrentMethod;
            var oldAwaitFinallyFrame = this.currentAwaitFinallyFrame;

            this.F.CurrentMethod = node.Symbol;
            this.currentAwaitFinallyFrame = new AwaitFinallyFrame(null, null);

            var result = base.VisitLambda(node);

            this.F.CurrentMethod = oldContainingSymbol;
            this.currentAwaitFinallyFrame = oldAwaitFinallyFrame;

            return result;
        }

        private AwaitFinallyFrame PushFrame(BoundTryStatement statement)
        {
            var newFrame = new AwaitFinallyFrame(currentAwaitFinallyFrame, analysis.Labels(statement));
            currentAwaitFinallyFrame = newFrame;
            return newFrame;
        }

        private void PopFrame()
        {
            var result = currentAwaitFinallyFrame;
            currentAwaitFinallyFrame = result.parent;
        }

        /// <summary>
        /// Analyses method body for Try blocks with awaits in finallies 
        /// Also collects labels that such blocks contain.
        /// </summary>
        private class AwaitInFinallyAnalysis : LabelCollector
        {
            // all try blocks with yields in them and complete set of lables inside those trys
            // NOTE: non-yielding Trys are transparently ignored - i.e. their labels are included
            //       in the label set of the nearest yielding-try parent  
            private Dictionary<BoundTryStatement, HashSet<LabelSymbol>> labelsInInterestingTry;

            private HashSet<BoundCatchBlock> awaitContainingCatches;

            // transient accumulators.
            private bool seenAwait;

            public AwaitInFinallyAnalysis(BoundStatement body)
            {
                this.seenAwait = false;
                this.Visit(body);
            }

            /// <summary>
            /// Returns true if a finally of the given try contains awaits
            /// </summary>
            public bool FinallyContainsAwaits(BoundTryStatement statement)
            {
                return labelsInInterestingTry != null && labelsInInterestingTry.ContainsKey(statement);
            }

            /// <summary>
            /// Returns true if a catch contains awaits
            /// </summary>
            internal bool CatchContainsAwait(BoundCatchBlock node)
            {
                return awaitContainingCatches != null && awaitContainingCatches.Contains(node);
            }

            /// <summary>
            /// Returns true if body contains await in a finally block.
            /// </summary>
            public bool ContainsAwaitInHandlers()
            {
                return labelsInInterestingTry != null || awaitContainingCatches != null;
            }

            /// <summary>
            /// Labels reachable from within this frame without invoking its finally. 
            /// null if there are no such labels.
            /// </summary>
            internal HashSet<LabelSymbol> Labels(BoundTryStatement statement)
            {
                return labelsInInterestingTry[statement];
            }

            public override BoundNode VisitTryStatement(BoundTryStatement node)
            {
                var origLabels = this.currentLabels;
                this.currentLabels = null;
                Visit(node.TryBlock);
                VisitList(node.CatchBlocks);

                var origSeenAwait = this.seenAwait;
                this.seenAwait = false;
                Visit(node.FinallyBlockOpt);

                if (this.seenAwait)
                {
                    // this try has awaits in the finally !
                    var labelsInInterestingTry = this.labelsInInterestingTry;
                    if (labelsInInterestingTry == null)
                    {
                        this.labelsInInterestingTry = labelsInInterestingTry = new Dictionary<BoundTryStatement, HashSet<LabelSymbol>>();
                    }

                    labelsInInterestingTry.Add(node, currentLabels);
                    currentLabels = origLabels;
                }
                else
                {
                    // this is a boring try without awaits in finally

                    // currentLabels = currentLabels U origLabels ;
                    if (currentLabels == null)
                    {
                        currentLabels = origLabels;
                    }
                    else if (origLabels != null)
                    {
                        currentLabels.UnionWith(origLabels);
                    }
                }

                this.seenAwait = this.seenAwait | origSeenAwait;
                return null;
            }

            public override BoundNode VisitCatchBlock(BoundCatchBlock node)
            {
                var origSeenAwait = this.seenAwait;
                this.seenAwait = false;

                var result = base.VisitCatchBlock(node);

                if (this.seenAwait)
                {
                    var awaitContainingCatches = this.awaitContainingCatches;
                    if (awaitContainingCatches == null)
                    {
                        this.awaitContainingCatches = awaitContainingCatches = new HashSet<BoundCatchBlock>();
                    }

                    this.awaitContainingCatches.Add(node);
                }

                this.seenAwait |= origSeenAwait;
                return result;
            }

            public override BoundNode VisitAwaitExpression(BoundAwaitExpression node)
            {
                this.seenAwait = true;
                return base.VisitAwaitExpression(node);
            }

            public override BoundNode VisitLambda(BoundLambda node)
            {
                var origLabels = this.currentLabels;
                var origSeenAwait = this.seenAwait;

                this.currentLabels = null;
                this.seenAwait = false;

                base.VisitLambda(node);

                this.currentLabels = origLabels;
                this.seenAwait = origSeenAwait;

                return null;
            }
        }

        // storage of various information about a given finally frame
        private class AwaitFinallyFrame
        {
            // Enclosing frame. Root frame does not have parent.
            public readonly AwaitFinallyFrame parent;

            // labels within this frame (branching to these labels does not go through finally).
            public readonly HashSet<LabelSymbol> labels;

            // proxy labels for branches leaving the frame. 
            // we build this on demand once we encounter leaving branches.
            // subsequent leaves to an already proxied label redirected to the proxy.
            // At the proxy lable we will execute finally and forward the control flow 
            // to the actual destination. (which could be proxied again in the parent)
            public Dictionary<LabelSymbol, LabelSymbol> proxyLabels = null;

            public List<LabelSymbol> proxiedLabels = null;

            public LabelSymbol returnProxy = null;
            public LocalSymbol returnValue = null;

            public AwaitFinallyFrame(AwaitFinallyFrame parent, HashSet<LabelSymbol> labels)
            {
                this.parent = parent;
                this.labels = labels;
            }

            public bool IsRoot()
            {
                return this.parent == null;
            }

            // returns a proxy for a label if branch must be hijacked to run finally
            // otherwise returns same label back
            public LabelSymbol ProxyLabelIfNeeded(LabelSymbol label)
            {
                // no need to proxy a label in the current frame or when we are at the root
                if (this.IsRoot() || (labels != null && labels.Contains(label)))
                {
                    return label;
                }

                var proxyLabels = this.proxyLabels;
                var proxiedLabels = this.proxiedLabels;
                if (proxyLabels == null)
                {
                    this.proxyLabels = proxyLabels = new Dictionary<LabelSymbol, LabelSymbol>();
                    this.proxiedLabels = proxiedLabels = new List<LabelSymbol>();
                }

                LabelSymbol proxy;
                if (!proxyLabels.TryGetValue(label, out proxy))
                {
                    proxy = new GeneratedLabelSymbol("proxy" + label.Name);
                    proxyLabels.Add(label, proxy);
                    proxiedLabels.Add(label);
                }

                return proxy;
            }

            public LabelSymbol ProxyReturnIfNeeded(
                MethodSymbol containingMethod,
                BoundExpression valueOpt,
                out LocalSymbol returnValue)
            {
                returnValue = null;

                // no need to proxy returns  at the root
                if (this.IsRoot())
                {
                    return null;
                }

                var returnProxy = this.returnProxy;
                if (returnProxy == null)
                {
                    this.returnProxy = returnProxy = new GeneratedLabelSymbol("returnProxy");
                }

                if (valueOpt != null)
                {
                    returnValue = this.returnValue;
                    if (returnValue == null)
                    {
                        this.returnValue = returnValue = new SynthesizedLocal(containingMethod, valueOpt.Type, SynthesizedLocalKind.LoweringTemp);
                    }
                }

                return returnProxy;
            }
        }

        private class AwaitCatchFrame
        {
            // object, stores the original caught exception
            // used to initialize the exception source inside the handler
            // also used in rethrow statements
            public readonly LocalSymbol pendingCaughtException;

            // int, stores the number of pending catch
            // 0 - means no catches are pending.
            public readonly LocalSymbol pendingCatch;

            // synthetic handlers produced by catch rewrite.
            // they will become switch sections when pending exception is dispatched.
            public readonly List<BoundBlock> handlers;

            // when catch local must be used from a filter
            // we need to "hoist" it up to ensure that both the filter 
            // and the catch access the same variable.
            // NOTE: it must be the same variable, not just same value. 
            //       The difference would be observable if filter mutates the variable
            //       or/and if a variable gets lifted into a closure.
            public readonly Dictionary<LocalSymbol, LocalSymbol> hoistedLocals;

            public AwaitCatchFrame(SyntheticBoundNodeFactory F)
            {
                this.pendingCaughtException = F.SynthesizedLocal(F.SpecialType(SpecialType.System_Object), kind: SynthesizedLocalKind.TryAwaitPendingCaughtException);
                this.pendingCatch = F.SynthesizedLocal(F.SpecialType(SpecialType.System_Int32), kind: SynthesizedLocalKind.TryAwaitPendingCatch);
                this.handlers = new List<BoundBlock>();
                this.hoistedLocals = new Dictionary<LocalSymbol, LocalSymbol>();
            }

            public void HoistLocal(LocalSymbol local, SyntheticBoundNodeFactory F)
            {
                if (!hoistedLocals.Keys.Any(l => l.Name == local.Name && l.Type == local.Type))
                {
                    hoistedLocals.Add(local, local);
                    return;
                }

                // code uses "await" in two sibling catches with exception filters
                // locals with same names and types may cause problems if they are lifted
                // and become fields with identical signatures.
                // To avoid such problems we will mangle the name of the second local.
                // This will only affect debugging of this extremely rare case.
                var newLocal = F.SynthesizedLocal(local.Type, kind: SynthesizedLocalKind.ExceptionFilterAwaitHoistedExceptionLocal);
                hoistedLocals.Add(local, newLocal);
            }
        }
    }
}