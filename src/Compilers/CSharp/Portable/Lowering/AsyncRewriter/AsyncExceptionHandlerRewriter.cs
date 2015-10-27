// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The purpose of this rewriter is to replace await-containing catch and finally handlers
    /// with surrogate replacements that keep actual handler code in regular code blocks.
    /// That allows these constructs to be further lowered at the async lowering pass.
    /// </summary>
    internal sealed class AsyncExceptionHandlerRewriter : BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator 
    {
        private readonly bool _generateDebugInfo;
        private readonly CSharpCompilation _compilation;
        private readonly SyntheticBoundNodeFactory _F;
        private readonly DiagnosticBag _diagnostics;
        private readonly AwaitInFinallyAnalysis _analysis;

        private AwaitCatchFrame _currentAwaitCatchFrame;
        private AwaitFinallyFrame _currentAwaitFinallyFrame = new AwaitFinallyFrame();

        private AsyncExceptionHandlerRewriter(
            MethodSymbol containingMethod,
            NamedTypeSymbol containingType,
            SyntheticBoundNodeFactory factory,
            CSharpCompilation compilation,
            DiagnosticBag diagnostics,
            AwaitInFinallyAnalysis analysis)
        {
            _generateDebugInfo = containingMethod.GenerateDebugInfo;
            _compilation = compilation;
            _F = factory;
            _F.CurrentMethod = containingMethod;
            Debug.Assert(factory.CurrentType == (containingType ?? containingMethod.ContainingType));
            _diagnostics = diagnostics;
            _analysis = analysis;
        }

        /// <summary>
        /// Lower a block of code by performing local rewritings. 
        /// The goal is to not have exception handlers that contain awaits in them.
        /// 
        /// 1) Await containing finally blocks:
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
        ///     }catch (Exception ex){
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
            var rewriter = new AsyncExceptionHandlerRewriter(containingSymbol, containingType, factory, compilation, diagnostics, analysis);
            var loweredStatement = (BoundStatement)rewriter.Visit(statement);

            return loweredStatement;
        }

        public override BoundNode VisitTryStatement(BoundTryStatement node)
        {
            var tryStatementSyntax = node.Syntax;
            // If you add a syntax kind to the assertion below, please also ensure
            // that the scenario has been tested with Edit-and-Continue.
            Debug.Assert(
                tryStatementSyntax.IsKind(SyntaxKind.TryStatement) ||
                tryStatementSyntax.IsKind(SyntaxKind.UsingStatement) ||
                tryStatementSyntax.IsKind(SyntaxKind.ForEachStatement));

            BoundStatement finalizedRegion;
            BoundBlock rewrittenFinally;

            var finallyContainsAwaits = _analysis.FinallyContainsAwaits(node);
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
                    return _F.Try((BoundBlock)finalizedRegion, ImmutableArray<BoundCatchBlock>.Empty, rewrittenFinally);
                }
            }

            // rewrite finalized region (try and catches) in the current frame
            var frame = PushFrame(node);
            finalizedRegion = RewriteFinalizedRegion(node);
            rewrittenFinally = (BoundBlock)this.VisitBlock(node.FinallyBlockOpt);
            PopFrame();

            var exceptionType = _F.SpecialType(SpecialType.System_Object);
            var pendingExceptionLocal = new SynthesizedLocal(_F.CurrentMethod, exceptionType, SynthesizedLocalKind.TryAwaitPendingException, tryStatementSyntax);
            var finallyLabel = _F.GenerateLabel("finallyLabel");
            var pendingBranchVar = new SynthesizedLocal(_F.CurrentMethod, _F.SpecialType(SpecialType.System_Int32), SynthesizedLocalKind.TryAwaitPendingBranch, tryStatementSyntax);

            var catchAll = _F.Catch(_F.Local(pendingExceptionLocal), _F.Block());

            var catchAndPendException = _F.Try(
                _F.Block(
                    finalizedRegion,
                    _F.HiddenSequencePoint(),
                    _F.Goto(finallyLabel),
                    PendBranches(frame, pendingBranchVar, finallyLabel)),
                ImmutableArray.Create(catchAll));

            var syntheticFinally = _F.Block(
                _F.HiddenSequencePoint(),
                _F.Label(finallyLabel),
                rewrittenFinally,
                _F.HiddenSequencePoint(),
                UnpendException(pendingExceptionLocal),
                UnpendBranches(
                    frame,
                    pendingBranchVar,
                    pendingExceptionLocal));


            var locals = ArrayBuilder<LocalSymbol>.GetInstance();
            var statements = ArrayBuilder<BoundStatement>.GetInstance();

            statements.Add(_F.HiddenSequencePoint());

            locals.Add(pendingExceptionLocal);
            statements.Add(_F.Assignment(_F.Local(pendingExceptionLocal), _F.Default(pendingExceptionLocal.Type)));
            locals.Add(pendingBranchVar);
            statements.Add(_F.Assignment(_F.Local(pendingBranchVar), _F.Default(pendingBranchVar.Type)));

            LocalSymbol returnLocal = frame.returnValue;
            if (returnLocal != null)
            {
                locals.Add(returnLocal);
            }

            statements.Add(catchAndPendException);
            statements.Add(syntheticFinally);

            var completeTry = _F.Block(
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

            var returnProxy = frame.returnProxyLabel;
            if (returnProxy != null)
            {
                PendBranch(bodyStatements, returnProxy, i, pendingBranchVar, finallyLabel);
            }

            return _F.Block(bodyStatements.ToImmutableAndFree());
        }

        private void PendBranch(
            ArrayBuilder<BoundStatement> bodyStatements,
            LabelSymbol proxy,
            int i,
            LocalSymbol pendingBranchVar,
            LabelSymbol finallyLabel)
        {
            // branch lands here
            bodyStatements.Add(_F.Label(proxy));

            // pend the branch
            bodyStatements.Add(_F.Assignment(_F.Local(pendingBranchVar), _F.Literal(i)));

            // skip other proxies
            bodyStatements.Add(_F.Goto(finallyLabel));
        }

        private BoundStatement UnpendBranches(
            AwaitFinallyFrame frame,
            SynthesizedLocal pendingBranchVar,
            SynthesizedLocal pendingException)
        {
            var parent = frame.ParentOpt;

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
                    var caseStatement = _F.SwitchSection(i, _F.Goto(parentProxy));
                    cases.Add(caseStatement);
                }
            }

            if (frame.returnProxyLabel != null)
            {
                BoundLocal pendingValue = null;
                if (frame.returnValue != null)
                {
                    pendingValue = _F.Local(frame.returnValue);
                }

                SynthesizedLocal returnValue;
                BoundStatement unpendReturn;

                var returnLabel = parent.ProxyReturnIfNeeded(_F.CurrentMethod, pendingValue, out returnValue);

                if (returnLabel == null)
                {
                    unpendReturn = new BoundReturnStatement(_F.Syntax, pendingValue);
                }
                else
                {
                    if (pendingValue == null)
                    {
                        unpendReturn = _F.Goto(returnLabel);
                    }
                    else
                    {
                        unpendReturn = _F.Block(
                            _F.Assignment(
                                _F.Local(returnValue),
                                pendingValue),
                            _F.Goto(returnLabel));
                    }
                }

                var caseStatement = _F.SwitchSection(i, unpendReturn);
                cases.Add(caseStatement);
            }

            return _F.Switch(_F.Local(pendingBranchVar), cases.ToImmutableAndFree());
        }

        public override BoundNode VisitGotoStatement(BoundGotoStatement node)
        {
            BoundExpression caseExpressionOpt = (BoundExpression)this.Visit(node.CaseExpressionOpt);
            BoundLabel labelExpressionOpt = (BoundLabel)this.Visit(node.LabelExpressionOpt);
            var proxyLabel = _currentAwaitFinallyFrame.ProxyLabelIfNeeded(node.Label);
            return node.Update(proxyLabel, caseExpressionOpt, labelExpressionOpt);
        }

        public override BoundNode VisitConditionalGoto(BoundConditionalGoto node)
        {
            Debug.Assert(node.Label == _currentAwaitFinallyFrame.ProxyLabelIfNeeded(node.Label), "conditional leave?");
            return base.VisitConditionalGoto(node);
        }

        public override BoundNode VisitReturnStatement(BoundReturnStatement node)
        {
            SynthesizedLocal returnValue;
            var returnLabel = _currentAwaitFinallyFrame.ProxyReturnIfNeeded(
                _F.CurrentMethod,
                node.ExpressionOpt,
                out returnValue);

            if (returnLabel == null)
            {
                return base.VisitReturnStatement(node);
            }

            var returnExpr = (BoundExpression)(this.Visit(node.ExpressionOpt));
            if (returnExpr != null)
            {
                return _F.Block(
                        _F.Assignment(
                            _F.Local(returnValue),
                            returnExpr),
                        _F.Goto(
                            returnLabel));
            }
            else
            {
                return _F.Goto(returnLabel);
            }
        }

        private BoundStatement UnpendException(LocalSymbol pendingExceptionLocal)
        {
            // create a temp. 
            // pendingExceptionLocal will certainly be captured, no need to access it over and over.
            LocalSymbol obj = _F.SynthesizedLocal(_F.SpecialType(SpecialType.System_Object));
            var objInit = _F.Assignment(_F.Local(obj), _F.Local(pendingExceptionLocal));

            // throw pendingExceptionLocal;
            BoundStatement rethrow = Rethrow(obj);

            return _F.Block(
                    ImmutableArray.Create<LocalSymbol>(obj),
                    objInit,
                    _F.If(
                        _F.ObjectNotEqual(
                            _F.Local(obj),
                            _F.Null(obj.Type)),
                        rethrow));
        }

        private BoundStatement Rethrow(LocalSymbol obj)
        {
            // conservative rethrow 
            BoundStatement rethrow = _F.Throw(_F.Local(obj));

            var exceptionDispatchInfoCapture = _F.WellKnownMethod(WellKnownMember.System_Runtime_ExceptionServices_ExceptionDispatchInfo__Capture, isOptional: true);
            var exceptionDispatchInfoThrow = _F.WellKnownMethod(WellKnownMember.System_Runtime_ExceptionServices_ExceptionDispatchInfo__Throw, isOptional: true);

            // if these helpers are available, we can rethrow with original stack info
            // as long as it derives from Exception
            if (exceptionDispatchInfoCapture != null && exceptionDispatchInfoThrow != null)
            {
                var ex = _F.SynthesizedLocal(_F.WellKnownType(WellKnownType.System_Exception));
                var assignment = _F.Assignment(
                    _F.Local(ex),
                    _F.As(_F.Local(obj), ex.Type));

                // better rethrow 
                rethrow = _F.Block(
                    ImmutableArray.Create(ex),
                    assignment,
                    _F.If(_F.ObjectEqual(_F.Local(ex), _F.Null(ex.Type)), rethrow),
                    // ExceptionDispatchInfo.Capture(pendingExceptionLocal).Throw();
                    _F.ExpressionStatement(
                        _F.Call(
                            _F.StaticCall(
                                exceptionDispatchInfoCapture.ContainingType,
                                exceptionDispatchInfoCapture,
                                _F.Local(ex)),
                            exceptionDispatchInfoThrow)));
            }

            return rethrow;
        }

        /// <summary>
        /// Rewrites Try/Catch part of the Try/Catch/Finally
        /// </summary>
        private BoundStatement RewriteFinalizedRegion(BoundTryStatement node)
        {
            var rewrittenTry = (BoundBlock)this.VisitBlock(node.TryBlock);

            var catches = node.CatchBlocks;
            if (catches.IsDefaultOrEmpty)
            {
                return rewrittenTry;
            }

            var origAwaitCatchFrame = _currentAwaitCatchFrame;
            _currentAwaitCatchFrame = null;

            var rewrittenCatches = this.VisitList(node.CatchBlocks);
            BoundStatement tryWithCatches = _F.Try(rewrittenTry, rewrittenCatches);

            var currentAwaitCatchFrame = _currentAwaitCatchFrame;
            if (currentAwaitCatchFrame != null)
            {
                var handledLabel = _F.GenerateLabel("handled");
                var handlersList = currentAwaitCatchFrame.handlers;
                var handlers = ArrayBuilder<BoundSwitchSection>.GetInstance(handlersList.Count);
                for (int i = 0, l = handlersList.Count; i < l; i++)
                {
                    handlers.Add(_F.SwitchSection(
                        i + 1,
                        _F.Block(
                            handlersList[i],
                            _F.Goto(handledLabel))));
                }

                tryWithCatches = _F.Block(
                    ImmutableArray.Create<LocalSymbol>(
                        currentAwaitCatchFrame.pendingCaughtException,
                        currentAwaitCatchFrame.pendingCatch).
                        AddRange(currentAwaitCatchFrame.GetHoistedLocals()),
                    _F.HiddenSequencePoint(),
                    _F.Assignment(
                        _F.Local(currentAwaitCatchFrame.pendingCatch),
                        _F.Default(currentAwaitCatchFrame.pendingCatch.Type)),
                    tryWithCatches,
                    _F.HiddenSequencePoint(),
                    _F.Switch(
                        _F.Local(currentAwaitCatchFrame.pendingCatch),
                        handlers.ToImmutableAndFree()),
                    _F.HiddenSequencePoint(),
                    _F.Label(handledLabel));
            }

            _currentAwaitCatchFrame = origAwaitCatchFrame;

            return tryWithCatches;
        }

        public override BoundNode VisitCatchBlock(BoundCatchBlock node)
        {
            if (!_analysis.CatchContainsAwait(node))
            {
                var origCurrentAwaitCatchFrame = _currentAwaitCatchFrame;
                _currentAwaitCatchFrame = null;

                var result = base.VisitCatchBlock(node);
                _currentAwaitCatchFrame = origCurrentAwaitCatchFrame;
                return result;
            }

            var currentAwaitCatchFrame = _currentAwaitCatchFrame;
            if (currentAwaitCatchFrame == null)
            {
                Debug.Assert(node.Syntax.IsKind(SyntaxKind.CatchClause));
                var tryStatementSyntax = (TryStatementSyntax)node.Syntax.Parent;

                currentAwaitCatchFrame = _currentAwaitCatchFrame = new AwaitCatchFrame(_F, tryStatementSyntax);
            }

            var catchType = node.ExceptionTypeOpt ?? _F.SpecialType(SpecialType.System_Object);
            var catchTemp = _F.SynthesizedLocal(catchType);

            var storePending = _F.AssignmentExpression(
                        _F.Local(currentAwaitCatchFrame.pendingCaughtException),
                        _F.Convert(currentAwaitCatchFrame.pendingCaughtException.Type,
                            _F.Local(catchTemp)));

            var setPendingCatchNum = _F.Assignment(
                            _F.Local(currentAwaitCatchFrame.pendingCatch),
                            _F.Literal(currentAwaitCatchFrame.handlers.Count + 1));

            //  catch (ExType exTemp)
            //  {
            //      pendingCaughtException = exTemp;
            //      catchNo = X;
            //  }
            BoundCatchBlock catchAndPend;
            var handlerLocals = ImmutableArray<LocalSymbol>.Empty;

            var filterOpt = node.ExceptionFilterOpt;
            if (filterOpt == null)
            {
                // store pending exception 
                // as the first statement in a catch
                catchAndPend = node.Update(
                    catchTemp,
                    _F.Local(catchTemp),
                    catchType,
                    exceptionFilterOpt: null,
                    body: _F.Block(
                        _F.HiddenSequencePoint(),
                        _F.ExpressionStatement(storePending),
                        setPendingCatchNum),
                    isSynthesizedAsyncCatchAll: node.IsSynthesizedAsyncCatchAll);

                // catch locals live on the synthetic catch handler block
                if ((object)node.LocalOpt != null)
                {
                    handlerLocals = ImmutableArray.Create(node.LocalOpt);
                }
            }
            else
            {
                // catch locals move up into hoisted locals
                // since we might need to access them from both the filter and the catch
                if ((object)node.LocalOpt != null)
                {
                    currentAwaitCatchFrame.HoistLocal(node.LocalOpt, _F);
                }

                // store pending exception 
                // as the first expression in a filter
                var sourceOpt = node.ExceptionSourceOpt;
                var rewrittenFilter = (BoundExpression)this.Visit(filterOpt);
                var newFilter = sourceOpt == null ?
                                _F.Sequence(
                                    storePending,
                                    rewrittenFilter) :
                                _F.Sequence(
                                    storePending,
                                    AssignCatchSource((BoundExpression)this.Visit(sourceOpt), currentAwaitCatchFrame),
                                    rewrittenFilter);

                catchAndPend = node.Update(
                    catchTemp,
                    _F.Local(catchTemp),
                    catchType,
                    exceptionFilterOpt: newFilter,
                    body: _F.Block(
                        _F.HiddenSequencePoint(),
                        setPendingCatchNum),
                    isSynthesizedAsyncCatchAll: node.IsSynthesizedAsyncCatchAll);
            }

            var handlerStatements = ArrayBuilder<BoundStatement>.GetInstance();

            handlerStatements.Add(_F.HiddenSequencePoint());

            if (filterOpt == null)
            {
                var sourceOpt = node.ExceptionSourceOpt;
                if (sourceOpt != null)
                {
                    BoundExpression assignSource = AssignCatchSource((BoundExpression)this.Visit(sourceOpt), currentAwaitCatchFrame);
                    handlerStatements.Add(_F.ExpressionStatement(assignSource));
                }
            }

            handlerStatements.Add((BoundStatement)this.Visit(node.Body));

            var handler = _F.Block(
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
                assignSource = _F.AssignmentExpression(
                                    rewrittenSource,
                                    _F.Convert(
                                        rewrittenSource.Type,
                                        _F.Local(currentAwaitCatchFrame.pendingCaughtException)));
            }

            return assignSource;
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            var catchFrame = _currentAwaitCatchFrame;
            LocalSymbol hoistedLocal;
            if (catchFrame == null || !catchFrame.TryGetHoistedLocal(node.LocalSymbol, out hoistedLocal))
            {
                return base.VisitLocal(node);
            }

            return node.Update(hoistedLocal, node.ConstantValueOpt, hoistedLocal.Type);
        }

        public override BoundNode VisitThrowStatement(BoundThrowStatement node)
        {
            if (node.ExpressionOpt != null || _currentAwaitCatchFrame == null)
            {
                return base.VisitThrowStatement(node);
            }

            return Rethrow(_currentAwaitCatchFrame.pendingCaughtException);
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            var oldContainingSymbol = _F.CurrentMethod;
            var oldAwaitFinallyFrame = _currentAwaitFinallyFrame;

            _F.CurrentMethod = node.Symbol;
            _currentAwaitFinallyFrame = new AwaitFinallyFrame();

            var result = base.VisitLambda(node);

            _F.CurrentMethod = oldContainingSymbol;
            _currentAwaitFinallyFrame = oldAwaitFinallyFrame;

            return result;
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            var oldContainingSymbol = _F.CurrentMethod;
            var oldAwaitFinallyFrame = _currentAwaitFinallyFrame;

            _F.CurrentMethod = node.Symbol;
            _currentAwaitFinallyFrame = new AwaitFinallyFrame();

            var result = base.VisitLocalFunctionStatement(node);

            _F.CurrentMethod = oldContainingSymbol;
            _currentAwaitFinallyFrame = oldAwaitFinallyFrame;

            return result;
        }

        private AwaitFinallyFrame PushFrame(BoundTryStatement statement)
        {
            var newFrame = new AwaitFinallyFrame(_currentAwaitFinallyFrame, _analysis.Labels(statement), (TryStatementSyntax)statement.Syntax);
            _currentAwaitFinallyFrame = newFrame;
            return newFrame;
        }

        private void PopFrame()
        {
            var result = _currentAwaitFinallyFrame;
            _currentAwaitFinallyFrame = result.ParentOpt;
        }

        /// <summary>
        /// Analyses method body for try blocks with awaits in finally blocks 
        /// Also collects labels that such blocks contain.
        /// </summary>
        private sealed class AwaitInFinallyAnalysis : LabelCollector
        {
            // all try blocks with yields in them and complete set of labels inside those try blocks
            // NOTE: non-yielding try blocks are transparently ignored - i.e. their labels are included
            //       in the label set of the nearest yielding-try parent  
            private Dictionary<BoundTryStatement, HashSet<LabelSymbol>> _labelsInInterestingTry;

            private HashSet<BoundCatchBlock> _awaitContainingCatches;

            // transient accumulators.
            private bool _seenAwait;

            public AwaitInFinallyAnalysis(BoundStatement body)
            {
                _seenAwait = false;
                this.Visit(body);
            }

            /// <summary>
            /// Returns true if a finally of the given try contains awaits
            /// </summary>
            public bool FinallyContainsAwaits(BoundTryStatement statement)
            {
                return _labelsInInterestingTry != null && _labelsInInterestingTry.ContainsKey(statement);
            }

            /// <summary>
            /// Returns true if a catch contains awaits
            /// </summary>
            internal bool CatchContainsAwait(BoundCatchBlock node)
            {
                return _awaitContainingCatches != null && _awaitContainingCatches.Contains(node);
            }

            /// <summary>
            /// Returns true if body contains await in a finally block.
            /// </summary>
            public bool ContainsAwaitInHandlers()
            {
                return _labelsInInterestingTry != null || _awaitContainingCatches != null;
            }

            /// <summary>
            /// Labels reachable from within this frame without invoking its finally. 
            /// null if there are no such labels.
            /// </summary>
            internal HashSet<LabelSymbol> Labels(BoundTryStatement statement)
            {
                return _labelsInInterestingTry[statement];
            }

            public override BoundNode VisitTryStatement(BoundTryStatement node)
            {
                var origLabels = this.currentLabels;
                this.currentLabels = null;
                Visit(node.TryBlock);
                VisitList(node.CatchBlocks);

                var origSeenAwait = _seenAwait;
                _seenAwait = false;
                Visit(node.FinallyBlockOpt);

                if (_seenAwait)
                {
                    // this try has awaits in the finally !
                    var labelsInInterestingTry = _labelsInInterestingTry;
                    if (labelsInInterestingTry == null)
                    {
                        _labelsInInterestingTry = labelsInInterestingTry = new Dictionary<BoundTryStatement, HashSet<LabelSymbol>>();
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

                _seenAwait = _seenAwait | origSeenAwait;
                return null;
            }

            public override BoundNode VisitCatchBlock(BoundCatchBlock node)
            {
                var origSeenAwait = _seenAwait;
                _seenAwait = false;

                var result = base.VisitCatchBlock(node);

                if (_seenAwait)
                {
                    var awaitContainingCatches = _awaitContainingCatches;
                    if (awaitContainingCatches == null)
                    {
                        _awaitContainingCatches = awaitContainingCatches = new HashSet<BoundCatchBlock>();
                    }

                    _awaitContainingCatches.Add(node);
                }

                _seenAwait |= origSeenAwait;
                return result;
            }

            public override BoundNode VisitAwaitExpression(BoundAwaitExpression node)
            {
                _seenAwait = true;
                return base.VisitAwaitExpression(node);
            }

            public override BoundNode VisitLambda(BoundLambda node)
            {
                var origLabels = this.currentLabels;
                var origSeenAwait = _seenAwait;

                this.currentLabels = null;
                _seenAwait = false;

                base.VisitLambda(node);

                this.currentLabels = origLabels;
                _seenAwait = origSeenAwait;

                return null;
            }

            public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
            {
                var origLabels = this.currentLabels;
                var origSeenAwait = _seenAwait;

                this.currentLabels = null;
                _seenAwait = false;

                base.VisitLocalFunctionStatement(node);

                this.currentLabels = origLabels;
                _seenAwait = origSeenAwait;

                return null;
            }
        }

        // storage of various information about a given finally frame
        private sealed class AwaitFinallyFrame
        {
            // Enclosing frame. Root frame does not have parent.
            public readonly AwaitFinallyFrame ParentOpt;

            // labels within this frame (branching to these labels does not go through finally).
            public readonly HashSet<LabelSymbol> LabelsOpt;

            // the try statement the frame is associated with
            private readonly TryStatementSyntax _tryStatementSyntaxOpt;

            // proxy labels for branches leaving the frame. 
            // we build this on demand once we encounter leaving branches.
            // subsequent leaves to an already proxied label redirected to the proxy.
            // At the proxy label we will execute finally and forward the control flow 
            // to the actual destination. (which could be proxied again in the parent)
            public Dictionary<LabelSymbol, LabelSymbol> proxyLabels;

            public List<LabelSymbol> proxiedLabels;

            public GeneratedLabelSymbol returnProxyLabel;
            public SynthesizedLocal returnValue;

            public AwaitFinallyFrame()
            {
                // root frame
            }

            public AwaitFinallyFrame(AwaitFinallyFrame parent, HashSet<LabelSymbol> labelsOpt, TryStatementSyntax tryStatementSyntax)
            {
                Debug.Assert(parent != null);
                Debug.Assert(tryStatementSyntax != null);

                this.ParentOpt = parent;
                this.LabelsOpt = labelsOpt;
                _tryStatementSyntaxOpt = tryStatementSyntax;
            }

            public bool IsRoot()
            {
                return this.ParentOpt == null;
            }

            // returns a proxy for a label if branch must be hijacked to run finally
            // otherwise returns same label back
            public LabelSymbol ProxyLabelIfNeeded(LabelSymbol label)
            {
                // no need to proxy a label in the current frame or when we are at the root
                if (this.IsRoot() || (LabelsOpt != null && LabelsOpt.Contains(label)))
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
                out SynthesizedLocal returnValue)
            {
                returnValue = null;

                // no need to proxy returns  at the root
                if (this.IsRoot())
                {
                    return null;
                }

                var returnProxy = this.returnProxyLabel;
                if (returnProxy == null)
                {
                    this.returnProxyLabel = returnProxy = new GeneratedLabelSymbol("returnProxy");
                }

                if (valueOpt != null)
                {
                    returnValue = this.returnValue;
                    if (returnValue == null)
                    {
                        Debug.Assert(_tryStatementSyntaxOpt != null);
                        this.returnValue = returnValue = new SynthesizedLocal(containingMethod, valueOpt.Type, SynthesizedLocalKind.AsyncMethodReturnValue, _tryStatementSyntaxOpt);
                    }
                }

                return returnProxy;
            }
        }

        private sealed class AwaitCatchFrame
        {
            // object, stores the original caught exception
            // used to initialize the exception source inside the handler
            // also used in rethrow statements
            public readonly SynthesizedLocal pendingCaughtException;

            // int, stores the number of pending catch
            // 0 - means no catches are pending.
            public readonly SynthesizedLocal pendingCatch;

            // synthetic handlers produced by catch rewrite.
            // they will become switch sections when pending exception is dispatched.
            public readonly List<BoundBlock> handlers;

            // when catch local must be used from a filter
            // we need to "hoist" it up to ensure that both the filter 
            // and the catch access the same variable.
            // NOTE: it must be the same variable, not just same value. 
            //       The difference would be observable if filter mutates the variable
            //       or/and if a variable gets lifted into a closure.
            private readonly Dictionary<LocalSymbol, LocalSymbol> _hoistedLocals;
            private readonly List<LocalSymbol> _orderedHoistedLocals;

            public AwaitCatchFrame(SyntheticBoundNodeFactory F, TryStatementSyntax tryStatementSyntax)
            {
                this.pendingCaughtException = new SynthesizedLocal(F.CurrentMethod, F.SpecialType(SpecialType.System_Object), SynthesizedLocalKind.TryAwaitPendingCaughtException, tryStatementSyntax);
                this.pendingCatch = new SynthesizedLocal(F.CurrentMethod, F.SpecialType(SpecialType.System_Int32), SynthesizedLocalKind.TryAwaitPendingCatch, tryStatementSyntax);

                this.handlers = new List<BoundBlock>();
                _hoistedLocals = new Dictionary<LocalSymbol, LocalSymbol>();
                _orderedHoistedLocals = new List<LocalSymbol>();
            }

            public void HoistLocal(LocalSymbol local, SyntheticBoundNodeFactory F)
            {
                if (!_hoistedLocals.Keys.Any(l => l.Name == local.Name && l.Type == local.Type))
                {
                    _hoistedLocals.Add(local, local);
                    _orderedHoistedLocals.Add(local);
                    return;
                }

                // code uses "await" in two sibling catches with exception filters
                // locals with same names and types may cause problems if they are lifted
                // and become fields with identical signatures.
                // To avoid such problems we will mangle the name of the second local.
                // This will only affect debugging of this extremely rare case.
                Debug.Assert(pendingCatch.SyntaxOpt.IsKind(SyntaxKind.TryStatement));
                var newLocal = F.SynthesizedLocal(local.Type, pendingCatch.SyntaxOpt, kind: SynthesizedLocalKind.ExceptionFilterAwaitHoistedExceptionLocal);

                _hoistedLocals.Add(local, newLocal);
                _orderedHoistedLocals.Add(newLocal);
            }

            public IEnumerable<LocalSymbol> GetHoistedLocals()
            {
                return _orderedHoistedLocals;
            }

            public bool TryGetHoistedLocal(LocalSymbol originalLocal, out LocalSymbol hoistedLocal)
            {
                return _hoistedLocals.TryGetValue(originalLocal, out hoistedLocal);
            }
        }
    }
}
