// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat;

internal sealed class SymbolMoniker(string scheme, string identifier)
{
    public string Scheme { get; } = scheme;

    public string Identifier { get; } = identifier;

    public static bool HasMoniker(ISymbol symbol)
    {
        // We don't create monikers for symbols with type substitutions
        if (!symbol.OriginalDefinition.Equals(symbol))
            return false;

        // Don't create monikers for all local things that cannot escape outside of a single file: downstream consumers simply treat this as meaning a references/definition result
        // doesn't need to be stitched together across files or multiple projects or repositories.
        if (symbol.Kind is SymbolKind.Local or
            SymbolKind.RangeVariable or
            SymbolKind.Label or
            SymbolKind.Alias)
        {
            return false;
        }

        // Don't create monikers for built in-operators. We could pick some sort of moniker for these, but I doubt anybody really needs to search for all uses of
        // + in the world's projects at once.
        if (symbol is IMethodSymbol method && method.MethodKind == MethodKind.BuiltinOperator)
            return false;

        // TODO: some symbols for things some things in crefs don't have a ContainingAssembly. We'll skip those for now but do
        // want those to work.
        if (symbol.Kind != SymbolKind.Namespace && symbol.ContainingAssembly == null)
            return false;

        return true;
    }

    public static SymbolMoniker Create(ISymbol symbol)
    {
        // This uses the existing format that earlier prototypes of the Roslyn LSIF tool implemented; a different format may make more sense long term, but changing the
        // moniker makes it difficult for other systems that have older LSIF indexes to the connect the two indexes together.

        if (!HasMoniker(symbol))
            throw new InvalidOperationException($"'{symbol}' does not have a moniker.");

        // Namespaces are special: they're just a name that exists in the ether between compilations
        if (symbol.Kind == SymbolKind.Namespace)
        {
            return new SymbolMoniker(WellKnownSymbolMonikerSchemes.DotnetNamespace, symbol.ToDisplayString());
        }

        var symbolMoniker = symbol.ContainingAssembly.Name + "#";

        if (symbol.Kind == SymbolKind.Parameter)
        {
            symbolMoniker += GetRequiredDocumentationCommentId(symbol.ContainingSymbol) + "#" + symbol.Name;
        }
        else
        {
            symbolMoniker += GetRequiredDocumentationCommentId(symbol);
        }

        return new SymbolMoniker(WellKnownSymbolMonikerSchemes.DotnetXmlDoc, symbolMoniker);

        static string GetRequiredDocumentationCommentId(ISymbol symbol)
        {
            symbol = symbol.OriginalDefinition;
            var documentationCommentId = symbol.GetDocumentationCommentId();

            if (documentationCommentId == null)
            {
                throw new Exception($"Unable to get documentation comment ID for {symbol.ToDisplayString()}");
            }

            return documentationCommentId;
        }
    }
}
