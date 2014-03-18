// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class SemanticMap
    {
        private class Walker : SyntaxWalker
        {
            private readonly SemanticModel semanticModel;
            private readonly SemanticMap map;
            private readonly CancellationToken cancellationToken;

            public Walker(SemanticModel semanticModel, SemanticMap map, CancellationToken cancellationToken) :
                base(SyntaxWalkerDepth.Token)
            {
                this.semanticModel = semanticModel;
                this.map = map;
                this.cancellationToken = cancellationToken;
            }

            public override void Visit(SyntaxNode node)
            {
                var info = semanticModel.GetSymbolInfo(node);
                if (!IsNone(info))
                {
                    map.expressionToInfoMap.Add(node, info);
                }

                base.Visit(node);
            }

            protected override void VisitToken(SyntaxToken token)
            {
                var info = semanticModel.GetSymbolInfo(token, cancellationToken);
                if (!IsNone(info))
                {
                    map.tokenToInfoMap.Add(token, info);
                }

                base.VisitToken(token);
            }

            private bool IsNone(SymbolInfo info)
            {
                return info.Symbol == null && info.CandidateSymbols.Length == 0;
            }
        }
    }
}