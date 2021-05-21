﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Operations;
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
            ImmutableArray<TypeWithAnnotations> typeArguments;
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
                        Debug.Assert(node.ApplicableMethods.All(m => !m.RequiresInstanceReceiver && TypeSymbol.Equals(m.ContainingType, firstContainer, TypeCompareKind.ConsiderEverything2)));

                        loweredReceiver = new BoundTypeExpression(node.Syntax, null, firstContainer);
                    }
                    else if (hasImplicitReceiver && _factory.TopLevelMethod is { RequiresInstanceReceiver: false })
                    {
                        // Calling a static method defined on the current class via its simple name.
                        Debug.Assert(_factory.CurrentType is { });
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

        private void EmbedIfNeedTo(BoundExpression receiver, ImmutableArray<MethodSymbol> methods, SyntaxNode syntaxNode)
        {
            // If we are calling a method on a NoPIA type, we need to embed all methods/properties
            // with the matching name of this dynamic invocation.
            var module = this.EmitModule;
            if (module != null && receiver != null && receiver.Type is { })
            {
                var assembly = receiver.Type.ContainingAssembly;

                if ((object)assembly != null && assembly.IsLinked)
                {
                    foreach (var m in methods)
                    {
                        module.EmbeddedTypesManagerOpt.EmbedMethodIfNeedTo(m.OriginalDefinition.GetCciAdapter(), syntaxNode, _diagnostics.DiagnosticBag);
                    }
                }
            }
        }

        private void EmbedIfNeedTo(BoundExpression receiver, ImmutableArray<PropertySymbol> properties, SyntaxNode syntaxNode)
        {
            // If we are calling a method on a NoPIA type, we need to embed all methods/properties
            // with the matching name of this dynamic invocation.
            var module = this.EmitModule;
            if (module != null && receiver is { Type: { } })
            {
                var assembly = receiver.Type.ContainingAssembly;

                if ((object)assembly != null && assembly.IsLinked)
                {
                    foreach (var p in properties)
                    {
                        module.EmbeddedTypesManagerOpt.EmbedPropertyIfNeedTo(p.OriginalDefinition.GetCciAdapter(), syntaxNode, _diagnostics.DiagnosticBag);
                    }
                }
            }
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            Debug.Assert(node != null);

            // Rewrite the receiver
            BoundExpression? rewrittenReceiver = VisitExpression(node.ReceiverOpt);

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
            SyntaxNode syntax,
            BoundExpression? rewrittenReceiver,
            MethodSymbol method,
            ImmutableArray<BoundExpression> rewrittenArguments,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            bool expanded,
            bool invokedAsExtensionMethod,
            ImmutableArray<int> argsToParamsOpt,
            LookupResultKind resultKind,
            TypeSymbol type,
            BoundCall? nodeOpt = null)
        {
            // We have already lowered each argument, but we may need some additional rewriting for the arguments,
            // such as generating a params array, re-ordering arguments based on argsToParamsOpt map, inserting arguments for optional parameters, etc.
            ImmutableArray<LocalSymbol> temps;
            rewrittenArguments = MakeArguments(
                syntax,
                rewrittenArguments,
                method,
                expanded,
                argsToParamsOpt,
                ref argumentRefKindsOpt,
                out temps,
                invokedAsExtensionMethod);

            return MakeCall(nodeOpt, syntax, rewrittenReceiver, method, rewrittenArguments, argumentRefKindsOpt, invokedAsExtensionMethod, resultKind, type, temps);
        }

        private BoundExpression MakeCall(
            BoundCall? node,
            SyntaxNode syntax,
            BoundExpression? rewrittenReceiver,
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
                    null,
                    null,
                    resultKind,
                    rewrittenArguments[0],
                    rewrittenArguments[1],
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
                    defaultArguments: default(BitVector),
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
                    default(BitVector),
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

        private BoundExpression MakeCall(SyntaxNode syntax, BoundExpression? rewrittenReceiver, MethodSymbol method, ImmutableArray<BoundExpression> rewrittenArguments, TypeSymbol type)
        {
            return MakeCall(
                node: null,
                syntax: syntax,
                rewrittenReceiver: rewrittenReceiver,
                method: method,
                rewrittenArguments: rewrittenArguments,
                argumentRefKinds: default(ImmutableArray<RefKind>),
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
                    case BoundKind.PassByCopy:
                        return IsSafeForReordering(((BoundPassByCopy)current).Expression, kind);
                    case BoundKind.Conversion:
                        {
                            BoundConversion conv = (BoundConversion)current;
                            switch (conv.ConversionKind)
                            {
                                case ConversionKind.AnonymousFunction:
                                case ConversionKind.ImplicitConstant:
                                case ConversionKind.MethodGroup:
                                case ConversionKind.NullLiteral:
                                case ConversionKind.DefaultLiteral:
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
                                case ConversionKind.ExplicitPointerToInteger:
                                case ConversionKind.ExplicitPointerToPointer:
                                case ConversionKind.ImplicitPointerToVoid:
                                case ConversionKind.ImplicitNullToPointer:
                                case ConversionKind.ExplicitIntegerToPointer:
                                    current = conv.Operand;
                                    break;

                                case ConversionKind.ExplicitUserDefined:
                                case ConversionKind.ImplicitUserDefined:
                                // expression trees rewrite this later.
                                // it is a kind of user defined conversions on IntPtr and in some cases can fail
                                case ConversionKind.IntPtr:
                                case ConversionKind.ImplicitThrow:
                                    return false;

                                default:
                                    // when this assert is hit, examine whether such conversion kind is 
                                    // 1) actually expected to get this far
                                    // 2) figure if it is possibly not producing or consuming any sideeffects (rare case)
                                    // 3) add a case for it
                                    Debug.Assert(false, "Unexpected conversion kind" + conv.ConversionKind);

                                    // it is safe to assume that conversion is not reorderable
                                    return false;
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
        /// </summary>
        private ImmutableArray<BoundExpression> MakeArguments(
            SyntaxNode syntax,
            ImmutableArray<BoundExpression> rewrittenArguments,
            Symbol methodOrIndexer,
            bool expanded,
            ImmutableArray<int> argsToParamsOpt,
            ref ImmutableArray<RefKind> argumentRefKindsOpt,
            out ImmutableArray<LocalSymbol> temps,
            bool invokedAsExtensionMethod = false,
            ThreeState enableCallerInfo = ThreeState.Unknown)
        {

            // We need to do a fancy rewrite under the following circumstances:
            // (1) a params array is being used; we need to generate the array.
            // (2) there were named arguments that reordered the arguments; we might
            //     have to generate temporaries to ensure that the arguments are 
            //     evaluated in source code order, not the actual call order.
            //
            // If none of those are the case then we can just take an early out.

            ArrayBuilder<LocalSymbol> temporariesBuilder = ArrayBuilder<LocalSymbol>.GetInstance();
            rewrittenArguments = _factory.MakeTempsForDiscardArguments(rewrittenArguments, temporariesBuilder);
            ImmutableArray<ParameterSymbol> parameters = methodOrIndexer.GetParameters();

            if (CanSkipRewriting(rewrittenArguments, methodOrIndexer, expanded, argsToParamsOpt, invokedAsExtensionMethod, false, out var isComReceiver))
            {
                temps = temporariesBuilder.ToImmutableAndFree();
                argumentRefKindsOpt = GetEffectiveArgumentRefKinds(argumentRefKindsOpt, parameters);

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
            // Goo(z: this.p, y: this.Q(), x: (object)10)
            //
            // The boxing of 10 can be reordered, but the fetch of this.p has to happen before the
            // call to this.Q() because the call could change the value of this.p. 
            //
            // We start by binding everything that is not obviously reorderable as a temporary, and
            // then run an optimizer to remove unnecessary temporaries.

            BoundExpression[] actualArguments = new BoundExpression[parameters.Length]; // The actual arguments that will be passed; one actual argument per formal parameter.
            ArrayBuilder<BoundAssignmentOperator> storesToTemps = ArrayBuilder<BoundAssignmentOperator>.GetInstance(rewrittenArguments.Length);
            ArrayBuilder<RefKind> refKinds = ArrayBuilder<RefKind>.GetInstance(parameters.Length, RefKind.None);

            // Step one: Store everything that is non-trivial into a temporary; record the
            // stores in storesToTemps and make the actual argument a reference to the temp.
            // Do not yet attempt to deal with params arrays or optional arguments.
            BuildStoresToTemps(
                expanded,
                argsToParamsOpt,
                parameters,
                argumentRefKindsOpt,
                rewrittenArguments,
                forceLambdaSpilling: false, // lambda conversions can be re-orderd in calls without side affects
                actualArguments,
                refKinds,
                storesToTemps);


            // all the formal arguments, except missing optionals, are now in place. 
            // Optimize away unnecessary temporaries.
            // Necessary temporaries have their store instructions merged into the appropriate 
            // argument expression.
            OptimizeTemporaries(actualArguments, storesToTemps, temporariesBuilder);

            // Step two: If we have a params array, build the array and fill in the argument.
            if (expanded)
            {
                actualArguments[actualArguments.Length - 1] = BuildParamsArray(syntax, methodOrIndexer, argsToParamsOpt, rewrittenArguments, parameters, actualArguments[actualArguments.Length - 1]);
            }

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

            Debug.Assert(actualArguments.All(static arg => arg is not null));
            return actualArguments.AsImmutableOrNull();
        }

        /// <summary>
        /// Patch refKinds for arguments that match 'In' parameters to have effective RefKind.
        /// For the purpose of further analysis we will mark the arguments as -
        /// - In        if was originally passed as None
        /// - StrictIn  if was originally passed as In
        /// Here and in the layers after the lowering we only care about None/notNone differences for the arguments
        /// Except for async stack spilling which needs to know whether arguments were originally passed as "In" and must obey "no copying" rule.
        /// </summary>
        private static ImmutableArray<RefKind> GetEffectiveArgumentRefKinds(ImmutableArray<RefKind> argumentRefKindsOpt, ImmutableArray<ParameterSymbol> parameters)
        {
            ArrayBuilder<RefKind>? refKindsBuilder = null;
            for (int i = 0; i < parameters.Length; i++)
            {
                var paramRefKind = parameters[i].RefKind;
                if (paramRefKind == RefKind.In)
                {
                    var argRefKind = argumentRefKindsOpt.IsDefault ? RefKind.None : argumentRefKindsOpt[i];

                    if (refKindsBuilder == null)
                    {
                        if (!argumentRefKindsOpt.IsDefault)
                        {
                            Debug.Assert(!argumentRefKindsOpt.IsEmpty);
                            refKindsBuilder = ArrayBuilder<RefKind>.GetInstance(parameters.Length);
                            refKindsBuilder.AddRange(argumentRefKindsOpt);
                        }
                        else
                        {
                            refKindsBuilder = ArrayBuilder<RefKind>.GetInstance(parameters.Length, fillWithValue: RefKind.None);
                        }
                    }

                    refKindsBuilder[i] = argRefKind == RefKind.None ? paramRefKind : RefKindExtensions.StrictIn;
                }
            }

            if (refKindsBuilder != null)
            {
                argumentRefKindsOpt = refKindsBuilder.ToImmutableAndFree();
            }

            // NOTE: we may have more arguments than parameters in a case of arglist. That is ok.
            Debug.Assert(argumentRefKindsOpt.IsDefault || argumentRefKindsOpt.Length >= parameters.Length);
            return argumentRefKindsOpt;
        }

        internal static ImmutableArray<IArgumentOperation> MakeArgumentsInEvaluationOrder(
            CSharpOperationFactory operationFactory,
            CSharpCompilation compilation,
            SyntaxNode syntax,
            ImmutableArray<BoundExpression> arguments,
            Symbol methodOrIndexer,
            bool expanded,
            ImmutableArray<int> argsToParamsOpt,
            BitVector defaultArguments,
            bool invokedAsExtensionMethod)
        {
            // We need to do a fancy rewrite under the following circumstances:
            // (1) a params array is being used; we need to generate the array. 
            // (2) named arguments were provided out-of-order of the parameters.
            //
            // If neither of those are the case then we can just take an early out.

            if (CanSkipRewriting(arguments, methodOrIndexer, expanded, argsToParamsOpt, invokedAsExtensionMethod, true, out _))
            {
                // In this case, the invocation is not in expanded form and there's no named argument provided.
                // So we just return list of arguments as is.

                ImmutableArray<ParameterSymbol> parameters = methodOrIndexer.GetParameters();
                ArrayBuilder<IArgumentOperation> argumentsBuilder = ArrayBuilder<IArgumentOperation>.GetInstance(arguments.Length);

                int i = 0;
                for (; i < parameters.Length; ++i)
                {
                    var argumentKind = defaultArguments[i] ? ArgumentKind.DefaultValue : ArgumentKind.Explicit;
                    argumentsBuilder.Add(operationFactory.CreateArgumentOperation(argumentKind, parameters[i].GetPublicSymbol(), arguments[i]));
                }

                // TODO: In case of __arglist, we will have more arguments than parameters, 
                //       set the parameter to null for __arglist argument for now.
                //       https://github.com/dotnet/roslyn/issues/19673
                for (; i < arguments.Length; ++i)
                {
                    var argumentKind = defaultArguments[i] ? ArgumentKind.DefaultValue : ArgumentKind.Explicit;
                    argumentsBuilder.Add(operationFactory.CreateArgumentOperation(argumentKind, null, arguments[i]));
                }

                Debug.Assert(methodOrIndexer.GetIsVararg() ^ parameters.Length == arguments.Length);

                return argumentsBuilder.ToImmutableAndFree();
            }

            return BuildArgumentsInEvaluationOrder(
                operationFactory,
                syntax,
                methodOrIndexer,
                expanded,
                argsToParamsOpt,
                defaultArguments,
                arguments,
                compilation);
        }

        // temporariesBuilder will be null when factory is null.
        private static bool CanSkipRewriting(
            ImmutableArray<BoundExpression> rewrittenArguments,
            Symbol methodOrIndexer,
            bool expanded,
            ImmutableArray<int> argsToParamsOpt,
            bool invokedAsExtensionMethod,
            bool ignoreComReceiver,
            out bool isComReceiver)
        {
            isComReceiver = false;

            // An applicable "vararg" method could not possibly be applicable in its expanded
            // form, and cannot possibly have named arguments or used optional parameters, 
            // because the __arglist() argument has to be positional and in the last position. 

            if (methodOrIndexer.GetIsVararg())
            {
                Debug.Assert(rewrittenArguments.Length == methodOrIndexer.GetParameterCount() + 1);
                Debug.Assert(argsToParamsOpt.IsDefault);
                Debug.Assert(!expanded);
                return true;
            }

            if (!ignoreComReceiver)
            {
                var receiverNamedType = invokedAsExtensionMethod ?
                                        ((MethodSymbol)methodOrIndexer).Parameters[0].Type as NamedTypeSymbol :
                                        methodOrIndexer.ContainingType;
                isComReceiver = receiverNamedType is { IsComImport: true };
            }

            return rewrittenArguments.Length == methodOrIndexer.GetParameterCount() &&
                argsToParamsOpt.IsDefault &&
                !expanded &&
                !isComReceiver;
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
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<RefKind> argumentRefKinds,
            ImmutableArray<BoundExpression> rewrittenArguments,
            bool forceLambdaSpilling,
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
                RefKind argRefKind = argumentRefKinds.RefKinds(a);
                RefKind paramRefKind = parameters[p].RefKind;

                Debug.Assert(arguments[p] == null);

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
                if (IsBeginningOfParamArray(p, a, expanded, arguments.Length, rewrittenArguments, argsToParamsOpt, out int paramArrayArgumentCount)
                    && a + paramArrayArgumentCount == rewrittenArguments.Length)
                {
                    return;
                }

                if ((!forceLambdaSpilling || !isLambdaConversion(argument)) &&
                    IsSafeForReordering(argument, argRefKind))
                {
                    arguments[p] = argument;
                }
                else
                {
                    var temp = _factory.StoreToTemp(
                        argument,
                        out BoundAssignmentOperator assignment,
                        refKind: paramRefKind == RefKind.In ? RefKind.In : argRefKind);
                    storesToTemps.Add(assignment);
                    arguments[p] = temp;
                }

                // Patch refKinds for arguments that match 'In' parameters to have effective RefKind
                // For the purpose of further analysis we will mark the arguments as -
                // - In        if was originally passed as None
                // - StrictIn  if was originally passed as In
                // Here and in the layers after the lowering we only care about None/notNone differences for the arguments
                // Except for async stack spilling which needs to know whether arguments were originally passed as "In" and must obey "no copying" rule.
                if (paramRefKind == RefKind.In)
                {
                    Debug.Assert(argRefKind == RefKind.None || argRefKind == RefKind.In);
                    argRefKind = argRefKind == RefKind.None ? RefKind.In : RefKindExtensions.StrictIn;
                }

                refKinds[p] = argRefKind;
            }

            return;

            bool isLambdaConversion(BoundExpression expr)
                => expr is BoundConversion conv && conv.ConversionKind == ConversionKind.AnonymousFunction;
        }

        // This fills in the arguments and parameters arrays in evaluation order.
        private static ImmutableArray<IArgumentOperation> BuildArgumentsInEvaluationOrder(
            CSharpOperationFactory operationFactory,
            SyntaxNode syntax,
            Symbol methodOrIndexer,
            bool expanded,
            ImmutableArray<int> argsToParamsOpt,
            BitVector defaultArguments,
            ImmutableArray<BoundExpression> arguments,
            CSharpCompilation compilation)
        {
            ImmutableArray<ParameterSymbol> parameters = methodOrIndexer.GetParameters();

            ArrayBuilder<IArgumentOperation> argumentsInEvaluationBuilder = ArrayBuilder<IArgumentOperation>.GetInstance(parameters.Length);

            bool visitedLastParam = false;

            // First, fill in all the explicitly provided arguments.
            for (int a = 0; a < arguments.Length; ++a)
            {
                BoundExpression argument = arguments[a];

                int p = (!argsToParamsOpt.IsDefault) ? argsToParamsOpt[a] : a;
                var parameter = parameters[p];

                if (!visitedLastParam)
                {
                    visitedLastParam = p == parameters.Length - 1;
                }

                ArgumentKind kind = defaultArguments[a] ? ArgumentKind.DefaultValue : ArgumentKind.Explicit;

                if (IsBeginningOfParamArray(p, a, expanded, parameters.Length, arguments, argsToParamsOpt, out int paramArrayArgumentCount))
                {
                    int firstNonParamArrayArgumentIndex = a + paramArrayArgumentCount;
                    Debug.Assert(firstNonParamArrayArgumentIndex <= arguments.Length);

                    kind = ArgumentKind.ParamArray;
                    ArrayBuilder<BoundExpression> paramArray = ArrayBuilder<BoundExpression>.GetInstance(paramArrayArgumentCount);

                    for (int i = a; i < firstNonParamArrayArgumentIndex; ++i)
                    {
                        paramArray.Add(arguments[i]);
                    }

                    // Set loop variable so the value for next iteration will be the index of the first non param-array argument after param-array argument(s).
                    a = firstNonParamArrayArgumentIndex - 1;

                    argument = CreateParamArrayArgument(syntax, parameter.Type, paramArray.ToImmutableAndFree(), compilation, localRewriter: null);
                }

                argumentsInEvaluationBuilder.Add(operationFactory.CreateArgumentOperation(kind, parameter.GetPublicSymbol(), argument));
            }

            // Finally, append the missing empty params array if necessary.
            var lastParam = !parameters.IsEmpty ? parameters[^1] : null;
            if (expanded && lastParam is object && !visitedLastParam)
            {
                Debug.Assert(lastParam.IsParams);

                // Create an empty array for omitted param array argument.
                BoundExpression argument = CreateParamArrayArgument(syntax, lastParam.Type, ImmutableArray<BoundExpression>.Empty, compilation, localRewriter: null);
                ArgumentKind kind = ArgumentKind.ParamArray;

                argumentsInEvaluationBuilder.Add(operationFactory.CreateArgumentOperation(kind, lastParam.GetPublicSymbol(), argument));
            }

            Debug.Assert(argumentsInEvaluationBuilder.All(static arg => arg is not null));
            return argumentsInEvaluationBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Returns true if the given argument is the beginning of a list of param array arguments (could be empty), otherwise returns false.
        /// When returns true, numberOfParamArrayArguments is set to the number of param array arguments.
        /// </summary>
        private static bool IsBeginningOfParamArray(
            int parameterIndex,
            int argumentIndex,
            bool expanded,
            int parameterCount,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<int> argsToParamsOpt,
            out int numberOfParamArrayArguments)
        {
            numberOfParamArrayArguments = 0;

            if (expanded && parameterIndex == parameterCount - 1)
            {
                int remainingArgument = argumentIndex + 1;
                for (; remainingArgument < arguments.Length; ++remainingArgument)
                {
                    int remainingParameter = (!argsToParamsOpt.IsDefault) ? argsToParamsOpt[remainingArgument] : remainingArgument;
                    if (remainingParameter != parameterCount - 1)
                    {
                        break;
                    }
                }
                numberOfParamArrayArguments = remainingArgument - argumentIndex;
                return true;
            }

            return false;
        }

        private BoundExpression BuildParamsArray(
            SyntaxNode syntax,
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

            var paramArrayType = parameters[paramsParam].Type;
            var arrayArgs = paramArray.ToImmutableAndFree();

            // If this is a zero-length array, rather than using "new T[0]", optimize with "Array.Empty<T>()" 
            // if it's available.  However, we also disable the optimization if we're in an expression lambda, the 
            // point of which is just to represent the semantics of an operation, and we don't know that all consumers
            // of expression lambdas will appropriately understand Array.Empty<T>().
            // We disable it for pointer types as well, since they cannot be used as Type Arguments.
            if (arrayArgs.Length == 0
                && !_inExpressionLambda
                && paramArrayType is ArrayTypeSymbol ats // could be false if there's a semantic error, e.g. the params parameter type isn't an array
                && !ats.ElementType.IsPointerOrFunctionPointer())
            {
                MethodSymbol? arrayEmpty = _compilation.GetWellKnownTypeMember(WellKnownMember.System_Array__Empty) as MethodSymbol;
                if (arrayEmpty != null) // will be null if Array.Empty<T> doesn't exist in reference assemblies
                {
                    _diagnostics.ReportUseSite(arrayEmpty, syntax);
                    // return an invocation of "Array.Empty<T>()"
                    arrayEmpty = arrayEmpty.Construct(ImmutableArray.Create(ats.ElementType));
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
                        defaultArguments: default(BitVector),
                        resultKind: LookupResultKind.Viable,
                        type: arrayEmpty.ReturnType);
                }
            }

            return CreateParamArrayArgument(syntax, paramArrayType, arrayArgs, _compilation, this);
        }

        private static BoundExpression CreateParamArrayArgument(SyntaxNode syntax,
            TypeSymbol paramArrayType,
            ImmutableArray<BoundExpression> arrayArgs,
            CSharpCompilation compilation,
            LocalRewriter? localRewriter)
        {

            TypeSymbol int32Type = compilation.GetSpecialType(SpecialType.System_Int32);
            BoundExpression arraySize = MakeLiteral(syntax, ConstantValue.Create(arrayArgs.Length), int32Type, localRewriter);

            return new BoundArrayCreation(
                syntax,
                ImmutableArray.Create(arraySize),
                new BoundArrayInitialization(syntax, arrayArgs) { WasCompilerGenerated = true },
                paramArrayType)
            { WasCompilerGenerated = true };
        }

        /// <summary>
        /// To create literal expression for IOperation, set localRewriter to null.
        /// </summary>
        private static BoundExpression MakeLiteral(SyntaxNode syntax, ConstantValue constantValue, TypeSymbol type, LocalRewriter? localRewriter)
        {
            if (localRewriter != null)
            {
                return localRewriter.MakeLiteral(syntax, constantValue, type);
            }
            else
            {
                return new BoundLiteral(syntax, constantValue, type, constantValue.IsBad) { WasCompilerGenerated = true };
            }
        }

        private static void OptimizeTemporaries(
            BoundExpression[] arguments,
            ArrayBuilder<BoundAssignmentOperator> storesToTemps,
            ArrayBuilder<LocalSymbol> temporariesBuilder)
        {
            Debug.Assert(arguments != null);
            Debug.Assert(storesToTemps != null);
            Debug.Assert(temporariesBuilder != null);

            if (storesToTemps.Count > 0)
            {
                int tempsNeeded = MergeArgumentsAndSideEffects(arguments, storesToTemps);
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
            ArrayBuilder<BoundAssignmentOperator> tempStores)
        {
            Debug.Assert(arguments != null);
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
                        Debug.Assert(value.Type is { });

                        // the matched store will not need to go into side-effects, only ones before it will
                        // remove the store to signal that we are not using its temp.
                        tempStores[correspondingStore] = null!;
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

            Debug.Assert(argsRefKindsBuilder != null);
            Debug.Assert(argsRefKindsBuilder.Count == parameters.Length);

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
