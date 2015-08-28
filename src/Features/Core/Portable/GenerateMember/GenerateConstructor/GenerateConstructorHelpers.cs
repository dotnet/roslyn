// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor
{
    internal static class GenerateConstructorHelpers
    {
        public static IMethodSymbol GetDelegatingConstructor(SymbolInfo symbolInfo, ISet<IMethodSymbol> candidateInstanceConstructors, INamedTypeSymbol containingType)
        {
            var symbol = symbolInfo.Symbol as IMethodSymbol;
            if (symbol == null && symbolInfo.CandidateSymbols.Length == 1)
            {
                // Even though the symbol info has a non-viable candidate symbol, we are trying to speculate a base constructor
                // invocation from a different position then where the invocation to it would be generated.
                // Passed in candidateInstanceConstructors actually represent all accessible and invocable constructor symbols.
                // So, we allow candidate symbol for inaccessible OR not creatable candidate reason if it is in the given candidateInstanceConstructors.
                if (symbolInfo.CandidateReason == CandidateReason.Inaccessible ||
                    (symbolInfo.CandidateReason == CandidateReason.NotCreatable && containingType.IsAbstract))
                {
                    symbol = symbolInfo.CandidateSymbols.Single() as IMethodSymbol;
                }
            }

            if (symbol != null && candidateInstanceConstructors.Contains(symbol))
            {
                return symbol;
            }

            return null;
        }
    }
}
