// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor
{
    internal static class GenerateConstructorHelpers
    {
        public static IMethodSymbol GetDelegatingConstructor(
            SemanticDocument document,
            SymbolInfo symbolInfo,
            ISet<IMethodSymbol> candidateInstanceConstructors,
            INamedTypeSymbol containingType,
            IList<ITypeSymbol> parameterTypes)
        {
            var symbol = symbolInfo.Symbol as IMethodSymbol;
            if (symbol == null && symbolInfo.CandidateSymbols.Length == 1)
            {
                // Even though the symbol info has a non-viable candidate symbol, we are trying 
                // to speculate a base constructor invocation from a different position then 
                // where the invocation to it would be generated. Passed in candidateInstanceConstructors 
                // actually represent all accessible and invocable constructor symbols. So, we allow 
                // candidate symbol for inaccessible OR not creatable candidate reason if it is in 
                // the given candidateInstanceConstructors.
                //
                // Note: if we get either of these cases, we ensure that we can at least convert 
                // the parameter types we have to the constructor parameter types.  This way we
                // don't accidentally think we delegate to a constructor in an abstract base class
                // when the parameter types don't match.
                if (symbolInfo.CandidateReason == CandidateReason.Inaccessible ||
                    (symbolInfo.CandidateReason == CandidateReason.NotCreatable && containingType.IsAbstract))
                {
                    var method = symbolInfo.CandidateSymbols.Single() as IMethodSymbol;
                    if (ParameterTypesMatch(document, parameterTypes, method))
                    {
                        symbol = method;
                    }
                }
            }

            if (symbol != null && candidateInstanceConstructors.Contains(symbol))
            {
                return symbol;
            }

            return null;
        }

        private static bool ParameterTypesMatch(SemanticDocument document, IList<ITypeSymbol> parameterTypes, IMethodSymbol method)
        {
            if (method == null)
            {
                return false;
            }

            if (parameterTypes.Count < method.Parameters.Length)
            {
                return false;
            }

            var compilation = document.SemanticModel.Compilation;
            var semanticFactsService = document.Document.GetLanguageService<ISemanticFactsService>();

            for (var i = 0; i < parameterTypes.Count; i++)
            {
                var type1 = parameterTypes[i];
                if (type1 != null)
                {
                    var type2 = method.Parameters[i].Type;

                    if (!semanticFactsService.IsAssignableTo(type1, type2, compilation))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
