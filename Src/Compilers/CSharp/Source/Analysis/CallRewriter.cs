using System.Collections.Generic;
using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    // The call rewriter takes call nodes that might have params arrays, named arguments or optional
    // arguments. It rewrites them into call nodes that have the correct number of arguments in the
    // correct order. 

    // UNDONE: * optional arguments are not yet implemented
    // TODO (tomat): Rename to ExpressionRewriter
    internal sealed class CallRewriter : BoundTreeRewriter
    {
        private MethodSymbol containingSymbol;
        private readonly NamedTypeSymbol containingType;
        private readonly Compilation compilation;

        private SynthesizedSubmissionFields previousSubmissionFields;

        private CallRewriter(MethodSymbol containingSymbol, NamedTypeSymbol containingType, SynthesizedSubmissionFields previousSubmissionFields, Compilation compilation)
        {
            this.compilation = compilation;
            this.containingSymbol = containingSymbol;
            this.containingType = containingType ?? containingSymbol.ContainingType;
            this.previousSubmissionFields = previousSubmissionFields;
        }

        public static BoundStatement Rewrite(BoundStatement node, MethodSymbol containingSymbol, NamedTypeSymbol containingType, SynthesizedSubmissionFields previousSubmissionFields, Compilation compilation)
        {
            Debug.Assert(node != null);
            var rewriter = new CallRewriter(containingSymbol, containingType, previousSubmissionFields, compilation);
            var result = (BoundStatement)rewriter.Visit(node);
            return result;
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            var oldContainingSymbol = this.containingSymbol;
            try
            {
                this.containingSymbol = node.Symbol;
                return base.VisitLambda(node);
            }
            finally
            {
                this.containingSymbol = oldContainingSymbol;
            }
        }

        public override BoundNode VisitPreviousSubmissionReference(BoundPreviousSubmissionReference node)
        {
            var targetType = (ImplicitTypeSymbol)node.Type;
            Debug.Assert(targetType.IsScriptClass);
            Debug.Assert(!containingSymbol.IsStatic);

            Debug.Assert(previousSubmissionFields != null);

            var targetScriptReference = previousSubmissionFields.GetOrMakeField(targetType);
            var thisReference = new BoundThisReference(null, null, containingSymbol.ThisParameter, containingType);
            return new BoundFieldAccess(null, null, thisReference, targetScriptReference, constantValueOpt: null);
        }

        public override BoundNode VisitHostObjectMemberReference(BoundHostObjectMemberReference node)
        {
            Debug.Assert(previousSubmissionFields != null);
            Debug.Assert(!containingSymbol.IsStatic);

            var hostObjectReference = previousSubmissionFields.GetHostObjectField();
            var thisReference = new BoundThisReference(null, null, containingSymbol.ThisParameter, containingType);
            return new BoundFieldAccess(null, null, thisReference, hostObjectReference, constantValueOpt: null);
        }

        private static bool IsSafeForReordering(BoundExpression expression, RefKind kind)
        {
            // To be safe for reordering an expression must not cause any observable side effect *or
            // observe any side effect*. Accessing a local by value, for example, is possibly not
            // safe for reordering because reading a local can give a different result if reordered
            // with respect to a write elsewhere.

            var current = expression;
            while (true)
            {
                if (current.ConstantValue != null)
                {
                    return true;
                }

                switch (current.Kind)
                {
                    default:
                        return false;
                    case BoundKind.Parameter:
                    case BoundKind.Local:
                        // A ref to a local variable or formal parameter is safe to reorder; it
                        // never has a side effect or consumes one.
                        return kind != RefKind.None;
                    case BoundKind.Conversion:
                        {
                            BoundConversion conv = (BoundConversion)current;
                            switch (conv.ConversionKind)
                            {
                                case ConversionKind.AnonymousFunction:
                                case ConversionKind.ImplicitConstant:
                                case ConversionKind.MethodGroup:
                                case ConversionKind.NullLiteral:
                                    return true;
                                case ConversionKind.Boxing:
                                case ConversionKind.Dynamic:
                                case ConversionKind.ExplicitEnumeration:
                                case ConversionKind.ExplicitNullable:
                                case ConversionKind.ExplicitNumeric:
                                case ConversionKind.ExplicitReference:
                                case ConversionKind.Identity:
                                case ConversionKind.ImplicitEnumeration:
                                case ConversionKind.ImplicitNullable:
                                case ConversionKind.ImplicitNumeric:
                                case ConversionKind.ImplicitReference:
                                case ConversionKind.Unboxing:
                                    current = conv.Operand;
                                    break;
                                case ConversionKind.ExplicitUserDefined:
                                case ConversionKind.ImplicitUserDefined:
                                    return false;
                                default:
                                    Debug.Fail("Unhandled conversion kind in reordering logic");
                                    return false;
                            }
                            break;
                        }
                }
            }
        }

        public override BoundNode VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            Debug.Assert(node != null);

            // Start by rewriting the arguments:
            var rewrittenArguments = VisitList(node.Arguments);

            // If the mapping from arguments to parameters is perfectly in order, no complex rewriting is needed.
            if (node.ArgsToParamsOpt.IsNull && !node.Expanded)
            {
                return node.Update(node.ConstructorOpt, rewrittenArguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt, node.Expanded, node.ArgsToParamsOpt, node.ConstantValueOpt, node.Type);
            }

            var argumentRefKinds = node.ArgumentRefKindsOpt;
            ReadOnlyArray<LocalSymbol> temps;

            Debug.Assert(node.ConstructorOpt != null, "rewriting arguments when there is no constructor");

            RewriteArguments(node.ConstructorOpt,
                        node.Expanded,
                        node.ArgsToParamsOpt,
                        ref argumentRefKinds,
                        ref rewrittenArguments,
                        out temps);

            if (temps.IsNullOrEmpty)
            {
                return node.Update(
                    node.ConstructorOpt,
                    rewrittenArguments,
                    ReadOnlyArray<string>.Null,
                    argumentRefKinds,
                    false,
                    ReadOnlyArray<int>.Null,
                    node.ConstantValueOpt,
                    node.Type);
            }
            else
            {
                return new BoundSequence(
                    null,
                    null,
                    temps,
                    ReadOnlyArray<BoundExpression>.Empty,
                    new BoundObjectCreationExpression(
                        node.Syntax,
                        node.SyntaxTree,
                        node.ConstructorOpt,
                        rewrittenArguments,
                        ReadOnlyArray<string>.Null,
                        argumentRefKinds,
                        false,
                        ReadOnlyArray<int>.Null,
                        node.ConstantValueOpt,
                        node.Type),
                    node.Type
                );
            }
        }

        public override BoundNode VisitExpressionStatement(BoundExpressionStatement node)
        {
            // Avoid rewriting if node has errors since
            // the expression may be invalid.
            if (node.HasErrors)
            {
                return node;
            }

            var assignmentOperator = node.Expression as BoundAssignmentOperator;
            if (assignmentOperator != null)
            {
                // Avoid extra temporary by indicating the expression value is not used.
                var expr = VisitAssignmentOperator(assignmentOperator, used: false);
                return node.Update(expr);
            }
            else
            {
                return base.VisitExpressionStatement(node);
            }
        }

        public override BoundNode VisitPropertyAccess(BoundPropertyAccess node)
        {
            // Avoid rewriting if node has errors since the accessor may not exist.
            if (node.HasErrors)
            {
                return base.VisitPropertyAccess(node);
            }

            // Rewrite property access into call to getter.
            var property = node.PropertySymbol.GetBaseProperty();
            var getMethod = property.GetMethod;
            Debug.Assert(getMethod != null);
            Debug.Assert(getMethod.Parameters.Count == 0);
            Debug.Assert(!getMethod.IsOverride);

            var rewrittenReceiver = (BoundExpression)Visit(node.ReceiverOpt);
            return BoundCall.SynthesizedCall(rewrittenReceiver, getMethod);
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            // Assume value of expression is used.
            return VisitAssignmentOperator(node, used: true);
        }

        private BoundExpression VisitAssignmentOperator(BoundAssignmentOperator node, bool used)
        {
            // Avoid rewriting if node has errors since at least
            // one of the operands is invalid.
            if (node.HasErrors)
            {
                return node;
            }

            var propertyAccessor = node.Left as BoundPropertyAccess;
            if (propertyAccessor == null)
            {
                return (BoundExpression)base.VisitAssignmentOperator(node);
            }

            // Rewrite property assignment into call to setter.
            var property = propertyAccessor.PropertySymbol.GetBaseProperty();
            var setMethod = property.SetMethod;
            Debug.Assert(setMethod != null);
            Debug.Assert(setMethod.Parameters.Count == 1);
            Debug.Assert(!setMethod.IsOverride);

            var rewrittenReceiver = (BoundExpression)Visit(propertyAccessor.ReceiverOpt);
            var rewrittenArgument = (BoundExpression)Visit(node.Right);

            if (used)
            {
                // Save expression value to a temporary before calling the
                // setter, and restore the temporary after the setter, so the
                // assignment can be used as an embedded expression.
                var exprType = rewrittenArgument.Type;
                var tempSymbol = new TempLocalSymbol(exprType, RefKind.None, containingSymbol);
                var tempLocal = new BoundLocal(null, null, tempSymbol, null, exprType);
                var saveTemp = new BoundAssignmentOperator(
                    null,
                    null,
                    tempLocal,
                    rewrittenArgument,
                    exprType);
                var call = BoundCall.SynthesizedCall(
                    rewrittenReceiver,
                    setMethod,
                    saveTemp);
                return new BoundSequence(
                    node.Syntax,
                    node.SyntaxTree,
                    ReadOnlyArray<LocalSymbol>.CreateFrom(tempSymbol),
                    ReadOnlyArray<BoundExpression>.CreateFrom(call),
                    tempLocal,
                    exprType);
            }
            else
            {
                return BoundCall.SynthesizedCall(
                    rewrittenReceiver,
                    setMethod,
                    rewrittenArgument);
            }
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            Debug.Assert(node != null);

            // Avoid rewriting if node has errors since one or more
            // of the arguments may not be the correct rvalue/lvalue.
            if (node.HasErrors)
            {
                return node;
            }

            // Start by rewriting the arguments and receiver:
            var rewrittenReceiver = (BoundExpression)Visit(node.ReceiverOpt);
            var rewrittenArguments = VisitList(node.Arguments);
            var method = node.Method;

            // If the mapping from arguments to parameters is perfectly in order, no complex rewriting is needed.
            if (node.ArgsToParamsOpt.IsNull && !node.Expanded)
            {
                return node.Update(rewrittenReceiver, method, rewrittenArguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt, node.Expanded, node.ArgsToParamsOpt, node.Type);
            }

            var argumentRefKinds = node.ArgumentRefKindsOpt;
            ReadOnlyArray<LocalSymbol> temps;

            RewriteArguments(method,
                        node.Expanded,
                        node.ArgsToParamsOpt,
                        ref argumentRefKinds,
                        ref rewrittenArguments,
                        out temps);

            var delegateNode = node as BoundDelegateCall;
            if (temps.IsNullOrEmpty)
            {
                return (delegateNode != null)
                    ? delegateNode.Update(
                        rewrittenReceiver,
                        method,
                        rewrittenArguments,
                        ReadOnlyArray<string>.Null,
                        argumentRefKinds,
                        false,
                        ReadOnlyArray<int>.Null,
                        node.Type)
                    : node.Update(
                        rewrittenReceiver,
                        method,
                        rewrittenArguments,
                        ReadOnlyArray<string>.Null,
                        argumentRefKinds,
                        false,
                        ReadOnlyArray<int>.Null,
                        node.Type);
            }
            else
            {
                return new BoundSequence(
                    null,
                    null,
                    temps,
                    ReadOnlyArray<BoundExpression>.Empty,
                    (delegateNode != null)
                        ? new BoundDelegateCall(
                            node.Syntax,
                            node.SyntaxTree,
                            rewrittenReceiver,
                            method,
                            rewrittenArguments,
                            ReadOnlyArray<string>.Null,
                            argumentRefKinds,
                            false,
                            ReadOnlyArray<int>.Null,
                            node.Type)
                        : new BoundCall(
                            node.Syntax,
                            node.SyntaxTree,
                            rewrittenReceiver,
                            method,
                            rewrittenArguments,
                            ReadOnlyArray<string>.Null,
                            argumentRefKinds,
                            false,
                            ReadOnlyArray<int>.Null,
                            node.Type),
                    node.Type
                );
            }
        }

        public override BoundNode VisitDelegateCall(BoundDelegateCall node)
        {
            return this.VisitCall(node);
        }

        public override BoundNode VisitBadExpression(BoundBadExpression node)
        {
            // Cannot recurse into BadExpression children since the BadExpression
            // may represent being unable to use the child as an lvalue or rvalue.
            return node;
        }

        /// <summary>
        /// Rewrites arguments of an invocation according to the receiving method. It is assumed
        /// that arguments match parameters, but may need to be expanded/reordered.
        /// </summary>
        private void RewriteArguments(
                MethodSymbol method,
                bool expanded,
                ReadOnlyArray<int> argsToParamsOpt,
                ref ReadOnlyArray<RefKind> argumentRefKinds,
                ref ReadOnlyArray<BoundExpression> rewrittenArguments,
                out ReadOnlyArray<LocalSymbol> temporaries)
        {

            // We have:
            // * a list of arguments, already converted to their proper types, 
            //   in source code order. Some optional arguments might be missing.
            // * a map showing which parameter each argument corresponds to. If
            //   this is null, then the argument to parameter mapping is one-to-one.
            // * the ref kind of each argument, in source code order. That is, whether
            //   the argument was marked as ref, out, or value (neither).
            // * a method symbol.
            // * whether the call is expanded or normal form.

            // We rewrite the call so that:
            // * if in its expanded form, we create the params array.
            // * if the call requires reordering of arguments because of named arguments, temporaries are generated as needed

            // Doing this transformation can move around refness in interesting ways. For example, consider
            //
            // A().M(y : ref B()[C()], x : out D());
            //
            // This will be created as a call with receiver A(), symbol M, argument list ( B()[C()], D() ),
            // name list ( y, x ) and ref list ( ref, out ).  We can rewrite this into temporaries:
            //
            // A().M( 
            //    seq ( ref int temp_y = ref B()[C()], out D() ),
            //    temp_y );
            // 
            // Now we have a call with receiver A(), symbol M, argument list as shown, no name list,
            // and ref list ( out, value ). We do not want to pass a *ref* to temp_y; the temporary
            // storage is not the thing being ref'd! We want to pass the *value* of temp_y, which
            // *contains* a reference.

            // We attempt to minimize the number of temporaries required. Arguments which neither
            // produce nor observe a side effect can be placed into their proper position without
            // recourse to a temporary. For example:
            //
            // Where(predicate: x=>x.Length!=0, sequence: S())
            //
            // can be rewritten without any temporaries because the conversion from lambda to
            // delegate does not produce any side effect that could be observed by S().
            //
            // By contrast:
            //
            // Foo(z: this.p, y: this.Q(), x: (object)10)
            //
            // The boxing of 10 can be reordered, but the fetch of this.p has to happen before the
            // call to this.Q() because the call could change the value of this.p. 
            //
            // We start by binding everything that is not obviously reorderable as a temporary, and
            // then run an optimizer to remove unnecessary temporaries.

            ReadOnlyArray<ParameterSymbol> parameters = method.Parameters;
            var parameterCount = parameters.Count;
            var arguments = new BoundExpression[parameterCount];
            temporaries = ReadOnlyArray<LocalSymbol>.Null;  // not using temps by default.

            List<RefKind> refKinds = null;
            if (argumentRefKinds.IsNotNull)
            {
                refKinds = new List<RefKind>(parameterCount);
                for (int p = 0; p < parameterCount; ++p)
                {
                    refKinds.Add(RefKind.None);
                }
            }

            ArrayBuilder<BoundAssignmentOperator> storesToTemps = null;
            ArrayBuilder<BoundExpression> paramArray = null;

            if (expanded)
            {
                paramArray = ArrayBuilder<BoundExpression>.GetInstance();
            }

            for (int a = 0; a < rewrittenArguments.Count; ++a)
            {
                var argument = rewrittenArguments[a];
                var p = (argsToParamsOpt.IsNotNull) ? argsToParamsOpt[a] : a;
                var refKind = argumentRefKinds.RefKinds(a);
                Debug.Assert(arguments[p] == null);
                if (expanded && p == parameterCount - 1)
                {
                    paramArray.Add(argument);
                    Debug.Assert(refKind == RefKind.None);
                }
                else if (IsSafeForReordering(argument, refKind))
                {
                    arguments[p] = argument;
                    if (refKinds != null)
                    {
                        refKinds[p] = refKind;
                    }
                }
                else
                {
                    if (storesToTemps == null)
                    {
                        storesToTemps = ArrayBuilder<BoundAssignmentOperator>.GetInstance(rewrittenArguments.Count);
                    }

                    var tempStore = TempHelpers.StoreToTemp(argument, refKind, containingSymbol);
                    storesToTemps.Add(tempStore.Item1);
                    arguments[p] = tempStore.Item2;
                }
            }

            if (expanded)
            {
                var paramArrayType = parameters[parameterCount - 1].Type;
                var arrayArgs = paramArray.ToReadOnlyAndFree();

                var int32Type = method.ContainingAssembly.GetPrimitiveType(Microsoft.Cci.PrimitiveTypeCode.Int32);

                arguments[parameterCount - 1] = new BoundArrayCreation(
                            null,
                            null,
                            ReadOnlyArray.Singleton<BoundExpression>(
                                new BoundLiteral(null, null, ConstantValue.Create(arrayArgs.Count), int32Type)),
                            new BoundArrayInitialization(null, null, arrayArgs),
                            paramArrayType);
            }

            for (int p = 0; p < parameterCount; ++p)
            {
                if (arguments[p] == null)
                {
                    Debug.Assert(parameters[p].IsOptional);

                    // UNDONE: Add optional arguments.
                }
            }

            if (storesToTemps != null)
            {
                int tempsNeeded = MergeArgumentsAndSideEffects(storesToTemps, arguments);

                if (tempsNeeded > 0)
                {
                    var temps = new LocalSymbol[tempsNeeded];
                    for (int i = 0, j = 0; i < storesToTemps.Count; i++)
                    {
                        var s = storesToTemps[i];
                        if (s != null)
                        {
                            temps[j++] = ((BoundLocal)s.Left).LocalSymbol;
                        }
                    }

                    temporaries = temps.AsReadOnlyWrap();
                }

                storesToTemps.Free();
            }

            // * The rewritten list of names is now null because the arguments have been reordered.
            // * The args-to-params map is now null because every argument exactly matches its parameter.
            // * The call is no longer in its expanded form.

            argumentRefKinds = refKinds == null ? ReadOnlyArray<RefKind>.Null : refKinds.AsReadOnly<RefKind>();
            rewrittenArguments = arguments.AsReadOnlyWrap();
        }

        /// <summary>
        /// Process tempStores and add them as sideeffects to arguments where needed. The return
        /// value tells how many temps are actually needed. For unnecesary temps the corresponding
        /// temp store will be cleared.
        /// </summary>
        private static int MergeArgumentsAndSideEffects(
            ArrayBuilder<BoundAssignmentOperator> tempStores,
            BoundExpression[] arguments)
        {
            Debug.Assert(tempStores != null);
            Debug.Assert(arguments != null);

            int tempsRemainedInUse = tempStores.Count;

            // Suppose we've got temporaries: t0 = A(), t1 = B(), t2 = C(), t4 = D(), t5 = E()
            // and arguments: t0, t2, t1, t4, 10, t5
            // We wish to produce arguments list: A(), SEQ(t2=B(), C()), t2, D(), 10, E()
            //
            // Our algorithm essentially finds temp stores that must happen before given argument load,
            // and if there are any they become sideefects of the given load
            //
            // Constraints:
            //    Stores must happen before corresponding loads. Casuality. 
            //    Stores cannot move relative to other stores. If arg was movable it would not need a temp.

            // So for each argument:
            // t0: emit t0 = A(), t0.               ===>  A()
            // t2: emit t1 = B(), t2 = C(), t2.     ===>  SEQ{ B(), C(), t2 }
            // t1: emit t1;                         ===>  t1          //all the dependencies of t1 must be already emitted
            // t4: emit t4 = D(), t4.               ===>  D()
            // t5: emit t5 = E(), t5.               ===>  E()

            int firstUnclaimedStore = 0;

            for (int a = 0; a < arguments.Length; ++a)
            {
                var argument = arguments[a];

                // if argument is a load, search for corresponding store. if store is found, extract
                // the actual expression we were storing and add it as an argument - this one does
                // not need a temp. if there are any unclaimed stores before the found one, add them
                // as sideeffects that preceed this arg, they cannot happen later.
                if (argument.Kind == BoundKind.Local)
                {
                    var correspondingStore = -1;
                    for (int i = firstUnclaimedStore; i < tempStores.Count; i++)
                    {
                        if (tempStores[i].Left == argument)
                        {
                            correspondingStore = i;
                            break;
                        }
                    }

                    // store found?
                    if (correspondingStore != -1)
                    {
                        var value = tempStores[correspondingStore].Right;

                        // the matched store will not need to go into sideffects, only ones before it will
                        // remove the store to signal that we are not using its temp.
                        tempStores[correspondingStore] = null;
                        tempsRemainedInUse--;

                        // no need for sideeffects?
                        // just combine store and load
                        if (correspondingStore == firstUnclaimedStore)
                        {
                            arguments[a] = value;
                        }
                        else
                        {
                            var sideffects = new BoundExpression[correspondingStore - firstUnclaimedStore];
                            for (int s = 0; s < sideffects.Length; s++)
                            {
                                sideffects[s] = tempStores[firstUnclaimedStore + s];
                            }

                            arguments[a] = new BoundSequence(
                                        null,
                                        null,
                                // this sequence does not own locals. Note that temps that
                                // we use for the rewrite are stored in one arg and loaded
                                // in another so they must live in a scope above.
                                        ReadOnlyArray<LocalSymbol>.Empty,
                                        sideffects.AsReadOnlyWrap(),
                                        value,
                                        value.Type);
                        }

                        firstUnclaimedStore = correspondingStore + 1;
                    }
                }
            }

            Debug.Assert(firstUnclaimedStore == tempStores.Count, "not all sideeffects were claimed");
            return tempsRemainedInUse;
        }
    }
}
