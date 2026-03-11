// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Helper used during state machine lowering (async / iterator) to "hoist" the initialization of
/// synthesized ref locals whose right-hand side may contain <c>await</c> or otherwise needs to be
/// evaluated before suspension points.
/// <para>
/// A ref local whose initializer contains side effects (invocations, indexing, field dereference with
/// a non-trivial receiver, etc.) must be rewritten so that:
/// <list type="number">
/// <item><description>All side effects are performed exactly once and in the original left-to-right order.</description></item>
/// <item><description>Potential exceptions (null dereference, bounds checks, etc.) occur before the first suspension (e.g. an <c>await</c> inside the method) just as they would have in the original code.</description></item>
/// <item><description>The resulting stored reference (the value of the ref local) remains stable across suspension (i.e. subsequent uses of the ref local after rewrites refer to a syntactically simple expression comprised only of the hoisted symbols and stable primitives).</description></item>
/// </list>
/// </para>
/// </summary>
internal class RefInitializationHoister<THoistedSymbol, THoistedAccess>(SyntheticBoundNodeFactory f, MethodSymbol originalMethod, TypeMap typeMap)
    where THoistedSymbol : Symbol
    where THoistedAccess : BoundExpression
{
    private readonly SyntheticBoundNodeFactory _factory = f;
    private readonly MethodSymbol _originalMethod = originalMethod;
    private readonly TypeMap _typeMap = typeMap;
    private bool _reportedError;

    /// <summary>
    /// Hoists the right-hand side of a ref local (or similar synthesized local) initialization.
    /// </summary>
    /// <typeparam name="TArg">Additional context type passed through to creation callbacks.</typeparam>
    /// <param name="local">The synthesized local whose initialization is being processed. Must be of a supported synthesized kind.</param>
    /// <param name="visitedRight">The (already recursively visited by the enclosing rewriter) original right-hand side expression.</param>
    /// <param name="proxies">Dictionary receiving a proxy replacement mapping from <paramref name="local"/> to a stable expression assembled from hoisted components.</param>
    /// <param name="createHoistedSymbol">Factory that creates a new hoisted symbol for a sub-expression value of a given <see cref="TypeSymbol"/>.</param>
    /// <param name="createHoistedAccess">Factory that produces an access expression to a previously created hoisted symbol.</param>
    /// <param name="arg">Additional context forwarded to the factories (typically state machine specific info).</param>
    /// <param name="isRuntimeAsync">True when performing lowered transformation for runtime-async (MoveNext-like) method body; affects some validation / debug assertions.</param>
    /// <returns>
    /// A sequence expression containing the side-effect assignments (and possibly a sacrificial evaluation)
    /// whose value is the final replacement expression used to initialize the ref local, or <c>null</c>
    /// if no side effects needed hoisting.
    /// </returns>
    /// <remarks>
    /// On success a <see cref="CapturedToExpressionSymbolReplacement{TSymbol}"/> entry is added for the local.
    /// Subsequent usages of the local are rewritten (elsewhere) to the replacement expression so the
    /// state machine no longer needs to track the original local.
    /// </remarks>
    internal BoundExpression? HoistRefInitialization<TArg>(
        LocalSymbol local,
        BoundExpression visitedRight,
        Dictionary<Symbol, CapturedSymbolReplacement> proxies,
        Func<TypeSymbol, TArg, LocalSymbol, THoistedSymbol> createHoistedSymbol,
        Func<THoistedSymbol, TArg, THoistedAccess> createHoistedAccess,
        TArg arg,
        bool isRuntimeAsync)
    {
        Debug.Assert(
            local switch
            {
                TypeSubstitutedLocalSymbol tsl => tsl.UnderlyingLocalSymbol,
                _ => local
            } is SynthesizedLocal
        );
        Debug.Assert(local.SynthesizedKind == SynthesizedLocalKind.Spill ||
                     (local.SynthesizedKind == SynthesizedLocalKind.ForEachArray && local.Type.HasInlineArrayAttribute(out _) && local.Type.TryGetInlineArrayElementField() is object));
        Debug.Assert(local.GetDeclaratorSyntax() != null);
#pragma warning disable format
        Debug.Assert(local.SynthesizedKind switch
                     {
                         SynthesizedLocalKind.Spill => this._originalMethod.IsAsync,
                         SynthesizedLocalKind.ForEachArray => this._originalMethod.IsAsync || this._originalMethod.IsIterator,
                         _ => false
                     });
#pragma warning restore format

        var sideEffects = ArrayBuilder<BoundExpression>.GetInstance();
        bool needsSacrificialEvaluation = false;
        var hoistedSymbols = ArrayBuilder<THoistedSymbol>.GetInstance();

        var replacement = HoistExpression(visitedRight, local, local.RefKind, sideEffects, hoistedSymbols, ref needsSacrificialEvaluation, createHoistedSymbol, createHoistedAccess, arg, isRuntimeAsync, isFieldAccessOfStruct: false);

        proxies.Add(local, new CapturedToExpressionSymbolReplacement<THoistedSymbol>(replacement, hoistedSymbols.ToImmutableAndFree(), isReusable: true));

        if (needsSacrificialEvaluation)
        {
            var type = _typeMap.SubstituteType(local.Type).Type;
            var sacrificialTemp = _factory.SynthesizedLocal(type, refKind: RefKind.Ref);
            Debug.Assert(TypeSymbol.Equals(type, replacement.Type, TypeCompareKind.ConsiderEverything2));
            return _factory.Sequence(ImmutableArray.Create(sacrificialTemp), sideEffects.ToImmutableAndFree(), _factory.AssignmentExpression(_factory.Local(sacrificialTemp), replacement, isRef: true));
        }

        if (sideEffects.Count == 0)
        {
            sideEffects.Free();
            return null;
        }

        var last = sideEffects.Last();
        sideEffects.RemoveLast();
        return _factory.Sequence(ImmutableArray<LocalSymbol>.Empty, sideEffects.ToImmutableAndFree(), last);
    }

    /// <summary>
    /// Recursively processes <paramref name="expr"/>, hoisting side-effecting / non-stable sub-expressions into
    /// separate symbols and returning a (syntactically) simpler expression tree composed only of:
    /// <list type="bullet">
    /// <item><description>Original stable nodes (e.g. constants, <c>this</c>, static readonly field access when allowed).</description></item>
    /// <item><description>Accesses to hoisted symbols created for previous sub-expressions.</description></item>
    /// </list>
    /// </summary>
    /// <typeparam name="TArg">Additional context argument type.</typeparam>
    /// <param name="expr">Expression to transform.</param>
    /// <param name="assignedLocal">The local being initialized ultimately (used for diagnostics and to associate hoisted symbols).</param>
    /// <param name="refKind">The desired ref kind of the full expression result (propagated selectively when it impacts legality of hoisting).</param>
    /// <param name="sideEffects">Builder collecting assignment expressions that realize side effects exactly once.</param>
    /// <param name="hoistedSymbols">Builder receiving the created hoisted symbols (parallel to <paramref name="sideEffects"/> order).</param>
    /// <param name="needsSacrificialEvaluation">Flag that is set if the final composed expression must still be evaluated once now (to force exceptions / checks) even though the reference itself is stable.</param>
    /// <param name="createHoistedSymbol">Callback that creates a hoisted symbol.</param>
    /// <param name="createHoistedAccess">Callback that creates an access to a hoisted symbol.</param>
    /// <param name="arg">Additional callback context.</param>
    /// <param name="isRuntimeAsync">Indicates we are in runtime-async lowering path; changes certain debug-time invariants.</param>
    /// <param name="isFieldAccessOfStruct">Whether the current expression is a field access whose receiver is a struct (important for reference preservation rules).</param>
    /// <returns>The replacement (side-effect free / stable) expression that can stand in for <paramref name="expr"/> after the collected side effects execute.</returns>
    private BoundExpression HoistExpression<TArg>(
        BoundExpression expr,
        LocalSymbol assignedLocal,
        RefKind refKind,
        ArrayBuilder<BoundExpression> sideEffects,
        ArrayBuilder<THoistedSymbol> hoistedSymbols,
        ref bool needsSacrificialEvaluation,
        Func<TypeSymbol, TArg, LocalSymbol, THoistedSymbol> createHoistedSymbol,
        Func<THoistedSymbol, TArg, THoistedAccess> createHoistedAccess,
        TArg arg,
        bool isRuntimeAsync,
        bool isFieldAccessOfStruct)
    {
        switch (expr.Kind)
        {
            case BoundKind.ArrayAccess:
                {
                    var array = (BoundArrayAccess)expr;
                    BoundExpression expression = HoistExpression(
                        array.Expression,
                        assignedLocal,
                        refKind: RefKind.None,
                        sideEffects,
                        hoistedSymbols,
                        ref needsSacrificialEvaluation,
                        createHoistedSymbol,
                        createHoistedAccess,
                        arg,
                        isRuntimeAsync,
                        isFieldAccessOfStruct: false);

                    var indices = ArrayBuilder<BoundExpression>.GetInstance();
                    foreach (var index in array.Indices)
                    {
                        indices.Add(HoistExpression(
                            index,
                            assignedLocal,
                            refKind: RefKind.None,
                            sideEffects,
                            hoistedSymbols,
                            ref needsSacrificialEvaluation,
                            createHoistedSymbol,
                            createHoistedAccess,
                            arg,
                            isRuntimeAsync,
                            isFieldAccessOfStruct: false));
                    }

                    needsSacrificialEvaluation = true; // need to force array index out of bounds exceptions
                    return array.Update(expression, indices.ToImmutableAndFree(), array.Type);
                }

            case BoundKind.FieldAccess:
                {
                    var field = (BoundFieldAccess)expr;
                    if (field.FieldSymbol.IsStatic)
                    {
                        // the address of a static field, and the value of a readonly static field, is stable
                        if (refKind != RefKind.None || field.FieldSymbol.IsReadOnly) return expr;
                        goto default;
                    }
                    Debug.Assert(field.ReceiverOpt != null);

                    if (refKind == RefKind.None)
                    {
                        goto default;
                    }

                    var isFieldOfStruct = !field.FieldSymbol.ContainingType.IsReferenceType;

                    var receiver = HoistExpression(
                        field.ReceiverOpt,
                        assignedLocal,
                        refKind: isFieldOfStruct ? refKind : RefKind.None,
                        sideEffects,
                        hoistedSymbols,
                        ref needsSacrificialEvaluation,
                        createHoistedSymbol,
                        createHoistedAccess,
                        arg,
                        isRuntimeAsync,
                        isFieldAccessOfStruct: isFieldOfStruct);
                    if (receiver.Kind != BoundKind.ThisReference && !isFieldOfStruct)
                    {
                        // Make sure that any potential NRE on the receiver happens before the await.
                        needsSacrificialEvaluation = true;
                    }

                    return _factory.Field(receiver, field.FieldSymbol);
                }

            case BoundKind.ThisReference:
            case BoundKind.BaseReference:
            case BoundKind.DefaultExpression:
                return expr;

            case BoundKind.Call:
                var call = (BoundCall)expr;
                // NOTE: There are two kinds of 'In' arguments that we may see at this point:
                //       - `RefKindExtensions.StrictIn`     (originally specified with 'in' modifier)
                //       - `RefKind.In`                     (specified with no modifiers and matched an 'in' or 'ref readonly' parameter)
                //
                //       It is allowed to spill ordinary `In` arguments by value if reference-preserving spilling is not possible.
                //       The "strict" ones do not permit implicit copying, so the same situation should result in an error.
                if (refKind != RefKind.None && refKind != RefKind.In)
                {
                    Debug.Assert(refKind is RefKindExtensions.StrictIn or RefKind.Ref or RefKind.Out);
                    if (call.Method.RefKind != RefKind.None)
                    {
                        _factory.Diagnostics.Add(ErrorCode.ERR_RefReturningCallAndAwait, _factory.Syntax.Location, call.Method);
                        _reportedError = true;
                    }
                }
                // method call is not referentially transparent, we can only spill the result value.
                refKind = RefKind.None;
                goto default;

            case BoundKind.ConditionalOperator:
                var conditional = (BoundConditionalOperator)expr;
                // NOTE: There are two kinds of 'In' arguments that we may see at this point:
                //       - `RefKindExtensions.StrictIn`     (originally specified with 'in' modifier)
                //       - `RefKind.In`                     (specified with no modifiers and matched an 'in' or 'ref readonly' parameter)
                //
                //       It is allowed to spill ordinary `In` arguments by value if reference-preserving spilling is not possible.
                //       The "strict" ones do not permit implicit copying, so the same situation should result in an error.
                if (refKind != RefKind.None && refKind != RefKind.RefReadOnly)
                {
                    Debug.Assert(refKind is RefKindExtensions.StrictIn or RefKind.Ref or RefKind.In);
                    Debug.Assert(conditional.IsRef);
                    _factory.Diagnostics.Add(ErrorCode.ERR_RefConditionalAndAwait, _factory.Syntax.Location);
                    _reportedError = true;
                }
                // conditional expr is not referentially transparent, we can only spill the result value.
                refKind = RefKind.None;
                goto default;

            default:
                if (expr.ConstantValueOpt != null)
                {
                    return expr;
                }

                if (refKind != RefKind.None)
                {
                    if (isRuntimeAsync)
                    {
                        // If an error was reported about ref escaping earlier, there could be illegal ref accesses later in the method,
                        // so we track that to ensure that we don't see unexpected cases here.
                        // This is an access to a field of a struct, or parameter or local of a type parameter, both of which happen by reference.
                        // The receiver should be a non-ref local or parameter.
                        Debug.Assert(_reportedError || isFieldAccessOfStruct || expr.Type!.IsTypeParameter());
                        Debug.Assert(_reportedError || expr is BoundLocal { LocalSymbol.RefKind: RefKind.None }
                                                            or BoundParameter { ParameterSymbol.RefKind: RefKind.None });

                        // If we need to hoist a spilled local or parameter, and the original was a parameter or local not by ref, then we just directly
                        // use the expression as-is. Making another hoisted copy would copy the value incorrectly.
                        if (expr is BoundLocal { LocalSymbol.RefKind: RefKind.None }
                                 or BoundParameter { ParameterSymbol.RefKind: RefKind.None })
                        {
                            Debug.Assert(assignedLocal.SynthesizedKind == SynthesizedLocalKind.Spill);
                            return expr;
                        }
                    }
                    else
                    {
                        throw ExceptionUtilities.UnexpectedValue(expr.Kind);
                    }
                }

                Debug.Assert(expr.Type is not null);
                var hoistedSymbol = createHoistedSymbol(expr.Type, arg, assignedLocal);
                hoistedSymbols.Add(hoistedSymbol);

                var replacement = createHoistedAccess(hoistedSymbol, arg);
                sideEffects.Add(_factory.AssignmentExpression(replacement, expr));
                return replacement;
        }
    }
}
