// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class BestTypeInferrer
    {
        public static NullableAnnotation GetNullableAnnotation(ArrayBuilder<TypeWithAnnotations> types)
        {
#if DEBUG
            var example = types.FirstOrDefault(t => t.HasType);
#endif

            var result = NullableAnnotation.NotAnnotated;
            foreach (var type in types)
            {
#if DEBUG
                Debug.Assert(!type.HasType || type.Equals(example, TypeCompareKind.AllIgnoreOptions));
#endif

                // This uses the covariant merging rules.
                result = result.Join(type.NullableAnnotation);
            }

            return result;
        }

        public static NullableFlowState GetNullableState(ArrayBuilder<TypeWithState> types)
        {
            NullableFlowState result = NullableFlowState.NotNull;
            foreach (var type in types)
            {
                result = result.Join(type.State);
            }

            return result;
        }

        /// <remarks>
        /// This method finds the best common type of a set of expressions as per section 7.5.2.14 of the specification.
        /// NOTE: If some or all of the expressions have error types, we return error type as the inference result.
        /// </remarks>
        public static TypeSymbol? InferBestType(
            ImmutableArray<BoundExpression> exprs,
            ConversionsBase conversions,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo,
            out bool inferredFromFunctionType)
        {
            // SPEC:    7.5.2.14 Finding the best common type of a set of expressions
            // SPEC:    In some cases, a common type needs to be inferred for a set of expressions. In particular, the element types of implicitly typed arrays and
            // SPEC:    the return types of anonymous functions with block bodies are found in this way.
            // SPEC:    Intuitively, given a set of expressions E1…Em this inference should be equivalent to calling a method:
            // SPEC:        T M<X>(X x1 … X xm)
            // SPEC:    with the Ei as arguments. 
            // SPEC:    More precisely, the inference starts out with an unfixed type variable X. Output type inferences are then made from each Ei to X.
            // SPEC:    Finally, X is fixed and, if successful, the resulting type S is the resulting best common type for the expressions.
            // SPEC:    If no such S exists, the expressions have no best common type.

            // All non-null types are candidates for best type inference.
            IEqualityComparer<TypeSymbol> comparer = conversions.IncludeNullability ? Symbols.SymbolEqualityComparer.ConsiderEverything : Symbols.SymbolEqualityComparer.IgnoringNullable;
            HashSet<TypeSymbol> candidateTypes = new HashSet<TypeSymbol>(comparer);
            foreach (BoundExpression expr in exprs)
            {
                TypeSymbol? type = expr.GetTypeOrFunctionType();

                if (type is { })
                {
                    if (type.ContainsErrorType())
                    {
                        if (type is FunctionTypeSymbol function)
                        {
                            inferredFromFunctionType = true;
                            return function.GetInternalDelegateType();
                        }
                        else
                        {
                            inferredFromFunctionType = false;
                            return type;
                        }
                    }

                    candidateTypes.Add(type);
                }
            }

            // Perform best type inference on candidate types.
            var builder = ArrayBuilder<TypeSymbol>.GetInstance(candidateTypes.Count);
            builder.AddRange(candidateTypes);
            var result = GetBestType(builder, conversions, ref useSiteInfo);
            builder.Free();

            if (result is FunctionTypeSymbol functionType)
            {
                result = functionType.GetInternalDelegateType();
                inferredFromFunctionType = result is { };
                return result;
            }

            inferredFromFunctionType = false;
            return result;
        }

        /// <remarks>
        /// This method implements best type inference for the conditional operator ?:.
        /// NOTE: If either expression is an error type, we return error type as the inference result.
        /// </remarks>
        public static TypeSymbol? InferBestTypeForConditionalOperator(
            BoundExpression expr1,
            BoundExpression expr2,
            Conversions conversions,
            out bool hadMultipleCandidates,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // SPEC:    The second and third operands, x and y, of the ?: operator control the type of the conditional expression. 
            // SPEC:    •	If x has type X and y has type Y then
            // SPEC:        o	If an implicit conversion (§6.1) exists from X to Y, but not from Y to X, then Y is the type of the conditional expression.
            // SPEC:        o	If an implicit conversion (§6.1) exists from Y to X, but not from X to Y, then X is the type of the conditional expression.
            // SPEC:        o	Otherwise, no expression type can be determined, and a compile-time error occurs.
            // SPEC:    •	If only one of x and y has a type, and both x and y, are implicitly convertible to that type, then that is the type of the conditional expression.
            // SPEC:    •	Otherwise, no expression type can be determined, and a compile-time error occurs.

            // A type is a candidate if all expressions are convertible to that type.
            ArrayBuilder<TypeSymbol> candidateTypes = ArrayBuilder<TypeSymbol>.GetInstance();
            try
            {
                var conversionsWithoutNullability = conversions.WithNullability(false);
                TypeSymbol? type1 = expr1.Type;

                if (type1 is { })
                {
                    if (type1.IsErrorType())
                    {
                        hadMultipleCandidates = false;
                        return type1;
                    }

                    if (conversionsWithoutNullability.ClassifyImplicitConversionFromExpression(expr2, type1, ref useSiteInfo).Exists)
                    {
                        candidateTypes.Add(type1);
                    }
                }

                TypeSymbol? type2 = expr2.Type;

                if (type2 is { })
                {
                    if (type2.IsErrorType())
                    {
                        hadMultipleCandidates = false;
                        return type2;
                    }

                    if (conversionsWithoutNullability.ClassifyImplicitConversionFromExpression(expr1, type2, ref useSiteInfo).Exists)
                    {
                        candidateTypes.Add(type2);
                    }
                }

                hadMultipleCandidates = candidateTypes.Count > 1;

                return GetBestType(candidateTypes, conversions, ref useSiteInfo);
            }
            finally
            {
                candidateTypes.Free();
            }
        }

        internal static TypeSymbol? GetBestType(
            ArrayBuilder<TypeSymbol> types,
            ConversionsBase conversions,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // This code assumes that the types in the list are unique. 

            // This code answers the famous Mike Montwill interview question: Can you find the 
            // unique best member of a set in O(n) time if the pairwise betterness algorithm 
            // might be intransitive?

            // Short-circuit some common cases.
            switch (types.Count)
            {
                case 0:
                    return null;
                case 1:
                    return checkType(types[0]);
            }

            TypeSymbol? best = null;
            int bestIndex = -1;
            for (int i = 0; i < types.Count; i++)
            {
                TypeSymbol? type = checkType(types[i]);
                if (type is null)
                {
                    continue;
                }
                if (best is null)
                {
                    best = type;
                    bestIndex = i;
                }
                else
                {
                    var better = Better(best, type, conversions, ref useSiteInfo);

                    if (better is null)
                    {
                        best = null;
                    }
                    else
                    {
                        best = better;
                        bestIndex = i;
                    }
                }
            }

            if (best is null)
            {
                return null;
            }

            // We have actually only determined that every type *after* best was worse. Now check
            // that every type *before* best was also worse.
            for (int i = 0; i < bestIndex; i++)
            {
                TypeSymbol? type = checkType(types[i]);
                if (type is null)
                {
                    continue;
                }
                TypeSymbol? better = Better(best, type, conversions, ref useSiteInfo);
                if (!best.Equals(better, TypeCompareKind.IgnoreNullableModifiersForReferenceTypes))
                {
                    return null;
                }
            }

            return best;

            static TypeSymbol? checkType(TypeSymbol type) =>
                type is FunctionTypeSymbol functionType && functionType.GetInternalDelegateType() is null ?
                null :
                type;
        }

        /// <summary>
        /// Returns the better type amongst the two, with some possible modifications (dynamic/object or tuple names).
        /// </summary>
        private static TypeSymbol? Better(
            TypeSymbol type1,
            TypeSymbol? type2,
            ConversionsBase conversions,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // Anything is better than an error sym.
            if (type1.IsErrorType())
            {
                return type2;
            }

            if (type2 is null || type2.IsErrorType())
            {
                return type1;
            }

            // Prefer types other than FunctionTypeSymbol.
            if (type1 is FunctionTypeSymbol)
            {
                if (!(type2 is FunctionTypeSymbol))
                {
                    return type2;
                }
            }
            else if (type2 is FunctionTypeSymbol)
            {
                return type1;
            }

            var conversionsWithoutNullability = conversions.WithNullability(false);
            var t1tot2 = conversionsWithoutNullability.ClassifyImplicitConversionFromTypeWhenNeitherOrBothFunctionTypes(type1, type2, ref useSiteInfo).Exists;
            var t2tot1 = conversionsWithoutNullability.ClassifyImplicitConversionFromTypeWhenNeitherOrBothFunctionTypes(type2, type1, ref useSiteInfo).Exists;

            if (t1tot2 && t2tot1)
            {
                if (type1.Equals(type2, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes))
                {
                    return type1.MergeEquivalentTypes(type2, VarianceKind.Out);
                }

                return null;
            }

            if (t1tot2)
            {
                return type2;
            }

            if (t2tot1)
            {
                return type1;
            }

            return null;
        }
    }
}
