// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal abstract partial class MethodExtractor
    {
        protected abstract partial class Analyzer
        {
            private class SymbolMapBuilder : SyntaxWalker
            {
                private readonly SemanticModel semanticModel;
                private readonly ISyntaxFactsService service;
                private readonly TextSpan span;
                private readonly Dictionary<ISymbol, List<SyntaxToken>> symbolMap;
                private readonly CancellationToken cancellationToken;

                public static Dictionary<ISymbol, List<SyntaxToken>> Build(
                    ISyntaxFactsService service,
                    SemanticModel semanticModel,
                    SyntaxNode root,
                    TextSpan span,
                    CancellationToken cancellationToken)
                {
                    Contract.ThrowIfNull(semanticModel);
                    Contract.ThrowIfNull(service);
                    Contract.ThrowIfNull(root);

                    var builder = new SymbolMapBuilder(service, semanticModel, span, cancellationToken);
                    builder.Visit(root);

                    return builder.symbolMap;
                }

                private SymbolMapBuilder(
                    ISyntaxFactsService service,
                    SemanticModel semanticModel,
                    TextSpan span,
                    CancellationToken cancellationToken)
                    : base(SyntaxWalkerDepth.Token)
                {
                    this.semanticModel = semanticModel;
                    this.service = service;
                    this.span = span;
                    this.symbolMap = new Dictionary<ISymbol, List<SyntaxToken>>();
                    this.cancellationToken = cancellationToken;
                }

                protected override void VisitToken(SyntaxToken token)
                {
                    if (token.IsMissing ||
                        token.Width() <= 0 ||
                        !this.service.IsIdentifier(token) ||
                        !this.span.Contains(token.Span) ||
                        this.service.IsNamedParameter(token.Parent))
                    {
                        return;
                    }

                    var symbolInfo = semanticModel.GetSymbolInfo(token, cancellationToken);
                    foreach (var sym in symbolInfo.GetAllSymbols())
                    {
                        // add binding result to map
                        var list = this.symbolMap.GetOrAdd(sym, _ => new List<SyntaxToken>());
                        list.Add(token);
                    }
                }
            }
        }
    }
}
