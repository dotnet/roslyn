// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        protected const string GetEnumeratorMethodName = WellKnownMemberNames.GetEnumeratorMethodName;
        private const string CurrentPropertyName = WellKnownMemberNames.CurrentPropertyName;
        private const string MoveNextMethodName = WellKnownMemberNames.MoveNextMethodName;
        protected const string GetAsyncEnumeratorMethodName = WellKnownMemberNames.GetAsyncEnumeratorMethodName;
        private const string MoveNextAsyncMethodName = WellKnownMemberNames.MoveNextAsyncMethodName;

        private BoundExpression UnwrapCollectionExpressionIfNullable(BoundExpression collectionExpr, DiagnosticBag diagnostics)
        {
            TypeSymbol collectionExprType = collectionExpr.Type;

            // If collectionExprType is a nullable type, then use the underlying type and take the value (i.e. .Value) of collectionExpr.
            // This behavior is not spec'd, but it's what Dev10 does.
            if ((object)collectionExprType != null && collectionExprType.IsNullableType())
            {
                SyntaxNode exprSyntax = collectionExpr.Syntax;

                MethodSymbol nullableValueGetter = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_Nullable_T_get_Value, diagnostics, exprSyntax);
                if ((object)nullableValueGetter != null)
                {
                    nullableValueGetter = nullableValueGetter.AsMember((NamedTypeSymbol)collectionExprType);

                    // Synthesized call, because we don't want to modify the type in the SemanticModel.
                    return BoundCall.Synthesized(
                        syntax: exprSyntax,
                        receiverOpt: collectionExpr,
                        method: nullableValueGetter);
                }
                else
                {
                    return new BoundBadExpression(
                            exprSyntax,
                            LookupResultKind.Empty,
                            ImmutableArray<Symbol>.Empty,
                            ImmutableArray.Create(collectionExpr),
                            collectionExprType.GetNullableUnderlyingType())
                        { WasCompilerGenerated = true }; // Don't affect the type in the SemanticModel.
                }
            }

            return collectionExpr;
        }

        protected enum EnumeratorResult
        {
            Succeeded,
            FailedNotReported,
            FailedAndReported
        }

        protected EnumeratorResult GetEnumeratorInfo(SyntaxNode syntax, SyntaxNode expr, ref ForEachEnumeratorInfo.Builder builder, ref BoundExpression collectionExpr, bool isAsync, DiagnosticBag diagnostics)
        {
            TypeSymbol collectionExprType = collectionExpr.Type;

            if (collectionExprType is null) // There's no way to enumerate something without a type.
            {
                if (!ReportConstantNullCollectionExpr(expr, collectionExpr, diagnostics))
                {
                    // Anything else with a null type is a method group or anonymous function
                    diagnostics.Add(ErrorCode.ERR_AnonMethGrpInForEach, expr.Location, collectionExpr.Display);
                }

                // CONSIDER: dev10 also reports ERR_ForEachMissingMember (i.e. failed pattern match).
                return EnumeratorResult.FailedAndReported;
            }

            if (collectionExpr.ResultKind == LookupResultKind.NotAValue)
            {
                // Short-circuiting to prevent strange behavior in the case where the collection
                // expression is a type expression and the type is enumerable.
                Debug.Assert(collectionExpr.HasAnyErrors); // should already have been reported
                return EnumeratorResult.FailedAndReported;
            }

            if (collectionExprType.Kind == SymbolKind.DynamicType && isAsync)
            {
                diagnostics.Add(ErrorCode.ERR_BadDynamicAwaitForEach, expr.Location);
                return EnumeratorResult.FailedAndReported;
            }

            // The spec specifically lists the collection, enumerator, and element types for arrays and dynamic.
            if (collectionExprType.Kind == SymbolKind.ArrayType || collectionExprType.Kind == SymbolKind.DynamicType)
            {
                if (ReportConstantNullCollectionExpr(expr, collectionExpr, diagnostics))
                {
                    return EnumeratorResult.FailedAndReported;
                }
                builder = GetDefaultEnumeratorInfo(syntax, builder, diagnostics, collectionExprType);
                return EnumeratorResult.Succeeded;
            }

            var unwrappedCollectionExpr = UnwrapCollectionExpressionIfNullable(collectionExpr, diagnostics);
            var unwrappedCollectionExprType = unwrappedCollectionExpr.Type;

            if (SatisfiesGetEnumeratorPattern(syntax, ref builder, unwrappedCollectionExpr, isAsync, viaExtensionMethod: false, diagnostics, expr))
            {
                collectionExpr = unwrappedCollectionExpr;
                if (ReportConstantNullCollectionExpr(expr, collectionExpr, diagnostics))
                {
                    return EnumeratorResult.FailedAndReported;
                }
                return createPatternBasedEnumeratorResult(ref builder, unwrappedCollectionExpr, isAsync, viaExtensionMethod: false, diagnostics);
            }

            if (!isAsync && IsIEnumerable(unwrappedCollectionExprType))
            {
                collectionExpr = unwrappedCollectionExpr;
                // This indicates a problem with the special IEnumerable type - it should have satisfied the GetEnumerator pattern.
                diagnostics.Add(ErrorCode.ERR_ForEachMissingMember, expr.Location, unwrappedCollectionExprType, GetEnumeratorMethodName);
                return EnumeratorResult.FailedAndReported;
            }
            if (isAsync && IsIAsyncEnumerable(unwrappedCollectionExprType))
            {
                collectionExpr = unwrappedCollectionExpr;
                // This indicates a problem with the well-known IAsyncEnumerable type - it should have satisfied the GetAsyncEnumerator pattern.
                diagnostics.Add(ErrorCode.ERR_AwaitForEachMissingMember, expr.Location, unwrappedCollectionExprType, GetAsyncEnumeratorMethodName);
                return EnumeratorResult.FailedAndReported;
            }

            if (SatisfiesIEnumerableInterfaces(ref builder, unwrappedCollectionExpr, isAsync, diagnostics, unwrappedCollectionExprType, expr) is not EnumeratorResult.FailedNotReported and var result)
            {
                collectionExpr = unwrappedCollectionExpr;
                return result;
            }

            // COMPAT:
            // In some rare cases, like MicroFramework, System.String does not implement foreach pattern.
            // For compat reasons we must still treat System.String as valid to use in a foreach
            // Similarly to the cases with array and dynamic, we will default to IEnumerable for binding purposes.
            // Lowering will not use iterator info with strings, so it is ok.
            if (!isAsync && collectionExprType.SpecialType == SpecialType.System_String)
            {
                if (ReportConstantNullCollectionExpr(expr, collectionExpr, diagnostics))
                {
                    return EnumeratorResult.FailedAndReported;
                }

                builder = GetDefaultEnumeratorInfo(syntax, builder, diagnostics, collectionExprType);
                return EnumeratorResult.Succeeded;
            }

            if (SatisfiesGetEnumeratorPattern(syntax, ref builder, collectionExpr, isAsync, viaExtensionMethod: true, diagnostics, expr))
            {
                return createPatternBasedEnumeratorResult(ref builder, collectionExpr, isAsync, viaExtensionMethod: true, diagnostics);
            }

            return EnumeratorResult.FailedNotReported;

            EnumeratorResult createPatternBasedEnumeratorResult(ref ForEachEnumeratorInfo.Builder builder, BoundExpression collectionExpr, bool isAsync, bool viaExtensionMethod, DiagnosticBag diagnostics)
            {
                Debug.Assert((object)builder.GetEnumeratorInfo != null);

                Debug.Assert(!(viaExtensionMethod && builder.GetEnumeratorInfo.Method.Parameters.IsDefaultOrEmpty));

                builder.CollectionType = viaExtensionMethod
                    ? builder.GetEnumeratorInfo.Method.Parameters[0].Type
                    : collectionExpr.Type;

                if (SatisfiesForEachPattern(syntax, ref builder, isAsync, diagnostics, expr))
                {
                    builder.ElementTypeWithAnnotations = ((PropertySymbol)builder.CurrentPropertyGetter.AssociatedSymbol).TypeWithAnnotations;

                    GetDisposalInfoForEnumerator(syntax, ref builder, isAsync, diagnostics);

                    return EnumeratorResult.Succeeded;
                }

                MethodSymbol getEnumeratorMethod = builder.GetEnumeratorInfo.Method;
                diagnostics.Add(isAsync ? ErrorCode.ERR_BadGetAsyncEnumerator : ErrorCode.ERR_BadGetEnumerator, expr.Location, getEnumeratorMethod.ReturnType, getEnumeratorMethod);
                return EnumeratorResult.FailedAndReported;
            }
        }

        private EnumeratorResult SatisfiesIEnumerableInterfaces(ref ForEachEnumeratorInfo.Builder builder, BoundExpression collectionExpr, bool isAsync, DiagnosticBag diagnostics, TypeSymbol unwrappedCollectionExprType, SyntaxNode expr)
        {
            if (!AllInterfacesContainsIEnumerable(ref builder, unwrappedCollectionExprType, isAsync, diagnostics, out bool foundMultipleGenericIEnumerableInterfaces, expr))
            {
                return EnumeratorResult.FailedNotReported;
            }

            if (ReportConstantNullCollectionExpr(expr, collectionExpr, diagnostics))
            {
                return EnumeratorResult.FailedAndReported;
            }

            SyntaxNode errorLocationSyntax = expr;

            if (foundMultipleGenericIEnumerableInterfaces)
            {
                diagnostics.Add(isAsync ? ErrorCode.ERR_MultipleIAsyncEnumOfT : ErrorCode.ERR_MultipleIEnumOfT, errorLocationSyntax.Location, unwrappedCollectionExprType,
                    isAsync ?
                        this.Compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerable_T) :
                        this.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T));
                return EnumeratorResult.FailedAndReported;
            }

            Debug.Assert((object)builder.CollectionType != null);

            NamedTypeSymbol collectionType = (NamedTypeSymbol)builder.CollectionType;
            if (collectionType.IsGenericType)
            {
                // If the type is generic, we have to search for the methods
                builder.ElementTypeWithAnnotations = collectionType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Single();

                MethodSymbol getEnumeratorMethod;
                if (isAsync)
                {
                    Debug.Assert(IsIAsyncEnumerable(collectionType.OriginalDefinition));

                    getEnumeratorMethod = (MethodSymbol)GetWellKnownTypeMember(Compilation, WellKnownMember.System_Collections_Generic_IAsyncEnumerable_T__GetAsyncEnumerator,
                        diagnostics, errorLocationSyntax.Location, isOptional: false);

                    // Well-known members are matched by signature: we shouldn't find it if it doesn't have exactly 1 parameter.
                    Debug.Assert(getEnumeratorMethod is null or { ParameterCount: 1 });

                    if (getEnumeratorMethod?.Parameters[0].IsOptional == false)
                    {
                        // This indicates a problem with the well-known IAsyncEnumerable type - it should have an optional cancellation token.
                        diagnostics.Add(ErrorCode.ERR_AwaitForEachMissingMember, expr.Location, unwrappedCollectionExprType, GetAsyncEnumeratorMethodName);
                        return EnumeratorResult.FailedAndReported;
                    }
                }
                else
                {
                    Debug.Assert(collectionType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
                    getEnumeratorMethod = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_Collections_Generic_IEnumerable_T__GetEnumerator, diagnostics, errorLocationSyntax);
                }

                MethodSymbol moveNextMethod = null;
                if ((object)getEnumeratorMethod != null)
                {
                    MethodSymbol specificGetEnumeratorMethod = getEnumeratorMethod.AsMember(collectionType);
                    TypeSymbol enumeratorType = specificGetEnumeratorMethod.ReturnType;

                    // IAsyncEnumerable<T>.GetAsyncEnumerator has a default param, so let's fill it in
                    builder.GetEnumeratorInfo = BindDefaultArguments(
                        specificGetEnumeratorMethod,
                        extensionReceiverOpt: null,
                        expanded: false,
                        collectionExpr.Syntax,
                        diagnostics,
                        // C# 8 shipped allowing the CancellationToken of `IAsyncEnumerable.GetAsyncEnumerator` to be non-optional,
                        // filling in a default value in that case. https://github.com/dotnet/roslyn/issues/50182 tracks making
                        // this an error and breaking the scenario.
                        assertMissingParametersAreOptional: false);

                    MethodSymbol currentPropertyGetter;
                    if (isAsync)
                    {
                        Debug.Assert(enumeratorType.OriginalDefinition.Equals(Compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerator_T)));

                        MethodSymbol moveNextAsync = (MethodSymbol)GetWellKnownTypeMember(Compilation, WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__MoveNextAsync,
                            diagnostics, errorLocationSyntax.Location, isOptional: false);

                        if ((object)moveNextAsync != null)
                        {
                            moveNextMethod = moveNextAsync.AsMember((NamedTypeSymbol)enumeratorType);
                        }

                        currentPropertyGetter = (MethodSymbol)GetWellKnownTypeMember(Compilation, WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__get_Current, diagnostics, errorLocationSyntax.Location, isOptional: false);
                    }
                    else
                    {
                        currentPropertyGetter = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_Collections_Generic_IEnumerator_T__get_Current, diagnostics, errorLocationSyntax);
                    }

                    if ((object)currentPropertyGetter != null)
                    {
                        builder.CurrentPropertyGetter = currentPropertyGetter.AsMember((NamedTypeSymbol)enumeratorType);
                    }
                }

                if (!isAsync)
                {
                    // NOTE: MoveNext is actually inherited from System.Collections.IEnumerator
                    moveNextMethod = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__MoveNext, diagnostics, errorLocationSyntax);
                }

                // We're operating with well-known members: we know MoveNext/MoveNextAsync have no parameters
                if (moveNextMethod is not null)
                {
                    builder.MoveNextInfo = MethodArgumentInfo.CreateParameterlessMethod(moveNextMethod);
                }
            }
            else
            {
                // Non-generic - use special members to avoid re-computing
                Debug.Assert(collectionType.SpecialType == SpecialType.System_Collections_IEnumerable);

                builder.GetEnumeratorInfo = GetParameterlessSpecialTypeMemberInfo(SpecialMember.System_Collections_IEnumerable__GetEnumerator, errorLocationSyntax, diagnostics);
                builder.CurrentPropertyGetter = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__get_Current, diagnostics, errorLocationSyntax);
                builder.MoveNextInfo = GetParameterlessSpecialTypeMemberInfo(SpecialMember.System_Collections_IEnumerator__MoveNext, errorLocationSyntax, diagnostics);
                builder.ElementTypeWithAnnotations = builder.CurrentPropertyGetter?.ReturnTypeWithAnnotations ?? TypeWithAnnotations.Create(GetSpecialType(SpecialType.System_Object, diagnostics, errorLocationSyntax));

                Debug.Assert((object)builder.GetEnumeratorInfo == null ||
                             builder.GetEnumeratorInfo.Method.ReturnType.SpecialType == SpecialType.System_Collections_IEnumerator);
            }

            // We don't know the runtime type, so we will have to insert a runtime check for IDisposable (with a conditional call to IDisposable.Dispose).
            builder.NeedsDisposal = true;
            return EnumeratorResult.Succeeded;
        }

        private static bool ReportConstantNullCollectionExpr(SyntaxNode expr, BoundExpression collectionExpr, DiagnosticBag diagnostics)
        {
            if (collectionExpr.ConstantValue is { IsNull: true })
            {
                // Spec seems to refer to null literals, but Dev10 reports anything known to be null.
                diagnostics.Add(ErrorCode.ERR_NullNotValid, expr.Location);
                return true;
            }
            return false;
        }

        private void GetDisposalInfoForEnumerator(SyntaxNode syntax, ref ForEachEnumeratorInfo.Builder builder, bool isAsync, DiagnosticBag diagnostics)
        {
            // NOTE: if IDisposable is not available at all, no diagnostics will be reported - we will just assume that
            // the enumerator is not disposable.  If it has IDisposable in its interface list, there will be a diagnostic there.
            // If IDisposable is available but its Dispose method is not, then diagnostics will be reported only if the enumerator
            // is potentially disposable.

            TypeSymbol enumeratorType = builder.GetEnumeratorInfo.Method.ReturnType;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            // For async foreach, we don't do the runtime check
            if ((!enumeratorType.IsSealed && !isAsync) ||
                this.Conversions.ClassifyImplicitConversionFromType(enumeratorType,
                    isAsync ? this.Compilation.GetWellKnownType(WellKnownType.System_IAsyncDisposable) : this.Compilation.GetSpecialType(SpecialType.System_IDisposable),
                    ref useSiteDiagnostics).IsImplicit)
            {
                builder.NeedsDisposal = true;
            }
            else if (Compilation.IsFeatureEnabled(MessageID.IDS_FeatureUsingDeclarations) &&
                     (enumeratorType.IsRefLikeType || isAsync))
            {
                // if it wasn't directly convertable to IDisposable, see if it is pattern-disposable
                // again, we throw away any binding diagnostics, and assume it's not disposable if we encounter errors
                var patternDisposeDiags = new DiagnosticBag();
                var receiver = new BoundDisposableValuePlaceholder(syntax, enumeratorType);
                MethodSymbol disposeMethod = TryFindDisposePatternMethod(receiver, syntax, isAsync, patternDisposeDiags);
                if (disposeMethod is object)
                {
                    Debug.Assert(!disposeMethod.IsExtensionMethod);
                    Debug.Assert(disposeMethod.ParameterRefKinds.IsDefaultOrEmpty);

                    var argsBuilder = ArrayBuilder<BoundExpression>.GetInstance(disposeMethod.ParameterCount);
                    var argsToParams = default(ImmutableArray<int>);
                    bool expanded = disposeMethod.HasParamsParameter();

                    BindDefaultArguments(
                        syntax,
                        disposeMethod.Parameters,
                        argsBuilder,
                        argumentRefKindsBuilder: null,
                        ref argsToParams,
                        out BitVector defaultArguments,
                        expanded,
                        enableCallerInfo: true,
                        diagnostics);

                    builder.NeedsDisposal = true;
                    builder.PatternDisposeInfo = new MethodArgumentInfo(disposeMethod, argsBuilder.ToImmutableAndFree(), argsToParams, defaultArguments, expanded);
                }
                patternDisposeDiags.Free();
            }

            diagnostics.Add(syntax, useSiteDiagnostics);
        }

        private ForEachEnumeratorInfo.Builder GetDefaultEnumeratorInfo(SyntaxNode syntax, ForEachEnumeratorInfo.Builder builder, DiagnosticBag diagnostics, TypeSymbol collectionExprType)
        {
            // NOTE: for arrays, we won't actually use any of these members - they're just for the API.
            builder.CollectionType = GetSpecialType(SpecialType.System_Collections_IEnumerable, diagnostics, syntax);

            if (collectionExprType.IsDynamic())
            {
                builder.ElementTypeWithAnnotations = TypeWithAnnotations.Create(
                    ((syntax as ForEachStatementSyntax)?.Type.IsVar == true) ?
                        (TypeSymbol)DynamicTypeSymbol.Instance :
                        GetSpecialType(SpecialType.System_Object, diagnostics, syntax));
            }
            else
            {
                builder.ElementTypeWithAnnotations = collectionExprType.SpecialType == SpecialType.System_String ?
                    TypeWithAnnotations.Create(GetSpecialType(SpecialType.System_Char, diagnostics, syntax)) :
                    ((ArrayTypeSymbol)collectionExprType).ElementTypeWithAnnotations;
            }

            // CONSIDER: 
            // For arrays and string none of these members will actually be emitted, so it seems strange to prevent compilation if they can't be found.
            // skip this work in the batch case?
            builder.GetEnumeratorInfo = GetParameterlessSpecialTypeMemberInfo(SpecialMember.System_Collections_IEnumerable__GetEnumerator, syntax, diagnostics);
            builder.CurrentPropertyGetter = (MethodSymbol)GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__get_Current, diagnostics, syntax);
            builder.MoveNextInfo = GetParameterlessSpecialTypeMemberInfo(SpecialMember.System_Collections_IEnumerator__MoveNext, syntax, diagnostics);

            Debug.Assert((object)builder.GetEnumeratorInfo == null ||
                         TypeSymbol.Equals(builder.GetEnumeratorInfo.Method.ReturnType, this.Compilation.GetSpecialType(SpecialType.System_Collections_IEnumerator), TypeCompareKind.ConsiderEverything2));

            // We don't know the runtime type, so we will have to insert a runtime check for IDisposable (with a conditional call to IDisposable.Dispose).
            builder.NeedsDisposal = true;
            return builder;
        }

        /// <summary>
        /// Check for a GetEnumerator (or GetAsyncEnumerator) method on collectionExprType.  Failing to satisfy the pattern is not an error -
        /// it just means that we have to check for an interface instead.
        /// </summary>
        /// <param name="collectionExpr">Expression over which to iterate.</param>
        /// <param name="diagnostics">Populated with *warnings* if there are near misses.</param>
        /// <param name="expr"></param>
        /// <param name="builder">Builder to fill in. <see cref="ForEachEnumeratorInfo.Builder.GetEnumeratorInfo"/> set if the pattern in satisfied.</param>
        /// <returns>True if the method was found (still have to verify that the return (i.e. enumerator) type is acceptable).</returns>
        /// <remarks>
        /// Only adds warnings, so does not affect control flow (i.e. no need to check for failure).
        /// </remarks>
        private bool SatisfiesGetEnumeratorPattern(SyntaxNode syntax, ref ForEachEnumeratorInfo.Builder builder, BoundExpression collectionExpr, bool isAsync, bool viaExtensionMethod, DiagnosticBag diagnostics, SyntaxNode expr)
        {
            string methodName = isAsync ? GetAsyncEnumeratorMethodName : GetEnumeratorMethodName;
            MethodArgumentInfo getEnumeratorInfo;
            if (viaExtensionMethod)
            {
                getEnumeratorInfo = FindForEachPatternMethodViaExtension(syntax, expr, collectionExpr, methodName, diagnostics);
            }
            else
            {
                var lookupResult = LookupResult.GetInstance();
                getEnumeratorInfo = FindForEachPatternMethod(syntax, collectionExpr.Type, methodName, lookupResult, warningsOnly: true, diagnostics, isAsync, expr);
                lookupResult.Free();
            }

            builder.GetEnumeratorInfo = getEnumeratorInfo;
            return (object)getEnumeratorInfo != null;
        }

        /// <summary>
        /// Perform a lookup for the specified method on the specified type.  Perform overload resolution
        /// on the lookup results.
        /// </summary>
        /// <param name="patternType">Type to search.</param>
        /// <param name="methodName">Method to search for.</param>
        /// <param name="lookupResult">Passed in for reusability.</param>
        /// <param name="warningsOnly">True if failures should result in warnings; false if they should result in errors.</param>
        /// <param name="diagnostics">Populated with binding diagnostics.</param>
        /// <returns>The desired method or null.</returns>
        private MethodArgumentInfo FindForEachPatternMethod(SyntaxNode syntax, TypeSymbol patternType, string methodName, LookupResult lookupResult, bool warningsOnly, DiagnosticBag diagnostics, bool isAsync, SyntaxNode expr)
        {
            Debug.Assert(lookupResult.IsClear);

            // Not using LookupOptions.MustBeInvocableMember because we don't want the corresponding lookup error.
            // We filter out non-methods below.
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            this.LookupMembersInType(
                lookupResult,
                patternType,
                methodName,
                arity: 0,
                basesBeingResolved: null,
                options: LookupOptions.Default,
                originalBinder: this,
                diagnose: false,
                useSiteDiagnostics: ref useSiteDiagnostics);

            diagnostics.Add(expr, useSiteDiagnostics);

            if (!lookupResult.IsMultiViable)
            {
                ReportPatternMemberLookupDiagnostics(lookupResult, patternType, methodName, warningsOnly, diagnostics, expr);
                return null;
            }

            ArrayBuilder<MethodSymbol> candidateMethods = ArrayBuilder<MethodSymbol>.GetInstance();

            foreach (Symbol member in lookupResult.Symbols)
            {
                if (member.Kind != SymbolKind.Method)
                {
                    candidateMethods.Free();

                    if (warningsOnly)
                    {
                        ReportEnumerableWarning(expr, diagnostics, patternType, member);
                    }
                    return null;
                }

                MethodSymbol method = (MethodSymbol)member;

                // SPEC VIOLATION: The spec says we should apply overload resolution, but Dev10 uses
                // some custom logic in ExpressionBinder.BindGrpToParams.  The biggest difference
                // we've found (so far) is that it only considers methods with expected number of parameters
                // (i.e. doesn't work with "params" or optional parameters).

                // Note: for pattern-based lookup for `await foreach` we accept `GetAsyncEnumerator` and
                // `MoveNextAsync` methods with optional/params parameters.
                if (method.ParameterCount == 0 || isAsync)
                {
                    candidateMethods.Add((MethodSymbol)member);
                }
            }

            MethodArgumentInfo patternInfo = PerformForEachPatternOverloadResolution(syntax, patternType, candidateMethods, warningsOnly, diagnostics, isAsync, expr);

            candidateMethods.Free();

            return patternInfo;
        }

        /// <summary>
        /// The overload resolution portion of FindForEachPatternMethod.
        /// If no arguments are passed in, then an empty argument list will be used.
        /// </summary>
        private MethodArgumentInfo PerformForEachPatternOverloadResolution(SyntaxNode syntax, TypeSymbol patternType, ArrayBuilder<MethodSymbol> candidateMethods, bool warningsOnly, DiagnosticBag diagnostics, bool isAsync, SyntaxNode expr)
        {
            var analyzedArguments = AnalyzedArguments.GetInstance();
            var typeArguments = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            var overloadResolutionResult = OverloadResolutionResult<MethodSymbol>.GetInstance();

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            // We create a dummy receiver of the invocation so MethodInvocationOverloadResolution knows it was invoked from an instance, not a type
            var dummyReceiver = new BoundImplicitReceiver(expr, patternType);
            this.OverloadResolution.MethodInvocationOverloadResolution(
                methods: candidateMethods,
                typeArguments: typeArguments,
                receiver: dummyReceiver,
                arguments: analyzedArguments,
                result: overloadResolutionResult,
                useSiteDiagnostics: ref useSiteDiagnostics);
            diagnostics.Add(expr, useSiteDiagnostics);

            MethodSymbol result = null;
            MethodArgumentInfo info = null;

            if (overloadResolutionResult.Succeeded)
            {
                result = overloadResolutionResult.ValidResult.Member;

                if (result.IsStatic || result.DeclaredAccessibility != Accessibility.Public)
                {
                    if (warningsOnly)
                    {
                        MessageID patternName = isAsync ? MessageID.IDS_FeatureAsyncStreams : MessageID.IDS_Collection;
                        diagnostics.Add(ErrorCode.WRN_PatternNotPublicOrNotInstance, expr.Location, patternType, patternName.Localize(), result);
                    }
                    result = null;
                }
                else if (result.CallsAreOmitted(syntax.SyntaxTree))
                {
                    // Calls to this method are omitted in the current syntax tree, i.e it is either a partial method with no implementation part OR a conditional method whose condition is not true in this source file.
                    // We don't want to allow this case.
                    result = null;
                }
                else
                {
                    var argsToParams = overloadResolutionResult.ValidResult.Result.ArgsToParamsOpt;
                    var expanded = overloadResolutionResult.ValidResult.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm;
                    BindDefaultArguments(
                        syntax,
                        result.Parameters,
                        analyzedArguments.Arguments,
                        analyzedArguments.RefKinds,
                        ref argsToParams,
                        out BitVector defaultArguments,
                        expanded,
                        enableCallerInfo: true,
                        diagnostics);

                    info = new MethodArgumentInfo(result, analyzedArguments.Arguments.ToImmutable(), argsToParams, defaultArguments, expanded);
                }
            }
            else if (overloadResolutionResult.GetAllApplicableMembers() is var applicableMembers && applicableMembers.Length > 1)
            {
                if (warningsOnly)
                {
                    diagnostics.Add(ErrorCode.WRN_PatternIsAmbiguous, expr.Location, patternType, MessageID.IDS_Collection.Localize(),
                        applicableMembers[0], applicableMembers[1]);
                }
            }

            overloadResolutionResult.Free();
            analyzedArguments.Free();
            typeArguments.Free();

            return info;
        }

        private MethodArgumentInfo FindForEachPatternMethodViaExtension(SyntaxNode syntax, SyntaxNode expr, BoundExpression collectionExpr, string methodName, DiagnosticBag diagnostics)
        {
            var analyzedArguments = AnalyzedArguments.GetInstance();

            var methodGroupResolutionResult = this.BindExtensionMethod(
                expr,
                methodName,
                analyzedArguments,
                collectionExpr,
                typeArgumentsWithAnnotations: default,
                isMethodGroupConversion: false,
                returnRefKind: default,
                returnType: null);

            diagnostics.AddRange(methodGroupResolutionResult.Diagnostics);

            var overloadResolutionResult = methodGroupResolutionResult.OverloadResolutionResult;
            if (overloadResolutionResult?.Succeeded ?? false)
            {
                var result = overloadResolutionResult.ValidResult.Member;

                if (result.CallsAreOmitted(syntax.SyntaxTree))
                {
                    // Calls to this method are omitted in the current syntax tree, i.e it is either a partial method with no implementation part OR a conditional method whose condition is not true in this source file.
                    // We don't want to allow this case.
                    methodGroupResolutionResult.Free();
                    analyzedArguments.Free();
                    return null;
                }

                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                var collectionConversion = this.Conversions.ClassifyConversionFromExpression(collectionExpr, result.Parameters[0].Type, ref useSiteDiagnostics);
                diagnostics.Add(syntax, useSiteDiagnostics);

                // Unconditionally convert here, to match what we set the ConvertedExpression to in the main BoundForEachStatement node.
                collectionExpr = new BoundConversion(
                    collectionExpr.Syntax,
                    collectionExpr,
                    collectionConversion,
                    @checked: CheckOverflowAtRuntime,
                    explicitCastInCode: false,
                    conversionGroupOpt: null,
                    ConstantValue.NotAvailable,
                    result.Parameters[0].Type);

                var info = BindDefaultArguments(result,
                    collectionExpr,
                    expanded: overloadResolutionResult.ValidResult.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm,
                    collectionExpr.Syntax,
                    diagnostics);
                methodGroupResolutionResult.Free();
                analyzedArguments.Free();
                return info;
            }
            else if (overloadResolutionResult?.GetAllApplicableMembers() is { } applicableMembers && applicableMembers.Length > 1)
            {
                diagnostics.Add(ErrorCode.WRN_PatternIsAmbiguous, expr.Location, collectionExpr.Type, MessageID.IDS_Collection.Localize(),
                    applicableMembers[0], applicableMembers[1]);
            }
            else if (overloadResolutionResult != null)
            {
                overloadResolutionResult.ReportDiagnostics(
                    binder: this,
                    location: expr.Location,
                    nodeOpt: expr,
                    diagnostics: diagnostics,
                    name: methodName,
                    receiver: null,
                    invokedExpression: expr,
                    arguments: methodGroupResolutionResult.AnalyzedArguments,
                    memberGroup: methodGroupResolutionResult.MethodGroup.Methods.ToImmutable(),
                    typeContainingConstructor: null,
                    delegateTypeBeingInvoked: null);
            }

            methodGroupResolutionResult.Free();
            analyzedArguments.Free();
            return null;
        }

        /// <summary>
        /// Called after it is determined that the expression being enumerated is of a type that
        /// has a GetEnumerator (or GetAsyncEnumerator) method.  Checks to see if the return type of the GetEnumerator
        /// method is suitable (i.e. has Current and MoveNext for regular case, 
        /// or Current and MoveNextAsync for async case).
        /// </summary>
        /// <param name="builder">Must be non-null and contain a non-null GetEnumeratorMethod.</param>
        /// <param name="diagnostics">Will be populated with pattern diagnostics.</param>
        /// <param name="expr"></param>
        /// <returns>True if the return type has suitable members.</returns>
        /// <remarks>
        /// It seems that every failure path reports the same diagnostics, so that is left to the caller.
        /// </remarks>
        private bool SatisfiesForEachPattern(SyntaxNode syntax, ref ForEachEnumeratorInfo.Builder builder, bool isAsync, DiagnosticBag diagnostics, SyntaxNode expr)
        {
            Debug.Assert((object)builder.GetEnumeratorInfo.Method != null);

            MethodSymbol getEnumeratorMethod = builder.GetEnumeratorInfo.Method;
            TypeSymbol enumeratorType = getEnumeratorMethod.ReturnType;

            switch (enumeratorType.TypeKind)
            {
                case TypeKind.Class:
                case TypeKind.Struct:
                case TypeKind.Interface:
                case TypeKind.TypeParameter: // Not specifically mentioned in the spec, but consistent with Dev10.
                case TypeKind.Dynamic: // Not specifically mentioned in the spec, but consistent with Dev10.
                    break;

                case TypeKind.Submission:
                    // submission class is synthesized and should never appear in a foreach:
                    throw ExceptionUtilities.UnexpectedValue(enumeratorType.TypeKind);

                default:
                    return false;
            }

            // Use a try-finally since there are many return points
            LookupResult lookupResult = LookupResult.GetInstance();
            try
            {
                // If we searched for the accessor directly, we could reuse FindForEachPatternMethod and we
                // wouldn't have to mangle CurrentPropertyName.  However, Dev10 searches for the property and
                // then extracts the accessor, so we should do the same (in case of accessors with non-standard
                // names).
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                this.LookupMembersInType(
                    lookupResult,
                    enumeratorType,
                    CurrentPropertyName,
                    arity: 0,
                    basesBeingResolved: null,
                    options: LookupOptions.Default, // properties are not invocable - their accessors are
                    originalBinder: this,
                    diagnose: false,
                    useSiteDiagnostics: ref useSiteDiagnostics);

                diagnostics.Add(expr, useSiteDiagnostics);
                useSiteDiagnostics = null;

                if (!lookupResult.IsSingleViable)
                {
                    ReportPatternMemberLookupDiagnostics(lookupResult, enumeratorType, CurrentPropertyName, warningsOnly: false, diagnostics: diagnostics, expr: expr);
                    return false;
                }

                // lookupResult.IsSingleViable above guaranteed there is exactly one symbol.
                Symbol lookupSymbol = lookupResult.SingleSymbolOrDefault;
                Debug.Assert((object)lookupSymbol != null);

                if (lookupSymbol.IsStatic || lookupSymbol.DeclaredAccessibility != Accessibility.Public || lookupSymbol.Kind != SymbolKind.Property)
                {
                    return false;
                }

                // NOTE: accessor can be inherited from overridden property
                MethodSymbol currentPropertyGetterCandidate = ((PropertySymbol)lookupSymbol).GetOwnOrInheritedGetMethod();

                if ((object)currentPropertyGetterCandidate == null)
                {
                    return false;
                }
                else
                {
                    bool isAccessible = this.IsAccessible(currentPropertyGetterCandidate, ref useSiteDiagnostics);
                    diagnostics.Add(expr, useSiteDiagnostics);

                    if (!isAccessible)
                    {
                        // NOTE: per Dev10 and the spec, the property has to be public, but the accessor just has to be accessible
                        return false;
                    }
                }

                builder.CurrentPropertyGetter = currentPropertyGetterCandidate;

                lookupResult.Clear(); // Reuse the same LookupResult

                MethodArgumentInfo moveNextMethodCandidate = FindForEachPatternMethod(syntax, enumeratorType,
                    isAsync ? MoveNextAsyncMethodName : MoveNextMethodName,
                    lookupResult, warningsOnly: false, diagnostics, isAsync, expr);

                if ((object)moveNextMethodCandidate == null ||
                    moveNextMethodCandidate.Method.IsStatic || moveNextMethodCandidate.Method.DeclaredAccessibility != Accessibility.Public ||
                    IsInvalidMoveNextMethod(moveNextMethodCandidate.Method, isAsync))
                {
                    return false;
                }

                builder.MoveNextInfo = moveNextMethodCandidate;

                return true;
            }
            finally
            {
                lookupResult.Free();
            }
        }

        private static bool IsInvalidMoveNextMethod(MethodSymbol moveNextMethodCandidate, bool isAsync)
        {
            if (isAsync)
            {
                // We'll verify the return type from `MoveNextAsync` when we try to bind the `await` for it
                return false;
            }

            // SPEC VIOLATION: Dev10 checks the return type of the original definition, rather than the return type of the actual method.
            return moveNextMethodCandidate.OriginalDefinition.ReturnType.SpecialType != SpecialType.System_Boolean;
        }

        private void ReportEnumerableWarning(SyntaxNode expr, DiagnosticBag diagnostics, TypeSymbol enumeratorType, Symbol patternMemberCandidate)
        {
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            if (this.IsAccessible(patternMemberCandidate, ref useSiteDiagnostics))
            {
                diagnostics.Add(ErrorCode.WRN_PatternBadSignature, expr.Location, enumeratorType, MessageID.IDS_Collection.Localize(), patternMemberCandidate);
            }

            diagnostics.Add(expr, useSiteDiagnostics);
        }

        protected static bool IsIEnumerable(TypeSymbol type)
        {
            switch (type.OriginalDefinition.SpecialType)
            {
                case SpecialType.System_Collections_IEnumerable:
                case SpecialType.System_Collections_Generic_IEnumerable_T:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsIAsyncEnumerable(TypeSymbol type)
        {
            return type.OriginalDefinition.Equals(Compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerable_T));
        }

        /// <summary>
        /// Checks if the given type implements (or extends, in the case of an interface),
        /// System.Collections.IEnumerable or System.Collections.Generic.IEnumerable&lt;T&gt;,
        /// (or System.Collections.Generic.IAsyncEnumerable&lt;T&gt;)
        /// for at least one T.
        /// </summary>
        /// <param name="builder">builder to fill in CollectionType.</param>
        /// <param name="type">Type to check.</param>
        /// <param name="diagnostics" />
        /// <param name="foundMultiple">True if multiple T's are found.</param>
        /// <param name="expr"></param>
        /// <returns>True if some IEnumerable is found (may still be ambiguous).</returns>
        private bool AllInterfacesContainsIEnumerable(ref ForEachEnumeratorInfo.Builder builder,
            TypeSymbol type,
            bool isAsync,
            DiagnosticBag diagnostics,
            out bool foundMultiple, SyntaxNode expr)
        {
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            NamedTypeSymbol implementedIEnumerable = ForEachLoopBinder.GetIEnumerableOfT(type, isAsync, Compilation, ref useSiteDiagnostics, out foundMultiple);

            // Prefer generic to non-generic, unless it is inaccessible.
            if (((object)implementedIEnumerable == null) || !this.IsAccessible(implementedIEnumerable, ref useSiteDiagnostics))
            {
                implementedIEnumerable = null;

                if (!isAsync)
                {
                    var implementedNonGeneric = this.Compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
                    if ((object)implementedNonGeneric != null)
                    {
                        var conversion = this.Conversions.ClassifyImplicitConversionFromType(type, implementedNonGeneric, ref useSiteDiagnostics);
                        if (conversion.IsImplicit)
                        {
                            implementedIEnumerable = implementedNonGeneric;
                        }
                    }
                }
            }

            diagnostics.Add(expr, useSiteDiagnostics);

            builder.CollectionType = implementedIEnumerable;
            return (object)implementedIEnumerable != null;
        }

        /// <summary>
        /// Report appropriate diagnostics when lookup of a pattern member (i.e. GetEnumerator, Current, or MoveNext) fails.
        /// </summary>
        /// <param name="lookupResult">Failed lookup result.</param>
        /// <param name="patternType">Type in which member was looked up.</param>
        /// <param name="memberName">Name of looked up member.</param>
        /// <param name="warningsOnly">True if failures should result in warnings; false if they should result in errors.</param>
        /// <param name="diagnostics">Populated appropriately.</param>
        /// <param name="expr"></param>
        private void ReportPatternMemberLookupDiagnostics(LookupResult lookupResult, TypeSymbol patternType, string memberName, bool warningsOnly, DiagnosticBag diagnostics, SyntaxNode expr)
        {
            if (lookupResult.Symbols.Any())
            {
                if (warningsOnly)
                {
                    ReportEnumerableWarning(expr, diagnostics, patternType, lookupResult.Symbols.First());
                }
                else
                {
                    lookupResult.Clear();

                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    this.LookupMembersInType(
                        lookupResult,
                        patternType,
                        memberName,
                        arity: 0,
                        basesBeingResolved: null,
                        options: LookupOptions.Default,
                        originalBinder: this,
                        diagnose: true,
                        useSiteDiagnostics: ref useSiteDiagnostics);

                    diagnostics.Add(expr, useSiteDiagnostics);

                    if (lookupResult.Error != null)
                    {
                        diagnostics.Add(lookupResult.Error, expr.Location);
                    }
                }
            }
            else if (!warningsOnly)
            {
                diagnostics.Add(ErrorCode.ERR_NoSuchMember, expr.Location, patternType, memberName);
            }
        }

        private MethodArgumentInfo GetParameterlessSpecialTypeMemberInfo(SpecialMember member, SyntaxNode syntax, DiagnosticBag diagnostics)
        {
            var resolvedMember = (MethodSymbol)GetSpecialTypeMember(member, diagnostics, syntax);
            Debug.Assert(resolvedMember is null or { ParameterCount: 0 });
            return resolvedMember is not null
                ? MethodArgumentInfo.CreateParameterlessMethod(resolvedMember)
                : null;
        }

        /// <param name="extensionReceiverOpt">If method is an extension method, this must be non-null.</param>
        private MethodArgumentInfo BindDefaultArguments(MethodSymbol method, BoundExpression extensionReceiverOpt, bool expanded, SyntaxNode syntax, DiagnosticBag diagnostics, bool assertMissingParametersAreOptional = true)
        {
            Debug.Assert((extensionReceiverOpt != null) == method.IsExtensionMethod);

            if (method.ParameterCount == 0)
            {
                return MethodArgumentInfo.CreateParameterlessMethod(method);
            }

            var argsBuilder = ArrayBuilder<BoundExpression>.GetInstance(method.ParameterCount);

            if (method.IsExtensionMethod)
            {
                argsBuilder.Add(extensionReceiverOpt);
            }

            ImmutableArray<int> argsToParams = default;
            BindDefaultArguments(
                syntax,
                method.Parameters,
                argsBuilder,
                argumentRefKindsBuilder: null,
                ref argsToParams,
                defaultArguments: out BitVector defaultArguments,
                expanded,
                enableCallerInfo: true,
                diagnostics,
                assertMissingParametersAreOptional);

            return new MethodArgumentInfo(method, argsBuilder.ToImmutableAndFree(), argsToParams, defaultArguments, expanded);
        }
    }
}
