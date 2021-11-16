﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
                private readonly SemanticModel _semanticModel;
                private readonly ISyntaxFactsService _service;
                private readonly TextSpan _span;
                private readonly Dictionary<ISymbol, List<SyntaxToken>> _symbolMap;
                private readonly CancellationToken _cancellationToken;

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

                    return builder._symbolMap;
                }

                private SymbolMapBuilder(
                    ISyntaxFactsService service,
                    SemanticModel semanticModel,
                    TextSpan span,
                    CancellationToken cancellationToken)
                    : base(SyntaxWalkerDepth.Token)
                {
                    _semanticModel = semanticModel;
                    _service = service;
                    _span = span;
                    _symbolMap = new Dictionary<ISymbol, List<SyntaxToken>>();
                    _cancellationToken = cancellationToken;
                }

                protected override void VisitToken(SyntaxToken token)
                {
                    if (token.IsMissing ||
                        token.Width() <= 0 ||
                        !_service.IsIdentifier(token) ||
                        !_span.Contains(token.Span) ||
                        _service.IsNameOfNamedArgument(token.Parent))
                    {
                        return;
                    }

                    var symbolInfo = _semanticModel.GetSymbolInfo(token, _cancellationToken);
                    foreach (var sym in symbolInfo.GetAllSymbols())
                    {
                        // add binding result to map
                        var list = _symbolMap.GetOrAdd(sym, _ => new List<SyntaxToken>());
                        list.Add(token);
                    }
                }
            }
        }
    }
}
