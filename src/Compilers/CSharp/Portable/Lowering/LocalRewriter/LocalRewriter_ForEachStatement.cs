// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        private static readonly AwaitDebugId s_moveNextAsyncAwaitId = new AwaitDebugId(RelativeStateOrdinal: 0);
        private static readonly AwaitDebugId s_disposeAsyncAwaitId = new AwaitDebugId(RelativeStateOrdinal: 1);

        /// <summary>
        /// This is the entry point for foreach-loop lowering.  It delegates to
        ///   RewriteEnumeratorForEachStatement
        ///   RewriteSingleDimensionalArrayForEachStatement
        ///   RewriteMultiDimensionalArrayForEachStatement
        ///   CanRewriteForEachAsFor
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

            BoundExpression collectionExpression = GetUnconvertedCollectionExpression(node, out _);
            TypeSymbol? nodeExpressionType = collectionExpression.Type;
            Debug.Assert(nodeExpressionType is { });
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
            else if (node.EnumeratorInfoOpt is { InlineArraySpanType: not WellKnownType.Unknown })
            {
                return RewriteInlineArrayForEachStatementAsFor(node);
            }
            else if (node.AwaitOpt is null && CanRewriteForEachAsFor(node.Syntax, nodeExpressionType, out var indexerGet, out var lengthGetter))
            {
                return RewriteForEachStatementAsFor(node, indexerGet, lengthGetter);
            }
            else
            {
                return RewriteEnumeratorForEachStatement(node);
            }
        }

        private bool CanRewriteForEachAsFor(SyntaxNode forEachSyntax, TypeSymbol nodeExpressionType, [NotNullWhen(true)] out MethodSymbol? indexerGet, [NotNullWhen(true)] out MethodSymbol? lengthGet)
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
                lengthGet = (MethodSymbol?)_factory.WellKnownMember(WellKnownMember.System_Span_T__get_Length, isOptional: true)?.SymbolAsMember(spanType);
                indexerGet = (MethodSymbol?)_factory.WellKnownMember(WellKnownMember.System_Span_T__get_Item, isOptional: true)?.SymbolAsMember(spanType);
            }
            else if ((object)origDefinition == this._compilation.GetWellKnownType(WellKnownType.System_ReadOnlySpan_T))
            {
                var spanType = (NamedTypeSymbol)nodeExpressionType;
                lengthGet = (MethodSymbol?)_factory.WellKnownMember(WellKnownMember.System_ReadOnlySpan_T__get_Length, isOptional: true)?.SymbolAsMember(spanType);
                indexerGet = (MethodSymbol?)_factory.WellKnownMember(WellKnownMember.System_ReadOnlySpan_T__get_Item, isOptional: true)?.SymbolAsMember(spanType);
            }

            return lengthGet is { } && indexerGet is { };
        }

        /// <summary>
        /// Lower a foreach loop that will enumerate a collection using an enumerator.
        ///
        /// <![CDATA[
        /// E e = ((C)(x)).GetEnumerator()  OR  ((C)(x)).GetAsyncEnumerator()
        /// try {
        ///     while (e.MoveNext())  OR  while (await e.MoveNextAsync())
        ///     {
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
            ForEachEnumeratorInfo? enumeratorInfo = node.EnumeratorInfoOpt;
            Debug.Assert(enumeratorInfo != null);

            BoundStatement? rewrittenBody = VisitStatement(node.Body);
            Debug.Assert(rewrittenBody is { });

            return RewriteForEachEnumerator(
                node,
                (BoundConversion)node.Expression,
                enumeratorInfo,
                node.ElementPlaceholder,
                node.ElementConversion,
                node.IterationVariables,
                node.DeconstructionOpt,
                node.AwaitOpt,
                node.BreakLabel,
                node.ContinueLabel,
                rewrittenBody);
        }

        private BoundStatement RewriteForEachEnumerator(
            BoundNode node,
            BoundConversion convertedCollection,
            ForEachEnumeratorInfo enumeratorInfo,
            BoundValuePlaceholder? elementPlaceholder,
            BoundExpression? elementConversion,
            ImmutableArray<LocalSymbol> iterationVariables,
            BoundForEachDeconstructStep? deconstruction,
            BoundAwaitableInfo? awaitableInfo,
            GeneratedLabelSymbol breakLabel,
            GeneratedLabelSymbol continueLabel,
            BoundStatement rewrittenBody)
        {
            var forEachSyntax = (CSharpSyntaxNode)node.Syntax;
            bool isAsync = awaitableInfo != null;

            BoundExpression rewrittenExpression = VisitExpression(convertedCollection.Operand);

            MethodArgumentInfo getEnumeratorInfo = enumeratorInfo.GetEnumeratorInfo;
            TypeSymbol enumeratorType = getEnumeratorInfo.Method.ReturnType;

            // E e
            LocalSymbol enumeratorVar = _factory.SynthesizedLocal(enumeratorType, syntax: forEachSyntax, kind: SynthesizedLocalKind.ForEachEnumerator);

            // Reference to e.
            BoundLocal boundEnumeratorVar = MakeBoundLocal(forEachSyntax, enumeratorVar, enumeratorType);

            var receiver = ConvertReceiverForInvocation(forEachSyntax, rewrittenExpression, getEnumeratorInfo.Method, convertedCollection.Conversion, enumeratorInfo.CollectionType);
            BoundExpression? firstRewrittenArgument = null;

            // If the GetEnumerator call is an extension method, then the first argument is the receiver. We want to replace
            // the first argument with our converted receiver and pass null as the receiver instead.
            if (getEnumeratorInfo.Method.IsExtensionMethod)
            {
                var builder = ArrayBuilder<BoundExpression>.GetInstance(getEnumeratorInfo.Arguments.Length);
                firstRewrittenArgument = receiver;
                builder.Add(firstRewrittenArgument);
                builder.AddRange(getEnumeratorInfo.Arguments, 1, getEnumeratorInfo.Arguments.Length - 1);
                getEnumeratorInfo = new MethodArgumentInfo(
                                            getEnumeratorInfo.Method,
                                            builder.ToImmutableAndFree(),
                                            defaultArguments: default,
                                            getEnumeratorInfo.Expanded);

                receiver = null;
            }

            // ((C)(x)).GetEnumerator();  OR  (x).GetEnumerator();  OR  async variants (which fill-in arguments for optional parameters)
            BoundExpression enumeratorVarInitValue = SynthesizeCall(getEnumeratorInfo, forEachSyntax, receiver,
                allowExtensionAndOptionalParameters: isAsync || getEnumeratorInfo.Method.IsExtensionMethod, firstRewrittenArgument: firstRewrittenArgument);

            // E e = ((C)(x)).GetEnumerator();
            BoundStatement enumeratorVarDecl = MakeLocalDeclaration(forEachSyntax, enumeratorVar, enumeratorVarInitValue);

            InstrumentForEachStatementCollectionVarDeclaration(node, ref enumeratorVarDecl);

            // (V)(T)e.Current
            BoundExpression iterationVarAssignValue = ApplyConversionIfNotIdentity(
                elementConversion,
                elementPlaceholder,
                ApplyConversionIfNotIdentity(
                    enumeratorInfo.CurrentConversion,
                    enumeratorInfo.CurrentPlaceholder,
                    BoundCall.Synthesized(
                        syntax: forEachSyntax,
                        receiverOpt: boundEnumeratorVar,
                        initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                        method: enumeratorInfo.CurrentPropertyGetter)));

            // V v = (V)(T)e.Current;  -OR-  (D1 d1, ...) = (V)(T)e.Current;

            BoundStatement iterationVarDecl = LocalOrDeconstructionDeclaration(forEachSyntax, deconstruction, iterationVariables, iterationVarAssignValue);

            InstrumentForEachStatementIterationVarDeclaration(node, ref iterationVarDecl);

            // while (e.MoveNext())  -OR-  while (await e.MoveNextAsync())
            // {
            //     V v = (V)(T)e.Current;  -OR-  (D1 d1, ...) = (V)(T)e.Current;
            //     /* node.Body */
            // }

            var rewrittenBodyBlock = CreateBlockDeclaringIterationVariables(iterationVariables, iterationVarDecl, rewrittenBody, forEachSyntax);
            BoundExpression rewrittenCondition = SynthesizeCall(
                methodArgumentInfo: enumeratorInfo.MoveNextInfo,
                syntax: forEachSyntax,
                receiver: boundEnumeratorVar,
                allowExtensionAndOptionalParameters: isAsync,
                firstRewrittenArgument: null);

            var disposalFinallyBlock = GetDisposalFinallyBlock(forEachSyntax, enumeratorInfo, enumeratorType, boundEnumeratorVar, out var hasAsyncDisposal);
            if (isAsync)
            {
                Debug.Assert(awaitableInfo is { GetResult: { } });

                // We need to be sure that when the disposal isn't async we reserve an unused state machine state number for it,
                // so that await foreach always produces 2 state machine states: one for MoveNextAsync and the other for DisposeAsync.
                // Otherwise, EnC wouldn't be able to map states when the disposal changes from having async dispose to not, or vice versa.
                var debugInfo = new BoundAwaitExpressionDebugInfo(s_moveNextAsyncAwaitId, ReservedStateMachineCount: (byte)(hasAsyncDisposal ? 0 : 1));

                rewrittenCondition = RewriteAwaitExpression(forEachSyntax, rewrittenCondition, awaitableInfo, awaitableInfo.GetResult.ReturnType, debugInfo, used: true);
            }

            BoundStatement whileLoop = RewriteWhileStatement(
                loop: node,
                rewrittenCondition,
                rewrittenBody: rewrittenBodyBlock,
                breakLabel: breakLabel,
                continueLabel: continueLabel,
                hasErrors: false);

            BoundStatement result;

            if (disposalFinallyBlock != null)
            {
                // try {
                //     while (e.MoveNext()) {
                //         V v = (V)(T)e.Current;  -OR-  (D1 d1, ...) = (V)(T)e.Current;
                //         /* loop body */
                //     }
                // }
                // finally {
                //     /* dispose of e */
                // }
                BoundStatement tryFinally = new BoundTryStatement(
                    forEachSyntax,
                    tryBlock: new BoundBlock(forEachSyntax, locals: ImmutableArray<LocalSymbol>.Empty, statements: ImmutableArray.Create(whileLoop)),
                    catchBlocks: ImmutableArray<BoundCatchBlock>.Empty,
                    finallyBlockOpt: disposalFinallyBlock);

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

        private bool TryGetDisposeMethod(SyntaxNode forEachSyntax, ForEachEnumeratorInfo enumeratorInfo, out MethodSymbol disposeMethod)
        {
            if (enumeratorInfo.IsAsync)
            {
                disposeMethod = (MethodSymbol)Binder.GetWellKnownTypeMember(_compilation, WellKnownMember.System_IAsyncDisposable__DisposeAsync, _diagnostics, syntax: forEachSyntax);
                return (object)disposeMethod != null;
            }

            return Binder.TryGetSpecialTypeMember(_compilation, SpecialMember.System_IDisposable__Dispose, forEachSyntax, _diagnostics, out disposeMethod);
        }

        /// <summary>
        /// There are three possible cases where we need disposal:
        /// - pattern-based disposal (we have a Dispose/DisposeAsync method)
        /// - interface-based disposal (the enumerator type converts to IDisposable/IAsyncDisposable)
        /// - we need to do a runtime check for IDisposable
        /// </summary>
        /// <returns>Finally block, or null if none should be emitted.</returns>
        private BoundBlock? GetDisposalFinallyBlock(
            CSharpSyntaxNode forEachSyntax,
            ForEachEnumeratorInfo enumeratorInfo,
            TypeSymbol enumeratorType,
            BoundLocal boundEnumeratorVar,
            out bool hasAsyncDisposal)
        {
            hasAsyncDisposal = false;

            if (!enumeratorInfo.NeedsDisposal)
            {
                return null;
            }

            NamedTypeSymbol? idisposableTypeSymbol = null;
            bool implementsInterface = false;
            MethodSymbol? disposeMethod = enumeratorInfo.PatternDisposeInfo?.Method; // pattern-based

            if (disposeMethod is null)
            {
                TryGetDisposeMethod(forEachSyntax, enumeratorInfo, out disposeMethod); // interface-based

                // This is a temporary workaround for https://github.com/dotnet/roslyn/issues/39948
                if (disposeMethod is null)
                {
                    return null;
                }

                idisposableTypeSymbol = disposeMethod.ContainingType;
                Debug.Assert(_factory.CurrentFunction is { });
                var conversions = _factory.CurrentFunction.ContainingAssembly.CorLibrary.TypeConversions;

                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo();
                implementsInterface = conversions.HasImplicitConversionToOrImplementsVarianceCompatibleInterface(enumeratorType, idisposableTypeSymbol, ref useSiteInfo, out _);
                _diagnostics.Add(forEachSyntax, useSiteInfo);
            }

            Binder.ReportDiagnosticsIfObsolete(_diagnostics, disposeMethod, forEachSyntax,
                                               hasBaseReceiver: false,
                                               containingMember: _factory.CurrentFunction,
                                               containingType: _factory.CurrentType,
                                               location: enumeratorInfo.Location);

            if (implementsInterface || !(enumeratorInfo.PatternDisposeInfo is null))
            {
                Conversion receiverConversion = enumeratorType.IsStructType() ?
                    Conversion.Boxing :
                    Conversion.ImplicitReference;

                BoundExpression receiver;
                BoundExpression disposeCall;
                var disposeInfo = enumeratorInfo.PatternDisposeInfo;
                if (disposeInfo is null)
                {
                    Debug.Assert(idisposableTypeSymbol is { });
                    disposeInfo = MethodArgumentInfo.CreateParameterlessMethod(disposeMethod);
                    receiver = ConvertReceiverForInvocation(forEachSyntax, boundEnumeratorVar, disposeMethod, receiverConversion, idisposableTypeSymbol);
                }
                else
                {
                    receiver = boundEnumeratorVar;
                }

                // ((IDisposable)e).Dispose() or e.Dispose() or await ((IAsyncDisposable)e).DisposeAsync() or await e.DisposeAsync()
                disposeCall = MakeCallWithNoExplicitArgument(disposeInfo, forEachSyntax, receiver, firstRewrittenArgument: null);

                BoundStatement disposeCallStatement;
                var disposeAwaitableInfoOpt = enumeratorInfo.DisposeAwaitableInfo;
                if (disposeAwaitableInfoOpt != null)
                {
                    // await /* disposeCall */
                    disposeCallStatement = WrapWithAwait(forEachSyntax, disposeCall, disposeAwaitableInfoOpt);
                    _sawAwaitInExceptionHandler = true;
                    hasAsyncDisposal = true;
                }
                else
                {
                    // ((IDisposable)e).Dispose(); or e.Dispose();
                    disposeCallStatement = new BoundExpressionStatement(forEachSyntax, disposeCall);
                }

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
                    var objectType = _factory.SpecialType(SpecialType.System_Object);
                    alwaysOrMaybeDisposeStmt = RewriteIfStatement(
                        syntax: forEachSyntax,
                        rewrittenCondition: _factory.IsNotNullReference(boundEnumeratorVar),
                        rewrittenConsequence: disposeCallStatement,
                        hasErrors: false);
                }

                return new BoundBlock(forEachSyntax,
                    locals: ImmutableArray<LocalSymbol>.Empty,
                    statements: ImmutableArray.Create(alwaysOrMaybeDisposeStmt));
            }
            else
            {
                // If we couldn't find either pattern-based or interface-based disposal, and the enumerator type isn't sealed,
                // and the loop isn't async, then we include a runtime check.
                Debug.Assert(!enumeratorType.IsSealed);
                Debug.Assert(!enumeratorInfo.IsAsync);
                Debug.Assert(idisposableTypeSymbol is { });
                Debug.Assert(disposeMethod is { });

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
                    operandPlaceholder: null,
                    operandConversion: null,
                    type: idisposableTypeSymbol);

                // IDisposable d = e as IDisposable;
                BoundStatement disposableVarDecl = MakeLocalDeclaration(forEachSyntax, disposableVar, disposableVarInitValue);

                // d.Dispose()
                BoundExpression disposeCall = BoundCall.Synthesized(syntax: forEachSyntax, receiverOpt: boundDisposableVar, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, method: disposeMethod);
                BoundStatement disposeCallStatement = new BoundExpressionStatement(forEachSyntax, expression: disposeCall);

                // if (d != null) d.Dispose();
                BoundStatement ifStmt = RewriteIfStatement(
                    syntax: forEachSyntax,
                    rewrittenCondition: new BoundBinaryOperator(forEachSyntax,
                        operatorKind: BinaryOperatorKind.NotEqual, // reference equality
                        left: boundDisposableVar,
                        right: MakeLiteral(forEachSyntax, constantValue: ConstantValue.Null, type: null),
                        constantValueOpt: null,
                        methodOpt: null,
                        constrainedToTypeOpt: null,
                        resultKind: LookupResultKind.Viable,
                        type: _compilation.GetSpecialType(SpecialType.System_Boolean)),
                    rewrittenConsequence: disposeCallStatement,
                    hasErrors: false);

                // IDisposable d = e as IDisposable;
                // if (d != null) d.Dispose();
                return new BoundBlock(forEachSyntax,
                    locals: ImmutableArray.Create(disposableVar),
                    statements: ImmutableArray.Create(disposableVarDecl, ifStmt));
            }
        }

        /// <summary>
        /// Produce:
        /// await /* disposeCall */;
        /// </summary>
        private BoundStatement WrapWithAwait(SyntaxNode forEachSyntax, BoundExpression disposeCall, BoundAwaitableInfo disposeAwaitableInfoOpt)
        {
            TypeSymbol awaitExpressionType = disposeAwaitableInfoOpt.GetResult?.ReturnType ?? _compilation.DynamicType;
            var debugInfo = new BoundAwaitExpressionDebugInfo(s_disposeAsyncAwaitId, ReservedStateMachineCount: 0);
            var awaitExpr = RewriteAwaitExpression(forEachSyntax, disposeCall, disposeAwaitableInfoOpt, awaitExpressionType, debugInfo, used: false);
            return new BoundExpressionStatement(forEachSyntax, awaitExpr);
        }

        /// <summary>
        /// Optionally apply a conversion to the receiver.
        ///
        /// If the receiver is of struct type and the method is an interface method, then skip the conversion.
        /// When we call the interface method directly - the code generator will detect it and generate a
        /// constrained virtual call.
        /// </summary>
        /// <param name="syntax">A syntax node to attach to the synthesized bound node.</param>
        /// <param name="receiver">Receiver of method call.</param>
        /// <param name="method">Method to invoke.</param>
        /// <param name="receiverConversion">Conversion to be applied to the receiver if not calling an interface method on a struct.</param>
        /// <param name="convertedReceiverType">Type of the receiver after applying the conversion.</param>
        private BoundExpression ConvertReceiverForInvocation(CSharpSyntaxNode syntax, BoundExpression receiver, MethodSymbol method, Conversion receiverConversion, TypeSymbol convertedReceiverType)
        {
            Debug.Assert(receiver.Type is { });
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
            }
            else
            {
                // ((Interface)receiver).InterfaceMethod()
                Debug.Assert(!receiverConversion.IsNumeric);

                receiver = MakeConversionNode(
                    syntax: syntax,
                    rewrittenOperand: receiver,
                    conversion: receiverConversion,
                    @checked: false,
                    rewrittenType: convertedReceiverType);
            }

            return receiver;
        }

        private BoundExpression SynthesizeCall(MethodArgumentInfo methodArgumentInfo, CSharpSyntaxNode syntax, BoundExpression? receiver, bool allowExtensionAndOptionalParameters, BoundExpression? firstRewrittenArgument)
        {
            if (allowExtensionAndOptionalParameters)
            {
                // Generate a call with zero explicit arguments, but with implicit arguments for optional and params parameters.
                return MakeCallWithNoExplicitArgument(methodArgumentInfo, syntax, receiver, firstRewrittenArgument: firstRewrittenArgument);
            }

            // Generate a call with literally zero arguments
            Debug.Assert(methodArgumentInfo.Arguments.IsEmpty);
            return BoundCall.Synthesized(syntax, receiver, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, methodArgumentInfo.Method, arguments: ImmutableArray<BoundExpression>.Empty);
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
        private BoundStatement RewriteForEachStatementAsFor<TArg>(BoundForEachStatement node, GetForEachStatementAsForPreamble? getPreamble, GetForEachStatementAsForItem<TArg> getItem, GetForEachStatementAsForLength<TArg> getLength, TArg arg)
        {
            BoundExpression collectionExpression = GetUnconvertedCollectionExpression(node, out _);

            BoundStatement? rewrittenBody = VisitStatement(node.Body);
            Debug.Assert(rewrittenBody is { });

            Debug.Assert(node.EnumeratorInfoOpt is not null);
            return RewriteForEachStatementAsFor<TArg>(
                node,
                getPreamble,
                getItem,
                getLength,
                arg,
                collectionExpression,
                node.EnumeratorInfoOpt,
                node.ElementPlaceholder,
                node.ElementConversion,
                node.IterationVariables,
                node.DeconstructionOpt,
                node.BreakLabel,
                node.ContinueLabel,
                rewrittenBody);
        }

        private BoundStatement RewriteForEachStatementAsFor<TArg>(
            BoundNode node,
            GetForEachStatementAsForPreamble? getPreamble,
            GetForEachStatementAsForItem<TArg> getItem,
            GetForEachStatementAsForLength<TArg> getLength,
            TArg arg,
            BoundExpression collectionExpression,
            ForEachEnumeratorInfo enumeratorInfo,
            BoundValuePlaceholder? elementPlaceholder,
            BoundExpression? elementConversion,
            ImmutableArray<LocalSymbol> iterationVariables,
            BoundForEachDeconstructStep? deconstructionOpt,
            GeneratedLabelSymbol breakLabel,
            GeneratedLabelSymbol continueLabel,
            BoundStatement rewrittenBody)
        {
            NamedTypeSymbol? collectionType = (NamedTypeSymbol?)collectionExpression.Type;
            Debug.Assert(collectionType is { });

            TypeSymbol intType = _compilation.GetSpecialType(SpecialType.System_Int32);
            TypeSymbol boolType = _compilation.GetSpecialType(SpecialType.System_Boolean);

            BoundExpression rewrittenExpression = VisitExpression(collectionExpression);

            var forEachSyntax = (CSharpSyntaxNode)node.Syntax;
            LocalSymbol? preambleLocal = null;
            RefKind collectionTempRefKind = RefKind.None;
            BoundStatement? collectionVarInitializationPreamble = getPreamble?.Invoke(this, forEachSyntax, enumeratorInfo, ref rewrittenExpression, out preambleLocal, out collectionTempRefKind);

            // Collection a
            LocalSymbol collectionTemp = _factory.SynthesizedLocal(collectionType, forEachSyntax, kind: SynthesizedLocalKind.ForEachArray, refKind: collectionTempRefKind);

            // Collection a = /*node.Expression*/;
            BoundStatement arrayVarDecl = MakeLocalDeclaration(forEachSyntax, collectionTemp, rewrittenExpression);

            if (collectionVarInitializationPreamble is object)
            {
                arrayVarDecl = new BoundStatementList(arrayVarDecl.Syntax, ImmutableArray.Create(collectionVarInitializationPreamble, arrayVarDecl)).MakeCompilerGenerated();
            }

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
            BoundExpression iterationVarInitValue = ApplyConversionIfNotIdentity(
                elementConversion,
                elementPlaceholder,
                getItem(this, forEachSyntax, enumeratorInfo, boundArrayVar, boundPositionVar, arg));

            // V v = (V)a[p];   /* OR */   (D1 d1, ...) = (V)a[p];
            BoundStatement iterationVariableDecl = LocalOrDeconstructionDeclaration(forEachSyntax, deconstructionOpt, iterationVariables, iterationVarInitValue);

            InstrumentForEachStatementIterationVarDeclaration(node, ref iterationVariableDecl);

            BoundStatement initializer = new BoundStatementList(forEachSyntax,
                        statements: ImmutableArray.Create<BoundStatement>(arrayVarDecl, positionVarDecl));

            // a.Length
            BoundExpression arrayLength = getLength(this, forEachSyntax, boundArrayVar, arg);

            // p < a.Length
            BoundExpression exitCondition = new BoundBinaryOperator(
                syntax: forEachSyntax,
                operatorKind: BinaryOperatorKind.IntLessThan,
                left: boundPositionVar,
                right: arrayLength,
                constantValueOpt: null,
                methodOpt: null,
                constrainedToTypeOpt: null,
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
                outerLocals: preambleLocal is null ? ImmutableArray.Create<LocalSymbol>(collectionTemp, positionVar) : ImmutableArray.Create<LocalSymbol>(preambleLocal, collectionTemp, positionVar),
                rewrittenInitializer: initializer,
                rewrittenCondition: exitCondition,
                rewrittenIncrement: positionIncrement,
                rewrittenBody: loopBody,
                breakLabel: breakLabel,
                continueLabel: continueLabel,
                hasErrors: node.HasErrors);

            InstrumentForEachStatement(node, ref result);

            return result;
        }

        private delegate BoundStatement? GetForEachStatementAsForPreamble(LocalRewriter rewriter, SyntaxNode syntax, ForEachEnumeratorInfo enumeratorInfo, ref BoundExpression rewrittenExpression, out LocalSymbol? preambleLocal, out RefKind collectionTempRefKind);
        private delegate BoundExpression GetForEachStatementAsForItem<TArg>(LocalRewriter rewriter, SyntaxNode syntax, ForEachEnumeratorInfo enumeratorInfo, BoundLocal boundArrayVar, BoundLocal boundPositionVar, TArg arg);
        private delegate BoundExpression GetForEachStatementAsForLength<TArg>(LocalRewriter rewriter, SyntaxNode syntax, BoundLocal boundArrayVar, TArg arg);

        private BoundStatement RewriteForEachStatementAsFor(BoundForEachStatement node, MethodSymbol indexerGet, MethodSymbol lengthGet)
        {
            return RewriteForEachStatementAsFor(node,
                                                getPreamble: null,
                                                getItem: static (LocalRewriter rewriter, SyntaxNode syntax, ForEachEnumeratorInfo enumeratorInfo, BoundLocal boundArrayVar, BoundLocal boundPositionVar, (MethodSymbol indexerGet, MethodSymbol lengthGet) arg) =>
                                                {
                                                    return BoundCall.Synthesized(
                                                               syntax: syntax,
                                                               receiverOpt: boundArrayVar,
                                                               initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                                                               arg.indexerGet,
                                                               boundPositionVar);
                                                },
                                                getLength: static (LocalRewriter rewriter, SyntaxNode syntax, BoundLocal boundArrayVar, (MethodSymbol indexerGet, MethodSymbol lengthGet) arg) =>
                                                {
                                                    return BoundCall.Synthesized(
                                                               syntax: syntax,
                                                               receiverOpt: boundArrayVar,
                                                               initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                                                               arg.lengthGet);
                                                },
                                                arg: (indexerGet, lengthGet));
        }

        private BoundStatement RewriteInlineArrayForEachStatementAsFor(BoundForEachStatement node)
        {
            return RewriteForEachStatementAsFor(node,
                                                getPreamble: GetInlineArrayForEachStatementPreambleDelegate(),
                                                getItem: GetInlineArrayForEachStatementGetItemDelegate(),
                                                getLength: GetInlineArrayForEachStatementGetLengthDelegate(),
                                                arg: null);
        }

        private static GetForEachStatementAsForPreamble GetInlineArrayForEachStatementPreambleDelegate()
        {
            return static (LocalRewriter rewriter, SyntaxNode syntax, ForEachEnumeratorInfo enumeratorInfo, ref BoundExpression rewrittenExpression, out LocalSymbol? preambleLocal, out RefKind collectionTempRefKind) =>
            {
                Debug.Assert(rewrittenExpression.Type is not null);

                BoundStatement? collectionVarInitializationPreamble = null;
                preambleLocal = null;
                if (enumeratorInfo.InlineArrayUsedAsValue)
                {
                    BoundLocal boundLocal = rewriter._factory.StoreToTemp(rewrittenExpression, out BoundAssignmentOperator? valueStore);
                    rewrittenExpression = boundLocal;
                    collectionVarInitializationPreamble = rewriter._factory.ExpressionStatement(valueStore);
                    preambleLocal = boundLocal.LocalSymbol;
                }

                collectionTempRefKind = enumeratorInfo.InlineArraySpanType == WellKnownType.System_Span_T ? RefKind.Ref : RefKindExtensions.StrictIn;
                return collectionVarInitializationPreamble;
            };
        }

        private static GetForEachStatementAsForItem<object?> GetInlineArrayForEachStatementGetItemDelegate()
        {
            return static (LocalRewriter rewriter, SyntaxNode syntax, ForEachEnumeratorInfo enumeratorInfo, BoundLocal boundArrayVar, BoundLocal boundPositionVar, object? _) =>
            {
                MethodSymbol elementRef;
                Debug.Assert(rewriter._factory.ModuleBuilderOpt is { });
                Debug.Assert(rewriter._diagnostics.DiagnosticBag is { });

                NamedTypeSymbol intType = rewriter._factory.SpecialType(SpecialType.System_Int32);
                if (enumeratorInfo.InlineArraySpanType == WellKnownType.System_Span_T)
                {
                    elementRef = rewriter._factory.ModuleBuilderOpt.EnsureInlineArrayElementRefExists(syntax, intType, rewriter._diagnostics.DiagnosticBag);
                }
                else
                {
                    Debug.Assert(enumeratorInfo.InlineArraySpanType == WellKnownType.System_ReadOnlySpan_T);
                    elementRef = rewriter._factory.ModuleBuilderOpt.EnsureInlineArrayElementRefReadOnlyExists(syntax, intType, rewriter._diagnostics.DiagnosticBag);
                }

                TypeSymbol inlineArrayType = boundArrayVar.Type;
                elementRef = elementRef.Construct(inlineArrayType, inlineArrayType.TryGetInlineArrayElementField()!.Type);

                return rewriter._factory.Call(null, elementRef, boundArrayVar, boundPositionVar, useStrictArgumentRefKinds: true);
            };
        }

        private static GetForEachStatementAsForLength<object?> GetInlineArrayForEachStatementGetLengthDelegate()
        {
            return static (LocalRewriter rewriter, SyntaxNode syntax, BoundLocal boundArrayVar, object? _) =>
            {
                _ = boundArrayVar.Type.HasInlineArrayAttribute(out int length);
                Debug.Assert(length > 0);
                BoundExpression arrayLength = rewriter._factory.Literal(length);
                return arrayLength;
            };
        }

        /// <summary>
        /// Takes the expression for the current value of the iteration variable and either
        /// (1) assigns it into a local, or
        /// (2) deconstructs it into multiple locals (if there is a deconstruct step).
        ///
        /// Produces <c>V v = /* expression */</c> or <c>(D1 d1, ...) = /* expression */</c>.
        /// </summary>
        private BoundStatement LocalOrDeconstructionDeclaration(
            CSharpSyntaxNode syntax,
            BoundForEachDeconstructStep? deconstruction,
            ImmutableArray<LocalSymbol> iterationVariables,
            BoundExpression iterationVarValue)
        {
            BoundStatement iterationVarDecl;

            if (deconstruction == null)
            {
                // V v = /* expression */
                Debug.Assert(iterationVariables.Length == 1);
                iterationVarDecl = MakeLocalDeclaration(syntax, iterationVariables[0], iterationVarValue);
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
            SyntaxNode forEachSyntax)
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
            BoundExpression collectionExpression = GetUnconvertedCollectionExpression(node, out _);
            Debug.Assert(collectionExpression.Type is { TypeKind: TypeKind.Array });

            BoundStatement? rewrittenBody = VisitStatement(node.Body);
            Debug.Assert(rewrittenBody is { });

            return RewriteSingleDimensionalArrayForEachEnumerator(
                node,
                collectionExpression,
                node.ElementPlaceholder,
                node.ElementConversion,
                node.IterationVariables,
                node.DeconstructionOpt,
                node.BreakLabel,
                node.ContinueLabel,
                rewrittenBody);
        }

        private BoundStatement RewriteSingleDimensionalArrayForEachEnumerator(
            BoundNode node,
            BoundExpression collectionExpression,
            BoundValuePlaceholder? elementPlaceholder,
            BoundExpression? elementConversion,
            ImmutableArray<LocalSymbol> iterationVariables,
            BoundForEachDeconstructStep? deconstruction,
            GeneratedLabelSymbol breakLabel,
            GeneratedLabelSymbol continueLabel,
            BoundStatement rewrittenBody)
        {
            Debug.Assert(collectionExpression.Type is { TypeKind: TypeKind.Array });

            var forEachSyntax = (CSharpSyntaxNode)node.Syntax;

            ArrayTypeSymbol arrayType = (ArrayTypeSymbol)collectionExpression.Type;
            BoundExpression rewrittenExpression = VisitExpression(collectionExpression);
            Debug.Assert(arrayType is { IsSZArray: true });

            TypeSymbol intType = _compilation.GetSpecialType(SpecialType.System_Int32);
            TypeSymbol boolType = _compilation.GetSpecialType(SpecialType.System_Boolean);

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
            BoundExpression iterationVarInitValue = ApplyConversionIfNotIdentity(
                elementConversion,
                elementPlaceholder,
                new BoundArrayAccess(
                    syntax: forEachSyntax,
                    expression: boundArrayVar,
                    indices: ImmutableArray.Create<BoundExpression>(boundPositionVar),
                    type: arrayType.ElementType));

            // V v = (V)a[p];   /* OR */   (D1 d1, ...) = (V)a[p];
            BoundStatement iterationVariableDecl = LocalOrDeconstructionDeclaration(forEachSyntax, deconstruction, iterationVariables, iterationVarInitValue);

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
                constrainedToTypeOpt: null,
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
                breakLabel: breakLabel,
                continueLabel: continueLabel,
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
            BoundExpression collectionExpression = GetUnconvertedCollectionExpression(node, out _);
            Debug.Assert(collectionExpression.Type is { TypeKind: TypeKind.Array });

            BoundStatement? rewrittenBody = VisitStatement(node.Body);
            Debug.Assert(rewrittenBody is { });

            return RewriteMultiDimensionalArrayForEachEnumerator(
                node,
                collectionExpression,
                node.ElementPlaceholder,
                node.ElementConversion,
                node.IterationVariables,
                node.DeconstructionOpt,
                node.BreakLabel,
                node.ContinueLabel,
                rewrittenBody);
        }

        private BoundStatement RewriteMultiDimensionalArrayForEachEnumerator(
            BoundNode node,
            BoundExpression collectionExpression,
            BoundValuePlaceholder? elementPlaceholder,
            BoundExpression? elementConversion,
            ImmutableArray<LocalSymbol> iterationVariables,
            BoundForEachDeconstructStep? deconstruction,
            GeneratedLabelSymbol breakLabel,
            GeneratedLabelSymbol continueLabel,
            BoundStatement rewrittenBody)
        {
            Debug.Assert(collectionExpression.Type is { TypeKind: TypeKind.Array });

            var forEachSyntax = (CSharpSyntaxNode)node.Syntax;

            ArrayTypeSymbol arrayType = (ArrayTypeSymbol)collectionExpression.Type;

            int rank = arrayType.Rank;
            Debug.Assert(!arrayType.IsSZArray);

            TypeSymbol intType = _compilation.GetSpecialType(SpecialType.System_Int32);
            TypeSymbol boolType = _compilation.GetSpecialType(SpecialType.System_Boolean);

            // Values we'll use every iteration
            MethodSymbol getLowerBoundMethod = UnsafeGetSpecialTypeMethod(forEachSyntax, SpecialMember.System_Array__GetLowerBound);
            MethodSymbol getUpperBoundMethod = UnsafeGetSpecialTypeMethod(forEachSyntax, SpecialMember.System_Array__GetUpperBound);

            BoundExpression rewrittenExpression = VisitExpression(collectionExpression);

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
                BoundExpression currentDimensionUpperBound = BoundCall.Synthesized(forEachSyntax, boundArrayVar, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, getUpperBoundMethod, dimensionArgument);

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
            BoundExpression iterationVarInitValue = ApplyConversionIfNotIdentity(
                elementConversion,
                elementPlaceholder,
                new BoundArrayAccess(forEachSyntax,
                    expression: boundArrayVar,
                    indices: ImmutableArray.Create((BoundExpression[])boundPositionVar),
                    type: arrayType.ElementType));

            // V v = (V)a[p_0, p_1, ...];   /* OR */   (D1 d1, ...) = (V)a[p_0, p_1, ...];

            BoundStatement iterationVarDecl = LocalOrDeconstructionDeclaration(forEachSyntax, deconstruction, iterationVariables, iterationVarInitValue);

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
            BoundStatement? forLoop = null;
            for (int dimension = rank - 1; dimension >= 0; dimension--)
            {
                ImmutableArray<BoundExpression> dimensionArgument = ImmutableArray.Create(
                    MakeLiteral(forEachSyntax,
                        constantValue: ConstantValue.Create(dimension, ConstantValueTypeDiscriminator.Int32),
                        type: intType));

                // a.GetLowerBound(dimension)
                BoundExpression currentDimensionLowerBound = BoundCall.Synthesized(forEachSyntax, boundArrayVar, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, getLowerBoundMethod, dimensionArgument);

                // int p_dimension = a.GetLowerBound(dimension);
                BoundStatement positionVarDecl = MakeLocalDeclaration(forEachSyntax, positionVar[dimension], currentDimensionLowerBound);

                GeneratedLabelSymbol breakLabelInner = dimension == 0 // outermost for-loop
                    ? breakLabel // i.e. the one that break statements will jump to
                    : new GeneratedLabelSymbol("break"); // Should not affect emitted code since unused

                // p_dimension <= q_dimension  //NB: OrEqual
                BoundExpression exitCondition = new BoundBinaryOperator(
                    syntax: forEachSyntax,
                    operatorKind: BinaryOperatorKind.IntLessThanOrEqual,
                    left: boundPositionVar[dimension],
                    right: boundUpperVar[dimension],
                    constantValueOpt: null,
                    methodOpt: null,
                    constrainedToTypeOpt: null,
                    resultKind: LookupResultKind.Viable,
                    type: boolType);

                // p_dimension = p_dimension + 1;
                BoundStatement positionIncrement = MakePositionIncrement(forEachSyntax, boundPositionVar[dimension], intType);

                BoundStatement body;
                GeneratedLabelSymbol continueLabelInner;

                if (forLoop == null)
                {
                    // innermost for-loop
                    body = innermostLoopBody;
                    continueLabelInner = continueLabel; //i.e. the one continue statements will actually jump to
                }
                else
                {
                    body = forLoop;
                    continueLabelInner = new GeneratedLabelSymbol("continue"); // Should not affect emitted code since unused
                }

                forLoop = RewriteForStatementWithoutInnerLocals(
                    original: node,
                    outerLocals: ImmutableArray.Create(positionVar[dimension]),
                    rewrittenInitializer: positionVarDecl,
                    rewrittenCondition: exitCondition,
                    rewrittenIncrement: positionIncrement,
                    rewrittenBody: body,
                    breakLabel: breakLabelInner,
                    continueLabel: continueLabelInner,
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
        private static BoundExpression GetUnconvertedCollectionExpression(BoundForEachStatement node, out Conversion collectionConversion)
        {
            var boundConversion = (BoundConversion)node.Expression;
            collectionConversion = boundConversion.Conversion;
            return boundConversion.Operand;
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
            var result = RewriteLocalDeclaration(
                originalOpt: null,
                syntax: syntax,
                localSymbol: local,
                rewrittenInitializer: rewrittenInitialValue);
            Debug.Assert(result is { });
            return result;
        }

        // Used to increment integer index into an array or string.
        private BoundStatement MakePositionIncrement(CSharpSyntaxNode syntax, BoundLocal boundPositionVar, TypeSymbol intType)
        {
            // A normal for-loop would have a sequence point on the increment.  We don't want that since the code is synthesized,
            // but we add a hidden sequence point to avoid disrupting the stepping experience.
            // A bound sequence point is permitted to have a null syntax to make a hidden sequence point.
            return BoundSequencePoint.CreateHidden(
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
                            constrainedToTypeOpt: null,
                            resultKind: LookupResultKind.Viable,
                            type: intType),
                        type: intType)));
        }

        private void InstrumentForEachStatementCollectionVarDeclaration(BoundNode node, [NotNullIfNotNull(nameof(collectionVarDecl))] ref BoundStatement? collectionVarDecl)
        {
            if (this.Instrument && node is BoundForEachStatement original)
            {
                collectionVarDecl = Instrumenter.InstrumentForEachStatementCollectionVarDeclaration(original, collectionVarDecl);
            }
        }

        private void InstrumentForEachStatementIterationVarDeclaration(BoundNode node, ref BoundStatement iterationVarDecl)
        {
            if (this.Instrument && node is BoundForEachStatement original)
            {
                CommonForEachStatementSyntax forEachSyntax = (CommonForEachStatementSyntax)original.Syntax;
                if (forEachSyntax is ForEachVariableStatementSyntax)
                {
                    iterationVarDecl = Instrumenter.InstrumentForEachStatementDeconstructionVariablesDeclaration(original, iterationVarDecl);
                }
                else
                {
                    iterationVarDecl = Instrumenter.InstrumentForEachStatementIterationVarDeclaration(original, iterationVarDecl);
                }
            }
        }

        private void InstrumentForEachStatement(BoundNode node, ref BoundStatement result)
        {
            if (this.Instrument && node is BoundForEachStatement original)
            {
                result = Instrumenter.InstrumentForEachStatement(original, result);
            }
        }
    }
}
