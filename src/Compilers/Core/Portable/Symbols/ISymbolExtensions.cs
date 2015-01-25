// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    public static class ISymbolExtensions
    {
        /// <summary>
        /// Returns the constructed form of the ReducedFrom property,
        /// including the type arguments that were either inferred during reduction or supplied at the call site.
        /// </summary>
        public static IMethodSymbol GetConstructedReducedFrom(this IMethodSymbol method)
        {
            if (method.MethodKind != MethodKind.ReducedExtension)
            {
                // not a reduced extension method
                return null;
            }

            var reducedFrom = method.ReducedFrom;
            if (!reducedFrom.IsGenericMethod)
            {
                // not generic, no inferences were made
                return reducedFrom;
            }

            var typeArgs = new ITypeSymbol[reducedFrom.TypeParameters.Length];

            // first seed with any type arguments from reduced method
            for (int i = 0, n = method.TypeParameters.Length; i < n; i++)
            {
                var arg = method.TypeArguments[i];

                // make sure we don't construct with type parameters originating from reduced symbol.
                if (arg.Equals(method.TypeParameters[i]))
                {
                    arg = method.TypeParameters[i].ReducedFrom;
                }

                typeArgs[method.TypeParameters[i].ReducedFrom.Ordinal] = arg;
            }

            // add any inferences
            for (int i = 0, n = reducedFrom.TypeParameters.Length; i < n; i++)
            {
                var inferredType = method.GetTypeInferredDuringReduction(reducedFrom.TypeParameters[i]);
                if (inferredType != null)
                {
                    typeArgs[i] = inferredType;
                }
            }

            return reducedFrom.Construct(typeArgs);
        }
    }
}
