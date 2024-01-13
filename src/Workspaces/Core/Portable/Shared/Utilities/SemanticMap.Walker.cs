// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class SemanticMap
    {
        private class Walker(SemanticModel semanticModel, SemanticMap map, CancellationToken cancellationToken)
        {
            public void Visit(SyntaxNode node)
            {
                foreach (var child in node.DescendantNodesAndTokensAndSelf())
                {
                    if (child.IsNode)
                    {
                        var childNode = child.AsNode()!;
                        var info = semanticModel.GetSymbolInfo(childNode);
                        if (!IsNone(info))
                        {
                            map._expressionToInfoMap.Add(childNode, info);
                        }
                    }
                    else if (child.IsToken)
                    {
                        var childToken = child.AsToken();
                        var info = semanticModel.GetSymbolInfo(childToken, cancellationToken);
                        if (!IsNone(info))
                        {
                            map._tokenToInfoMap.Add(childToken, info);
                        }
                    }
                    else
                    {
                        throw ExceptionUtilities.Unreachable();
                    }
                }
            }

            private static bool IsNone(SymbolInfo info)
                => info.Symbol == null && info.CandidateSymbols.Length == 0;
        }
    }
}
