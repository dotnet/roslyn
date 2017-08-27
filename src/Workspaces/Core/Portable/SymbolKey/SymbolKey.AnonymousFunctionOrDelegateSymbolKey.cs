﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        /// <summary>
        /// Anonymous functions and anonymous-delegates (the special VB synthesized delegate types),
        /// only come into existence when someone has explicitly written a lambda in their source 
        /// code. So to appropriately round-trip this symbol we store the location that the lambda
        /// was at so that we can find the symbol again when we resolve the key.
        /// </summary>
        private static class AnonymousFunctionOrDelegateSymbolKey
        {
            public static void Create(ISymbol symbol, SymbolKeyWriter visitor)
            {
                Debug.Assert(symbol.IsAnonymousDelegateType() || symbol.IsAnonymousFunction());

                // Write out if this was an anonymous delegate or anonymous function.
                // In both cases they'll have the same location (the location of 
                // the lambda that forced them into existence).  When we resolve the
                // symbol later, if it's an anonymous delegate, we'll first resolve to
                // the anonymous-function, then use that anonymous-functoin to get at
                // the synthesized anonymous delegate.
                visitor.WriteBoolean(symbol.IsAnonymousDelegateType());
                visitor.WriteLocation(symbol.Locations.FirstOrDefault());
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var isAnonymousDelegateType = reader.ReadBoolean();
                var location = reader.ReadLocation();

                var syntaxTree = location.SourceTree;
                if (syntaxTree == null)
                {
                    return default;
                }

                var semanticModel = reader.Compilation.GetSemanticModel(syntaxTree);
                var root = syntaxTree.GetRoot(reader.CancellationToken);
                var node = root.FindNode(location.SourceSpan, getInnermostNodeForTie: true);

                var symbol = semanticModel.GetSymbolInfo(node, reader.CancellationToken)
                                          .GetAnySymbol();

                // If this was a key for an anonymous delegate type, then go find the
                // associated delegate for this lambda and return that instead of the 
                // lambda function symbol itself.
                if (isAnonymousDelegateType)
                {
                    var anonymousDelegate = (symbol as IMethodSymbol).AssociatedAnonymousDelegate;
                    symbol = anonymousDelegate;
                }

                return new SymbolKeyResolution(symbol);
            }
        }
    }
}
