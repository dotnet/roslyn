﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        /// <summary>
        /// This is the entry point for foreach-loop lowering.  It delegates to
        ///   RewriteEnumeratorForEachStatement
        ///   RewriteSingleDimensionalArrayForEachStatement
        ///   RewriteMultiDimensionalArrayForEachStatement
        ///   CanRewriteForEachAsFor
        ///   RewriteAsyncForEachStatement
        /// </summary>
        /// <remarks>
        /// We are diverging from the C# 4 spec (and Dev10) to follow the C# 5 spec.
        /// The iteration variable will be declared *inside* each loop iteration,
        /// rather than outside the loop.
        /// </remarks>
        public override BoundNode VisitForEachStatement(BoundForEachStatement node)
        {
            // No point in performing this lowering if the node won't be emitted.
            if (node.HasErrors)
            {
                return node;
            }

            if (node.EnumeratorInfoOpt.IsAsync)
            {
                return RewriteAsyncForEachStatement(node);
            }

            BoundExpression collectionExpression = GetUnconvertedCollectionExpression(node);
            TypeSymbol nodeExpressionType = collectionExpression.Type;
            if (nodeExpressionType.Kind == SymbolKind.ArrayType)
            {
                ArrayTypeSymbol arrayType = (ArrayTypeSymbol)nodeExpressionType;
                if (arrayType.IsSZArray)
                {
                    return RewriteSingleDimensionalArrayForEachStatement(node);
                }
                else
                {
                    return RewriteMultiDimensionalArrayForEachStatement(node);
                }
            }
            else if (CanRewriteForEachAsFor(node.Syntax, nodeExpressionType, out var indexerGet, out var lengthGetter))
            {
                return RewriteForEachStatementAsFor(node, indexerGet, lengthGetter);
            }
            else
            {
                return RewriteEnumeratorForEachStatement(node);
            }
        }

        private bool CanRewriteForEachAsFor(SyntaxNode forEachSyntax, TypeSymbol nodeExpressionType, out MethodSymbol indexerGet, out MethodSymbol lengthGet)
        {
            lengthGet = indexerGet = null;
            var origDefinition = nodeExpressionType.OriginalDefinition;

            if (origDefinition.SpecialType == SpecialType.System_String)
            {
                lengthGet = UnsafeGetSpecialTypeMethod(forEachSyntax, SpecialMember.System_String__Length);
                indexerGet = UnsafeGetSpecialTypeMethod(forEachSyntax, SpecialMember.System_String__Chars);
            }
            else if ((object)origDefinition == this._compilation.GetWellKnownType(WellKnownType.System_Span_T))
            {
                var spanType = (NamedTypeSymbol)nodeExpressionType;
                lengthGet = (MethodSymbol)_factory.WellKnownMember(WellKnownMember.System_Span_T__get_Length, isOptional: true)?.SymbolAsMember(spanType);
                indexerGet = (MethodSymbol)_factory.WellKnownMember(WellKnownMember.System_Span_T__get_Item, isOptional: true)?.SymbolAsMember(spanType);
            }
            else if ((object)origDefinition == this._compilation.GetWellKnownType(WellKnownType.System_ReadOnlySpan_T))
            {
                var spanType = (NamedTypeSymbol)nodeExpressionType;
                lengthGet = (MethodSymbol)_factory.WellKnownMember(WellKnownMember.System_ReadOnlySpan_T__get_Length, isOptional: true)?.SymbolAsMember(spanType);
                indexerGet = (MethodSymbol)_factory.WellKnownMember(WellKnownMember.System_ReadOnlySpan_T__get_Item, isOptional: true)?.SymbolAsMember(spanType);
            }

            return (object)lengthGet != null && (object)indexerGet != null;
        }

        /// <summary>
        /// Lower an async foreach loop.
        ///
        /// <![CDATA[
        /// E e = ((C)(x)).GetAsyncEnumerator()
        /// try
        /// {
        ///     while (await e.WaitForNextAsync()) /* outer loop */
        ///     {
        ///         while (true) /* inner loop */
        ///         {
        ///             V v = (V)e.TryGetNext(out bool success);
        ///             if (!success) goto outer_loop_continue;
        ///             /* loop.Body */
        ///             /* loop.ContinueLabel: */
        ///         }
        ///         outer_loop_continue:
        ///     }
        ///     /* loop.BreakLabel: */
        /// }
        /// finally { await e.DisposeAsync(); }
        /// ]]>
        /// </summary>
        private BoundStatement RewriteAsyncForEachStatement(BoundForEachStatement loop)
        {
            _sawAwait = true;
            var forEachSyntax = (CommonForEachStatementSyntax)loop.Syntax;
            ForEachEnumeratorInfo enumeratorInfo = loop.EnumeratorInfoOpt;

            Debug.Assert(loop.AwaitOpt != null && (object)loop.AwaitOpt.GetResult != null);
            Debug.Assert(enumeratorInfo.CurrentPropertyGetter is null);
            // PROTOTYPE(async-streams) Is it possible for null GetResult for some kind of dynamic case?

            // E e
            TypeSymbol enumeratorType = enumeratorInfo.GetEnumeratorMethod.ReturnType;
            LocalSymbol enumeratorVar = _factory.SynthesizedLocal(enumeratorType, syntax: forEachSyntax, kind: SynthesizedLocalKind.ForEachEnumerator);

            // Reference to e
            BoundLocal boundEnumeratorVar = MakeBoundLocal(forEachSyntax, enumeratorVar, enumeratorVar.Type).MakeCompilerGenerated();

            // ((C)(x)).GetAsyncEnumerator() or (x).GetAsyncEnumerator();
            BoundExpression collectionExpression = GetUnconvertedCollectionExpression(loop);
            BoundExpression enumeratorVarInitValue = SynthesizeCall(forEachSyntax, collectionExpression, enumeratorInfo.GetEnumeratorMethod, enumeratorInfo.CollectionConversion, enumeratorInfo.CollectionType);

            // E e = ((C)(x)).GetAsyncEnumerator();
            BoundStatement enumeratorVarDecl = MakeLocalDeclaration(forEachSyntax, enumeratorVar, enumeratorVarInitValue);

            // e.WaitForNextAsync()
            BoundExpression waitForNext = BoundCall.Synthesized(forEachSyntax, boundEnumeratorVar, enumeratorInfo.WaitForNextAsyncMethod);

            // await e.WaitForNextAsync()
            BoundAwaitExpression awaitWaitForNext = new BoundAwaitExpression(forEachSyntax, waitForNext, loop.AwaitOpt, loop.AwaitOpt.GetResult.ReturnType).MakeCompilerGenerated();

            // bool success
            LocalSymbol successVar = _factory.SynthesizedLocal(enumeratorInfo.TryGetNextMethod.ParameterTypes[0], syntax: forEachSyntax, kind: SynthesizedLocalKind.LoweringTemp);

            // Reference to success
            BoundLocal boundSuccessVar = MakeBoundLocal(forEachSyntax, successVar, successVar.Type).MakeCompilerGenerated();

            // /* loop.Body */
            BoundStatement rewrittenBody = (BoundStatement)Visit(loop.Body);

            // e.TryGetNext(out bool success)
            BoundCall tryGetNext = new BoundCall(forEachSyntax,
                  receiverOpt: boundEnumeratorVar,
                  method: enumeratorInfo.TryGetNextMethod,
                  arguments: ImmutableArray.Create<BoundExpression>(boundSuccessVar),
                  argumentNamesOpt: default,
                  argumentRefKindsOpt: ImmutableArray.Create(RefKind.Out),
                  isDelegateCall: false,
                  expanded: false,
                  invokedAsExtensionMethod: false,
                  argsToParamsOpt: default,
                  resultKind: LookupResultKind.Viable,
                  binderOpt: null,
                  type: enumeratorInfo.TryGetNextMethod.ReturnType,
                  hasErrors: false).MakeCompilerGenerated();

            // (V)(T)e.TryGetNext(out bool success)
            TypeSymbol elementType = enumeratorInfo.ElementType;
            BoundExpression iterationVarAssignValue = MakeConversionNode(
                syntax: forEachSyntax,
                rewrittenOperand: MakeConversionNode(
                    syntax: forEachSyntax,
                    rewrittenOperand: tryGetNext,
                    conversion: enumeratorInfo.CurrentConversion,
                    rewrittenType: elementType,
                    @checked: loop.Checked),
                conversion: loop.ElementConversion,
                rewrittenType: loop.IterationVariableType.Type,
                @checked: loop.Checked); // PROTOTYPE(async-streams) confirm what the two conversions are and how checked affects them (test that)

            // V v = (V)(T)e.TryGetNext(out bool success); or (D1 d1, ...) = (V)(T)e.TryGetNext(out bool success);
            ImmutableArray<LocalSymbol> iterationVariables = loop.IterationVariables;
            BoundStatement iterationVarDecl = LocalOrDeconstructionDeclaration(loop, iterationVariables, iterationVarAssignValue);

            // PROTOTYPE(async-streams)
            //InstrumentForEachStatementIterationVarDeclaration(node, ref iterationVarDecl);

            // outer_loop_continue:
            GeneratedLabelSymbol outerLoopContinueLabel = new GeneratedLabelSymbol("outer_loop_continue");
            BoundLabelStatement outerLoopContinueStatement = new BoundLabelStatement(forEachSyntax, outerLoopContinueLabel, hasErrors: false).MakeCompilerGenerated();

            // goto outer_loop_continue;
            BoundStatement continueOuterLoop = new BoundGotoStatement(forEachSyntax, outerLoopContinueLabel, loop.HasErrors).MakeCompilerGenerated();

            // !success
            BoundExpression notSuccess = MakeUnaryOperator(UnaryOperatorKind.BoolLogicalNegation, forEachSyntax, method: null, boundSuccessVar, boundSuccessVar.Type);

            // if (!success) goto outer_loop_continue;
            BoundStatement checkAndBreak = RewriteIfStatement(forEachSyntax, notSuccess, continueOuterLoop, rewrittenAlternativeOpt: null, hasErrors: false).MakeCompilerGenerated();

            //     V v = (V)(T)e.TryGetNext(out bool success);
            //     if (!success) goto outer_loop_continue;
            //     /* loop.Body */
            //     /* loop.ContinueLabel: */
            var innerLoopBodyBlock = CreateBlockDeclaringIterationVariables(iterationVariables.Concat(successVar),
                iterationVarDecl, checkAndBreak, rewrittenBody, loop.ContinueLabel, forEachSyntax);

            // while (true)
            // {
            //     V v = (V)(T)e.TryGetNext(out bool success);
            //     if (!success) goto outer_loop_continue;
            //     /* loop.Body */
            //     /* loop.ContinueLabel: */
            // }
            BoundStatement innerLoop = MakeWhileTrueLoop(loop, innerLoopBodyBlock);

            // while (await e.WaitForNextAsync())
            // {
            //     /* inner loop */
            //     outer_loop_continue:
            // }
            // /* loop.BreakLabel: */
            BoundStatement outerLoopWithBreakLabel = RewriteWhileStatement(
                loop: loop,
                rewrittenCondition: awaitWaitForNext,
                rewrittenBody: innerLoop,
                breakLabel: loop.BreakLabel,
                continueLabel: outerLoopContinueLabel,
                hasErrors: false);

            // try
            // {
            //     /* outer loop */
            //     /* loop.BreakLabel: */
            // }
            // finally { await e.DisposeAsync(); }
            // - OR variant without disposal: -
            // /* outer loop */
            // /* loop.BreakLabel: */
            BoundStatement outerLoopOrTryFinally = outerLoopWithBreakLabel;
            if (loop.EnumeratorInfoOpt.NeedsDisposeMethod && TryGetWellKnownTypeMember(syntax: null, WellKnownMember.System_IAsyncDisposable__DisposeAsync,
                out MethodSymbol disposeAsyncMethod, location: forEachSyntax.AwaitKeyword.GetLocation()))
            {
                outerLoopOrTryFinally = WrapWithTryFinallyDispose(forEachSyntax, loop.EnumeratorInfoOpt, enumeratorType, boundEnumeratorVar, outerLoopWithBreakLabel, disposeAsyncMethod, loop.EnumeratorInfoOpt.DisposeAwaitableInfo);
            }

            // E e = ((C)(x)).GetAsyncEnumerator()
            // try
            // {
            //     /* outer loop */
            //     /* loop.BreakLabel: */
            // }
            // finally { await e.DisposeAsync(); }
            BoundStatement result = new BoundBlock(
                syntax: forEachSyntax,
                locals: ImmutableArray.Create(enumeratorVar),
                statements: ImmutableArray.Create(enumeratorVarDecl, outerLoopOrTryFinally));

            // PROTOTYPE(async-streams)
            //InstrumentForEachStatement(node, ref result);

            return result;
        }

        /// <summary>
        /// Lower a foreach loop that will enumerate a collection using an enumerator.
        ///
        /// <![CDATA[
        /// E e = ((C)(x)).GetEnumerator()
        /// try {
        ///     while (e.MoveNext()) {
        ///         V v = (V)(T)e.Current;  -OR-  (D1 d1, ...) = (V)(T)e.Current;
        ///         // body
        ///     }
        /// }
        /// finally {
        ///     // clean up e
        /// }
        /// ]]>
        /// </summary>
        private BoundStatement RewriteEnumeratorForEachStatement(BoundForEachStatement node)
        {
            var forEachSyntax = (CommonForEachStatementSyntax)node.Syntax;

            ForEachEnumeratorInfo enumeratorInfo = node.EnumeratorInfoOpt;
            Debug.Assert(enumeratorInfo != null);

            BoundExpression collectionExpression = GetUnconvertedCollectionExpression(node);
            BoundExpression rewrittenExpression = (BoundExpression)Visit(collectionExpression);
            BoundStatement rewrittenBody = (BoundStatement)Visit(node.Body);

            TypeSymbol enumeratorType = enumeratorInfo.GetEnumeratorMethod.ReturnType;
            TypeSymbol elementType = enumeratorInfo.ElementType;

            // E e
            LocalSymbol enumeratorVar = _factory.SynthesizedLocal(enumeratorType, syntax: forEachSyntax, kind: SynthesizedLocalKind.ForEachEnumerator);

            // Reference to e.
            BoundLocal boundEnumeratorVar = MakeBoundLocal(forEachSyntax, enumeratorVar, enumeratorType);

            // ((C)(x)).GetEnumerator() or (x).GetEnumerator();
            BoundExpression enumeratorVarInitValue = SynthesizeCall(forEachSyntax, rewrittenExpression, enumeratorInfo.GetEnumeratorMethod, enumeratorInfo.CollectionConversion, enumeratorInfo.CollectionType);

            // E e = ((C)(x)).GetEnumerator();
            BoundStatement enumeratorVarDecl = MakeLocalDeclaration(forEachSyntax, enumeratorVar, enumeratorVarInitValue);

            InstrumentForEachStatementCollectionVarDeclaration(node, ref enumeratorVarDecl);

            //(V)(T)e.Current
            BoundExpression iterationVarAssignValue = MakeConversionNode(
                syntax: forEachSyntax,
                rewrittenOperand: MakeConversionNode(
                    syntax: forEachSyntax,
                    rewrittenOperand: BoundCall.Synthesized(
                        syntax: forEachSyntax,
                        receiverOpt: boundEnumeratorVar,
                        method: enumeratorInfo.CurrentPropertyGetter),
                    conversion: enumeratorInfo.CurrentConversion,
                    rewrittenType: elementType,
                    @checked: node.Checked),
                conversion: node.ElementConversion,
                rewrittenType: node.IterationVariableType.Type,
                @checked: node.Checked);

            // V v = (V)(T)e.Current;  -OR-  (D1 d1, ...) = (V)(T)e.Current;

            ImmutableArray<LocalSymbol> iterationVariables = node.IterationVariables;
            BoundStatement iterationVarDecl = LocalOrDeconstructionDeclaration(node, iterationVariables, iterationVarAssignValue);

            InstrumentForEachStatementIterationVarDeclaration(node, ref iterationVarDecl);

            // while (e.MoveNext()) {
            //     V v = (V)(T)e.Current;  -OR-  (D1 d1, ...) = (V)(T)e.Current;
            //     /* node.Body */
            // }

            var rewrittenBodyBlock = CreateBlockDeclaringIterationVariables(iterationVariables, iterationVarDecl, rewrittenBody, forEachSyntax);

            BoundStatement whileLoop = RewriteWhileStatement(
                loop: node,
                rewrittenCondition: BoundCall.Synthesized(
                    syntax: forEachSyntax,
                    receiverOpt: boundEnumeratorVar,
                    method: enumeratorInfo.MoveNextMethod),
                rewrittenBody: rewrittenBodyBlock,
                breakLabel: node.BreakLabel,
                continueLabel: node.ContinueLabel,
                hasErrors: false);

            BoundStatement result;

            MethodSymbol disposeMethod;
            if (enumeratorInfo.NeedsDisposeMethod && Binder.TryGetSpecialTypeMember(_compilation, SpecialMember.System_IDisposable__Dispose, forEachSyntax, _diagnostics, out disposeMethod))
            {
                BoundStatement tryFinally = WrapWithTryFinallyDispose(forEachSyntax, enumeratorInfo, enumeratorType, boundEnumeratorVar, whileLoop, disposeMethod, disposeAwaitableInfoOpt: null);

                // E e = ((C)(x)).GetEnumerator();
                // try {
                //     /* as above */
                result = new BoundBlock(
                    syntax: forEachSyntax,
                    locals: ImmutableArray.Create(enumeratorVar),
                    statements: ImmutableArray.Create<BoundStatement>(enumeratorVarDecl, tryFinally));
            }
            else
            {
                // E e = ((C)(x)).GetEnumerator();
                // while (e.MoveNext()) {
                //     V v = (V)(T)e.Current;  -OR-  (D1 d1, ...) = (V)(T)e.Current;
                //     /* loop body */
                // }
                result = new BoundBlock(
                    syntax: forEachSyntax,
                    locals: ImmutableArray.Create(enumeratorVar),
                    statements: ImmutableArray.Create<BoundStatement>(enumeratorVarDecl, whileLoop));
            }

            InstrumentForEachStatement(node, ref result);

            return result;
        }

        private BoundStatement WrapWithTryFinallyDispose(CommonForEachStatementSyntax forEachSyntax, ForEachEnumeratorInfo enumeratorInfo, 
            TypeSymbol enumeratorType, BoundLocal boundEnumeratorVar, BoundStatement rewrittenBody, MethodSymbol disposeMethod, AwaitableInfo disposeAwaitableInfoOpt)
        {
            Binder.ReportDiagnosticsIfObsolete(_diagnostics, disposeMethod, forEachSyntax,
                                               hasBaseReceiver: false,
                                               containingMember: _factory.CurrentMethod,
                                               containingType: _factory.CurrentType,
                                               location: enumeratorInfo.Location);

            BoundBlock finallyBlockOpt;
            var idisposableTypeSymbol = disposeMethod.ContainingType;
            var conversions = new TypeConversions(_factory.CurrentMethod.ContainingAssembly.CorLibrary);

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var isImplicit = conversions.ClassifyImplicitConversionFromType(enumeratorType, idisposableTypeSymbol, ref useSiteDiagnostics).IsImplicit;
            _diagnostics.Add(forEachSyntax, useSiteDiagnostics);

            if (isImplicit)
            {
                Debug.Assert(enumeratorInfo.NeedsDisposeMethod);

                Conversion receiverConversion = enumeratorType.IsStructType() ?
                    Conversion.Boxing :
                    Conversion.ImplicitReference;

                // ((IDisposable)e).Dispose() or e.Dispose() or await ((IAsyncDisposable)e).DisposeAsync() or await e.DisposeAsync()
                BoundExpression disposeCall = SynthesizeCall(forEachSyntax, boundEnumeratorVar, disposeMethod, receiverConversion, idisposableTypeSymbol);
                if (disposeAwaitableInfoOpt != null)
                {
                    // await /* disposeCall */
                    disposeCall = WrapWithAwait(forEachSyntax, disposeCall, disposeAwaitableInfoOpt);
                    _sawAwaitInExceptionHandler = true;
                }

                // ((IDisposable)e).Dispose(); or e.Dispose(); or async variants
                BoundStatement disposeCallStatement = new BoundExpressionStatement(forEachSyntax, disposeCall);

                BoundStatement alwaysOrMaybeDisposeStmt;
                if (enumeratorType.IsValueType)
                {
                    // No way for the struct to be nullable and disposable.
                    Debug.Assert(enumeratorType.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T);

                    // For non-nullable structs, no null check is required.
                    alwaysOrMaybeDisposeStmt = disposeCallStatement;
                }
                else
                {
                    // NB: cast to object missing from spec.  Needed to ignore user-defined operators and box type parameters.
                    // if ((object)e != null) ((IDisposable)e).Dispose(); 
                    alwaysOrMaybeDisposeStmt = RewriteIfStatement(
                        syntax: forEachSyntax,
                        rewrittenCondition: new BoundBinaryOperator(forEachSyntax,
                            operatorKind: BinaryOperatorKind.NotEqual,
                            left: MakeConversionNode(
                                syntax: forEachSyntax,
                                rewrittenOperand: boundEnumeratorVar,
                                conversion: enumeratorInfo.EnumeratorConversion,
                                rewrittenType: _compilation.GetSpecialType(SpecialType.System_Object),
                                @checked: false),
                            right: MakeLiteral(forEachSyntax,
                                constantValue: ConstantValue.Null,
                                type: null),
                            constantValueOpt: null,
                            methodOpt: null,
                            resultKind: LookupResultKind.Viable,
                            type: _compilation.GetSpecialType(SpecialType.System_Boolean)),
                        rewrittenConsequence: disposeCallStatement,
                        rewrittenAlternativeOpt: null,
                        hasErrors: false);
                }

                finallyBlockOpt = new BoundBlock(forEachSyntax,
                    locals: ImmutableArray<LocalSymbol>.Empty,
                    statements: ImmutableArray.Create(alwaysOrMaybeDisposeStmt));
            }
            else
            {
                Debug.Assert(!enumeratorType.IsSealed);

                // IDisposable d
                LocalSymbol disposableVar = _factory.SynthesizedLocal(idisposableTypeSymbol);

                // Reference to d.
                BoundLocal boundDisposableVar = MakeBoundLocal(forEachSyntax, disposableVar, idisposableTypeSymbol);

                BoundTypeExpression boundIDisposableTypeExpr = new BoundTypeExpression(forEachSyntax,
                    aliasOpt: null,
                    type: idisposableTypeSymbol);

                // e as IDisposable
                BoundExpression disposableVarInitValue = new BoundAsOperator(forEachSyntax,
                    operand: boundEnumeratorVar,
                    targetType: boundIDisposableTypeExpr,
                    conversion: Conversion.ExplicitReference, // Explicit so the emitter won't optimize it away.
                    type: idisposableTypeSymbol);

                // IDisposable d = e as IDisposable;
                BoundStatement disposableVarDecl = MakeLocalDeclaration(forEachSyntax, disposableVar, disposableVarInitValue);

                // d.Dispose() or async variant
                BoundExpression disposeCall = BoundCall.Synthesized(syntax: forEachSyntax, receiverOpt: boundDisposableVar, method: disposeMethod);
                if (disposeAwaitableInfoOpt != null)
                {
                    // await d.DisposeAsync()
                    disposeCall = WrapWithAwait(forEachSyntax, disposeCall, disposeAwaitableInfoOpt);
                    _sawAwaitInExceptionHandler = true;
                }

                // if (d != null) d.Dispose();
                BoundStatement ifStmt = RewriteIfStatement(
                    syntax: forEachSyntax,
                    rewrittenCondition: new BoundBinaryOperator(forEachSyntax,
                        operatorKind: BinaryOperatorKind.NotEqual, // reference equality
                        left: boundDisposableVar,
                        right: MakeLiteral(forEachSyntax, constantValue: ConstantValue.Null, type: null),
                        constantValueOpt: null,
                        methodOpt: null,
                        resultKind: LookupResultKind.Viable,
                        type: _compilation.GetSpecialType(SpecialType.System_Boolean)),
                    rewrittenConsequence: new BoundExpressionStatement(forEachSyntax, expression: disposeCall),
                    rewrittenAlternativeOpt: null,
                    hasErrors: false);

                // IDisposable d = e as IDisposable;
                // if (d != null) d.Dispose();
                finallyBlockOpt = new BoundBlock(forEachSyntax,
                    locals: ImmutableArray.Create(disposableVar),
                    statements: ImmutableArray.Create(disposableVarDecl, ifStmt));
            }

            // try {
            //     while (e.MoveNext()) {
            //         V v = (V)(T)e.Current;  -OR-  (D1 d1, ...) = (V)(T)e.Current;
            //         /* loop body */
            //     }
            // }
            // finally {
            //     /* dispose of e */
            // }
            BoundStatement tryFinally = new BoundTryStatement(forEachSyntax,
                tryBlock: new BoundBlock(forEachSyntax,
                    locals: ImmutableArray<LocalSymbol>.Empty,
                    statements: ImmutableArray.Create<BoundStatement>(rewrittenBody)),
                catchBlocks: ImmutableArray<BoundCatchBlock>.Empty,
                finallyBlockOpt: finallyBlockOpt);
            return tryFinally;
        }

        /// <summary>
        /// Produce:
        /// await /* disposeCall */
        /// </summary>
        private BoundExpression WrapWithAwait(CommonForEachStatementSyntax forEachSyntax, BoundExpression disposeCall, AwaitableInfo disposeAwaitableInfoOpt)
        {
            TypeSymbol awaitExpressionType = disposeAwaitableInfoOpt.GetResult?.ReturnType ?? _compilation.DynamicType;
            BoundAwaitExpression awaitExpr = new BoundAwaitExpression(forEachSyntax, disposeCall, disposeAwaitableInfoOpt, awaitExpressionType) { WasCompilerGenerated = true };
            return (BoundExpression)VisitAwaitExpression(awaitExpr);
        }

        /// <summary>
        /// Synthesize a no-argument call to a given method, possibly applying a conversion to the receiver.
        /// 
        /// If the receiver is of struct type and the method is an interface method, then skip the conversion
        /// and just call the interface method directly - the code generator will detect this and generate a 
        /// constrained virtual call.
        /// </summary>
        /// <param name="syntax">A syntax node to attach to the synthesized bound node.</param>
        /// <param name="receiver">Receiver of method call.</param>
        /// <param name="method">Method to invoke.</param>
        /// <param name="receiverConversion">Conversion to be applied to the receiver if not calling an interface method on a struct.</param>
        /// <param name="convertedReceiverType">Type of the receiver after applying the conversion.</param>
        /// <returns>A BoundExpression representing the call.</returns>
        private BoundExpression SynthesizeCall(CSharpSyntaxNode syntax, BoundExpression receiver, MethodSymbol method, Conversion receiverConversion, TypeSymbol convertedReceiverType)
        {
            if (!receiver.Type.IsReferenceType && method.ContainingType.IsInterface)
            {
                Debug.Assert(receiverConversion.IsImplicit && !receiverConversion.IsUserDefined);

                // NOTE: The spec says that disposing of a struct enumerator won't cause any 
                // unnecessary boxing to occur.  However, Dev10 extends this improvement to the
                // GetEnumerator call as well.

                // We're going to let the emitter take care of avoiding the extra boxing. 
                // When it sees an interface call to a struct, it will generate a constrained
                // virtual call, which will skip boxing, if possible.

                // CONSIDER: In cases where the struct implicitly implements the interface method
                // (i.e. with a public method), we could save a few bytes of IL by creating a 
                // BoundCall to the struct method rather than the interface method (so that the
                // emitter wouldn't need to create a constrained virtual call).  It is not clear 
                // what effect this would have on back compat.

                // NOTE: This call does not correspond to anything that can be written in C# source.
                // We're invoking the interface method directly on the struct (which may have a private
                // explicit implementation).  The code generator knows how to handle it though.

                // receiver.InterfaceMethod()
                return BoundCall.Synthesized(syntax, receiver, method);
            }
            else
            {
                // ((Interface)receiver).InterfaceMethod()
                Debug.Assert(!receiverConversion.IsNumeric);

                return BoundCall.Synthesized(
                    syntax: syntax,
                    receiverOpt: MakeConversionNode(
                        syntax: syntax,
                        rewrittenOperand: receiver,
                        conversion: receiverConversion,
                        @checked: false,
                        rewrittenType: convertedReceiverType),
                    method: method);
            }
        }

        /// <summary>
        /// Lower a foreach loop that will enumerate a collection via indexing.
        /// 
        /// <![CDATA[
        /// 
        /// Indexable a = x;
        /// for (int p = 0; p < a.Length; p = p + 1) {
        ///     V v = (V)a[p];   /* OR */   (D1 d1, ...) = (V)a[p];
        ///     // body
        /// }
        /// 
        /// ]]>
        /// </summary>
        /// <remarks>
        /// NOTE: We're assuming that sequence points have already been generated.
        /// Otherwise, lowering to for-loops would generated spurious ones.
        /// </remarks>
        private BoundStatement RewriteForEachStatementAsFor(BoundForEachStatement node, MethodSymbol indexerGet, MethodSymbol lengthGet)
        {
            var forEachSyntax = (CommonForEachStatementSyntax)node.Syntax;

            BoundExpression collectionExpression = GetUnconvertedCollectionExpression(node);
            NamedTypeSymbol collectionType = (NamedTypeSymbol)collectionExpression.Type;

            TypeSymbol intType = _compilation.GetSpecialType(SpecialType.System_Int32);
            TypeSymbol boolType = _compilation.GetSpecialType(SpecialType.System_Boolean);

            BoundExpression rewrittenExpression = (BoundExpression)Visit(collectionExpression);
            BoundStatement rewrittenBody = (BoundStatement)Visit(node.Body);

            // Collection a
            LocalSymbol collectionTemp = _factory.SynthesizedLocal(collectionType, forEachSyntax, kind: SynthesizedLocalKind.ForEachArray);

            // Collection a = /*node.Expression*/;
            BoundStatement arrayVarDecl = MakeLocalDeclaration(forEachSyntax, collectionTemp, rewrittenExpression);

            InstrumentForEachStatementCollectionVarDeclaration(node, ref arrayVarDecl);

            // Reference to a.
            BoundLocal boundArrayVar = MakeBoundLocal(forEachSyntax, collectionTemp, collectionType);

            // int p
            LocalSymbol positionVar = _factory.SynthesizedLocal(intType, syntax: forEachSyntax, kind: SynthesizedLocalKind.ForEachArrayIndex);

            // Reference to p.
            BoundLocal boundPositionVar = MakeBoundLocal(forEachSyntax, positionVar, intType);

            // int p = 0;
            BoundStatement positionVarDecl = MakeLocalDeclaration(forEachSyntax, positionVar,
                MakeLiteral(forEachSyntax, ConstantValue.Default(SpecialType.System_Int32), intType));

            // (V)a[p]
            BoundExpression iterationVarInitValue = MakeConversionNode(
                syntax: forEachSyntax,
                rewrittenOperand: BoundCall.Synthesized(
                    syntax: forEachSyntax,
                    receiverOpt: boundArrayVar,
                    indexerGet,
                    boundPositionVar),
                conversion: node.ElementConversion,
                rewrittenType: node.IterationVariableType.Type,
                @checked: node.Checked);

            // V v = (V)a[p];   /* OR */   (D1 d1, ...) = (V)a[p];
            ImmutableArray<LocalSymbol> iterationVariables = node.IterationVariables;
            BoundStatement iterationVariableDecl = LocalOrDeconstructionDeclaration(node, iterationVariables, iterationVarInitValue);

            InstrumentForEachStatementIterationVarDeclaration(node, ref iterationVariableDecl);

            BoundStatement initializer = new BoundStatementList(forEachSyntax,
                        statements: ImmutableArray.Create<BoundStatement>(arrayVarDecl, positionVarDecl));

            // a.Length
            BoundExpression arrayLength = BoundCall.Synthesized(
                syntax: forEachSyntax,
                receiverOpt: boundArrayVar,
                lengthGet);

            // p < a.Length
            BoundExpression exitCondition = new BoundBinaryOperator(
                syntax: forEachSyntax,
                operatorKind: BinaryOperatorKind.IntLessThan,
                left: boundPositionVar,
                right: arrayLength,
                constantValueOpt: null,
                methodOpt: null,
                resultKind: LookupResultKind.Viable,
                type: boolType);

            // p = p + 1;
            BoundStatement positionIncrement = MakePositionIncrement(forEachSyntax, boundPositionVar, intType);

            // {
            //     V v = (V)a[p];    /* OR */   (D1 d1, ...) = (V)a[p];
            //     /*node.Body*/
            // }

            BoundStatement loopBody = CreateBlockDeclaringIterationVariables(iterationVariables, iterationVariableDecl, rewrittenBody, forEachSyntax);

            // for (Collection a = /*node.Expression*/, int p = 0; p < a.Length; p = p + 1) {
            //     V v = (V)a[p];   /* OR */   (D1 d1, ...) = (V)a[p];
            //     /*node.Body*/
            // }
            BoundStatement result = RewriteForStatementWithoutInnerLocals(
                original: node,
                outerLocals: ImmutableArray.Create<LocalSymbol>(collectionTemp, positionVar),
                rewrittenInitializer: initializer,
                rewrittenCondition: exitCondition,
                rewrittenIncrement: positionIncrement,
                rewrittenBody: loopBody,
                breakLabel: node.BreakLabel,
                continueLabel: node.ContinueLabel,
                hasErrors: node.HasErrors);

            InstrumentForEachStatement(node, ref result);

            return result;
        }

        /// <summary>
        /// Takes the expression for the current value of the iteration variable and either
        /// (1) assigns it into a local, or
        /// (2) deconstructs it into multiple locals (if there is a deconstruct step).
        ///
        /// Produces `V v = /* expression */` or `(D1 d1, ...) = /* expression */`.
        /// </summary>
        private BoundStatement LocalOrDeconstructionDeclaration(
                                    BoundForEachStatement forEachBound,
                                    ImmutableArray<LocalSymbol> iterationVariables,
                                    BoundExpression iterationVarValue)
        {
            var forEachSyntax = (CommonForEachStatementSyntax)forEachBound.Syntax;

            BoundStatement iterationVarDecl;
            BoundForEachDeconstructStep deconstruction = forEachBound.DeconstructionOpt;

            if (deconstruction == null)
            {
                // V v = /* expression */
                Debug.Assert(iterationVariables.Length == 1);
                iterationVarDecl = MakeLocalDeclaration(forEachSyntax, iterationVariables[0], iterationVarValue);
            }
            else
            {
                // (D1 d1, ...) = /* expression */
                var assignment = deconstruction.DeconstructionAssignment;

                AddPlaceholderReplacement(deconstruction.TargetPlaceholder, iterationVarValue);
                BoundExpression loweredAssignment = VisitExpression(assignment);
                iterationVarDecl = new BoundExpressionStatement(assignment.Syntax, loweredAssignment);
                RemovePlaceholderReplacement(deconstruction.TargetPlaceholder);
            }

            return iterationVarDecl;
        }

        private static BoundBlock CreateBlockDeclaringIterationVariables(
            ImmutableArray<LocalSymbol> iterationVariables,
            BoundStatement iteratorVariableInitialization,
            BoundStatement rewrittenBody,
            CommonForEachStatementSyntax forEachSyntax)
        {
            // The scope of the iteration variable is the embedded statement syntax.
            // However consider the following foreach statement:
            //
            //   foreach (int x in ...) { int y = ...; F(() => x); F(() => y));
            //
            // We currently generate 2 closures. One containing variable x, the other variable y.
            // The EnC source mapping infrastructure requires each closure within a method body
            // to have a unique syntax offset. Hence we associate the bound block declaring the
            // iteration variable with the foreach statement, not the embedded statement.
            return new BoundBlock(
                forEachSyntax,
                locals: iterationVariables,
                statements: ImmutableArray.Create(iteratorVariableInitialization, rewrittenBody));
        }

        private static BoundBlock CreateBlockDeclaringIterationVariables(
            ImmutableArray<LocalSymbol> iterationVariables,
            BoundStatement iteratorVariableInitialization,
            BoundStatement checkAndBreak,
            BoundStatement rewrittenBody,
            LabelSymbol continueLabel,
            CommonForEachStatementSyntax forEachSyntax)
        {
            // The scope of the iteration variable is the embedded statement syntax.
            // However consider the following foreach statement:
            //
            //   foreach await (int x in ...) { int y = ...; F(() => x); F(() => y));
            //
            // We currently generate 2 closures. One containing variable x, the other variable y.
            // The EnC source mapping infrastructure requires each closure within a method body
            // to have a unique syntax offset. Hence we associate the bound block declaring the
            // iteration variable with the foreach statement, not the embedded statement.
            return new BoundBlock(
                forEachSyntax,
                locals: iterationVariables,
                statements: ImmutableArray.Create(
                    iteratorVariableInitialization,
                    checkAndBreak,
                    rewrittenBody,
                    new BoundLabelStatement(forEachSyntax, continueLabel)));
        }

        /// <summary>
        /// Lower a foreach loop that will enumerate a single-dimensional array.
        /// 
        /// A[] a = x;
        /// for (int p = 0; p &lt; a.Length; p = p + 1) {
        ///     V v = (V)a[p];   /* OR */   (D1 d1, ...) = (V)a[p];
        ///     // body
        /// }
        /// </summary>
        /// <remarks>
        /// We will follow Dev10 in diverging from the C# 4 spec by ignoring Array's 
        /// implementation of IEnumerable and just indexing into its elements.
        /// 
        /// NOTE: We're assuming that sequence points have already been generated.
        /// Otherwise, lowering to for-loops would generated spurious ones.
        /// </remarks>
        private BoundStatement RewriteSingleDimensionalArrayForEachStatement(BoundForEachStatement node)
        {
            var forEachSyntax = (CommonForEachStatementSyntax)node.Syntax;

            BoundExpression collectionExpression = GetUnconvertedCollectionExpression(node);
            Debug.Assert(collectionExpression.Type.IsArray());

            ArrayTypeSymbol arrayType = (ArrayTypeSymbol)collectionExpression.Type;

            Debug.Assert(arrayType.IsSZArray);

            TypeSymbol intType = _compilation.GetSpecialType(SpecialType.System_Int32);
            TypeSymbol boolType = _compilation.GetSpecialType(SpecialType.System_Boolean);

            BoundExpression rewrittenExpression = (BoundExpression)Visit(collectionExpression);
            BoundStatement rewrittenBody = (BoundStatement)Visit(node.Body);

            // A[] a
            LocalSymbol arrayVar = _factory.SynthesizedLocal(arrayType, syntax: forEachSyntax, kind: SynthesizedLocalKind.ForEachArray);

            // A[] a = /*node.Expression*/;
            BoundStatement arrayVarDecl = MakeLocalDeclaration(forEachSyntax, arrayVar, rewrittenExpression);

            InstrumentForEachStatementCollectionVarDeclaration(node, ref arrayVarDecl);

            // Reference to a.
            BoundLocal boundArrayVar = MakeBoundLocal(forEachSyntax, arrayVar, arrayType);

            // int p
            LocalSymbol positionVar = _factory.SynthesizedLocal(intType, syntax: forEachSyntax, kind: SynthesizedLocalKind.ForEachArrayIndex);

            // Reference to p.
            BoundLocal boundPositionVar = MakeBoundLocal(forEachSyntax, positionVar, intType);

            // int p = 0;
            BoundStatement positionVarDecl = MakeLocalDeclaration(forEachSyntax, positionVar,
                MakeLiteral(forEachSyntax, ConstantValue.Default(SpecialType.System_Int32), intType));

            // (V)a[p]
            BoundExpression iterationVarInitValue = MakeConversionNode(
                syntax: forEachSyntax,
                rewrittenOperand: new BoundArrayAccess(
                    syntax: forEachSyntax,
                    expression: boundArrayVar,
                    indices: ImmutableArray.Create<BoundExpression>(boundPositionVar),
                    type: arrayType.ElementType),
                conversion: node.ElementConversion,
                rewrittenType: node.IterationVariableType.Type,
                @checked: node.Checked);

            // V v = (V)a[p];   /* OR */   (D1 d1, ...) = (V)a[p];
            ImmutableArray<LocalSymbol> iterationVariables = node.IterationVariables;
            BoundStatement iterationVariableDecl = LocalOrDeconstructionDeclaration(node, iterationVariables, iterationVarInitValue);

            InstrumentForEachStatementIterationVarDeclaration(node, ref iterationVariableDecl);

            BoundStatement initializer = new BoundStatementList(forEachSyntax,
                        statements: ImmutableArray.Create<BoundStatement>(arrayVarDecl, positionVarDecl));

            // a.Length
            BoundExpression arrayLength = new BoundArrayLength(
                syntax: forEachSyntax,
                expression: boundArrayVar,
                type: intType);

            // p < a.Length
            BoundExpression exitCondition = new BoundBinaryOperator(
                syntax: forEachSyntax,
                operatorKind: BinaryOperatorKind.IntLessThan,
                left: boundPositionVar,
                right: arrayLength,
                constantValueOpt: null,
                methodOpt: null,
                resultKind: LookupResultKind.Viable,
                type: boolType);

            // p = p + 1;
            BoundStatement positionIncrement = MakePositionIncrement(forEachSyntax, boundPositionVar, intType);

            // {
            //     V v = (V)a[p];    /* OR */   (D1 d1, ...) = (V)a[p];
            //     /*node.Body*/
            // }

            BoundStatement loopBody = CreateBlockDeclaringIterationVariables(iterationVariables, iterationVariableDecl, rewrittenBody, forEachSyntax);

            // for (A[] a = /*node.Expression*/, int p = 0; p < a.Length; p = p + 1) {
            //     V v = (V)a[p];   /* OR */   (D1 d1, ...) = (V)a[p];
            //     /*node.Body*/
            // }
            BoundStatement result = RewriteForStatementWithoutInnerLocals(
                original: node,
                outerLocals: ImmutableArray.Create<LocalSymbol>(arrayVar, positionVar),
                rewrittenInitializer: initializer,
                rewrittenCondition: exitCondition,
                rewrittenIncrement: positionIncrement,
                rewrittenBody: loopBody,
                breakLabel: node.BreakLabel,
                continueLabel: node.ContinueLabel,
                hasErrors: node.HasErrors);

            InstrumentForEachStatement(node, ref result);

            return result;
        }

        /// <summary>
        /// Lower a foreach loop that will enumerate a multi-dimensional array.
        /// 
        /// A[...] a = x;
        /// int q_0 = a.GetUpperBound(0), q_1 = a.GetUpperBound(1), ...;
        /// for (int p_0 = a.GetLowerBound(0); p_0 &lt;= q_0; p_0 = p_0 + 1)
        ///     for (int p_1 = a.GetLowerBound(1); p_1 &lt;= q_1; p_1 = p_1 + 1)
        ///         ...
        ///             {
        ///                 V v = (V)a[p_0, p_1, ...];   /* OR */   (D1 d1, ...) = (V)a[p_0, p_1, ...];
        ///                 /* body */
        ///             }
        /// </summary>
        /// <remarks>
        /// We will follow Dev10 in diverging from the C# 4 spec by ignoring Array's 
        /// implementation of IEnumerable and just indexing into its elements.
        /// 
        /// NOTE: We're assuming that sequence points have already been generated.
        /// Otherwise, lowering to nested for-loops would generated spurious ones.
        /// </remarks>
        private BoundStatement RewriteMultiDimensionalArrayForEachStatement(BoundForEachStatement node)
        {
            var forEachSyntax = (CommonForEachStatementSyntax)node.Syntax;

            BoundExpression collectionExpression = GetUnconvertedCollectionExpression(node);
            Debug.Assert(collectionExpression.Type.IsArray());

            ArrayTypeSymbol arrayType = (ArrayTypeSymbol)collectionExpression.Type;

            int rank = arrayType.Rank;
            Debug.Assert(!arrayType.IsSZArray);

            TypeSymbol intType = _compilation.GetSpecialType(SpecialType.System_Int32);
            TypeSymbol boolType = _compilation.GetSpecialType(SpecialType.System_Boolean);

            // Values we'll use every iteration
            MethodSymbol getLowerBoundMethod = UnsafeGetSpecialTypeMethod(forEachSyntax, SpecialMember.System_Array__GetLowerBound);
            MethodSymbol getUpperBoundMethod = UnsafeGetSpecialTypeMethod(forEachSyntax, SpecialMember.System_Array__GetUpperBound);

            BoundExpression rewrittenExpression = (BoundExpression)Visit(collectionExpression);
            BoundStatement rewrittenBody = (BoundStatement)Visit(node.Body);

            // A[...] a
            LocalSymbol arrayVar = _factory.SynthesizedLocal(arrayType, syntax: forEachSyntax, kind: SynthesizedLocalKind.ForEachArray);
            BoundLocal boundArrayVar = MakeBoundLocal(forEachSyntax, arrayVar, arrayType);

            // A[...] a = /*node.Expression*/;
            BoundStatement arrayVarDecl = MakeLocalDeclaration(forEachSyntax, arrayVar, rewrittenExpression);

            InstrumentForEachStatementCollectionVarDeclaration(node, ref arrayVarDecl);

            // NOTE: dev10 initializes all of the upper bound temps before entering the loop (as opposed to
            // initializing each one at the corresponding level of nesting).  Doing it at the same time as
            // the lower bound would make this code a bit simpler, but it would make it harder to compare
            // the roslyn and dev10 IL.

            // int q_0, q_1, ...
            LocalSymbol[] upperVar = new LocalSymbol[rank];
            BoundLocal[] boundUpperVar = new BoundLocal[rank];
            BoundStatement[] upperVarDecl = new BoundStatement[rank];
            for (int dimension = 0; dimension < rank; dimension++)
            {
                // int q_dimension
                upperVar[dimension] = _factory.SynthesizedLocal(intType, syntax: forEachSyntax, kind: SynthesizedLocalKind.ForEachArrayLimit);
                boundUpperVar[dimension] = MakeBoundLocal(forEachSyntax, upperVar[dimension], intType);

                ImmutableArray<BoundExpression> dimensionArgument = ImmutableArray.Create(
                    MakeLiteral(forEachSyntax,
                        constantValue: ConstantValue.Create(dimension, ConstantValueTypeDiscriminator.Int32),
                        type: intType));

                // a.GetUpperBound(dimension)
                BoundExpression currentDimensionUpperBound = BoundCall.Synthesized(forEachSyntax, boundArrayVar, getUpperBoundMethod, dimensionArgument);

                // int q_dimension = a.GetUpperBound(dimension);
                upperVarDecl[dimension] = MakeLocalDeclaration(forEachSyntax, upperVar[dimension], currentDimensionUpperBound);
            }

            // int p_0, p_1, ...
            LocalSymbol[] positionVar = new LocalSymbol[rank];
            BoundLocal[] boundPositionVar = new BoundLocal[rank];
            for (int dimension = 0; dimension < rank; dimension++)
            {
                positionVar[dimension] = _factory.SynthesizedLocal(intType, syntax: forEachSyntax, kind: SynthesizedLocalKind.ForEachArrayIndex);
                boundPositionVar[dimension] = MakeBoundLocal(forEachSyntax, positionVar[dimension], intType);
            }

            // (V)a[p_0, p_1, ...]
            BoundExpression iterationVarInitValue = MakeConversionNode(
                syntax: forEachSyntax,
                rewrittenOperand: new BoundArrayAccess(forEachSyntax,
                    expression: boundArrayVar,
                    indices: ImmutableArray.Create((BoundExpression[])boundPositionVar),
                    type: arrayType.ElementType),
                conversion: node.ElementConversion,
                rewrittenType: node.IterationVariableType.Type,
                @checked: node.Checked);

            // V v = (V)a[p_0, p_1, ...];   /* OR */   (D1 d1, ...) = (V)a[p_0, p_1, ...];

            ImmutableArray<LocalSymbol> iterationVariables = node.IterationVariables;
            BoundStatement iterationVarDecl = LocalOrDeconstructionDeclaration(node, iterationVariables, iterationVarInitValue);

            InstrumentForEachStatementIterationVarDeclaration(node, ref iterationVarDecl);

            // {
            //     V v = (V)a[p_0, p_1, ...];   /* OR */   (D1 d1, ...) = (V)a[p_0, p_1, ...];
            //     /* node.Body */
            // }

            BoundStatement innermostLoopBody = CreateBlockDeclaringIterationVariables(iterationVariables, iterationVarDecl, rewrittenBody, forEachSyntax);

            // work from most-nested to least-nested
            // for (int p_0 = a.GetLowerBound(0); p_0 <= q_0; p_0 = p_0 + 1)
            //     for (int p_1 = a.GetLowerBound(0); p_1 <= q_1; p_1 = p_1 + 1)
            //         ...
            //             {
            //                 V v = (V)a[p_0, p_1, ...];   /* OR */   (D1 d1, ...) = (V)a[p_0, p_1, ...];
            //                 /* body */
            //             }
            BoundStatement forLoop = null;
            for (int dimension = rank - 1; dimension >= 0; dimension--)
            {
                ImmutableArray<BoundExpression> dimensionArgument = ImmutableArray.Create(
                    MakeLiteral(forEachSyntax,
                        constantValue: ConstantValue.Create(dimension, ConstantValueTypeDiscriminator.Int32),
                        type: intType));

                // a.GetLowerBound(dimension)
                BoundExpression currentDimensionLowerBound = BoundCall.Synthesized(forEachSyntax, boundArrayVar, getLowerBoundMethod, dimensionArgument);

                // int p_dimension = a.GetLowerBound(dimension);
                BoundStatement positionVarDecl = MakeLocalDeclaration(forEachSyntax, positionVar[dimension], currentDimensionLowerBound);

                GeneratedLabelSymbol breakLabel = dimension == 0 // outermost for-loop
                    ? node.BreakLabel // i.e. the one that break statements will jump to
                    : new GeneratedLabelSymbol("break"); // Should not affect emitted code since unused

                // p_dimension <= q_dimension  //NB: OrEqual
                BoundExpression exitCondition = new BoundBinaryOperator(
                    syntax: forEachSyntax,
                    operatorKind: BinaryOperatorKind.IntLessThanOrEqual,
                    left: boundPositionVar[dimension],
                    right: boundUpperVar[dimension],
                    constantValueOpt: null,
                    methodOpt: null,
                    resultKind: LookupResultKind.Viable,
                    type: boolType);

                // p_dimension = p_dimension + 1;
                BoundStatement positionIncrement = MakePositionIncrement(forEachSyntax, boundPositionVar[dimension], intType);

                BoundStatement body;
                GeneratedLabelSymbol continueLabel;

                if (forLoop == null)
                {
                    // innermost for-loop
                    body = innermostLoopBody;
                    continueLabel = node.ContinueLabel; //i.e. the one continue statements will actually jump to
                }
                else
                {
                    body = forLoop;
                    continueLabel = new GeneratedLabelSymbol("continue"); // Should not affect emitted code since unused
                }

                forLoop = RewriteForStatementWithoutInnerLocals(
                    original: node,
                    outerLocals: ImmutableArray.Create(positionVar[dimension]),
                    rewrittenInitializer: positionVarDecl,
                    rewrittenCondition: exitCondition,
                    rewrittenIncrement: positionIncrement,
                    rewrittenBody: body,
                    breakLabel: breakLabel,
                    continueLabel: continueLabel,
                    hasErrors: node.HasErrors);
            }

            Debug.Assert(forLoop != null);

            BoundStatement result = new BoundBlock(
                forEachSyntax,
                ImmutableArray.Create(arrayVar).Concat(upperVar.AsImmutableOrNull()),
                ImmutableArray.Create(arrayVarDecl).Concat(upperVarDecl.AsImmutableOrNull()).Add(forLoop));

            InstrumentForEachStatement(node, ref result);

            return result;
        }

        /// <summary>
        /// So that the binding info can return an appropriate SemanticInfo.Converted type for the collection
        /// expression of a foreach node, it is wrapped in a BoundConversion to the collection type in the
        /// initial bound tree.  However, we may be able to optimize away (or entirely disregard) the conversion
        /// so we pull out the bound node for the underlying expression.
        /// </summary>
        private static BoundExpression GetUnconvertedCollectionExpression(BoundForEachStatement node)
        {
            var boundExpression = node.Expression;
            if (boundExpression.Kind == BoundKind.Conversion)
            {
                return ((BoundConversion)boundExpression).Operand;
            }

            // Conversion was an identity conversion and the LocalRewriter must have optimized away the
            // BoundConversion node, we can return the expression itself.
            return boundExpression;
        }

        private static BoundLocal MakeBoundLocal(CSharpSyntaxNode syntax, LocalSymbol local, TypeSymbol type)
        {
            return new BoundLocal(syntax,
                localSymbol: local,
                constantValueOpt: null,
                type: type);
        }

        private BoundStatement MakeLocalDeclaration(CSharpSyntaxNode syntax, LocalSymbol local, BoundExpression rewrittenInitialValue)
        {
            return RewriteLocalDeclaration(
                originalOpt: null,
                syntax: syntax,
                localSymbol: local,
                rewrittenInitializer: rewrittenInitialValue);
        }

        // Used to increment integer index into an array or string.
        private BoundStatement MakePositionIncrement(CSharpSyntaxNode syntax, BoundLocal boundPositionVar, TypeSymbol intType)
        {
            // A normal for-loop would have a sequence point on the increment.  We don't want that since the code is synthesized,
            // but we add a hidden sequence point to avoid disrupting the stepping experience.
            return new BoundSequencePoint(null,
                statementOpt: new BoundExpressionStatement(syntax,
                    expression: new BoundAssignmentOperator(syntax,
                        left: boundPositionVar,
                        right: new BoundBinaryOperator(syntax,
                            operatorKind: BinaryOperatorKind.IntAddition, // unchecked, never overflows since array/string index can't be >= Int32.MaxValue
                            left: boundPositionVar,
                            right: MakeLiteral(syntax,
                                constantValue: ConstantValue.Create(1),
                                type: intType),
                            constantValueOpt: null,
                            methodOpt: null,
                            resultKind: LookupResultKind.Viable,
                            type: intType),
                        type: intType)));
        }

        private void InstrumentForEachStatementCollectionVarDeclaration(BoundForEachStatement original, ref BoundStatement collectionVarDecl)
        {
            if (this.Instrument)
            {
                collectionVarDecl = _instrumenter.InstrumentForEachStatementCollectionVarDeclaration(original, collectionVarDecl);
            }
        }

        private void InstrumentForEachStatementIterationVarDeclaration(BoundForEachStatement original, ref BoundStatement iterationVarDecl)
        {
            if (this.Instrument)
            {
                CommonForEachStatementSyntax forEachSyntax = (CommonForEachStatementSyntax)original.Syntax;
                if (forEachSyntax is ForEachVariableStatementSyntax)
                {
                    iterationVarDecl = _instrumenter.InstrumentForEachStatementDeconstructionVariablesDeclaration(original, iterationVarDecl);
                }
                else
                {
                    iterationVarDecl = _instrumenter.InstrumentForEachStatementIterationVarDeclaration(original, iterationVarDecl);
                }
            }
        }

        private void InstrumentForEachStatement(BoundForEachStatement original, ref BoundStatement result)
        {
            if (this.Instrument)
            {
                result = _instrumenter.InstrumentForEachStatement(original, result);
            }
        }

        /// <summary>
        /// Produce a while(true) loop
        ///
        /// <![CDATA[
        /// still-true:
        /// /* body */
        /// goto still-true;
        /// ]]> 
        /// </summary>
        private BoundStatement MakeWhileTrueLoop(BoundForEachStatement loop, BoundBlock body)
        {
            Debug.Assert(loop.EnumeratorInfoOpt.IsAsync);
            SyntaxNode syntax = loop.Syntax;
            GeneratedLabelSymbol startLabel = new GeneratedLabelSymbol("still-true");
            BoundStatement startLabelStatement = new BoundLabelStatement(syntax, startLabel);

            if (this.Instrument)
            {
                startLabelStatement = new BoundSequencePoint(null, startLabelStatement);
            }

            // still-true:
            // /* body */
            // goto still-true;
            return BoundStatementList.Synthesized(syntax, hasErrors: false,
                 startLabelStatement,
                 body,
                 new BoundGotoStatement(syntax, startLabel));
        }
    }
}
