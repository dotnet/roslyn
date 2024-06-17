// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp.CodeGen;

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
            // Dynamic can't have created handler conversions because we don't know target types.
            AssertNoImplicitInterpolatedStringHandlerConversions(node.Arguments);
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

        private void InterceptCallAndAdjustArguments(
            ref MethodSymbol method,
            ref BoundExpression? receiverOpt,
            ref ImmutableArray<BoundExpression> arguments,
            ref ImmutableArray<RefKind> argumentRefKindsOpt,
            bool invokedAsExtensionMethod,
            Syntax.SimpleNameSyntax? nameSyntax)
        {
            if (this._compilation.TryGetInterceptor(nameSyntax) is not var (attributeLocation, interceptor))
            {
                // The call was not intercepted.
                return;
            }

            Debug.Assert(nameSyntax != null);
            Debug.Assert(interceptor.IsDefinition);
            Debug.Assert(!interceptor.ContainingType.IsGenericType);

            if (interceptor.Arity != 0)
            {
                var typeArgumentsBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance();
                method.ContainingType.GetAllTypeArgumentsNoUseSiteDiagnostics(typeArgumentsBuilder);
                typeArgumentsBuilder.AddRange(method.TypeArgumentsWithAnnotations);

                var netArity = typeArgumentsBuilder.Count;
                if (netArity == 0)
                {
                    this._diagnostics.Add(ErrorCode.ERR_InterceptorCannotBeGeneric, attributeLocation, interceptor, method);
                    typeArgumentsBuilder.Free();
                    return;
                }
                else if (interceptor.Arity != netArity)
                {
                    this._diagnostics.Add(ErrorCode.ERR_InterceptorArityNotCompatible, attributeLocation, interceptor, netArity, method);
                    typeArgumentsBuilder.Free();
                    return;
                }

                interceptor = interceptor.Construct(typeArgumentsBuilder.ToImmutableAndFree());
                if (!interceptor.CheckConstraints(new ConstraintsHelper.CheckConstraintsArgs(this._compilation, this._compilation.Conversions, includeNullability: true, attributeLocation, this._diagnostics)))
                {
                    return;
                }
            }

            if (method.MethodKind is not MethodKind.Ordinary)
            {
                this._diagnostics.Add(ErrorCode.ERR_InterceptableMethodMustBeOrdinary, attributeLocation, nameSyntax.Identifier.ValueText);
                return;
            }

            var containingMethod = this._factory.CurrentFunction;
            Debug.Assert(containingMethod is not null);

            var useSiteInfo = this.GetNewCompoundUseSiteInfo();
            var isAccessible = AccessCheck.IsSymbolAccessible(interceptor, containingMethod.ContainingType, ref useSiteInfo);
            this._diagnostics.Add(attributeLocation, useSiteInfo);
            if (!isAccessible)
            {
                this._diagnostics.Add(ErrorCode.ERR_InterceptorNotAccessible, attributeLocation, interceptor, containingMethod);
                return;
            }

            // When the original call is to an instance method, and the interceptor is an extension method,
            // we need to take special care to intercept with the extension method as though it is being called in reduced form.
            Debug.Assert(receiverOpt is not BoundTypeExpression || method.IsStatic);
            var needToReduce = receiverOpt is not (null or BoundTypeExpression) && interceptor.IsExtensionMethod;
            var symbolForCompare = needToReduce ? ReducedExtensionMethodSymbol.Create(interceptor, receiverOpt!.Type, _compilation, out _) : interceptor;

            if (!MemberSignatureComparer.InterceptorsComparer.Equals(method, symbolForCompare))
            {
                this._diagnostics.Add(ErrorCode.ERR_InterceptorSignatureMismatch, attributeLocation, method, interceptor);
                return;
            }

            _ = SourceMemberContainerTypeSymbol.CheckValidNullableMethodOverride(
                _compilation,
                method,
                symbolForCompare,
                _diagnostics,
                static (diagnostics, method, interceptor, topLevel, attributeLocation) =>
                {
                    diagnostics.Add(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnInterceptor, attributeLocation, method);
                },
                static (diagnostics, method, interceptor, implementingParameter, blameAttributes, attributeLocation) =>
                {
                    diagnostics.Add(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnInterceptor, attributeLocation, new FormattedSymbol(implementingParameter, SymbolDisplayFormat.ShortFormat), method);
                },
                extraArgument: attributeLocation);

            if (!MemberSignatureComparer.InterceptorsStrictComparer.Equals(method, symbolForCompare))
            {
                this._diagnostics.Add(ErrorCode.WRN_InterceptorSignatureMismatch, attributeLocation, method, interceptor);
            }

            method.TryGetThisParameter(out var methodThisParameter);
            var interceptorThisParameterForCompare = needToReduce ? interceptor.Parameters[0] :
                interceptor.TryGetThisParameter(out var interceptorThisParameter) ? interceptorThisParameter : null;
            switch (methodThisParameter, interceptorThisParameterForCompare)
            {
                case (not null, null):
                case (not null, not null) when !methodThisParameter.Type.Equals(interceptorThisParameterForCompare.Type, TypeCompareKind.ObliviousNullableModifierMatchesAny)
                        || methodThisParameter.RefKind != interceptorThisParameterForCompare.RefKind:
                    this._diagnostics.Add(ErrorCode.ERR_InterceptorMustHaveMatchingThisParameter, attributeLocation, methodThisParameter, method);
                    return;
                case (null, not null):
                    this._diagnostics.Add(ErrorCode.ERR_InterceptorMustNotHaveThisParameter, attributeLocation, method);
                    return;
                default:
                    break;
            }

            if (invokedAsExtensionMethod && interceptor.IsStatic && !interceptor.IsExtensionMethod)
            {
                // Special case when intercepting an extension method call in reduced form with a non-extension.
                this._diagnostics.Add(ErrorCode.ERR_InterceptorMustHaveMatchingThisParameter, attributeLocation, method.Parameters[0], method);
                return;
            }

            if (SourceMemberContainerTypeSymbol.CheckValidScopedOverride(
                method,
                symbolForCompare,
                this._diagnostics,
                static (diagnostics, method, symbolForCompare, implementingParameter, blameAttributes, attributeLocation) =>
                {
                    diagnostics.Add(ErrorCode.ERR_InterceptorScopedMismatch, attributeLocation, method, symbolForCompare);
                },
                extraArgument: attributeLocation,
                allowVariance: true,
                // Since we've already reduced 'symbolForCompare', we compare as though it is not an extension.
                invokedAsExtensionMethod: false))
            {
                return;
            }

            if (needToReduce)
            {
                Debug.Assert(methodThisParameter is not null);
                Debug.Assert(receiverOpt?.Type is not null);

                // Usually we expect the receiver to already be converted to the this parameter type.
                // However, in the case of a non-reference type receiver, where the this parameter is some base reference type,
                // for example a struct type and System.ValueType respectively, we need to convert the receiver to parameter type,
                // because we can't use the same `.constrained` calling pattern here which we would have used for an instance method receiver.
                Debug.Assert(receiverOpt.Type.Equals(interceptor.Parameters[0].Type, TypeCompareKind.AllIgnoreOptions)
                    || (!receiverOpt.Type.IsReferenceType && interceptor.Parameters[0].Type.IsReferenceType));
                receiverOpt = MakeConversionNode(receiverOpt, interceptor.Parameters[0].Type, @checked: false, markAsChecked: true);

                arguments = arguments.Insert(0, receiverOpt);
                receiverOpt = null;

                // CodeGenerator.EmitArguments requires that we have a fully-filled-out argumentRefKindsOpt for any ref/in/out arguments.
                var thisRefKind = methodThisParameter.RefKind;
                if (argumentRefKindsOpt.IsDefault && thisRefKind != RefKind.None)
                {
                    argumentRefKindsOpt = method.Parameters.SelectAsArray(static param => param.RefKind);
                }

                if (!argumentRefKindsOpt.IsDefault)
                {
                    argumentRefKindsOpt = argumentRefKindsOpt.Insert(0, thisRefKind);
                }
            }

            method = interceptor;

            return;
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            Debug.Assert(node != null);

            BoundExpression rewrittenCall;

            if (TryGetReceiver(node, out BoundCall? receiver1))
            {
                // Handle long call chain of both instance and extension method invocations.
                var calls = ArrayBuilder<BoundCall>.GetInstance();

                calls.Push(node);
                node = receiver1;

                while (TryGetReceiver(node, out BoundCall? receiver2))
                {
                    calls.Push(node);
                    node = receiver2;
                }

                // Rewrite the receiver
                BoundExpression? rewrittenReceiver = VisitExpression(node.ReceiverOpt);

                do
                {
                    rewrittenCall = visitArgumentsAndFinishRewrite(node, rewrittenReceiver);
                    rewrittenReceiver = rewrittenCall;
                }
                while (calls.TryPop(out node!));

                calls.Free();
            }
            else
            {
                // Rewrite the receiver
                BoundExpression? rewrittenReceiver = VisitExpression(node.ReceiverOpt);
                rewrittenCall = visitArgumentsAndFinishRewrite(node, rewrittenReceiver);
            }

            return rewrittenCall;

            BoundExpression visitArgumentsAndFinishRewrite(BoundCall node, BoundExpression? rewrittenReceiver)
            {
                MethodSymbol method = node.Method;
                ImmutableArray<int> argsToParamsOpt = node.ArgsToParamsOpt;
                ImmutableArray<RefKind> argRefKindsOpt = node.ArgumentRefKindsOpt;
                ImmutableArray<BoundExpression> arguments = node.Arguments;
                bool invokedAsExtensionMethod = node.InvokedAsExtensionMethod;

                // Rewritten receiver can be actually the first argument of an extension invocation.
                BoundExpression? firstRewrittenArgument = null;
                if (rewrittenReceiver is not null && node.ReceiverOpt is null)
                {
                    Debug.Assert(invokedAsExtensionMethod && !arguments.IsEmpty);
                    firstRewrittenArgument = rewrittenReceiver;
                    rewrittenReceiver = null;
                }

                ArrayBuilder<LocalSymbol>? temps = null;
                var rewrittenArguments = VisitArgumentsAndCaptureReceiverIfNeeded(
                    ref rewrittenReceiver,
                    captureReceiverMode: ReceiverCaptureMode.Default,
                    arguments,
                    method,
                    argsToParamsOpt,
                    argRefKindsOpt,
                    storesOpt: null,
                    ref temps,
                    firstRewrittenArgument: firstRewrittenArgument);

                rewrittenArguments = MakeArguments(
                    rewrittenArguments,
                    method,
                    node.Expanded,
                    argsToParamsOpt,
                    ref argRefKindsOpt,
                    ref temps,
                    invokedAsExtensionMethod);

                // PROTOTYPE(roles): Disable interception for extensions?
                InterceptCallAndAdjustArguments(ref method, ref rewrittenReceiver, ref rewrittenArguments, ref argRefKindsOpt, invokedAsExtensionMethod, node.InterceptableNameSyntax);

                if (Instrument)
                {
                    // PROTOTYPE(roles): Test this code path
                    Instrumenter.InterceptCallAndAdjustArguments(ref method, ref rewrittenReceiver, ref rewrittenArguments, ref argRefKindsOpt);
                }

                var rewrittenCall = MakeCall(node, node.Syntax, rewrittenReceiver, method, rewrittenArguments, argRefKindsOpt, node.ResultKind, temps.ToImmutableAndFree());

                if (Instrument)
                {
                    rewrittenCall = Instrumenter.InstrumentCall(node, rewrittenCall);
                }

                return rewrittenCall;
            }
        }

        /// <summary>
        /// Gets the instance or extension invocation receiver if any.
        /// </summary>
        internal static bool TryGetReceiver(BoundCall node, [MaybeNullWhen(returnValue: false)] out BoundCall receiver)
        {
            if (node.ReceiverOpt is BoundCall instanceReceiver)
            {
                receiver = instanceReceiver;
                return true;
            }

            if (node.InvokedAsExtensionMethod && node.Arguments is [BoundCall extensionReceiver, ..])
            {
                Debug.Assert(node.ReceiverOpt is null);
                receiver = extensionReceiver;
                return true;
            }

            receiver = null;
            return false;
        }

        private BoundExpression MakeCall(
            BoundCall? node,
            SyntaxNode syntax,
            BoundExpression? rewrittenReceiver,
            MethodSymbol method,
            ImmutableArray<BoundExpression> rewrittenArguments,
            ImmutableArray<RefKind> argumentRefKinds,
            LookupResultKind resultKind,
            ImmutableArray<LocalSymbol> temps)
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
                    methodOpt: null,
                    constrainedToTypeOpt: null,
                    resultKind,
                    rewrittenArguments[0],
                    rewrittenArguments[1],
                    method.ReturnType);
            }
            else if (node == null)
            {
                rewrittenBoundCall = new BoundCall(
                    syntax,
                    rewrittenReceiver,
                    initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                    method,
                    rewrittenArguments,
                    argumentNamesOpt: default(ImmutableArray<string?>),
                    argumentRefKinds,
                    isDelegateCall: false,
                    expanded: false,
                    invokedAsExtensionMethod: false,
                    argsToParamsOpt: default(ImmutableArray<int>),
                    defaultArguments: default(BitVector),
                    resultKind: resultKind,
                    type: method.ReturnType);
            }
            else
            {
                rewrittenBoundCall = node.Update(
                    rewrittenReceiver,
                    initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                    method,
                    rewrittenArguments,
                    argumentNamesOpt: default(ImmutableArray<string?>),
                    argumentRefKinds,
                    node.IsDelegateCall,
                    expanded: false,
                    invokedAsExtensionMethod: false,
                    argsToParamsOpt: default(ImmutableArray<int>),
                    defaultArguments: default(BitVector),
                    node.ResultKind,
                    method.ReturnType);
            }

            Debug.Assert(rewrittenBoundCall.Type is not null);

            if (!temps.IsDefaultOrEmpty)
            {
                return new BoundSequence(
                    syntax,
                    locals: temps,
                    sideEffects: ImmutableArray<BoundExpression>.Empty,
                    value: rewrittenBoundCall,
                    type: rewrittenBoundCall.Type);
            }

            return rewrittenBoundCall;
        }

        private BoundExpression MakeCall(SyntaxNode syntax, BoundExpression? rewrittenReceiver, MethodSymbol method, ImmutableArray<BoundExpression> rewrittenArguments)
        {
            return MakeCall(
                node: null,
                syntax: syntax,
                rewrittenReceiver: rewrittenReceiver,
                method: method,
                rewrittenArguments: rewrittenArguments,
                argumentRefKinds: default(ImmutableArray<RefKind>),
                resultKind: LookupResultKind.Viable,
                temps: default);
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
                if (current.ConstantValueOpt != null)
                {
                    return true;
                }

                switch (current.Kind)
                {
                    default:
                        return false;
                    case BoundKind.Parameter:
                        Debug.Assert(!IsCapturedPrimaryConstructorParameter(expression));
                        goto case BoundKind.Local;

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

        internal static bool IsCapturedPrimaryConstructorParameter(BoundExpression expression)
        {
            return expression is BoundParameter { ParameterSymbol: { ContainingSymbol: SynthesizedPrimaryConstructor primaryCtor } parameter } &&
                   primaryCtor.GetCapturedParameters().ContainsKey(parameter);
        }

        private enum ReceiverCaptureMode
        {
            /// <summary>
            /// No special capture of the receiver, unless arguments need to refer to it.
            /// For example, in case of a string interpolation handler.
            /// </summary>
            Default = 0,

            /// <summary>
            /// Used for a regular indexer compound assignment rewrite.
            /// Everything is going to be in a single setter call with a getter call inside its value argument.
            /// Only receiver and the indexes can be evaluated prior to evaluating the setter call. 
            /// </summary>
            CompoundAssignment,

            /// <summary>
            /// Used for situations when additional arbitrary side-effects are possibly involved.
            /// Think about deconstruction, etc.
            /// </summary>
            UseTwiceComplex
        }

        /// <summary>
        /// Visits all arguments of a method, doing any necessary rewriting for interpolated string handler conversions that
        /// might be present in the arguments and creating temps for any discard parameters.
        /// </summary>
        private ImmutableArray<BoundExpression> VisitArgumentsAndCaptureReceiverIfNeeded(
            [NotNullIfNotNull(nameof(rewrittenReceiver))] ref BoundExpression? rewrittenReceiver,
            ReceiverCaptureMode captureReceiverMode,
            ImmutableArray<BoundExpression> arguments,
            Symbol methodOrIndexer,
            ImmutableArray<int> argsToParamsOpt,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            ArrayBuilder<BoundExpression>? storesOpt,
            ref ArrayBuilder<LocalSymbol>? tempsOpt,
            BoundExpression? firstRewrittenArgument = null)
        {
            Debug.Assert(argumentRefKindsOpt.IsDefault || argumentRefKindsOpt.Length == arguments.Length);
            var requiresInstanceReceiver = methodOrIndexer.RequiresInstanceReceiver() && methodOrIndexer is not MethodSymbol { MethodKind: MethodKind.Constructor } and not FunctionPointerMethodSymbol;
            Debug.Assert(!requiresInstanceReceiver || rewrittenReceiver != null || _inExpressionLambda);
            Debug.Assert(captureReceiverMode == ReceiverCaptureMode.Default || (requiresInstanceReceiver && rewrittenReceiver != null && storesOpt is object));

            BoundLocal? receiverTemp = null;
            BoundAssignmentOperator? assignmentToTemp = null;

            if (captureReceiverMode != ReceiverCaptureMode.Default ||
                (requiresInstanceReceiver && arguments.Any(a => usesReceiver(a))))
            {
                Debug.Assert(!_inExpressionLambda);
                Debug.Assert(rewrittenReceiver is object);
                Debug.Assert(rewrittenReceiver.Type is { });

                RefKind refKind;

                if (captureReceiverMode != ReceiverCaptureMode.Default)
                {
                    // SPEC VIOLATION: It is not very clear when receiver of constrained callvirt is dereferenced - when pushed (in lexical order),
                    // SPEC VIOLATION: or when actual call is executed. The actual behavior seems to be implementation specific in different JITs.
                    // SPEC VIOLATION: To not depend on that, the right thing to do here is to store the value of the variable 
                    // SPEC VIOLATION: when variable has reference type (regular temp), and store variable's location when it has a value type. (ref temp)
                    // SPEC VIOLATION: in a case of unconstrained generic type parameter a runtime test (default(T) == null) would be needed
                    // SPEC VIOLATION: However, for compatibility with Dev12 we will continue treating all generic type parameters, constrained or not,
                    // SPEC VIOLATION: as value types.

                    refKind = rewrittenReceiver.Type.IsValueType || rewrittenReceiver.Type.Kind == SymbolKind.TypeParameter ? RefKind.Ref : RefKind.None;
                }
                else
                {
                    refKind = rewrittenReceiver.GetRefKind();

                    if (refKind == RefKind.None &&
                        !rewrittenReceiver.Type.IsReferenceType &&
                        Binder.HasHome(rewrittenReceiver,
                                       Binder.AddressKind.Constrained,
                                       _factory.CurrentFunction,
                                       peVerifyCompatEnabled: false,
                                       stackLocalsOpt: null))
                    {
                        refKind = RefKind.Ref;
                    }
                }

                receiverTemp = _factory.StoreToTemp(rewrittenReceiver, out assignmentToTemp, refKind);

                tempsOpt ??= ArrayBuilder<LocalSymbol>.GetInstance();
                tempsOpt.Add(receiverTemp.LocalSymbol);
            }

            ImmutableArray<BoundExpression> rewrittenArguments;

            if (arguments.IsEmpty)
            {
                rewrittenArguments = arguments;
            }
            else
            {
                var argumentsAssignedToTemp = BitVector.Null;
                var visitedArgumentsBuilder = ArrayBuilder<BoundExpression>.GetInstance(arguments.Length);
                var parameters = methodOrIndexer.GetParameters();

#if DEBUG
                var saveTempsOpt = tempsOpt;
#endif

                for (int i = 0; i < arguments.Length; i++)
                {
                    var argument = arguments[i];
                    if (argument is BoundDiscardExpression discard)
                    {
                        ensureTempTrackingSetup(ref tempsOpt, ref argumentsAssignedToTemp);
                        visitedArgumentsBuilder.Add(_factory.MakeTempForDiscard(discard, tempsOpt));
                        argumentsAssignedToTemp[i] = true;
                        continue;
                    }

                    ImmutableArray<BoundInterpolatedStringArgumentPlaceholder> argumentPlaceholders = addInterpolationPlaceholderReplacements(
                        parameters,
                        visitedArgumentsBuilder,
                        i,
                        receiverTemp,
                        ref tempsOpt,
                        ref argumentsAssignedToTemp);

                    visitedArgumentsBuilder.Add(i == 0 && firstRewrittenArgument is not null
                        ? firstRewrittenArgument
                        : VisitExpression(argument));

                    foreach (var placeholder in argumentPlaceholders)
                    {
                        // We didn't set this one up, so we can't remove it.
                        if (placeholder.ArgumentIndex == BoundInterpolatedStringArgumentPlaceholder.TrailingConstructorValidityParameter)
                        {
                            continue;
                        }

                        RemovePlaceholderReplacement(placeholder);
                    }
                }

#if DEBUG
                Debug.Assert(saveTempsOpt is object || tempsOpt?.Count is null or > 0);
#endif
                rewrittenArguments = visitedArgumentsBuilder.ToImmutableAndFree();
            }

            if (receiverTemp is object)
            {
                Debug.Assert(assignmentToTemp is object);
                Debug.Assert(tempsOpt is object);

                BoundAssignmentOperator? extraRefInitialization = null;

                if (receiverTemp.LocalSymbol.IsRef &&
                    CodeGenerator.IsPossibleReferenceTypeReceiverOfConstrainedCall(receiverTemp) &&
                    !CodeGenerator.ReceiverIsKnownToReferToTempIfReferenceType(receiverTemp) &&
                    (captureReceiverMode == ReceiverCaptureMode.UseTwiceComplex ||
                     !CodeGenerator.IsSafeToDereferenceReceiverRefAfterEvaluatingArguments(rewrittenArguments)))
                {
                    ReferToTempIfReferenceTypeReceiver(receiverTemp, ref assignmentToTemp, out extraRefInitialization, tempsOpt);
                }

                if (storesOpt is object)
                {
                    if (extraRefInitialization is object)
                    {
                        storesOpt.Add(extraRefInitialization);
                    }

                    storesOpt.Add(assignmentToTemp);
                    rewrittenReceiver = receiverTemp;
                }
                else
                {
                    rewrittenReceiver = _factory.Sequence(
                        ImmutableArray<LocalSymbol>.Empty,
                        extraRefInitialization is object ? ImmutableArray.Create<BoundExpression>(extraRefInitialization, assignmentToTemp) : ImmutableArray.Create<BoundExpression>(assignmentToTemp),
                        receiverTemp);
                }
            }

            return rewrittenArguments;

            void ensureTempTrackingSetup([NotNull] ref ArrayBuilder<LocalSymbol>? tempsOpt, ref BitVector positionsAssignedToTemp)
            {
                tempsOpt ??= ArrayBuilder<LocalSymbol>.GetInstance();

                if (positionsAssignedToTemp.IsNull)
                {
                    positionsAssignedToTemp = BitVector.Create(arguments.Length);
                }
            }

            ImmutableArray<BoundInterpolatedStringArgumentPlaceholder> addInterpolationPlaceholderReplacements(
                ImmutableArray<ParameterSymbol> parameters,
                ArrayBuilder<BoundExpression> visitedArgumentsBuilder,
                int argumentIndex,
                BoundLocal? receiverTemp,
                ref ArrayBuilder<LocalSymbol>? tempsOpt,
                ref BitVector argumentsAssignedToTemp)
            {
                var argument = arguments[argumentIndex];

                if (argument is BoundConversion { ConversionKind: ConversionKind.InterpolatedStringHandler, Operand: BoundInterpolatedString or BoundBinaryOperator } conversion)
                {
                    // Handler conversions are not supported in expression lambdas.
                    Debug.Assert(!_inExpressionLambda);
                    var interpolationData = conversion.Operand.GetInterpolatedStringHandlerData();

                    if (interpolationData.ArgumentPlaceholders.Length > (interpolationData.HasTrailingHandlerValidityParameter ? 1 : 0))
                    {
                        Debug.Assert(!((BoundConversion)argument).ExplicitCastInCode);

                        // We have an interpolated string handler conversion that needs context from the surrounding arguments. We need to store
                        // all arguments up to and including the last argument needed by this interpolated string conversion into temps, in order
                        // to ensure we're keeping lexical ordering of side effects.
                        ensureTempTrackingSetup(ref tempsOpt, ref argumentsAssignedToTemp);
                        Debug.Assert(!argumentsAssignedToTemp.IsNull);

                        foreach (var placeholder in interpolationData.ArgumentPlaceholders)
                        {
                            // Replace each needed placeholder with a sequence of store and evaluate the temp.
                            var argIndex = placeholder.ArgumentIndex;
                            Debug.Assert(argIndex < argumentIndex);

                            BoundLocal local;
                            switch (argIndex)
                            {
                                case BoundInterpolatedStringArgumentPlaceholder.InstanceParameter:
                                    Debug.Assert(usesReceiver(argument));
                                    Debug.Assert(requiresInstanceReceiver);
                                    Debug.Assert(receiverTemp is object);
                                    local = receiverTemp;
                                    break;

                                case >= 0 when argumentsAssignedToTemp[argIndex]:
                                    local = visitedArgumentsBuilder[argIndex] switch
                                    {
                                        BoundSequence { Value: BoundLocal l } => l,
                                        BoundLocal l => l, // Can happen for discard arguments
                                        var u => throw ExceptionUtilities.UnexpectedValue(u.Kind)
                                    };
                                    break;

                                case >= 0:
                                    Debug.Assert(visitedArgumentsBuilder[argIndex] != null);
                                    var paramIndex = argsToParamsOpt.IsDefault ? argIndex : argsToParamsOpt[argIndex];
                                    RefKind argRefKind = argumentRefKindsOpt.RefKinds(argIndex);
                                    RefKind paramRefKind = parameters[paramIndex].RefKind;
                                    var visitedArgument = visitedArgumentsBuilder[argIndex];
                                    local = _factory.StoreToTemp(visitedArgument, out var store, refKind: paramRefKind is RefKind.In or RefKind.RefReadOnlyParameter ? RefKind.In : argRefKind);
                                    tempsOpt.Add(local.LocalSymbol);
                                    visitedArgumentsBuilder[argIndex] = _factory.Sequence(ImmutableArray<LocalSymbol>.Empty, ImmutableArray.Create<BoundExpression>(store), local);
                                    argumentsAssignedToTemp[argIndex] = true;
                                    break;

                                case BoundInterpolatedStringArgumentPlaceholder.TrailingConstructorValidityParameter:
                                    // Visiting the interpolated string itself will allocate the temp for this one.
                                    continue;

                                default:
                                    throw ExceptionUtilities.UnexpectedValue(argIndex);
                            }

                            AddPlaceholderReplacement(placeholder, local);
                        }

                        return interpolationData.ArgumentPlaceholders;
                    }
                }

                return ImmutableArray<BoundInterpolatedStringArgumentPlaceholder>.Empty;
            }

            static bool usesReceiver(BoundExpression argument)
            {
                if (argument is BoundConversion { ConversionKind: ConversionKind.InterpolatedStringHandler, Operand: BoundInterpolatedString or BoundBinaryOperator } conversion)
                {
                    var interpolationData = conversion.Operand.GetInterpolatedStringHandlerData();

                    if (interpolationData.ArgumentPlaceholders.Length > (interpolationData.HasTrailingHandlerValidityParameter ? 1 : 0))
                    {
                        Debug.Assert(!((BoundConversion)argument).ExplicitCastInCode);

                        foreach (var placeholder in interpolationData.ArgumentPlaceholders)
                        {
                            if (placeholder.ArgumentIndex == BoundInterpolatedStringArgumentPlaceholder.InstanceParameter)
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
        }

        private void ReferToTempIfReferenceTypeReceiver(BoundLocal receiverTemp, ref BoundAssignmentOperator assignmentToTemp, out BoundAssignmentOperator? extraRefInitialization, ArrayBuilder<LocalSymbol> temps)
        {
            Debug.Assert(assignmentToTemp.IsRef);

            var receiverType = receiverTemp.Type;
            Debug.Assert(receiverType is object);

            // A case where T is actually a class must be handled specially.
            // Taking a reference to a class instance is fragile because the value behind the 
            // reference might change while arguments are evaluated. However, the call should be
            // performed on the instance that is behind reference at the time we push the
            // reference to the stack. So, for a class we need to emit a reference to a temporary
            // location, rather than to the original location

            BoundLocal cache = _factory.Local(_factory.SynthesizedLocal(receiverType));

            temps.Add(cache.LocalSymbol);

            if (!receiverType.IsReferenceType)
            {
                // Store receiver ref to a different ref local - intermediate ref
                var intermediateRef = _factory.Local(_factory.SynthesizedLocal(receiverType, refKind: RefKind.Ref));
                temps.Add(intermediateRef.LocalSymbol);
                extraRefInitialization = assignmentToTemp.Update(intermediateRef, assignmentToTemp.Right, assignmentToTemp.IsRef, assignmentToTemp.Type);

                // `receiverTemp` initialization is adjusted as follows:
                // If we are dealing with a value type, use value of the intermediate ref.
                // Otherwise, use an address of a temp where we store the underlying reference type instance.
                assignmentToTemp =
                    assignmentToTemp.Update(
                        assignmentToTemp.Left,
#pragma warning disable format
                        new BoundComplexConditionalReceiver(receiverTemp.Syntax,
                                                            intermediateRef,
                                                            _factory.Sequence(new BoundExpression[] { _factory.AssignmentExpression(cache, intermediateRef) }, cache),
                                                            receiverType) { WasCompilerGenerated = true },
#pragma warning restore format
                        assignmentToTemp.IsRef,
                        assignmentToTemp.Type);

                // SpillSequenceSpiller should be able to recognize this node in order to handle its spilling. 
                Debug.Assert(SpillSequenceSpiller.IsComplexConditionalInitializationOfReceiverRef(assignmentToTemp, out _, out _, out _, out _));
            }
            else
            {
                extraRefInitialization = null;

                // We are dealing with a reference type. We can simply copy the instance into a temp and 
                // use its address instead.  
                assignmentToTemp =
                    assignmentToTemp.Update(
                        assignmentToTemp.Left,
                        _factory.Sequence(new BoundExpression[] { _factory.AssignmentExpression(cache, assignmentToTemp.Right) }, cache),
                        assignmentToTemp.IsRef,
                        assignmentToTemp.Type);
            }

            ((SynthesizedLocal)receiverTemp.LocalSymbol).SetIsKnownToReferToTempIfReferenceType();
            Debug.Assert(CodeGenerator.ReceiverIsKnownToReferToTempIfReferenceType(receiverTemp));
        }

        /// <summary>
        /// Rewrites arguments of an invocation according to the receiving method or indexer.
        /// It is assumed that each argument has already been lowered, but we may need
        /// additional rewriting for the arguments, such as re-ordering
        /// arguments based on <paramref name="argsToParamsOpt"/> map, etc.
        /// </summary>
        private ImmutableArray<BoundExpression> MakeArguments(
            ImmutableArray<BoundExpression> rewrittenArguments,
            Symbol methodOrIndexer,
            bool expanded,
            ImmutableArray<int> argsToParamsOpt,
            ref ImmutableArray<RefKind> argumentRefKindsOpt,
            [NotNull] ref ArrayBuilder<LocalSymbol>? temps,
            bool invokedAsExtensionMethod = false)
        {

            // We need to do a fancy rewrite under the following circumstances:
            // (1) there were named arguments that reordered the arguments; we might
            //     have to generate temporaries to ensure that the arguments are 
            //     evaluated in source code order, not the actual call order.
            //
            // If none of those are the case then we can just take an early out.

            Debug.Assert(rewrittenArguments.All(arg => arg is not BoundDiscardExpression), "Discards should have been substituted by VisitArguments");
            temps ??= ArrayBuilder<LocalSymbol>.GetInstance();
            ImmutableArray<ParameterSymbol> parameters = methodOrIndexer.GetParameters();
            BoundExpression? optimized;

            Debug.Assert(expanded ? rewrittenArguments.Length == parameters.Length : rewrittenArguments.Length >= parameters.Length);
            Debug.Assert(rewrittenArguments.Count(a => a.IsParamsArrayOrCollection) <= (expanded ? 1 : 0));

            if (CanSkipRewriting(rewrittenArguments, methodOrIndexer, argsToParamsOpt, invokedAsExtensionMethod, false, out var isComReceiver))
            {
                argumentRefKindsOpt = GetEffectiveArgumentRefKinds(argumentRefKindsOpt, parameters);

                if (expanded && TryOptimizeParamsArray(rewrittenArguments[rewrittenArguments.Length - 1], out optimized))
                {
                    return rewrittenArguments.SetItem(rewrittenArguments.Length - 1, optimized);
                }

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
            BuildStoresToTemps(
                expanded,
                argsToParamsOpt,
                parameters,
                argumentRefKindsOpt,
                rewrittenArguments,
                forceLambdaSpilling: false, // lambda conversions can be re-ordered in calls without side affects
                actualArguments,
                refKinds,
                storesToTemps);

            // all the formal arguments are now in place. 
            // Optimize away unnecessary temporaries.
            // Necessary temporaries have their store instructions merged into the appropriate
            // argument expression.
            OptimizeTemporaries(actualArguments, storesToTemps, temps);

            storesToTemps.Free();

            if (expanded && TryOptimizeParamsArray(actualArguments[actualArguments.Length - 1], out optimized))
            {
                actualArguments[actualArguments.Length - 1] = optimized;
            }

            if (isComReceiver)
            {
                RewriteArgumentsForComCall(parameters, actualArguments, refKinds, temps);
            }

            // * The refkind map is now filled out to match the arguments.
            // * The list of parameter names is now null because the arguments have been reordered.
            // * The args-to-params map is now null because every argument exactly matches its parameter.
            // * The call is no longer in its expanded form.

            argumentRefKindsOpt = GetRefKindsOrNull(refKinds);
            refKinds.Free();

            Debug.Assert(actualArguments.All(static arg => arg is not null));
            return actualArguments.AsImmutableOrNull();
        }

        private bool TryOptimizeParamsArray(BoundExpression possibleParamsArray, [NotNullWhen(true)] out BoundExpression? optimized)
        {
            if (possibleParamsArray.IsParamsArrayOrCollection && !_inExpressionLambda && ((BoundArrayCreation)possibleParamsArray).Bounds is [BoundLiteral { ConstantValueOpt.Value: 0 }])
            {
                optimized = CreateArrayEmptyCallIfAvailable(possibleParamsArray.Syntax, ((ArrayTypeSymbol)possibleParamsArray.Type!).ElementType);
                if (optimized is { })
                {
                    return true;
                }
            }

            optimized = null;
            return false;
        }

        /// <summary>
        /// Patch refKinds for arguments that match 'in', 'ref', or 'ref readonly' parameters to have effective RefKind.
        /// For the purpose of further analysis we will mark the arguments as -
        /// - In                    if was originally passed as None and matches an 'in' or 'ref readonly' parameter
        /// - StrictIn              if was originally passed as In or Ref and matches an 'in' or 'ref readonly' parameter
        /// - Ref                   if the argument is an interpolated string literal subject to an interpolated string handler conversion. No other types
        ///                         are patched here.
        /// Here and in the layers after the lowering we only care about None/notNone differences for the arguments
        /// Except for async stack spilling which needs to know whether arguments were originally passed as "In" and must obey "no copying" rule.
        /// </summary>
        private static ImmutableArray<RefKind> GetEffectiveArgumentRefKinds(ImmutableArray<RefKind> argumentRefKindsOpt, ImmutableArray<ParameterSymbol> parameters)
        {
            ArrayBuilder<RefKind>? refKindsBuilder = null;
            for (int i = 0; i < parameters.Length; i++)
            {
                var paramRefKind = parameters[i].RefKind;
                if (paramRefKind is RefKind.In or RefKind.RefReadOnlyParameter)
                {
                    var argRefKind = argumentRefKindsOpt.IsDefault ? RefKind.None : argumentRefKindsOpt[i];
                    fillRefKindsBuilder(argumentRefKindsOpt, parameters, ref refKindsBuilder);
                    refKindsBuilder[i] = argRefKind == RefKind.None ? RefKind.In : RefKindExtensions.StrictIn;
                }
                else if (paramRefKind == RefKind.Ref)
                {
                    var argRefKind = argumentRefKindsOpt.IsDefault ? RefKind.None : argumentRefKindsOpt[i];
                    if (argRefKind == RefKind.None)
                    {
                        // Interpolated strings used as interpolated string handlers are allowed to match ref parameters without `ref`
                        Debug.Assert(parameters[i].Type is NamedTypeSymbol { IsInterpolatedStringHandlerType: true, IsValueType: true });

                        fillRefKindsBuilder(argumentRefKindsOpt, parameters, ref refKindsBuilder);
                        refKindsBuilder[i] = RefKind.Ref;
                    }
                }
            }

            if (refKindsBuilder != null)
            {
                argumentRefKindsOpt = refKindsBuilder.ToImmutableAndFree();
            }

            // NOTE: we may have more arguments than parameters in a case of arglist. That is ok.
            Debug.Assert(argumentRefKindsOpt.IsDefault || argumentRefKindsOpt.Length >= parameters.Length);
            return argumentRefKindsOpt;

            static void fillRefKindsBuilder(ImmutableArray<RefKind> argumentRefKindsOpt, ImmutableArray<ParameterSymbol> parameters, [NotNull] ref ArrayBuilder<RefKind>? refKindsBuilder)
            {
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
            }
        }

        // temporariesBuilder will be null when factory is null.
        internal static bool CanSkipRewriting(
            ImmutableArray<BoundExpression> rewrittenArguments,
            Symbol methodOrIndexer,
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

        private delegate BoundExpression ParamsArrayElementRewriter<TArg>(BoundExpression element, ref TArg arg);
        private static BoundExpression RewriteParamsArray<TArg>(BoundExpression paramsArray, ParamsArrayElementRewriter<TArg> elementRewriter, ref TArg arg)
        {
            Debug.Assert(paramsArray.IsParamsArrayOrCollection);

            if (paramsArray is BoundArrayCreation { Bounds: [BoundLiteral] bounds, InitializerOpt: BoundArrayInitialization { Initializers: var elements } initialization } creation)
            {
                ArrayBuilder<BoundExpression>? elementsBuilder = null;

                for (int i = 0; i < elements.Length; i++)
                {
                    var element = elements[i];
                    var replacement = elementRewriter(element, ref arg);

                    if (element != replacement)
                    {
                        if (elementsBuilder == null)
                        {
                            elementsBuilder = ArrayBuilder<BoundExpression>.GetInstance(elements.Length);
                            elementsBuilder.AddRange(elements, i);
                        }

                        elementsBuilder.Add(replacement);
                    }
                    else if (elementsBuilder is { })
                    {
                        elementsBuilder.Add(replacement);
                    }
                }

                if (elementsBuilder is { })
                {
                    return creation.Update(bounds, initialization.Update(elementsBuilder.ToImmutableAndFree()), creation.Type);
                }
                else
                {
                    return creation;
                }
            }
            else
            {
                throw ExceptionUtilities.Unreachable();
            }
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
            Debug.Assert(rewrittenArguments.Length == parameters.Length);
            Debug.Assert(rewrittenArguments.Count(a => a.IsParamsArrayOrCollection) <= (expanded ? 1 : 0));

            for (int a = 0; a < rewrittenArguments.Length; ++a)
            {
                BoundExpression argument = rewrittenArguments[a];
                int p = (!argsToParamsOpt.IsDefault) ? argsToParamsOpt[a] : a;
                RefKind argRefKind = argumentRefKinds.RefKinds(a);
                RefKind paramRefKind = parameters[p].RefKind;

                Debug.Assert(arguments[p] == null);

                if (argument.IsParamsArrayOrCollection)
                {
                    Debug.Assert(expanded);
                    Debug.Assert(p == parameters.Length - 1);
                    Debug.Assert(argRefKind == RefKind.None);
                    refKinds[p] = argRefKind;

                    if (a == rewrittenArguments.Length - 1)
                    {
                        arguments[p] = argument;
                    }
                    else
                    {
                        // Storing the array creation instead changes IL for
                        // Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen.CodeGenTests.NamedParamsOptimizationAndParams002​
                        // unit test.
                        (LocalRewriter rewriter, bool forceLambdaSpilling, ArrayBuilder<BoundAssignmentOperator> storesToTemps) arg = (rewriter: this, forceLambdaSpilling, storesToTemps);
                        arguments[p] = RewriteParamsArray(
                                           argument,
                                           static (BoundExpression element, ref (LocalRewriter rewriter, bool forceLambdaSpilling, ArrayBuilder<BoundAssignmentOperator> storesToTemps) arg) =>
                                               arg.rewriter.StoreArgumentToTempIfNecessary(arg.forceLambdaSpilling, arg.storesToTemps, element, RefKind.None, RefKind.None),
                                           ref arg);

                        Debug.Assert(arguments[p].IsParamsArrayOrCollection);
                    }

                    continue;
                }

                arguments[p] = StoreArgumentToTempIfNecessary(forceLambdaSpilling, storesToTemps, argument, argRefKind, paramRefKind);

                // Patch refKinds for arguments that match 'in' or 'ref readonly' parameters to have effective RefKind
                // For the purpose of further analysis we will mark the arguments as -
                // - In                    if was originally passed as None and matches an 'in' or 'ref readonly' parameter
                // - StrictIn              if was originally passed as In or Ref and matches an 'in' or 'ref readonly' parameter
                // Here and in the layers after the lowering we only care about None/notNone differences for the arguments
                // Except for async stack spilling which needs to know whether arguments were originally passed as "In" and must obey "no copying" rule.
                if (paramRefKind is RefKind.In or RefKind.RefReadOnlyParameter)
                {
                    Debug.Assert(argRefKind is RefKind.None or RefKind.In or RefKind.Ref);
                    argRefKind = argRefKind == RefKind.None ? RefKind.In : RefKindExtensions.StrictIn;
                }

                refKinds[p] = argRefKind;
            }

            return;
        }

        private BoundExpression StoreArgumentToTempIfNecessary(bool forceLambdaSpilling, ArrayBuilder<BoundAssignmentOperator> storesToTemps, BoundExpression argument, RefKind argRefKind, RefKind paramRefKind)
        {
            if ((!forceLambdaSpilling || !isLambdaConversion(argument)) &&
                IsSafeForReordering(argument, argRefKind))
            {
                return argument;
            }
            else
            {
                var temp = _factory.StoreToTemp(
                    argument,
                    out BoundAssignmentOperator assignment,
                    refKind: paramRefKind is RefKind.In or RefKind.RefReadOnlyParameter
                        ? (argRefKind == RefKind.None ? RefKind.In : RefKindExtensions.StrictIn)
                        : argRefKind);
                storesToTemps.Add(assignment);
                return temp;
            }

            bool isLambdaConversion(BoundExpression expr)
                => expr is BoundConversion conv && conv.ConversionKind == ConversionKind.AnonymousFunction;
        }

        private BoundExpression CreateEmptyArray(SyntaxNode syntax, ArrayTypeSymbol arrayType)
        {
            BoundExpression? arrayEmpty = CreateArrayEmptyCallIfAvailable(syntax, arrayType.ElementType);
            if (arrayEmpty is { })
            {
                return arrayEmpty;
            }
            // new T[0]
            return new BoundArrayCreation(
                syntax,
                ImmutableArray.Create<BoundExpression>(
                    new BoundLiteral(
                        syntax,
                        ConstantValue.Create(0),
                        _compilation.GetSpecialType(SpecialType.System_Int32))),
                initializerOpt: null,
                arrayType)
            { WasCompilerGenerated = true };
        }

        private BoundExpression? CreateArrayEmptyCallIfAvailable(SyntaxNode syntax, TypeSymbol elementType)
        {
            if (elementType.IsPointerOrFunctionPointer())
            {
                // Pointer types cannot be used as type arguments.
                return null;
            }

            MethodSymbol? arrayEmpty = _compilation.GetSpecialTypeMember(SpecialMember.System_Array__Empty) as MethodSymbol;
            if (arrayEmpty is null) // will be null if Array.Empty<T> doesn't exist in reference assemblies
            {
                return null;
            }

            _diagnostics.ReportUseSite(arrayEmpty, syntax);
            // return an invocation of "Array.Empty<T>()"
            arrayEmpty = arrayEmpty.Construct(ImmutableArray.Create(elementType));
            return new BoundCall(
                syntax,
                receiverOpt: null,
                        initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                arrayEmpty,
                ImmutableArray<BoundExpression>.Empty,
                default(ImmutableArray<string?>),
                default(ImmutableArray<RefKind>),
                isDelegateCall: false,
                expanded: false,
                invokedAsExtensionMethod: false,
                argsToParamsOpt: default(ImmutableArray<int>),
                defaultArguments: default(BitVector),
                resultKind: LookupResultKind.Viable,
                type: arrayEmpty.ReturnType);
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

                if (argument.IsParamsArrayOrCollection)
                {
                    (ArrayBuilder<BoundAssignmentOperator> tempStores, int tempsRemainedInUse, int firstUnclaimedStore) arg = (tempStores, tempsRemainedInUse, firstUnclaimedStore);
                    arguments[a] = RewriteParamsArray(
                                       argument,
                                       static (BoundExpression element, ref (ArrayBuilder<BoundAssignmentOperator> tempStores, int tempsRemainedInUse, int firstUnclaimedStore) arg) =>
                                           mergeArgumentAndSideEffect(element, arg.tempStores, ref arg.tempsRemainedInUse, ref arg.firstUnclaimedStore),
                                       ref arg);
                    tempsRemainedInUse = arg.tempsRemainedInUse;
                    firstUnclaimedStore = arg.firstUnclaimedStore;
                }
                else
                {
                    arguments[a] = mergeArgumentAndSideEffect(argument, tempStores, ref tempsRemainedInUse, ref firstUnclaimedStore);
                }
            }

            Debug.Assert(firstUnclaimedStore == tempStores.Count, "not all side-effects were claimed");
            return tempsRemainedInUse;

            static BoundExpression mergeArgumentAndSideEffect(BoundExpression argument, ArrayBuilder<BoundAssignmentOperator> tempStores, ref int tempsRemainedInUse, ref int firstUnclaimedStore)
            {
                // if argument is a load, search for corresponding store. if store is found, extract
                // the actual expression we were storing and add it as an argument - this one does
                // not need a temp. if there are any unclaimed stores before the found one, add them
                // as side effects that precede this arg, they cannot happen later.
                // NOTE: missing optional parameters are not filled yet and therefore nulls - no need to do anything for them
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
                        Debug.Assert(value.Type is { });

                        // the matched store will not need to go into side-effects, only ones before it will
                        // remove the store to signal that we are not using its temp.
                        tempStores[correspondingStore] = null!;
                        tempsRemainedInUse--;

                        // no need for side-effects?
                        // just combine store and load
                        if (correspondingStore == firstUnclaimedStore)
                        {
                            argument = value;
                        }
                        else
                        {
                            var sideeffects = new BoundExpression[correspondingStore - firstUnclaimedStore];
                            for (int s = 0; s < sideeffects.Length; s++)
                            {
                                sideeffects[s] = tempStores[firstUnclaimedStore + s];
                            }

                            argument = new BoundSequence(
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

                return argument;
            }
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
