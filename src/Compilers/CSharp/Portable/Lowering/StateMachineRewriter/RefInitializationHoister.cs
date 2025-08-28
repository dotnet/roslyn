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

internal class RefInitializationHoister<THoistedSymbolType, THoistedAccess>(SyntheticBoundNodeFactory f, MethodSymbol originalMethod, TypeMap typeMap)
    where THoistedSymbolType : Symbol
    where THoistedAccess : BoundExpression
{
    private readonly SyntheticBoundNodeFactory F = f;
    private readonly MethodSymbol OriginalMethod = originalMethod;
    private readonly TypeMap TypeMap = typeMap;

    internal BoundExpression? HoistRefInitialization<TArg>(
        LocalSymbol local,
        BoundExpression visitedRight,
        Dictionary<Symbol, CapturedSymbolReplacement> proxies,
        Func<TypeSymbol, TArg, LocalSymbol, THoistedSymbolType> createHoistedSymbol,
        Func<THoistedSymbolType, TArg, THoistedAccess> createHoistedAccess,
        TArg arg)
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
                             SynthesizedLocalKind.Spill => this.OriginalMethod.IsAsync,
                             SynthesizedLocalKind.ForEachArray => this.OriginalMethod.IsAsync || this.OriginalMethod.IsIterator,
                             _ => false
                         });
#pragma warning restore format

        var sideEffects = ArrayBuilder<BoundExpression>.GetInstance();
        bool needsSacrificialEvaluation = false;
        var hoistedSymbols = ArrayBuilder<THoistedSymbolType>.GetInstance();

        var replacement = HoistExpression(visitedRight, local, local.RefKind, sideEffects, hoistedSymbols, ref needsSacrificialEvaluation, createHoistedSymbol, createHoistedAccess, arg);

        proxies.Add(local, new CapturedToExpressionSymbolReplacement<THoistedSymbolType>(replacement, hoistedSymbols.ToImmutableAndFree(), isReusable: true));

        if (needsSacrificialEvaluation)
        {
            var type = TypeMap.SubstituteType(local.Type).Type;
            var sacrificialTemp = F.SynthesizedLocal(type, refKind: RefKind.Ref);
            Debug.Assert(TypeSymbol.Equals(type, replacement.Type, TypeCompareKind.ConsiderEverything2));
            return F.Sequence(ImmutableArray.Create(sacrificialTemp), sideEffects.ToImmutableAndFree(), F.AssignmentExpression(F.Local(sacrificialTemp), replacement, isRef: true));
        }

        if (sideEffects.Count == 0)
        {
            sideEffects.Free();
            return null;
        }

        var last = sideEffects.Last();
        sideEffects.RemoveLast();
        return F.Sequence(ImmutableArray<LocalSymbol>.Empty, sideEffects.ToImmutableAndFree(), last);
    }

    private BoundExpression HoistExpression<TArg>(
        BoundExpression expr,
        LocalSymbol assignedLocal,
        RefKind refKind,
        ArrayBuilder<BoundExpression> sideEffects,
        ArrayBuilder<THoistedSymbolType> hoistedSymbols,
        ref bool needsSacrificialEvaluation,
        Func<TypeSymbol, TArg, LocalSymbol, THoistedSymbolType> hoister,
        Func<THoistedSymbolType, TArg, THoistedAccess> createHoistedAccess,
        TArg arg)
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
                        hoister,
                        createHoistedAccess,
                        arg);

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
                            hoister,
                            createHoistedAccess,
                            arg));
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
                        hoister,
                        createHoistedAccess,
                        arg);
                    if (receiver.Kind != BoundKind.ThisReference && !isFieldOfStruct)
                    {
                        needsSacrificialEvaluation = true; // need the null check in field receiver
                    }

                    return F.Field(receiver, field.FieldSymbol);
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
                        F.Diagnostics.Add(ErrorCode.ERR_RefReturningCallAndAwait, F.Syntax.Location, call.Method);
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
                    F.Diagnostics.Add(ErrorCode.ERR_RefConditionalAndAwait, F.Syntax.Location);
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
                    throw ExceptionUtilities.UnexpectedValue(expr.Kind);
                }

                Debug.Assert(expr.Type is not null);
                var hoistedSymbol = hoister(expr.Type, arg, assignedLocal);
                hoistedSymbols.Add(hoistedSymbol);

                var replacement = createHoistedAccess(hoistedSymbol, arg);
                sideEffects.Add(F.AssignmentExpression(replacement, expr));
                return replacement;
        }
    }
}
