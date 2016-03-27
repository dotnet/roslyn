// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitDynamicInvocation(BoundDynamicInvocation node)
        {
            return VisitDynamicInvocation(node, resultDiscarded: false);
        }

        public BoundExpression VisitDynamicInvocation(BoundDynamicInvocation node, bool resultDiscarded)
        {
            var loweredArguments = VisitList(node.Arguments);

            bool hasImplicitReceiver;
            BoundExpression loweredReceiver;
            ImmutableArray<TypeSymbol> typeArguments;
            string name;
            switch (node.Expression.Kind)
            {
                case BoundKind.MethodGroup:
                    // method invocation
                    BoundMethodGroup methodGroup = (BoundMethodGroup)node.Expression;
                    typeArguments = methodGroup.TypeArgumentsOpt;
                    name = methodGroup.Name;
                    hasImplicitReceiver = (methodGroup.Flags & BoundMethodGroupFlags.HasImplicitReceiver) != 0;

                    // Should have been eliminated during binding of dynamic invocation:
                    Debug.Assert(methodGroup.ReceiverOpt == null || methodGroup.ReceiverOpt.Kind != BoundKind.TypeOrValueExpression);

                    if (methodGroup.ReceiverOpt == null)
                    {
                        // Calling a static method defined on an outer class via its simple name.
                        NamedTypeSymbol firstContainer = node.ApplicableMethods.First().ContainingType;
                        Debug.Assert(node.ApplicableMethods.All(m => m.IsStatic && m.ContainingType == firstContainer));

                        loweredReceiver = new BoundTypeExpression(node.Syntax, null, firstContainer);
                    }
                    else if (hasImplicitReceiver && _factory.TopLevelMethod.IsStatic)
                    {
                        // Calling a static method defined on the current class via its simple name.
                        loweredReceiver = new BoundTypeExpression(node.Syntax, null, _factory.CurrentType);
                    }
                    else
                    {
                        loweredReceiver = VisitExpression(methodGroup.ReceiverOpt);
                    }

                    // If we are calling a method on a NoPIA type, we need to embed all methods/properties
                    // with the matching name of this dynamic invocation.
                    EmbedIfNeedTo(loweredReceiver, methodGroup.Methods, node.Syntax);

                    break;

                case BoundKind.DynamicMemberAccess:
                    // method invocation
                    var memberAccess = (BoundDynamicMemberAccess)node.Expression;
                    name = memberAccess.Name;
                    typeArguments = memberAccess.TypeArgumentsOpt;
                    loweredReceiver = VisitExpression(memberAccess.Receiver);
                    hasImplicitReceiver = false;
                    break;

                default:
                    // delegate invocation
                    var loweredExpression = VisitExpression(node.Expression);
                    return _dynamicFactory.MakeDynamicInvocation(loweredExpression, loweredArguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt, resultDiscarded).ToExpression();
            }

            Debug.Assert(loweredReceiver != null);
            return _dynamicFactory.MakeDynamicMemberInvocation(
                name,
                loweredReceiver,
                typeArguments,
                loweredArguments,
                node.ArgumentNamesOpt,
                node.ArgumentRefKindsOpt,
                hasImplicitReceiver,
                resultDiscarded).ToExpression();
        }

        private void EmbedIfNeedTo(BoundExpression receiver, ImmutableArray<MethodSymbol> methods, CSharpSyntaxNode syntaxNode)
        {
            // If we are calling a method on a NoPIA type, we need to embed all methods/properties
            // with the matching name of this dynamic invocation.
            var module = this.EmitModule;
            if (module != null && receiver != null && (object)receiver.Type != null)
            {
                var assembly = receiver.Type.ContainingAssembly;

                if ((object)assembly != null && assembly.IsLinked)
                {
                    foreach (var m in methods)
                    {
                        module.EmbeddedTypesManagerOpt.EmbedMethodIfNeedTo(m.OriginalDefinition, syntaxNode, _diagnostics);
                    }
                }
            }
        }

        private void EmbedIfNeedTo(BoundExpression receiver, ImmutableArray<PropertySymbol> properties, CSharpSyntaxNode syntaxNode)
        {
            // If we are calling a method on a NoPIA type, we need to embed all methods/properties
            // with the matching name of this dynamic invocation.
            var module = this.EmitModule;
            if (module != null && receiver != null && (object)receiver.Type != null)
            {
                var assembly = receiver.Type.ContainingAssembly;

                if ((object)assembly != null && assembly.IsLinked)
                {
                    foreach (var p in properties)
                    {
                        module.EmbeddedTypesManagerOpt.EmbedPropertyIfNeedTo(p.OriginalDefinition, syntaxNode, _diagnostics);
                    }
                }
            }
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            Debug.Assert(node != null);

            // Rewrite the receiver
            BoundExpression rewrittenReceiver = VisitExpression(node.ReceiverOpt);

            // Rewrite the arguments.
            // NOTE: We may need additional argument rewriting such as generating a params array, re-ordering arguments based on argsToParamsOpt map, inserting arguments for optional parameters, etc.
            // NOTE: This is done later by MakeArguments, for now we just lower each argument.
            var rewrittenArguments = VisitList(node.Arguments);

            return MakeCall(
                syntax: node.Syntax,
                rewrittenReceiver: rewrittenReceiver,
                method: node.Method,
                rewrittenArguments: rewrittenArguments,
                argumentRefKindsOpt: node.ArgumentRefKindsOpt,
                expanded: node.Expanded,
                invokedAsExtensionMethod: node.InvokedAsExtensionMethod,
                argsToParamsOpt: node.ArgsToParamsOpt,
                resultKind: node.ResultKind,
                type: node.Type,
                nodeOpt: node);
        }

        private BoundExpression MakeCall(
            CSharpSyntaxNode syntax,
            BoundExpression rewrittenReceiver,
            MethodSymbol method,
            ImmutableArray<BoundExpression> rewrittenArguments,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            bool expanded,
            bool invokedAsExtensionMethod,
            ImmutableArray<int> argsToParamsOpt,
            LookupResultKind resultKind,
            TypeSymbol type,
            BoundCall nodeOpt = null)
        {
            // We have already lowered each argument, but we may need some additional rewriting for the arguments,
            // such as generating a params array, re-ordering arguments based on argsToParamsOpt map, inserting arguments for optional parameters, etc.
            ImmutableArray<LocalSymbol> temps;
            rewrittenArguments = MakeArguments(syntax, rewrittenArguments, method, method, expanded, argsToParamsOpt, ref argumentRefKindsOpt, out temps, invokedAsExtensionMethod);

            return MakeCall(nodeOpt, syntax, rewrittenReceiver, method, rewrittenArguments, argumentRefKindsOpt, invokedAsExtensionMethod, resultKind, type, temps);
        }

        private BoundExpression MakeCall(
            BoundCall node,
            CSharpSyntaxNode syntax,
            BoundExpression rewrittenReceiver,
            MethodSymbol method,
            ImmutableArray<BoundExpression> rewrittenArguments,
            ImmutableArray<RefKind> argumentRefKinds,
            bool invokedAsExtensionMethod,
            LookupResultKind resultKind,
            TypeSymbol type,
            ImmutableArray<LocalSymbol> temps = default(ImmutableArray<LocalSymbol>))
        {
            BoundExpression rewrittenBoundCall;

            if (method.IsStatic &&
                method.ContainingType.IsObjectType() &&
                !_inExpressionLambda &&
                (object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_Object__ReferenceEquals))
            {
                Debug.Assert(rewrittenArguments.Length == 2);

                // ECMA - 335
                // I.8.2.5.1 Identity
                //           ...
                //           Identity is implemented on System.Object via the ReferenceEquals method.
                rewrittenBoundCall = new BoundBinaryOperator(
                    syntax,
                    BinaryOperatorKind.ObjectEqual,
                    rewrittenArguments[0],
                    rewrittenArguments[1],
                    null,
                    null,
                    resultKind,
                    type);
            }
            else if (node == null)
            {
                rewrittenBoundCall = new BoundCall(
                    syntax,
                    rewrittenReceiver,
                    method,
                    rewrittenArguments,
                    default(ImmutableArray<string>),
                    argumentRefKinds,
                    isDelegateCall: false,
                    expanded: false,
                    invokedAsExtensionMethod: invokedAsExtensionMethod,
                    argsToParamsOpt: default(ImmutableArray<int>),
                    resultKind: resultKind,
                    type: type);
            }
            else
            {
                rewrittenBoundCall = node.Update(
                    rewrittenReceiver,
                    method,
                    rewrittenArguments,
                    default(ImmutableArray<string>),
                    argumentRefKinds,
                    node.IsDelegateCall,
                    false,
                    node.InvokedAsExtensionMethod,
                    default(ImmutableArray<int>),
                    node.ResultKind,
                    node.Type);
            }

            if (!temps.IsDefaultOrEmpty)
            {
                return new BoundSequence(
                    syntax,
                    locals: temps,
                    sideEffects: ImmutableArray<BoundExpression>.Empty,
                    value: rewrittenBoundCall,
                    type: type);
            }

            return rewrittenBoundCall;
        }

        private BoundExpression MakeCall(CSharpSyntaxNode syntax, BoundExpression rewrittenReceiver, MethodSymbol method, ImmutableArray<BoundExpression> rewrittenArguments, TypeSymbol type)
        {
            return MakeCall(
                node: null,
                syntax: syntax,
                rewrittenReceiver: rewrittenReceiver,
                method: method,
                rewrittenArguments: rewrittenArguments,
                argumentRefKinds: ImmutableArray<RefKind>.Empty,
                invokedAsExtensionMethod: false,
                resultKind: LookupResultKind.Viable,
                type: type);
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
                                case ConversionKind.ImplicitDynamic:
                                case ConversionKind.ExplicitDynamic:
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
                                case ConversionKind.PointerToInteger:
                                case ConversionKind.PointerToPointer:
                                case ConversionKind.PointerToVoid:
                                case ConversionKind.NullToPointer:
                                case ConversionKind.IntegerToPointer:
                                    current = conv.Operand;
                                    break;
                                case ConversionKind.ExplicitUserDefined:
                                case ConversionKind.ImplicitUserDefined:
                                case ConversionKind.ImplicitThrow:
                                    return false;
                                default:
                                    // Unhandled conversion kind in reordering logic
                                    throw ExceptionUtilities.UnexpectedValue(conv.ConversionKind);
                            }
                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Rewrites arguments of an invocation according to the receiving method or indexer.
        /// It is assumed that each argument has already been lowered, but we may need
        /// additional rewriting for the arguments, such as generating a params array, re-ordering
        /// arguments based on <paramref name="argsToParamsOpt"/> map, inserting arguments for optional parameters, etc.
        /// <paramref name="optionalParametersMethod"/> is the method used for values of any optional parameters.
        /// For indexers, this method must be an accessor, and for methods it must be the method
        /// itself. <paramref name="optionalParametersMethod"/> is needed for indexers since getter and setter
        /// may have distinct optional parameter values.
        /// </summary>
        private ImmutableArray<BoundExpression> MakeArguments(
            CSharpSyntaxNode syntax,
            ImmutableArray<BoundExpression> rewrittenArguments,
            Symbol methodOrIndexer,
            MethodSymbol optionalParametersMethod,
            bool expanded,
            ImmutableArray<int> argsToParamsOpt,
            ref ImmutableArray<RefKind> argumentRefKindsOpt,
            out ImmutableArray<LocalSymbol> temps,
            bool invokedAsExtensionMethod = false,
            ThreeState enableCallerInfo = ThreeState.Unknown)
        {
            // Either the methodOrIndexer is a property, in which case the method used
            // for optional parameters is an accessor of that property (or an overridden
            // property), or the methodOrIndexer is used for optional parameters directly.
            Debug.Assert(((methodOrIndexer.Kind == SymbolKind.Property) && optionalParametersMethod.IsAccessor()) ||
                ReferenceEquals(methodOrIndexer, optionalParametersMethod));

            // We need to do a fancy rewrite under the following circumstances:
            // (1) a params array is being used; we need to generate the array.
            // (2) there were named arguments that reordered the arguments; we might
            //     have to generate temporaries to ensure that the arguments are 
            //     evaluated in source code order, not the actual call order.
            // (3) there were optional parameters that had no corresponding arguments.
            //
            // If none of those are the case then we can just take an early out.

            // An applicable "vararg" method could not possibly be applicable in its expanded
            // form, and cannot possibly have named arguments or used optional parameters, 
            // because the __arglist() argument has to be positional and in the last position. 


            if (methodOrIndexer.GetIsVararg())
            {
                Debug.Assert(rewrittenArguments.Length == methodOrIndexer.GetParameterCount() + 1);
                Debug.Assert(argsToParamsOpt.IsDefault);
                Debug.Assert(!expanded);
                temps = default(ImmutableArray<LocalSymbol>);
                return rewrittenArguments;
            }

            var receiverNamedType = invokedAsExtensionMethod ?
                                    ((MethodSymbol)methodOrIndexer).Parameters[0].Type.TypeSymbol as NamedTypeSymbol :
                                    methodOrIndexer.ContainingType;

            bool isComReceiver = (object)receiverNamedType != null && receiverNamedType.IsComImport;

            if (rewrittenArguments.Length == methodOrIndexer.GetParameterCount() &&
                argsToParamsOpt.IsDefault &&
                !expanded &&
                !isComReceiver)
            {
                temps = default(ImmutableArray<LocalSymbol>);
                return rewrittenArguments;
            }


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

            ImmutableArray<ParameterSymbol> parameters = methodOrIndexer.GetParameters();
            BoundExpression[] actualArguments = new BoundExpression[parameters.Length]; // The actual arguments that will be passed; one actual argument per formal parameter.
            ArrayBuilder<BoundAssignmentOperator> storesToTemps = ArrayBuilder<BoundAssignmentOperator>.GetInstance(rewrittenArguments.Length);
            ArrayBuilder<RefKind> refKinds = ArrayBuilder<RefKind>.GetInstance(parameters.Length, RefKind.None);

            // Step one: Store everything that is non-trivial into a temporary; record the
            // stores in storesToTemps and make the actual argument a reference to the temp.
            // Do not yet attempt to deal with params arrays or optional arguments.
            BuildStoresToTemps(expanded, argsToParamsOpt, argumentRefKindsOpt, rewrittenArguments, actualArguments, refKinds, storesToTemps);


            // all the formal arguments, except missing optionals, are now in place. 
            // Optimize away unnecessary temporaries.
            // Necessary temporaries have their store instructions merged into the appropriate 
            // argument expression.
            ArrayBuilder<LocalSymbol> temporariesBuilder = ArrayBuilder<LocalSymbol>.GetInstance();
            OptimizeTemporaries(actualArguments, refKinds, storesToTemps, temporariesBuilder);

            // Step two: If we have a params array, build the array and fill in the argument.
            if (expanded)
            {
                actualArguments[actualArguments.Length - 1] = BuildParamsArray(syntax, methodOrIndexer, argsToParamsOpt, rewrittenArguments, parameters, actualArguments[actualArguments.Length - 1]);
            }

            // Step three: Now fill in the optional arguments.
            InsertMissingOptionalArguments(syntax, optionalParametersMethod.Parameters, actualArguments, enableCallerInfo);

            if (isComReceiver)
            {
                RewriteArgumentsForComCall(parameters, actualArguments, refKinds, temporariesBuilder);
            }

            temps = temporariesBuilder.ToImmutableAndFree();
            storesToTemps.Free();

            // * The refkind map is now filled out to match the arguments.
            // * The list of parameter names is now null because the arguments have been reordered.
            // * The args-to-params map is now null because every argument exactly matches its parameter.
            // * The call is no longer in its expanded form.

            argumentRefKindsOpt = GetRefKindsOrNull(refKinds);
            refKinds.Free();

            return actualArguments.AsImmutableOrNull();
        }

        private static ImmutableArray<RefKind> GetRefKindsOrNull(ArrayBuilder<RefKind> refKinds)
        {
            foreach (var refKind in refKinds)
            {
                if (refKind != RefKind.None)
                {
                    return refKinds.ToImmutable();
                }
            }
            return default(ImmutableArray<RefKind>);
        }

        // This fills in the arguments, refKinds and storesToTemps arrays.
        private void BuildStoresToTemps(
            bool expanded,
            ImmutableArray<int> argsToParamsOpt,
            ImmutableArray<RefKind> argumentRefKinds,
            ImmutableArray<BoundExpression> rewrittenArguments,
            /* out */ BoundExpression[] arguments,
            /* out */ ArrayBuilder<RefKind> refKinds,
            /* out */ ArrayBuilder<BoundAssignmentOperator> storesToTemps)
        {
            Debug.Assert(refKinds.Count == arguments.Length);
            Debug.Assert(storesToTemps.Count == 0);

            for (int a = 0; a < rewrittenArguments.Length; ++a)
            {
                BoundExpression argument = rewrittenArguments[a];
                int p = (!argsToParamsOpt.IsDefault) ? argsToParamsOpt[a] : a;
                RefKind refKind = argumentRefKinds.RefKinds(a);
                Debug.Assert(arguments[p] == null);

                if (expanded && p == arguments.Length - 1)
                {
                    // Unfortunately, we violate the specification and allow:
                    // M(int q, params int[] x) ... M(x : X(), q : Q());
                    // which means that we cannot bail out just because
                    // an argument of an expanded-form call corresponds to
                    // the parameter array. We need to make sure that the
                    // side effects of X() and Q() continue to happen in the right
                    // order here.
                    //
                    // Fortunately, we do disallow M(x : 123, x : 345, x : 456).
                    // 
                    // Here's what we'll do. If all the remaining arguments
                    // correspond to elements in the parameter array then 
                    // we can bail out here without creating any temporaries.
                    // The next step in the call rewriter will deal with gathering
                    // up the elements. 
                    //
                    // However, if there are other elements after this one
                    // that do not correspond to elements in the parameter array
                    // then we need to create a temporary as usual. The step that
                    // produces the parameter array will need to deal with that
                    // eventuality.

                    bool canBail = true;
                    for (int remainingArgument = a + 1; remainingArgument < rewrittenArguments.Length; ++remainingArgument)
                    {
                        int remainingParameter = (!argsToParamsOpt.IsDefault) ? argsToParamsOpt[remainingArgument] : remainingArgument;
                        if (remainingParameter != arguments.Length - 1)
                        {
                            canBail = false;
                            break;
                        }
                    }
                    if (canBail)
                    {
                        return;
                    }
                }

                if (IsSafeForReordering(argument, refKind))
                {
                    arguments[p] = argument;
                    refKinds[p] = refKind;
                }
                else
                {
                    BoundAssignmentOperator assignment;
                    var temp = _factory.StoreToTemp(argument, out assignment, refKind: refKind);
                    storesToTemps.Add(assignment);
                    arguments[p] = temp;
                }
            }
        }

        private BoundExpression BuildParamsArray(
            CSharpSyntaxNode syntax,
            Symbol methodOrIndexer,
            ImmutableArray<int> argsToParamsOpt,
            ImmutableArray<BoundExpression> rewrittenArguments,
            ImmutableArray<ParameterSymbol> parameters,
            BoundExpression tempStoreArgument)
        {
            ArrayBuilder<BoundExpression> paramArray = ArrayBuilder<BoundExpression>.GetInstance();
            int paramsParam = parameters.Length - 1;

            if (tempStoreArgument != null)
            {
                paramArray.Add(tempStoreArgument);
                // Special case: see comment in BuildStoresToTemps above; if there 
                // is an argument already in the slot then it is the only element in 
                // the params array. 
            }
            else
            {
                for (int a = 0; a < rewrittenArguments.Length; ++a)
                {
                    BoundExpression argument = rewrittenArguments[a];
                    int p = (!argsToParamsOpt.IsDefault) ? argsToParamsOpt[a] : a;
                    if (p == paramsParam)
                    {
                        paramArray.Add(argument);
                    }
                }
            }

            var paramArrayType = parameters[paramsParam].Type.TypeSymbol;
            var arrayArgs = paramArray.ToImmutableAndFree();

            // If this is a zero-length array, rather than using "new T[0]", optimize with "Array.Empty<T>()" 
            // if it's available.  However, we also disable the optimization if we're in an expression lambda, the 
            // point of which is just to represent the semantics of an operation, and we don't know that all consumers
            // of expression lambdas will appropriately understand Array.Empty<T>().
            if (arrayArgs.Length == 0 && !_inExpressionLambda)
            {
                ArrayTypeSymbol ats = paramArrayType as ArrayTypeSymbol;
                if (ats != null) // could be null if there's a semantic error, e.g. the params parameter type isn't an array
                {
                    MethodSymbol arrayEmpty = _compilation.GetWellKnownTypeMember(WellKnownMember.System_Array__Empty) as MethodSymbol;
                    if (arrayEmpty != null) // will be null if Array.Empty<T> doesn't exist in reference assemblies
                    {
                        // return an invocation of "Array.Empty<T>()"
                        arrayEmpty = arrayEmpty.Construct(ImmutableArray.Create(ats.ElementType.TypeSymbol));
                        return new BoundCall(
                            syntax,
                            null,
                            arrayEmpty,
                            ImmutableArray<BoundExpression>.Empty,
                            default(ImmutableArray<string>),
                            default(ImmutableArray<RefKind>),
                            isDelegateCall: false,
                            expanded: false,
                            invokedAsExtensionMethod: false,
                            argsToParamsOpt: default(ImmutableArray<int>),
                            resultKind: LookupResultKind.Viable,
                            type: arrayEmpty.ReturnType.TypeSymbol);
                    }
                }
            }

            var int32Type = methodOrIndexer.ContainingAssembly.GetPrimitiveType(Microsoft.Cci.PrimitiveTypeCode.Int32);

            return new BoundArrayCreation(
                syntax,
                ImmutableArray.Create(
                    MakeLiteral(syntax, ConstantValue.Create(arrayArgs.Length), int32Type)),
                new BoundArrayInitialization(syntax, arrayArgs),
                paramArrayType);
        }

        private static void OptimizeTemporaries(
            BoundExpression[] arguments,
            ArrayBuilder<RefKind> refKinds,
            ArrayBuilder<BoundAssignmentOperator> storesToTemps,
            ArrayBuilder<LocalSymbol> temporariesBuilder)
        {
            Debug.Assert(arguments != null);
            Debug.Assert(refKinds != null);
            Debug.Assert(arguments.Length == refKinds.Count);
            Debug.Assert(storesToTemps != null);
            Debug.Assert(temporariesBuilder != null);

            if (storesToTemps.Count > 0)
            {
                int tempsNeeded = MergeArgumentsAndSideEffects(arguments, refKinds, storesToTemps);
                if (tempsNeeded > 0)
                {
                    foreach (BoundAssignmentOperator s in storesToTemps)
                    {
                        if (s != null)
                        {
                            temporariesBuilder.Add(((BoundLocal)s.Left).LocalSymbol);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Process tempStores and add them as side-effects to arguments where needed. The return
        /// value tells how many temps are actually needed. For unnecessary temps the corresponding
        /// temp store will be cleared.
        /// </summary>
        private static int MergeArgumentsAndSideEffects(
            BoundExpression[] arguments,
            ArrayBuilder<RefKind> refKinds,
            ArrayBuilder<BoundAssignmentOperator> tempStores)
        {
            Debug.Assert(arguments != null);
            Debug.Assert(refKinds != null);
            Debug.Assert(tempStores != null);

            int tempsRemainedInUse = tempStores.Count;

            // Suppose we've got temporaries: t0 = A(), t1 = B(), t2 = C(), t4 = D(), t5 = E()
            // and arguments: t0, t2, t1, t4, 10, t5
            //
            // We wish to produce arguments list: A(), SEQ(t1=B(), C()), t1, D(), 10, E()
            //
            // Our algorithm essentially finds temp stores that must happen before given argument
            // load, and if there are any they become side effects of the given load.
            // Stores immediately followed by loads of the same thing can be eliminated.
            //
            // Constraints:
            //    Stores must happen before corresponding loads.
            //    Stores cannot move relative to other stores. If arg was movable it would not need a temp.

            int firstUnclaimedStore = 0;

            for (int a = 0; a < arguments.Length; ++a)
            {
                var argument = arguments[a];

                // if argument is a load, search for corresponding store. if store is found, extract
                // the actual expression we were storing and add it as an argument - this one does
                // not need a temp. if there are any unclaimed stores before the found one, add them
                // as side effects that precede this arg, they cannot happen later.
                // NOTE: missing optional parameters are not filled yet and therefore nulls - no need to do anything for them
                if (argument?.Kind == BoundKind.Local)
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

                        // When we created the temp, we dropped the argument RefKind
                        // since the local contained its own RefKind. Since we're removing
                        // the temp, the argument RefKind needs to be restored.
                        refKinds[a] = ((BoundLocal)argument).LocalSymbol.RefKind;

                        // the matched store will not need to go into side-effects, only ones before it will
                        // remove the store to signal that we are not using its temp.
                        tempStores[correspondingStore] = null;
                        tempsRemainedInUse--;

                        // no need for side-effects?
                        // just combine store and load
                        if (correspondingStore == firstUnclaimedStore)
                        {
                            arguments[a] = value;
                        }
                        else
                        {
                            var sideeffects = new BoundExpression[correspondingStore - firstUnclaimedStore];
                            for (int s = 0; s < sideeffects.Length; s++)
                            {
                                sideeffects[s] = tempStores[firstUnclaimedStore + s];
                            }

                            arguments[a] = new BoundSequence(
                                        value.Syntax,
                                        // this sequence does not own locals. Note that temps that
                                        // we use for the rewrite are stored in one arg and loaded
                                        // in another so they must live in a scope above.
                                        ImmutableArray<LocalSymbol>.Empty,
                                        sideeffects.AsImmutableOrNull(),
                                        value,
                                        value.Type);
                        }

                        firstUnclaimedStore = correspondingStore + 1;
                    }
                }
            }

            Debug.Assert(firstUnclaimedStore == tempStores.Count, "not all side-effects were claimed");
            return tempsRemainedInUse;
        }

        private void InsertMissingOptionalArguments(CSharpSyntaxNode syntax,
            ImmutableArray<ParameterSymbol> parameters,
            BoundExpression[] arguments,
            ThreeState enableCallerInfo = ThreeState.Unknown)
        {
            for (int p = 0; p < arguments.Length; ++p)
            {
                if (arguments[p] == null)
                {
                    ParameterSymbol parameter = parameters[p];
                    Debug.Assert(parameter.IsOptional);
                    arguments[p] = GetDefaultParameterValue(syntax, parameter, enableCallerInfo);
                    Debug.Assert(arguments[p].Type == parameter.Type.TypeSymbol);
                }
            }
        }

        private static SourceLocation GetCallerLocation(CSharpSyntaxNode syntax, ThreeState enableCallerInfo)
        {
            switch (enableCallerInfo)
            {
                case ThreeState.False:
                    return null;
                case ThreeState.True:
                    return new SourceLocation(syntax.GetFirstToken());
            }

            Debug.Assert(enableCallerInfo == ThreeState.Unknown);

            switch (syntax.Kind())
            {
                case SyntaxKind.InvocationExpression:
                    return new SourceLocation(((InvocationExpressionSyntax)syntax).ArgumentList.OpenParenToken);
                case SyntaxKind.ObjectCreationExpression:
                    return new SourceLocation(((ObjectCreationExpressionSyntax)syntax).NewKeyword);
                case SyntaxKind.BaseConstructorInitializer:
                case SyntaxKind.ThisConstructorInitializer:
                    return new SourceLocation(((ConstructorInitializerSyntax)syntax).ArgumentList.OpenParenToken);
                case SyntaxKind.ElementAccessExpression:
                    return new SourceLocation(((ElementAccessExpressionSyntax)syntax).ArgumentList.OpenBracketToken);
                case SyntaxKind.FromClause:
                case SyntaxKind.GroupClause:
                case SyntaxKind.JoinClause:
                case SyntaxKind.JoinIntoClause:
                case SyntaxKind.LetClause:
                case SyntaxKind.OrderByClause:
                case SyntaxKind.SelectClause:
                case SyntaxKind.WhereClause:
                    return new SourceLocation(syntax.GetFirstToken());
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the default value for the <paramref name="parameter"/>.
        /// </summary>
        /// <param name="syntax">
        /// A syntax node corresponding to the invocation.
        /// </param>
        /// <param name="parameter">
        /// A parameter to get the default value for.
        /// </param>
        /// <param name="enableCallerInfo">
        /// Indicates if caller info is to be enabled when processing this optional parameter.
        /// The value <see cref="ThreeState.Unknown"/> means the decision is to be made based on the shape of the <paramref name="syntax"/> node.
        /// </param>
        /// <remarks>
        /// DELIBERATE SPEC VIOLATION: When processing an implicit invocation of an <c>Add</c> method generated
        /// for an element-initializer in a collection-initializer, the parameter <paramref name="enableCallerInfo"/> 
        /// is set to <see cref="ThreeState.True"/>. It means that if the optional parameter is annotated with <see cref="CallerLineNumberAttribute"/>,
        /// <see cref="CallerFilePathAttribute"/> or <see cref="CallerMemberNameAttribute"/>, and there is no explicit argument corresponding to it,
        /// we will provide caller information as a value of this parameter.
        /// This is done to match the native compiler behavior and user requests (see http://roslyn.codeplex.com/workitem/171). This behavior
        /// does not match the C# spec that currently requires to provide caller information only in explicit invocations and query expressions.
        /// </remarks>
        private BoundExpression GetDefaultParameterValue(CSharpSyntaxNode syntax, ParameterSymbol parameter, ThreeState enableCallerInfo)
        {
            // TODO: Ideally, the enableCallerInfo parameter would be of just bool type with only 'true' and 'false' values, and all callers
            // explicitly provided one of those values, so that we do not rely on shape of syntax nodes in the rewriter. There are not many immediate callers, 
            // but often the immediate caller does not have the required information, so all possible call chains should be analyzed and possibly updated
            // to pass this information, and this might be a big task. We should consider doing this when the time permits.

            TypeSymbol parameterType = parameter.Type.TypeSymbol;
            Debug.Assert(parameter.IsOptional);
            ConstantValue defaultConstantValue = parameter.ExplicitDefaultConstantValue;
            BoundExpression defaultValue;
            SourceLocation callerSourceLocation;

            // For compatibility with the native compiler we treat all bad imported constant
            // values as default(T).  
            if (defaultConstantValue != null && defaultConstantValue.IsBad)
            {
                defaultConstantValue = ConstantValue.Null;
            }

            if (parameter.IsCallerLineNumber && ((callerSourceLocation = GetCallerLocation(syntax, enableCallerInfo)) != null))
            {
                int line = callerSourceLocation.SourceTree.GetDisplayLineNumber(callerSourceLocation.SourceSpan);
                BoundExpression lineLiteral = MakeLiteral(syntax, ConstantValue.Create(line), _compilation.GetSpecialType(SpecialType.System_Int32));

                if (parameterType.IsNullableType())
                {
                    defaultValue = MakeConversion(lineLiteral, parameterType.GetNullableUnderlyingType(), false);

                    // wrap it in a nullable ctor.
                    defaultValue = new BoundObjectCreationExpression(
                        syntax,
                        GetNullableMethod(syntax, parameterType, SpecialMember.System_Nullable_T__ctor),
                        defaultValue);
                }
                else
                {
                    defaultValue = MakeConversion(lineLiteral, parameterType, false);
                }
            }
            else if (parameter.IsCallerFilePath && ((callerSourceLocation = GetCallerLocation(syntax, enableCallerInfo)) != null))
            {
                string path = callerSourceLocation.SourceTree.GetDisplayPath(callerSourceLocation.SourceSpan, _compilation.Options.SourceReferenceResolver);
                BoundExpression memberNameLiteral = MakeLiteral(syntax, ConstantValue.Create(path), _compilation.GetSpecialType(SpecialType.System_String));
                defaultValue = MakeConversion(memberNameLiteral, parameterType, false);
            }
            else if (parameter.IsCallerMemberName && ((callerSourceLocation = GetCallerLocation(syntax, enableCallerInfo)) != null))
            {
                string memberName;

                switch (_factory.TopLevelMethod.MethodKind)
                {
                    case MethodKind.Constructor:
                    case MethodKind.StaticConstructor:
                        // See if the code is actually part of a field, field-like event or property initializer and return the name of the corresponding member.
                        var memberDecl = syntax.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault();

                        if (memberDecl != null)
                        {
                            BaseFieldDeclarationSyntax fieldDecl;

                            if (memberDecl.Kind() == SyntaxKind.PropertyDeclaration)
                            {
                                var propDecl = (PropertyDeclarationSyntax)memberDecl;
                                EqualsValueClauseSyntax initializer = propDecl.Initializer;

                                if (initializer != null && initializer.Span.Contains(syntax.Span))
                                {
                                    memberName = propDecl.Identifier.ValueText;
                                    break;
                                }
                            }
                            else if ((fieldDecl = memberDecl as BaseFieldDeclarationSyntax) != null)
                            {
                                memberName = null;

                                foreach (VariableDeclaratorSyntax varDecl in fieldDecl.Declaration.Variables)
                                {
                                    EqualsValueClauseSyntax initializer = varDecl.Initializer;

                                    if (initializer != null && initializer.Span.Contains(syntax.Span))
                                    {
                                        memberName = varDecl.Identifier.ValueText;
                                        break;
                                    }
                                }

                                if (memberName != null)
                                {
                                    break;
                                }
                            }
                        }

                        goto default;

                    default:
                        memberName = _factory.TopLevelMethod.GetMemberCallerName();
                        break;
                }

                BoundExpression memberNameLiteral = MakeLiteral(syntax, ConstantValue.Create(memberName), _compilation.GetSpecialType(SpecialType.System_String));
                defaultValue = MakeConversion(memberNameLiteral, parameterType, false);
            }
            else if (defaultConstantValue == ConstantValue.NotAvailable)
            {
                // There is no constant value given for the parameter in source/metadata.
                if (parameterType.IsDynamic() || parameterType.SpecialType == SpecialType.System_Object)
                {
                    // We have something like M([Optional] object x). We have special handling for such situations.
                    defaultValue = GetDefaultParameterSpecial(syntax, parameter);
                }
                else
                {
                    // The argument to M([Optional] int x) becomes default(int)
                    defaultValue = new BoundDefaultOperator(syntax, parameterType);
                }
            }
            else if (defaultConstantValue.IsNull && parameterType.IsValueType)
            {
                // We have something like M(int? x = null) or M(S x = default(S)),
                // so replace the argument with default(int?).
                defaultValue = new BoundDefaultOperator(syntax, parameterType);
            }
            else if (parameterType.IsNullableType())
            {
                // We have something like M(double? x = 1.23), so replace the argument
                // with new double?(1.23).

                TypeSymbol constantType = _compilation.GetSpecialType(defaultConstantValue.SpecialType);
                defaultValue = MakeLiteral(syntax, defaultConstantValue, constantType);

                // The parameter's underlying type might not match the constant type. For example, we might have
                // a default value of 5 (an integer) but a parameter type of decimal?.

                defaultValue = MakeConversion(defaultValue, parameterType.GetNullableUnderlyingType(), @checked: false, acceptFailingConversion: true);

                // Finally, wrap it in a nullable ctor.
                defaultValue = new BoundObjectCreationExpression(
                    syntax,
                    GetNullableMethod(syntax, parameterType, SpecialMember.System_Nullable_T__ctor),
                    defaultValue);
            }
            else if (defaultConstantValue.IsNull || defaultConstantValue.IsBad)
            {
                defaultValue = MakeLiteral(syntax, defaultConstantValue, parameterType);
            }
            else
            {
                // We have something like M(double x = 1.23), so replace the argument with 1.23.

                TypeSymbol constantType = _compilation.GetSpecialType(defaultConstantValue.SpecialType);
                defaultValue = MakeLiteral(syntax, defaultConstantValue, constantType);
                // The parameter type might not match the constant type.
                defaultValue = MakeConversion(defaultValue, parameterType, @checked: false, acceptFailingConversion: true);
            }

            return defaultValue;
        }

        private BoundExpression GetDefaultParameterSpecial(CSharpSyntaxNode syntax, ParameterSymbol parameter)
        {
            // We have a call to a method M([Optional] object x) which omits the argument. The value we generate
            // for the argument depends on the presence or absence of other attributes. The rules are:
            //
            // * If the parameter is marked as [MarshalAs(Interface)], [MarshalAs(IUnknown)] or [MarshalAs(IDispatch)]
            //   then the argument is null.
            // * Otherwise, if the parameter is marked as [IUnknownConstant] then the argument is
            //   new UnknownWrapper(null)
            // * Otherwise, if the parameter is marked as [IDispatchConstant] then the argument is
            //    new DispatchWrapper(null)
            // * Otherwise, the argument is Type.Missing.

            BoundExpression defaultValue;

            if (parameter.IsMarshalAsObject)
            {
                // default(object)
                defaultValue = new BoundDefaultOperator(syntax, parameter.Type.TypeSymbol);
            }
            else if (parameter.IsIUnknownConstant)
            {
                // new UnknownWrapper(default(object))
                var methodSymbol = (MethodSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_InteropServices_UnknownWrapper__ctor);
                var argument = new BoundDefaultOperator(syntax, parameter.Type.TypeSymbol);
                defaultValue = new BoundObjectCreationExpression(syntax, methodSymbol, argument);
            }
            else if (parameter.IsIDispatchConstant)
            {
                // new DispatchWrapper(default(object))
                var methodSymbol = (MethodSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_InteropServices_DispatchWrapper__ctor);
                var argument = new BoundDefaultOperator(syntax, parameter.Type.TypeSymbol);
                defaultValue = new BoundObjectCreationExpression(syntax, methodSymbol, argument);
            }
            else
            {
                // Type.Missing
                var fieldSymbol = (FieldSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Type__Missing);
                defaultValue = new BoundFieldAccess(syntax, null, fieldSymbol, ConstantValue.NotAvailable);
            }

            defaultValue = MakeConversion(defaultValue, parameter.Type.TypeSymbol, @checked: false);

            return defaultValue;
        }

        // Omit ref feature for COM interop: We can pass arguments by value for ref parameters if we are calling a method/property on an instance of a COM imported type.
        // We should have ignored the 'ref' on the parameter during overload resolution for the given method call.
        // If we had any ref omitted argument for the given call, we create a temporary local and
        // replace the argument with the following BoundSequence: { side-effects: { temp = argument }, value = { ref temp } }
        // NOTE: The temporary local must be scoped to live across the entire BoundCall node,
        // otherwise the codegen optimizer might re-use the same temporary for multiple ref-omitted arguments for this call.
        private void RewriteArgumentsForComCall(
            ImmutableArray<ParameterSymbol> parameters,
            BoundExpression[] actualArguments, //already re-ordered to match parameters
            ArrayBuilder<RefKind> argsRefKindsBuilder,
            ArrayBuilder<LocalSymbol> temporariesBuilder)
        {
            Debug.Assert(actualArguments != null);
            Debug.Assert(actualArguments.Length == parameters.Length);

            Debug.Assert(argsRefKindsBuilder == null || argsRefKindsBuilder.Count == parameters.Length);

            var argsCount = actualArguments.Length;

            for (int argIndex = 0; argIndex < argsCount; ++argIndex)
            {
                RefKind paramRefKind = parameters[argIndex].RefKind;
                RefKind argRefKind = argsRefKindsBuilder[argIndex];

                // Rewrite only if the argument was passed with no ref/out and the
                // parameter was declared ref. 
                if (argRefKind != RefKind.None || paramRefKind != RefKind.Ref)
                {
                    continue;
                }

                var argument = actualArguments[argIndex];
                if (argument.Kind == BoundKind.Local)
                {
                    var localRefKind = ((BoundLocal)argument).LocalSymbol.RefKind;
                    if (localRefKind == RefKind.Ref)
                    {
                        // Already passing an address from the ref local.
                        continue;
                    }

                    Debug.Assert(localRefKind == RefKind.None);
                }

                BoundAssignmentOperator boundAssignmentToTemp;
                BoundLocal boundTemp = _factory.StoreToTemp(argument, out boundAssignmentToTemp);

                actualArguments[argIndex] = new BoundSequence(
                    argument.Syntax,
                    locals: ImmutableArray<LocalSymbol>.Empty,
                    sideEffects: ImmutableArray.Create<BoundExpression>(boundAssignmentToTemp),
                    value: boundTemp,
                    type: boundTemp.Type);
                argsRefKindsBuilder[argIndex] = RefKind.Ref;

                temporariesBuilder.Add(boundTemp.LocalSymbol);
            }
        }

        public override BoundNode VisitDynamicMemberAccess(BoundDynamicMemberAccess node)
        {
            // InvokeMember operation:
            if (node.Invoked)
            {
                return node;
            }

            // GetMember operation:
            Debug.Assert(node.TypeArgumentsOpt.IsDefault);
            var loweredReceiver = VisitExpression(node.Receiver);
            return _dynamicFactory.MakeDynamicGetMember(loweredReceiver, node.Name, node.Indexed).ToExpression();
        }
    }
}
