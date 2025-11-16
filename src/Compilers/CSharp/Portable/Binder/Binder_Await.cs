// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This portion of the binder converts an AwaitExpressionSyntax into a BoundExpression
    /// </summary>
    internal partial class Binder
    {
        private BoundExpression BindAwait(AwaitExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            MessageID.IDS_FeatureAsync.CheckFeatureAvailability(diagnostics, node.AwaitKeyword);

            BoundExpression expression = BindRValueWithoutTargetType(node.Expression, diagnostics);

            return BindAwait(expression, node, diagnostics);
        }

        private BoundAwaitExpression BindAwait(BoundExpression expression, SyntaxNode node, BindingDiagnosticBag diagnostics)
        {
            bool hasErrors = false;
            var placeholder = new BoundAwaitableValuePlaceholder(expression.Syntax, expression.Type);

            ReportBadAwaitDiagnostics(node, diagnostics, ref hasErrors);
            var info = BindAwaitInfo(placeholder, node, diagnostics, ref hasErrors, expressionOpt: expression);

            // Spec 7.7.7.2:
            // The expression await t is classified the same way as the expression (t).GetAwaiter().GetResult(). Thus,
            // if the return type of GetResult is void, the await-expression is classified as nothing. If it has a
            // non-void return type T, the await-expression is classified as a value of type T.
            TypeSymbol awaitExpressionType = (info.GetResult ?? info.RuntimeAsyncAwaitCall?.Method)?.ReturnType ?? (hasErrors ? CreateErrorType() : Compilation.DynamicType);

            return new BoundAwaitExpression(node, expression, info, debugInfo: default, awaitExpressionType, hasErrors);
        }

        internal void ReportBadAwaitDiagnostics(SyntaxNodeOrToken nodeOrToken, BindingDiagnosticBag diagnostics, ref bool hasErrors)
        {
            hasErrors |= ReportBadAwaitWithoutAsync(nodeOrToken, diagnostics);
            hasErrors |= ReportBadAwaitContext(nodeOrToken, diagnostics);
        }

        internal BoundAwaitableInfo BindAwaitInfo(BoundAwaitableValuePlaceholder getAwaiterPlaceholder, SyntaxNode node, BindingDiagnosticBag diagnostics, ref bool hasErrors, BoundExpression? expressionOpt = null)
        {
            bool hasGetAwaitableErrors = !GetAwaitableExpressionInfo(
                expressionOpt ?? getAwaiterPlaceholder,
                getAwaiterPlaceholder,
                out bool isDynamic,
                out BoundExpression? getAwaiter,
                out PropertySymbol? isCompleted,
                out MethodSymbol? getResult,
                getAwaiterGetResultCall: out _,
                out BoundCall? runtimeAsyncAwaitCall,
                out BoundAwaitableValuePlaceholder? runtimeAsyncAwaitPlaceholder,
                node,
                diagnostics);
            hasErrors |= hasGetAwaitableErrors;

            return new BoundAwaitableInfo(node, getAwaiterPlaceholder, isDynamic: isDynamic, getAwaiter, isCompleted, getResult, runtimeAsyncAwaitCall, runtimeAsyncAwaitPlaceholder, hasErrors: hasGetAwaitableErrors) { WasCompilerGenerated = true };
        }

        /// <summary>
        /// Return true iff an await with this subexpression would be legal where the expression appears.
        /// </summary>
        private bool CouldBeAwaited(BoundExpression expression)
        {
            // If the expression doesn't have a type, just bail out now. Also,
            // the dynamic type is always awaitable in an async method and
            // could generate a lot of noise if we warned on it. Finally, we only want
            // to warn on method calls, not other kinds of expressions.

            if (expression.Kind != BoundKind.Call ||
                expression.HasAnyErrors)
            {
                return false;
            }

            var type = expression.Type;
            if (type is null ||
                type.IsDynamic() ||
                type.IsVoidType())
            {
                return false;
            }

            var call = (BoundCall)expression;
            Debug.Assert(!call.IsErroneousNode);

            // First check if the target method is async.
            if ((object)call.Method != null && call.Method.IsAsync)
            {
                return true;
            }

            // Then check if the method call returns a WinRT async type.
            if (ImplementsWinRTAsyncInterface(call.Type))
            {
                return true;
            }

            // Finally, if we're in an async method, and the expression could be awaited, report that it is instead discarded.
            var containingMethod = this.ContainingMemberOrLambda as MethodSymbol;
            if (containingMethod is null
                || !(containingMethod.IsAsync || containingMethod is SynthesizedSimpleProgramEntryPointSymbol))
            {
                return false;
            }

            if (ContextForbidsAwait)
            {
                return false;
            }

            // Could we bind await on this expression (ignoring whether we are in async context)?
            var syntax = expression.Syntax;
            if (ReportBadAwaitContext(syntax, BindingDiagnosticBag.Discarded))
            {
                return false;
            }

            return GetAwaitableExpressionInfo(expression, getAwaiterGetResultCall: out _, runtimeAsyncAwaitCall: out _,
                node: syntax, diagnostics: BindingDiagnosticBag.Discarded);
        }

        /// <summary>
        /// Assuming we are in an async method, return true if we're in a context where await would be illegal.
        /// Specifically, return true if we're in a lock or catch filter.
        /// </summary>
        private bool ContextForbidsAwait
        {
            get
            {
                return this.Flags.Includes(BinderFlags.InCatchFilter) ||
                    this.Flags.Includes(BinderFlags.InLockBody);
            }
        }

        /// <summary>
        /// Reports an error if the await expression did not occur in an async context.
        /// </summary>
        /// <returns>True if the expression contains errors.</returns>
        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "'await without async' refers to the error scenario.")]
        private bool ReportBadAwaitWithoutAsync(SyntaxNodeOrToken nodeOrToken, BindingDiagnosticBag diagnostics)
        {
            DiagnosticInfo? info = null;
            var containingMemberOrLambda = this.ContainingMemberOrLambda;
            if (containingMemberOrLambda is object)
            {
                switch (containingMemberOrLambda.Kind)
                {
                    case SymbolKind.Field:
                        if (containingMemberOrLambda.ContainingType.IsScriptClass)
                        {
                            if (((FieldSymbol)containingMemberOrLambda).IsStatic)
                            {
                                info = new CSDiagnosticInfo(ErrorCode.ERR_BadAwaitInStaticVariableInitializer);
                            }
                            else
                            {
                                return false;
                            }
                        }
                        break;
                    case SymbolKind.Method:
                        var method = (MethodSymbol)containingMemberOrLambda;
                        if (method.IsAsync)
                        {
                            return false;
                        }
                        if (method.MethodKind == MethodKind.AnonymousFunction)
                        {
                            info = method.IsImplicitlyDeclared ?
                                // The await expression occurred in a query expression:
                                new CSDiagnosticInfo(ErrorCode.ERR_BadAwaitInQuery) :
                                new CSDiagnosticInfo(ErrorCode.ERR_BadAwaitWithoutAsyncLambda, ((LambdaSymbol)method).MessageID.Localize());
                        }
                        else
                        {
                            if (method.ReturnsVoid)
                            {
                                info = new CSDiagnosticInfo(ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod);
                            }
                            else if (method.IsIterator && 
                                     (method.ReturnType.IsIAsyncEnumerableType(Compilation) || 
                                      method.ReturnType.IsIAsyncEnumeratorType(Compilation)))
                            {
                                // For async iterators, use the generic error that doesn't suggest changing the return type
                                info = new CSDiagnosticInfo(ErrorCode.ERR_BadAwaitWithoutAsync);
                            }
                            else
                            {
                                info = new CSDiagnosticInfo(ErrorCode.ERR_BadAwaitWithoutAsyncMethod, method.ReturnType);
                            }
                        }
                        break;
                }
            }
            if (info == null)
            {
                info = new CSDiagnosticInfo(ErrorCode.ERR_BadAwaitWithoutAsync);
            }
            Error(diagnostics, info, nodeOrToken.GetLocation()!);
            return true;
        }

        /// <summary>
        /// Report diagnostics if the await expression occurs in a context where it is not allowed.
        /// </summary>
        /// <returns>True if errors were found.</returns>
        private bool ReportBadAwaitContext(SyntaxNodeOrToken nodeOrToken, BindingDiagnosticBag diagnostics)
        {
            if (this.InUnsafeRegion && !this.Flags.Includes(BinderFlags.AllowAwaitInUnsafeContext))
            {
                Error(diagnostics, ErrorCode.ERR_AwaitInUnsafeContext, nodeOrToken.GetLocation()!);
                return true;
            }
            else if (this.Flags.Includes(BinderFlags.InLockBody))
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaitInLock, nodeOrToken.GetLocation()!);
                return true;
            }
            else if (this.Flags.Includes(BinderFlags.InCatchFilter))
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaitInCatchFilter, nodeOrToken.GetLocation()!);
                return true;
            }
            else if (this.Flags.Includes(BinderFlags.InFinallyBlock) &&
                (nodeOrToken.SyntaxTree as CSharpSyntaxTree)?.Options?.IsFeatureEnabled(MessageID.IDS_AwaitInCatchAndFinally) == false)
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaitInFinally, nodeOrToken.GetLocation()!);
                return true;
            }
            else if (this.Flags.Includes(BinderFlags.InCatchBlock) &&
                (nodeOrToken.SyntaxTree as CSharpSyntaxTree)?.Options?.IsFeatureEnabled(MessageID.IDS_AwaitInCatchAndFinally) == false)
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaitInCatch, nodeOrToken.GetLocation()!);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Finds and validates the required members of an awaitable expression, as described in spec 7.7.7.1.
        /// </summary>
        /// <returns>True if the expression is awaitable; false otherwise.</returns>
        internal bool GetAwaitableExpressionInfo(
            BoundExpression expression,
            out BoundExpression? getAwaiterGetResultCall,
            out BoundCall? runtimeAsyncAwaitCall,
            SyntaxNode node,
            BindingDiagnosticBag diagnostics)
        {
            return GetAwaitableExpressionInfo(expression, expression, out _, out _, out _, out _, out getAwaiterGetResultCall, out runtimeAsyncAwaitCall, out _, node, diagnostics);
        }

        private bool GetAwaitableExpressionInfo(
            BoundExpression expression,
            BoundExpression getAwaiterArgument,
            out bool isDynamic,
            out BoundExpression? getAwaiter,
            out PropertySymbol? isCompleted,
            out MethodSymbol? getResult,
            out BoundExpression? getAwaiterGetResultCall,
            out BoundCall? runtimeAsyncAwaitCall,
            out BoundAwaitableValuePlaceholder? runtimeAsyncAwaitCallPlaceholder,
            SyntaxNode node,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(TypeSymbol.Equals(expression.Type, getAwaiterArgument.Type, TypeCompareKind.ConsiderEverything));

            isDynamic = false;
            getAwaiter = null;
            isCompleted = null;
            getResult = null;
            getAwaiterGetResultCall = null;
            runtimeAsyncAwaitCall = null;
            runtimeAsyncAwaitCallPlaceholder = null;

            if (!ValidateAwaitedExpression(expression, node, diagnostics))
            {
                return false;
            }

            if (expression.HasDynamicType())
            {
                // https://github.com/dotnet/roslyn/issues/79762: Handle runtime async here
                isDynamic = true;
                return true;
            }

            var isRuntimeAsyncEnabled = Compilation.IsRuntimeAsyncEnabledIn(this.ContainingMemberOrLambda);

            // When RuntimeAsync is enabled, we first check for whether there is an AsyncHelpers.Await method that can handle the expression.

            if (isRuntimeAsyncEnabled && tryGetRuntimeAwaitHelper(expression, out runtimeAsyncAwaitCallPlaceholder, out runtimeAsyncAwaitCall, diagnostics))
            {
                return true;
            }

            if (!GetGetAwaiterMethod(getAwaiterArgument, node, diagnostics, out getAwaiter))
            {
                return false;
            }

            TypeSymbol awaiterType = getAwaiter.Type!;
            return GetIsCompletedProperty(awaiterType, node, expression.Type!, diagnostics, out isCompleted)
                && AwaiterImplementsINotifyCompletion(awaiterType, node, diagnostics)
                && GetGetResultMethod(getAwaiter, node, expression.Type!, diagnostics, out getResult, out getAwaiterGetResultCall)
                && (!isRuntimeAsyncEnabled || getRuntimeAwaitAwaiter(awaiterType, out runtimeAsyncAwaitCall, out runtimeAsyncAwaitCallPlaceholder, expression.Syntax, diagnostics));

            bool tryGetRuntimeAwaitHelper(BoundExpression expression, out BoundAwaitableValuePlaceholder? placeholder, out BoundCall? runtimeAwaitCall, BindingDiagnosticBag diagnostics)
            {
                // For any `await expr` with where `expr` has type `E`, the compiler will attempt to match it to a helper method in `System.Runtime.CompilerServices.AsyncHelpers`. The following algorithm is used:

                // 1. If `E` has generic arity greater than 1, no match is found and instead move to [await any other type].
                // 2. `System.Runtime.CompilerServices.AsyncHelpers` from corelib (the library that defines `System.Object` and has no references) is fetched.
                // 3. All methods named `Await` are put into a group called `M`.
                // 4. For every `Mi` in `M`:
                //    1. If `Mi`'s generic arity does not match `E`, it is removed.
                //    2. If `Mi` takes more than 1 parameter (named `P`), it is removed.
                //    3. If `Mi` has a generic arity of 0, all of the following must be true, or `Mi` is removed:
                //       1. The return type is `System.Void`
                //       2. There is an identity or implicit reference conversion from `E` to the type of `P`.
                //    4. Otherwise, if `Mi` has a generic arity of 1 with type param `Tm`, all of the following must be true, or `Mi` is removed:
                //      2. The generic parameter of `E` is `Te`
                //      3. `Ti` satisfies any constraints on `Tm`
                //      4. `Mie` is `Mi` with `Te` substituted for `Tm`, and `Pe` is the resulting parameter of `Mie`
                //      5. There is an identity or implicit reference conversion from `E` to the type of `Pe`
                // 6. If only one `Mi` remains, that method is used for the following rewrites. Otherwise, we instead move to [await any other type].
                runtimeAwaitCall = null;
                placeholder = null;

                if (expression.Type is not NamedTypeSymbol { Arity: 0 or 1 } exprType)
                {
                    return false;
                }

                var asyncHelpersType = GetSpecialType(InternalSpecialType.System_Runtime_CompilerServices_AsyncHelpers, diagnostics, expression.Syntax);
                if (asyncHelpersType.IsErrorType())
                {
                    return false;
                }

                var awaitMembers = asyncHelpersType.GetMembers("Await");

                foreach (var member in awaitMembers)
                {
                    if (!isApplicableMethod(exprType, member, node, diagnostics, this, out MethodSymbol? method, out Conversion argumentConversion))
                    {
                        continue;
                    }

                    if (runtimeAwaitCall is not null)
                    {
                        runtimeAwaitCall = null;
                        placeholder = null;
                        return false;
                    }

                    placeholder = new BoundAwaitableValuePlaceholder(expression.Syntax, expression.Type);

                    BoundExpression argument = CreateConversion(placeholder, argumentConversion, destination: method.Parameters[0].Type, diagnostics);

                    if (argument is BoundConversion)
                    {
                        argument.WasCompilerGenerated = true;
                    }

                    runtimeAwaitCall = new BoundCall(
                        expression.Syntax,
                        receiverOpt: null,
                        initialBindingReceiverIsSubjectToCloning: ThreeState.False,
                        method,
                        [argument],
                        argumentNamesOpt: default,
                        argumentRefKindsOpt: default,
                        isDelegateCall: false,
                        expanded: false,
                        invokedAsExtensionMethod: false,
                        argsToParamsOpt: default,
                        defaultArguments: default,
                        resultKind: LookupResultKind.Viable,
                        method.ReturnType)
                    {
                        WasCompilerGenerated = true
                    };
                }

                if (runtimeAwaitCall is null)
                {
                    return false;
                }

                reportObsoleteDiagnostics(this, diagnostics, runtimeAwaitCall.Method, expression.Syntax);
                return true;

                static bool isApplicableMethod(
                    NamedTypeSymbol exprType,
                    Symbol member,
                    SyntaxNode node,
                    BindingDiagnosticBag diagnostics,
                    Binder @this,
                    [NotNullWhen(true)] out MethodSymbol? awaitMethod,
                    out Conversion conversion)
                {
                    conversion = default;
                    awaitMethod = null;
                    if (member is not MethodSymbol method
                        || method.Arity != exprType.Arity
                        || method.ParameterCount != 1)
                    {
                        return false;
                    }

                    if (method.Arity == 0)
                    {
                        if (method.ReturnsVoid && isValidConversion(exprType, method, node, diagnostics, @this, out conversion))
                        {
                            awaitMethod = method;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        var unsubstitutedReturnType = method.ReturnType;
                        if ((object)unsubstitutedReturnType != method.TypeArgumentsWithAnnotations[0].Type)
                        {
                            return false;
                        }

                        var substitutedMethod = method.Construct(exprType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics);
                        var tempDiagnostics = BindingDiagnosticBag.GetInstance(diagnostics);
                        if (!ConstraintsHelper.CheckConstraints(
                            substitutedMethod,
                            new ConstraintsHelper.CheckConstraintsArgs(@this.Compilation, @this.Conversions, includeNullability: false, node.Location, tempDiagnostics)))
                        {
                            tempDiagnostics.Free();
                            return false;
                        }

                        if (!isValidConversion(exprType, substitutedMethod, node, diagnostics, @this, out conversion))
                        {
                            tempDiagnostics.Free();
                            return false;
                        }

                        awaitMethod = substitutedMethod;
                        diagnostics.AddRangeAndFree(tempDiagnostics);
                        return true;
                    }
                }

                static bool isValidConversion(TypeSymbol exprType, MethodSymbol method, SyntaxNode node, BindingDiagnosticBag diagnostics, Binder @this, out Conversion conversion)
                {
                    CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = @this.GetNewCompoundUseSiteInfo(diagnostics);
                    conversion = @this.Conversions.ClassifyImplicitConversionFromType(
                        exprType,
                        method.Parameters[0].Type,
                        ref useSiteInfo);

                    var result = conversion is { IsImplicit: true, Kind: ConversionKind.Identity or ConversionKind.ImplicitReference };
                    if (result)
                    {
                        diagnostics.Add(node, useSiteInfo);
                    }

                    return result;
                }
            }

            bool getRuntimeAwaitAwaiter(TypeSymbol awaiterType, out BoundCall? runtimeAwaitAwaiterCall, out BoundAwaitableValuePlaceholder? placeholder, SyntaxNode syntax, BindingDiagnosticBag diagnostics)
            {
                // Use site info is discarded because we don't actually do this conversion, we just need to know which generic
                // method to call. The helpers are generic, so the final call will actually just be an identity conversion.
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                var useUnsafeAwait = Compilation.Conversions.ClassifyImplicitConversionFromType(
                    awaiterType,
                    Compilation.GetSpecialType(InternalSpecialType.System_Runtime_CompilerServices_ICriticalNotifyCompletion),
                    ref discardedUseSiteInfo).IsImplicit;

                var awaitMethod = (MethodSymbol?)GetSpecialTypeMember(
                    useUnsafeAwait
                        ? SpecialMember.System_Runtime_CompilerServices_AsyncHelpers__UnsafeAwaitAwaiter_TAwaiter
                        : SpecialMember.System_Runtime_CompilerServices_AsyncHelpers__AwaitAwaiter_TAwaiter,
                    diagnostics,
                    syntax);

                if (awaitMethod is null)
                {
                    runtimeAwaitAwaiterCall = null;
                    placeholder = null;
                    return false;
                }

                Debug.Assert(awaitMethod is { Arity: 1 });

                var runtimeAwaitAwaiterMethod = awaitMethod.Construct(awaiterType);
                ConstraintsHelper.CheckConstraints(
                    runtimeAwaitAwaiterMethod,
                    new ConstraintsHelper.CheckConstraintsArgs(this.Compilation, this.Conversions, includeNullability: false, syntax.Location, diagnostics));

                reportObsoleteDiagnostics(this, diagnostics, runtimeAwaitAwaiterMethod, syntax);

                placeholder = new BoundAwaitableValuePlaceholder(syntax, awaiterType);

                runtimeAwaitAwaiterCall = new BoundCall(
                    syntax,
                    receiverOpt: null,
                    initialBindingReceiverIsSubjectToCloning: ThreeState.False,
                    runtimeAwaitAwaiterMethod,
                    [placeholder],
                    argumentNamesOpt: default,
                    argumentRefKindsOpt: default,
                    isDelegateCall: false,
                    expanded: false,
                    invokedAsExtensionMethod: false,
                    argsToParamsOpt: default,
                    defaultArguments: default,
                    resultKind: LookupResultKind.Viable,
                    runtimeAwaitAwaiterMethod.ReturnType)
                {
                    WasCompilerGenerated = true
                };

                return true;
            }

            static void reportObsoleteDiagnostics(Binder @this, BindingDiagnosticBag diagnostics, MethodSymbol method, SyntaxNode syntax)
            {
                @this.ReportDiagnosticsIfObsolete(diagnostics, method, syntax, hasBaseReceiver: false);
                @this.ReportDiagnosticsIfObsolete(diagnostics, method.ContainingType, syntax, hasBaseReceiver: false);
            }
        }

        /// <summary>
        /// Validates the awaited expression, returning true if no errors are found.
        /// </summary>
        private static bool ValidateAwaitedExpression(BoundExpression expression, SyntaxNode node, BindingDiagnosticBag diagnostics)
        {
            if (expression.HasAnyErrors)
            {
                // The appropriate diagnostics have already been reported.
                return false;
            }

            if (expression.Type is null)
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaitArgIntrinsic, node, expression.Display);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Finds the GetAwaiter method of an awaitable expression.
        /// </summary>
        /// <remarks>
        /// Spec 7.7.7.1:
        /// An awaitable expression t has an accessible instance or extension method called GetAwaiter with no
        /// parameters and no type parameters, and a return type A that meets the additional requirements for an
        /// Awaiter.
        /// NOTE: this is an error in the spec.  An extension method of the form
        /// Awaiter&lt;T&gt; GetAwaiter&lt;T&gt;(this Task&lt;T&gt;) may be used.
        /// </remarks>
        private bool GetGetAwaiterMethod(BoundExpression expression, SyntaxNode node, BindingDiagnosticBag diagnostics, [NotNullWhen(true)] out BoundExpression? getAwaiterCall)
        {
            RoslynDebug.Assert(expression.Type is object);
            if (expression.Type.IsVoidType())
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaitArgVoidCall, node);
                getAwaiterCall = null;
                return false;
            }

            getAwaiterCall = MakeInvocationExpression(node, expression, WellKnownMemberNames.GetAwaiter, ImmutableArray<BoundExpression>.Empty, diagnostics);
            if (getAwaiterCall.HasAnyErrors) // && !expression.HasAnyErrors?
            {
                getAwaiterCall = null;
                return false;
            }

            if (getAwaiterCall.Kind != BoundKind.Call)
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaitArg, node, expression.Type);
                getAwaiterCall = null;
                return false;
            }

            var call = (BoundCall)getAwaiterCall;
            Debug.Assert(!call.IsErroneousNode);

            var getAwaiterMethod = call.Method;
            if (getAwaiterMethod is ErrorMethodSymbol ||
                call.Expanded || HasOptionalParameters(getAwaiterMethod) || // We might have been able to resolve a GetAwaiter overload with optional parameters, so check for that here
                getAwaiterMethod.ReturnsVoid) // If GetAwaiter returns void, don't bother checking that it returns an Awaiter.
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaitArg, node, expression.Type);
                getAwaiterCall = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Finds the IsCompleted property of an Awaiter type.
        /// </summary>
        /// <remarks>
        /// Spec 7.7.7.1:
        /// An Awaiter A has an accessible, readable instance property IsCompleted of type bool.
        /// </remarks>
        private bool GetIsCompletedProperty(TypeSymbol awaiterType, SyntaxNode node, TypeSymbol awaitedExpressionType, BindingDiagnosticBag diagnostics, [NotNullWhen(true)] out PropertySymbol? isCompletedProperty)
        {
            var receiver = new BoundLiteral(node, ConstantValue.Null, awaiterType);
            var name = WellKnownMemberNames.IsCompleted;
            var qualified = BindInstanceMemberAccess(node, node, receiver, name, 0, default(SeparatedSyntaxList<TypeSyntax>), default(ImmutableArray<TypeWithAnnotations>), invoked: false, indexed: false, diagnostics);
            if (qualified.HasAnyErrors)
            {
                isCompletedProperty = null;
                return false;
            }

            if (qualified is not BoundPropertyAccess { PropertySymbol: { } propertySymbol } || propertySymbol.IsExtensionBlockMember())
            {
                Error(diagnostics, ErrorCode.ERR_NoSuchMember, node, awaiterType, WellKnownMemberNames.IsCompleted);
                isCompletedProperty = null;
                return false;
            }

            isCompletedProperty = propertySymbol;
            if (isCompletedProperty.IsWriteOnly)
            {
                Error(diagnostics, ErrorCode.ERR_PropertyLacksGet, node, isCompletedProperty);
                isCompletedProperty = null;
                return false;
            }

            if (isCompletedProperty.Type.SpecialType != SpecialType.System_Boolean)
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaiterPattern, node, awaiterType, awaitedExpressionType);
                isCompletedProperty = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks that the Awaiter implements System.Runtime.CompilerServices.INotifyCompletion.
        /// </summary>
        /// <remarks>
        /// Spec 7.7.7.1:
        /// An Awaiter A implements the interface System.Runtime.CompilerServices.INotifyCompletion.
        /// </remarks>
        private bool AwaiterImplementsINotifyCompletion(TypeSymbol awaiterType, SyntaxNode node, BindingDiagnosticBag diagnostics)
        {
            var INotifyCompletion = GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_INotifyCompletion, diagnostics, node);
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);

            var conversion = this.Conversions.ClassifyImplicitConversionFromType(awaiterType, INotifyCompletion, ref useSiteInfo);
            if (!conversion.IsImplicit)
            {
                diagnostics.Add(node, useSiteInfo);
                Error(diagnostics, ErrorCode.ERR_DoesntImplementAwaitInterface, node, awaiterType, INotifyCompletion);
                return false;
            }

            Debug.Assert(conversion.IsValid);
            return true;
        }

        /// <summary>
        /// Finds the GetResult method of an Awaiter type.
        /// </summary>
        /// <remarks>
        /// Spec 7.7.7.1:
        /// An Awaiter A has an accessible instance method GetResult with no parameters and no type parameters.
        /// </remarks>
        private bool GetGetResultMethod(BoundExpression awaiterExpression, SyntaxNode node, TypeSymbol awaitedExpressionType, BindingDiagnosticBag diagnostics, out MethodSymbol? getResultMethod, [NotNullWhen(true)] out BoundExpression? getAwaiterGetResultCall)
        {
            var awaiterType = awaiterExpression.Type;
            getAwaiterGetResultCall = MakeInvocationExpression(node, awaiterExpression, WellKnownMemberNames.GetResult, ImmutableArray<BoundExpression>.Empty, diagnostics);
            if (getAwaiterGetResultCall.HasAnyErrors)
            {
                getResultMethod = null;
                getAwaiterGetResultCall = null;
                return false;
            }

            RoslynDebug.Assert(awaiterType is object);
            if (getAwaiterGetResultCall.Kind != BoundKind.Call)
            {
                Error(diagnostics, ErrorCode.ERR_NoSuchMember, node, awaiterType, WellKnownMemberNames.GetResult);
                getResultMethod = null;
                getAwaiterGetResultCall = null;
                return false;
            }

            var call = (BoundCall)getAwaiterGetResultCall;
            Debug.Assert(!call.IsErroneousNode);

            getResultMethod = call.Method;
            if (getResultMethod.IsExtensionMethod || getResultMethod.IsExtensionBlockMember())
            {
                Error(diagnostics, ErrorCode.ERR_NoSuchMember, node, awaiterType, WellKnownMemberNames.GetResult);
                getResultMethod = null;
                getAwaiterGetResultCall = null;
                return false;
            }

            if (call.Expanded || HasOptionalParameters(getResultMethod) || getResultMethod.IsConditional)
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaiterPattern, node, awaiterType, awaitedExpressionType);
                getResultMethod = null;
                getAwaiterGetResultCall = null;
                return false;
            }

            // The lack of a GetResult method will be reported by ValidateGetResult().
            return true;
        }

        private static bool HasOptionalParameters(MethodSymbol method)
        {
            RoslynDebug.Assert(method != null);

            if (method.ParameterCount != 0)
            {
                var parameter = method.Parameters[method.ParameterCount - 1];
                return parameter.IsOptional;
            }

            return false;
        }
    }
}
