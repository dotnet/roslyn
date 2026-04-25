// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor;

internal static class SymbolExtensions
{
    /// <summary>
    /// Converts the symbol to a string representation. This is the equivalent of calling
    /// ISymbol.ToDisplayString()
    /// </summary>
    /// <returns>A formatted string representation of the symbol.</returns>
    internal static string GetDefaultDisplayString(this ISymbol typeSymbol)
        => SymbolCache.GetSymbolData(typeSymbol).GetDefaultDisplayString();

    /// <summary>
    /// Converts the symbol to a string representation. This is the equivalent of calling
    /// ISymbol.ToDisplayString(WellKnownSymbolDisplayFormats.FullNameTypeDisplayFormat)
    /// </summary>
    /// <returns>A formatted string representation of the symbol.</returns>
    internal static string GetFullName(this ISymbol typeSymbol)
        => SymbolCache.GetSymbolData(typeSymbol).GetFullName();

    /// <summary>
    /// Converts the symbol to a string representation. This is the equivalent of calling
    /// ISymbol.ToDisplayString(WellKnownSymbolDisplayFormats.GloballyQualifiedFullNameTypeDisplayFormat)
    /// </summary>
    /// <returns>A formatted string representation of the symbol.</returns>
    internal static string GetGloballyQualifiedFullName(this ISymbol typeSymbol)
        => SymbolCache.GetSymbolData(typeSymbol).GetGloballyQualifiedFullName();

    internal static bool HasFullName(this AttributeData attribute, string fullName)
        => attribute.AttributeClass is { } attributeClass && attributeClass.HasFullName(fullName);

    internal static bool TryGetAttribute(
        this ISymbol symbol, string fullName, [NotNullWhen(true)] out AttributeData? result)
        => TryGetAttribute(symbol.GetAttributes(), fullName, out result);

    private static bool TryGetAttribute(ImmutableArray<AttributeData> attributes, string fullName, [NotNullWhen(true)] out AttributeData? result)
    {
        result = GetAttribute(attributes, fullName);
        return result != null;
    }

    internal static AttributeData? GetAttribute(this ISymbol symbol, string fullName)
        => GetAttribute(symbol.GetAttributes(), fullName);

    private static AttributeData? GetAttribute(ImmutableArray<AttributeData> attributes, string fullName)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.HasFullName(fullName))
            {
                return attribute;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if <paramref name="typeSymbol"/> has the same fully qualified name as <paramref name="fullName"/>.
    /// </summary>
    /// <remarks>
    /// This is a *very* simple implementation that doesn't handle all cases. It's only intended to be used
    /// for a small set of known types. If you add a new type to be looked up by this function you must ensure
    /// the simple check works correctly for it.
    /// </remarks>
    /// <returns>true if the type has the specified full name</returns>
    internal static bool HasFullName(this ITypeSymbol typeSymbol, string fullName)
    {
        // generally we expect this call to fail, so try and match on the simple name first, before
        // checking the fully qualified name, to prevent unnecessary string allocation from ToDisplayString
        if (typeSymbol.Name.Length > fullName.Length)
        {
            return false;
        }

        var fullNameSpan = fullName.AsSpan();

        // The simple name doesn't include generic type params, so skip over them in the full name if they are present.
        // UNDONE: This will need adjustment if this method should need to handle types overloaded on the number
        // of generic type parameters.
        if (fullNameSpan[^1] == '>')
        {
            var endIndex = fullNameSpan.LastIndexOf('<');
            Debug.Assert(endIndex >= 0, "Symbol name ends in '<' but does not contain '>'!");
            fullNameSpan = fullNameSpan[..endIndex];
        }

        // The strategy here is to walk backward through the various simple names in the full name and test
        // them against the symbol parent chain.

        for (ISymbol symbol = typeSymbol; !IsGlobalNamespace(symbol); symbol = symbol.ContainingSymbol)
        {
            var symbolNameSpan = symbol.Name.AsSpan();

            // If the full name span doesn't end with the current symbol name, this isn't a match.
            if (!fullNameSpan.EndsWith(symbolNameSpan, StringComparison.Ordinal))
            {
                return false;
            }

            fullNameSpan = fullNameSpan[..^symbolNameSpan.Length];

            if (fullNameSpan.Length == 0)
            {
                // We just completed testing against full name.
                // Check to see if the parent symbol is the global namespace. If it is,
                // we're at the last symbol that we need to check and have a match!
                // Otherwise, this match failed.
                return IsGlobalNamespace(symbol.ContainingSymbol);
            }

            // Check the previous character. If it isn't a '.', we don't have a match.
            // UNDONE: This will need adjustment if this method is expected to work properly with
            // nested types using a '+' character.
            if (fullNameSpan[^1] != '.')
            {
                return false;
            }

            // Skip the dot and loop to check the parent symbol.
            fullNameSpan = fullNameSpan[..^1];
        }

        return false;

        static bool IsGlobalNamespace(ISymbol symbol)
        {
            return symbol is INamespaceSymbol { IsGlobalNamespace: true };
        }
    }
}
