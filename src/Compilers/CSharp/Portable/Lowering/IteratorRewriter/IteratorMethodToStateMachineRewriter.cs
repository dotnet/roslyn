// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class IteratorMethodToStateMachineRewriter : MethodToStateMachineRewriter
    {
        /// <summary>
        /// The field of the generated iterator class that underlies the Current property.
        /// </summary>
        private readonly FieldSymbol _current;

        /// <summary>
        /// Tells us if a particular try contains yield returns
        /// </summary>
        private YieldsInTryAnalysis _yieldsInTryAnalysis;

        /// <summary>
        /// When this is more that 0, returns are emitted as "methodValue = value; goto exitLabel;"
        /// </summary>
        private int _tryNestingLevel;
        private LabelSymbol _exitLabel;
        private LocalSymbol _methodValue;

        /// <summary>
        /// The current iterator finally frame in the tree of finally frames.
        /// By default there is a root finally frame.
        /// Root frame does not have a handler, but may contain nested frames.
        /// </summary>
        private IteratorFinallyFrame _currentFinallyFrame = new IteratorFinallyFrame();

        /// <summary>
        /// Finally state of the next Finally frame if such created.
        /// Finally state is a negative decreasing number starting with -3. (-2 is used for something else).
        /// Root frame has finally state -1.
        /// 
        /// The Finally state is the state that we are in when "between states".
        /// Regular states are positive and are the only states that can be resumed to.
        /// The purpose of distinct finally states is to have enough information about 
        /// which finally handlers must run when we need to finalize iterator after a fault. 
        /// </summary>
        private int _nextFinalizeState = StateMachineStates.FinishedStateMachine - 1;  // -3

        internal IteratorMethodToStateMachineRewriter(
            SyntheticBoundNodeFactory F,
            MethodSymbol originalMethod,
            FieldSymbol state,
            FieldSymbol current,
            IReadOnlySet<Symbol> hoistedVariables,
            IReadOnlyDictionary<Symbol, CapturedSymbolReplacement> nonReusableLocalProxies,
            SynthesizedLocalOrdinalsDispenser synthesizedLocalOrdinals,
            VariableSlotAllocator slotAllocatorOpt,
            int nextFreeHoistedLocalSlot,
            DiagnosticBag diagnostics)
            : base(F, originalMethod, state, hoistedVariables, nonReusableLocalProxies, synthesizedLocalOrdinals, slotAllocatorOpt, nextFreeHoistedLocalSlot, diagnostics, useFinalizerBookkeeping: false)
        {
            _current = current;
        }

        internal void GenerateMoveNextAndDispose(BoundStatement body, SynthesizedImplementationMethod moveNextMethod, SynthesizedImplementationMethod disposeMethod)
        {
            // scan body for yielding try blocks
            _yieldsInTryAnalysis = new YieldsInTryAnalysis(body);
            if (_yieldsInTryAnalysis.ContainsYieldsInTrys())
            {
                // adjust for the method Try/Fault block that we will put around the body.
                _tryNestingLevel++;
            }

            /////////////////////////////////// 
            // Generate the body for MoveNext()
            ///////////////////////////////////

            F.CurrentFunction = moveNextMethod;
            int initialState;
            GeneratedLabelSymbol initialLabel;
            AddState(out initialState, out initialLabel);
            var newBody = (BoundStatement)Visit(body);

            // switch(cachedState) {
            //    case 0: goto state_0;
            //    case 1: goto state_1;
            //    //etc
            //    default: return false;
            // }
            // state_0:
            // state = -1;
            // [optional: cachedThis = capturedThis;] 
            // [[rewritten body]]
            newBody = F.Block((object)cachedThis == null ?
                                ImmutableArray.Create(cachedState) :
                                ImmutableArray.Create(cachedState, cachedThis),

                    F.HiddenSequencePoint(),
                    F.Assignment(F.Local(cachedState), F.Field(F.This(), stateField)),
                    CacheThisIfNeeded(),
                    Dispatch(),
                    GenerateReturn(finished: true),
                    F.Label(initialLabel),
                    F.Assignment(F.Field(F.This(), stateField), F.Literal(StateMachineStates.NotStartedStateMachine)),
                    newBody);

            //
            // C# spec requires that iterators trap all exceptions and self-dispose eagerly.
            //
            // 10.14.4.1 The MoveNext method
            // . . .
            // When an exception is thrown and propagated out of the iterator block:
            // o   Appropriate finally blocks in the iterator body will have been executed by the exception propagation.
            // o   The state of the enumerator object is changed to after.
            // o   The exception propagation continues to the caller of the MoveNext method.
            // . . .
            //
            if (_yieldsInTryAnalysis.ContainsYieldsInTrys())
            {
                // try 
                // {
                //    body;
                // }
                // fault
                // {
                //    this.Dispose();
                // }

                var faultBlock = F.Block(F.ExpressionStatement(F.Call(F.This(), disposeMethod)));
                newBody = F.Fault((BoundBlock)newBody, faultBlock);
            }

            newBody = HandleReturn(newBody);
            F.CloseMethod(F.SequencePoint(body.Syntax, newBody));

            /////////////////////////////////// 
            // Generate the body for Dispose().
            ///////////////////////////////////
            F.CurrentFunction = disposeMethod;
            var rootFrame = _currentFinallyFrame;

            if (rootFrame.knownStates == null)
            {
                // nothing to finalize
                F.CloseMethod(F.Return());
            }
            else
            {
                var stateLocal = F.SynthesizedLocal(stateField.Type);
                var state = F.Local(stateLocal);

                var disposeBody = F.Block(
                                    ImmutableArray.Create<LocalSymbol>(stateLocal),
                                    F.Assignment(F.Local(stateLocal), F.Field(F.This(), stateField)),
                                    EmitFinallyFrame(rootFrame, state),
                                    F.Return());

                F.CloseMethod(disposeBody);
            }
        }

        private BoundStatement HandleReturn(BoundStatement newBody)
        {
            if ((object)_exitLabel == null)
            {
                //   body;
                //   return false;
                newBody = F.Block(
                        newBody,
                        F.Return(F.Literal(false)));
            }
            else
            {
                //   body;
                //   methodValue = false;
                // exitLabel:
                //   return methodValue;
                newBody = F.Block(
                        ImmutableArray.Create<LocalSymbol>(_methodValue),
                        newBody,
                        F.Assignment(this.F.Local(_methodValue), this.F.Literal(true)),
                        F.Label(_exitLabel),
                        F.Return(this.F.Local(_methodValue)));
            }

            return newBody;
        }

        /// <summary>
        /// Produces a Try/Finally if frame has a handler (otherwise a regular block).
        /// Handler goes into the Finally.
        /// If there are nested frames, they are emitted into the try block.
        /// This way the handler for the current frame is guaranteed to run even if 
        /// nested handlers throw exceptions.
        /// 
        /// {
        ///     switch(state)
        ///     {
        ///         case state1:
        ///         case state2:
        ///         case state3:
        ///         case state4:
        ///             try
        ///             {
        ///                 switch(state)
        ///                 {
        ///                     case state3:
        ///                     case state4:
        ///                         try
        ///                         {
        ///                             ... more nested state dispatches if any ....
        ///                         }
        ///                         finally
        ///                         {
        ///                             // handler for a try where state3 and state4 can be observed
        ///                             handler_3_4()
        ///                         }
        ///                         break;
        ///                  }
        ///             }
        ///             finally
        ///             {
        ///                 // handler for a try where state1 and state2 can be observed
        ///                 handler_1_2()
        ///             }
        ///             break;
        ///             
        ///         case state5:
        ///             ... another dispatch of nested states to their finally blocks ...
        ///             break;
        ///     }
        /// }
        /// 
        /// </summary>
        private BoundStatement EmitFinallyFrame(IteratorFinallyFrame frame, BoundLocal state)
        {
            BoundStatement body = null;
            if (frame.knownStates != null)
            {
                var breakLabel = F.GenerateLabel("break");
                var sections = from ft in frame.knownStates
                               group ft.Key by ft.Value into g
                               select F.SwitchSection(
                                    new List<int>(g),
                                    EmitFinallyFrame(g.Key, state),
                                    F.Goto(breakLabel));

                body = F.Block(
                    F.Switch(state, sections.ToImmutableArray()),
                    F.Label(breakLabel));
            }

            if (!frame.IsRoot())
            {
                var tryBlock = body != null ? F.Block(body) : F.Block();
                body = F.Try(
                    tryBlock,
                    ImmutableArray<BoundCatchBlock>.Empty,
                    F.Block(F.ExpressionStatement(F.Call(F.This(), frame.handler))));
            }

            Debug.Assert(body != null, "we should have either sub-dispatch or a handler");
            return body;
        }

        protected override BoundStatement GenerateReturn(bool finished)
        {
            BoundLiteral result = this.F.Literal(!finished);

            if (_tryNestingLevel == 0)
            {
                return F.Return(result);
            }
            else
            {
                if ((object)_exitLabel == null)
                {
                    _exitLabel = this.F.GenerateLabel("exitLabel");
                    _methodValue = F.SynthesizedLocal(result.Type);
                }

                var gotoExit = F.Goto(_exitLabel);

                if (finished)
                {
                    // since we are finished, we need to treat this as a potential Leave
                    gotoExit = (BoundGotoStatement)VisitGotoStatement(gotoExit);
                }

                return this.F.Block(
                     F.Assignment(this.F.Local(_methodValue), result),
                     gotoExit);
            }
        }


        #region Visitors

        public override BoundNode VisitYieldBreakStatement(BoundYieldBreakStatement node)
        {
            return GenerateReturn(finished: true);
        }

        public override BoundNode VisitYieldReturnStatement(BoundYieldReturnStatement node)
        {
            //     yield return expression;
            // is translated to
            //     this.current = expression;
            //     this.state = <next_state>;
            //     return true;
            //     <next_state_label>: ;
            //     <hidden sequence point>
            //     this.state = finalizeState;
            int stateNumber;
            GeneratedLabelSymbol resumeLabel;
            AddState(out stateNumber, out resumeLabel);
            _currentFinallyFrame.AddState(stateNumber);

            var rewrittenExpression = (BoundExpression)Visit(node.Expression);

            return F.Block(
                F.Assignment(F.Field(F.This(), _current), rewrittenExpression),
                F.Assignment(F.Field(F.This(), stateField), F.Literal(stateNumber)),
                GenerateReturn(finished: false),
                F.Label(resumeLabel),
                F.HiddenSequencePoint(),
                F.Assignment(F.Field(F.This(), stateField), F.Literal(_currentFinallyFrame.finalizeState)));
        }

        public override BoundNode VisitGotoStatement(BoundGotoStatement node)
        {
            BoundExpression caseExpressionOpt = (BoundExpression)this.Visit(node.CaseExpressionOpt);
            BoundLabel labelExpressionOpt = (BoundLabel)this.Visit(node.LabelExpressionOpt);
            var proxyLabel = _currentFinallyFrame.ProxyLabelIfNeeded(node.Label);
            Debug.Assert(node.Label == proxyLabel || !(F.CurrentFunction is IteratorFinallyMethodSymbol), "should not be proxying branches in finally");
            return node.Update(proxyLabel, caseExpressionOpt, labelExpressionOpt);
        }

        public override BoundNode VisitConditionalGoto(BoundConditionalGoto node)
        {
            Debug.Assert(node.Label == _currentFinallyFrame.ProxyLabelIfNeeded(node.Label), "conditional leave?");
            return base.VisitConditionalGoto(node);
        }

        public override BoundNode VisitTryStatement(BoundTryStatement node)
        {
            // if node contains no yields, do regular rewrite in the current frame.
            if (!ContainsYields(node))
            {
                _tryNestingLevel++;
                var result = node.Update(
                                    (BoundBlock)Visit(node.TryBlock),
                                    VisitList(node.CatchBlocks),
                                    (BoundBlock)Visit(node.FinallyBlockOpt),
                                    node.FinallyLabelOpt,
                                    node.PreferFaultHandler);

                _tryNestingLevel--;
                return result;
            }

            Debug.Assert(node.CatchBlocks.IsEmpty, "try with yields must have no catches");
            Debug.Assert(node.FinallyBlockOpt != null, "try with yields must have finally");

            // rewrite TryBlock in a new frame.
            var frame = PushFrame(node);
            _tryNestingLevel++;
            var rewrittenBody = (BoundStatement)this.Visit(node.TryBlock);

            Debug.Assert(!frame.IsRoot());
            Debug.Assert(frame.parent.knownStates.ContainsValue(frame), "parent must be aware about states in the child frame");

            var finallyMethod = frame.handler;
            var origMethod = F.CurrentFunction;

            // rewrite finally block into a Finally method.
            F.CurrentFunction = finallyMethod;
            var rewrittenHandler = (BoundStatement)this.Visit(node.FinallyBlockOpt);

            _tryNestingLevel--;
            PopFrame();

            // {
            //      this.state = parentFinalizeState;
            //      body;
            //      return;
            // }
            Debug.Assert(frame.parent.finalizeState == _currentFinallyFrame.finalizeState);
            rewrittenHandler = F.Block((object)this.cachedThis != null ?
                                            ImmutableArray.Create(this.cachedThis) :
                                            ImmutableArray<LocalSymbol>.Empty,
                                F.Assignment(F.Field(F.This(), stateField), F.Literal(frame.parent.finalizeState)),
                                CacheThisIfNeeded(),
                                rewrittenHandler,
                                F.Return()
                            );

            F.CloseMethod(rewrittenHandler);
            F.CurrentFunction = origMethod;


            var bodyStatements = ArrayBuilder<BoundStatement>.GetInstance();

            // add a call to the handler after the try body.
            //
            // {
            //      this.state = finalizeState;
            //      body;
            //      this.Finally();   // will reset the state to the finally state of the parent.
            // }
            bodyStatements.Add(F.Assignment(F.Field(F.This(), stateField), F.Literal(frame.finalizeState)));
            bodyStatements.Add(rewrittenBody);
            bodyStatements.Add(F.ExpressionStatement(F.Call(F.This(), finallyMethod)));

            // handle proxy labels if have any
            if (frame.proxyLabels != null)
            {
                var dropThrough = F.GenerateLabel("dropThrough");
                bodyStatements.Add(F.Goto(dropThrough));
                var parent = frame.parent;

                foreach (var p in frame.proxyLabels)
                {
                    var proxy = p.Value;
                    var destination = p.Key;

                    // branch lands here
                    bodyStatements.Add(F.Label(proxy));

                    // finalize current state, proceed to destination.
                    bodyStatements.Add(F.ExpressionStatement(F.Call(F.This(), finallyMethod)));

                    // let the parent forward the branch appropriately
                    var parentProxy = parent.ProxyLabelIfNeeded(destination);
                    bodyStatements.Add(F.Goto(parentProxy));
                }

                bodyStatements.Add(F.Label(dropThrough));
            }

            return F.Block(bodyStatements.ToImmutableAndFree());
        }

        private IteratorFinallyFrame PushFrame(BoundTryStatement statement)
        {
            var state = _nextFinalizeState--;

            var finallyMethod = MakeSynthesizedFinally(state);
            var newFrame = new IteratorFinallyFrame(_currentFinallyFrame, state, finallyMethod, _yieldsInTryAnalysis.Labels(statement));
            newFrame.AddState(state);

            _currentFinallyFrame = newFrame;
            return newFrame;
        }

        private void PopFrame()
        {
            var result = _currentFinallyFrame;
            _currentFinallyFrame = result.parent;
        }

        private bool ContainsYields(BoundTryStatement statement)
        {
            return _yieldsInTryAnalysis.ContainsYields(statement);
        }

        private IteratorFinallyMethodSymbol MakeSynthesizedFinally(int state)
        {
            var stateMachineType = (IteratorStateMachine)F.CurrentType;
            var finallyMethod = new IteratorFinallyMethodSymbol(stateMachineType, GeneratedNames.MakeIteratorFinallyMethodName(state));

            F.ModuleBuilderOpt.AddSynthesizedDefinition(stateMachineType, finallyMethod);
            return finallyMethod;
        }

        #endregion
    }
}
