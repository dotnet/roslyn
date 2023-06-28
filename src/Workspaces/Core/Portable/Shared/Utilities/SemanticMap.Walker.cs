// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class SemanticMap
    {
        private class Walker(SemanticModel semanticModel, SemanticMap map, CancellationToken cancellationToken) : SyntaxWalker(SyntaxWalkerDepth.Token)
        {
            public override void Visit(SyntaxNode node)
            {
                var info = semanticModel.GetSymbolInfo(node);
                if (!IsNone(info))
                {
                    map._expressionToInfoMap.Add(node, info);
                }

                base.Visit(node);
            }

            protected override void VisitToken(SyntaxToken token)
            {
                var info = semanticModel.GetSymbolInfo(token, cancellationToken);
                if (!IsNone(info))
                {
                    map._tokenToInfoMap.Add(token, info);
                }

                base.VisitToken(token);
            }

            private static bool IsNone(SymbolInfo info)
                => info.Symbol == null && info.CandidateSymbols.Length == 0;
        }
    }
}
