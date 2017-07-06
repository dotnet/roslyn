﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class BestTypeInferrer
    {
        private readonly Conversions _conversions;

        private BestTypeInferrer(Conversions conversions)
        {
            _conversions = conversions;
        }

        public static TypeSymbol InferBestType(ImmutableArray<TypeSymbol> types, Conversions conversions, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var inferrer = new BestTypeInferrer(conversions);
            return inferrer.GetBestType(types, ref useSiteDiagnostics);
        }

        /// <remarks>
        /// This method finds the best common type of a set of expressions as per section 7.5.2.14 of the specification.
        /// NOTE: If some or all of the expressions have error types, we return error type as the inference result.
        /// </remarks>
        public static TypeSymbol InferBestType(ImmutableArray<BoundExpression> exprs, Conversions conversions, out bool hadMultipleCandidates, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
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
            HashSet<TypeSymbol> candidateTypes = new HashSet<TypeSymbol>();
            foreach (BoundExpression expr in exprs)
            {
                TypeSymbol type = expr.Type;

                if ((object)type != null)
                {
                    if (type.IsErrorType())
                    {
                        hadMultipleCandidates = false;
                        return type;
                    }

                    candidateTypes.Add(type);
                }
            }

            hadMultipleCandidates = candidateTypes.Count > 1;

            // Perform best type inference on candidate types.
            return InferBestType(candidateTypes.AsImmutableOrEmpty(), conversions, ref useSiteDiagnostics);
        }

        /// <remarks>
        /// This method implements best type inference for the conditional operator ?:.
        /// NOTE: If either expression is an error type, we return error type as the inference result.
        /// </remarks>
        public static TypeSymbol InferBestTypeForConditionalOperator(BoundExpression expr1, BoundExpression expr2, Conversions conversions, out bool hadMultipleCandidates, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
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

            TypeSymbol type1 = expr1.Type;

            if ((object)type1 != null)
            {
                if (type1.IsErrorType())
                {
                    candidateTypes.Free();
                    hadMultipleCandidates = false;
                    return type1;
                }

                if (conversions.ClassifyImplicitConversionFromExpression(expr2, type1, ref useSiteDiagnostics).Exists)
                {
                    candidateTypes.Add(type1);
                }
            }

            TypeSymbol type2 = expr2.Type;

            if ((object)type2 != null && type2 != type1)
            {
                if (type2.IsErrorType())
                {
                    candidateTypes.Free();
                    hadMultipleCandidates = false;
                    return type2;
                }

                if (conversions.ClassifyImplicitConversionFromExpression(expr1, type2, ref useSiteDiagnostics).Exists)
                {
                    candidateTypes.Add(type2);
                }
            }

            hadMultipleCandidates = candidateTypes.Count > 1;

            return InferBestType(candidateTypes.ToImmutableAndFree(), conversions, ref useSiteDiagnostics);
        }

        private TypeSymbol GetBestType(ImmutableArray<TypeSymbol> types, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // This code assumes that the types in the list are unique. 

            // This code answers the famous Mike Montwill interview question: Can you find the 
            // unique best member of a set in O(n) time if the pairwise betterness algorithm 
            // might be intransitive?

            // Short-circuit some common cases.
            if (types.IsEmpty)
            {
                return null;
            }
            else if (types.Length == 1)
            {
                return types[0];
            }

            TypeSymbol best = null;
            int bestIndex = -1;
            for(int i = 0; i < types.Length; i++)
            {
                TypeSymbol type = types[i];
                if ((object)best == null)
                {
                    best = type;
                    bestIndex = i;
                }
                else
                {
                    var better = Better(best, type, ref useSiteDiagnostics);

                    if ((object)better == null)
                    {
                        best = null;
                    }
                    else if ((object)better != (object)best)
                    {
                        best = better;
                        bestIndex = i;
                    }
                }
            }

            if ((object)best == null)
            {
                return null;
            }

            // We have actually only determined that every type *after* best was worse. Now check
            // that every type *before* best was also worse.
            for (int i = 0; i < bestIndex; i++)
            {
                TypeSymbol type = types[i];
                TypeSymbol better = Better(best, type, ref useSiteDiagnostics);

                if (better != best)
                {
                    return null;
                }
            }

            return best;
        }

        /// <summary>
        /// Returns the better type amongst the two, with some possible modifications (dynamic/object or tuple names).
        /// </summary>
        private TypeSymbol Better(TypeSymbol type1, TypeSymbol type2, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // Anything is better than an error sym.
            if (type1.IsErrorType())
            {
                return type2;
            }

            if ((object)type2 == null || type2.IsErrorType())
            {
                return type1;
            }

            var t1tot2 = _conversions.ClassifyImplicitConversionFromType(type1, type2, ref useSiteDiagnostics).Exists;
            var t2tot1 = _conversions.ClassifyImplicitConversionFromType(type2, type1, ref useSiteDiagnostics).Exists;

            if (t1tot2 && t2tot1)
            {
                if (type1.IsDynamic())
                {
                    return type1;
                }

                if (type2.IsDynamic())
                {
                    return type2;
                }

                if (type1.Equals(type2, TypeCompareKind.IgnoreDynamicAndTupleNames))
                {
                    return MethodTypeInferrer.MergeTupleNames(MethodTypeInferrer.MergeDynamic(type1, type2, _conversions.CorLibrary), type2);
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
