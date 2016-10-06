// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

                visitor.WriteBoolean(symbol.IsAnonymousDelegateType());
                visitor.WriteLocation(symbol.Locations.FirstOrDefault());
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var isAnonymousDelegateType = reader.ReadBoolean();
                var location = reader.ReadLocation();

                var syntaxTree = location.SourceTree;
                if (syntaxTree != null)
                {
                    var semanticModel = reader.Compilation.GetSemanticModel(syntaxTree);
                    var root = syntaxTree.GetRoot(reader.CancellationToken);
                    var node = root.FindNode(location.SourceSpan, getInnermostNodeForTie: true);

                    var symbol = semanticModel.GetSymbolInfo(node, reader.CancellationToken)
                                              .GetAnySymbol();

                    if (isAnonymousDelegateType)
                    {
                        var anonymousDelegate = (symbol as IMethodSymbol).AssociatedAnonymousDelegate;
                        symbol = anonymousDelegate;
                    }

                    return new SymbolKeyResolution(symbol);
                }

                return default(SymbolKeyResolution);
            }
        }
    }
}