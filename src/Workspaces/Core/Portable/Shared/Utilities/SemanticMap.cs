// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

internal sealed partial class SemanticMap
{
    private readonly Dictionary<SyntaxNode, SymbolInfo> _expressionToInfoMap = [];

    private readonly Dictionary<SyntaxToken, SymbolInfo> _tokenToInfoMap = [];

    private SemanticMap()
    {
    }

    internal static SemanticMap From(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
    {
        var map = new SemanticMap();
        var walker = new Walker(semanticModel, map, cancellationToken);
        walker.Visit(node);
        return map;
    }

    public IEnumerable<ISymbol> AllReferencedSymbols
    {
        get
        {
            return _expressionToInfoMap.Values.Concat(_tokenToInfoMap.Values).Select(info => info.Symbol).Distinct();
        }
    }
}
