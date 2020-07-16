// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class SemanticMap
    {
        private class Walker : SyntaxWalker
        {
            private readonly SemanticModel _semanticModel;
            private readonly SemanticMap _map;
            private readonly CancellationToken _cancellationToken;

            public Walker(SemanticModel semanticModel, SemanticMap map, CancellationToken cancellationToken)
                : base(SyntaxWalkerDepth.Token)
            {
                _semanticModel = semanticModel;
                _map = map;
                _cancellationToken = cancellationToken;
            }

            public override void Visit(SyntaxNode node)
            {
                var info = _semanticModel.GetSymbolInfo(node);
                if (!IsNone(info))
                {
                    _map._expressionToInfoMap.Add(node, info);
                }

                base.Visit(node);
            }

            protected override void VisitToken(SyntaxToken token)
            {
                var info = _semanticModel.GetSymbolInfo(token, _cancellationToken);
                if (!IsNone(info))
                {
                    _map._tokenToInfoMap.Add(token, info);
                }

                base.VisitToken(token);
            }

            private static bool IsNone(SymbolInfo info)
                => info.Symbol == null && info.CandidateSymbols.Length == 0;
        }
    }
}
