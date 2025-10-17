// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Utilities;

internal static class WorkspacePathUtilities
{
    /// <summary>
    /// Returns true if a type name matches a document name. We use
    /// case insensitive matching to determine this match so that files
    /// "a.cs" and "A.cs" both match a class called "A" 
    /// </summary>
    public static bool TypeNameMatchesDocumentName(Document document, string typeName)
        => GetTypeNameFromDocumentName(document)?.Equals(typeName, StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Standard way to get the display name from a SyntaxNode. If the display
    /// name is null, returns false. Otherwise uses <see cref="TypeNameMatchesDocumentName(Document, string)"/>
    /// </summary>
    public static bool TypeNameMatchesDocumentName(Document document, SyntaxNode typeDeclaration, ISyntaxFacts syntaxFacts)
    {
        var name = syntaxFacts.GetDisplayName(typeDeclaration, DisplayNameOptions.None);
        return name != null && TypeNameMatchesDocumentName(document, name);
    }

    /// <summary>
    /// Gets a type name based on a document name. Returns null
    /// if the document has no name or the document has invalid characters in the name
    /// such that <see cref="Path.GetFileNameWithoutExtension(string?)"/> would throw.
    /// </summary>
    public static string? GetTypeNameFromDocumentName(Document document)
    {
        if (document.Name is null)
        {
            return null;
        }

        return IOUtilities.PerformIO(() => Path.GetFileNameWithoutExtension(document.Name));
    }

    /// <summary>
    /// Returns true if the document name matches the expected file name for the given symbol,
    /// considering nested type naming conventions (e.g., "Outer.Inner.cs" for type Inner nested in Outer).
    /// Uses case-insensitive matching.
    /// </summary>
    public static bool TypeNameMatchesDocumentNameWithContainers(Document document, ISymbol symbol)
    {
        var expectedFileName = GetExpectedFileNameForSymbol(symbol);
        var actualFileName = GetTypeNameFromDocumentName(document);
        return expectedFileName is not null
            && actualFileName is not null
            && expectedFileName.Equals(actualFileName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the expected file name (without extension) for a symbol based on its containing types.
    /// For example, for type "Inner" nested in "Outer", returns "Outer.Inner".
    /// For a top-level type "MyClass", returns "MyClass".
    /// Returns null if the symbol is not a named type.
    /// </summary>
    public static string? GetExpectedFileNameForSymbol(ISymbol symbol)
    {
        if (symbol is not INamedTypeSymbol)
            return null;

        var parts = new List<string>();
        var current = symbol;
        while (current is not null)
        {
            if (current is INamedTypeSymbol namedType)
            {
                parts.Add(namedType.Name);
            }
            current = current.ContainingType;
        }

        parts.Reverse();
        return string.Join(".", parts);
    }

    /// <summary>
    /// Updates a document name to reflect the rename of a symbol in its containership chain.
    /// For example, if renaming "Inner" to "Foo" in file "Outer.Inner.cs", returns "Outer.Foo.cs".
    /// If renaming "Outer" to "Bar" in file "Outer.Inner.cs", returns "Bar.Inner.cs".
    /// Returns null if the document name cannot be updated.
    /// </summary>
    public static string? GetUpdatedDocumentNameForSymbolRename(
        Document document,
        ISymbol oldSymbol,
        string newSymbolName)
    {
        if (oldSymbol is not INamedTypeSymbol)
            return null;

        var extension = IOUtilities.PerformIO(() => Path.GetExtension(document.Name));
        if (extension is null)
            return null;

        var oldFileNameWithoutExtension = GetTypeNameFromDocumentName(document);
        if (oldFileNameWithoutExtension is null)
            return null;

        // Parse the old file name to see if it follows the nested type pattern
        var oldFileParts = oldFileNameWithoutExtension.Split('.');
        
        // If the old file name has only one part, use simple renaming
        if (oldFileParts.Length == 1)
        {
            // Simple rename: "Inner.cs" -> "Foo.cs"
            return newSymbolName + extension;
        }
        
        // Build the full qualified name for the symbol (e.g., "Outer" or "Outer.Inner")
        var symbolQualifiedName = GetExpectedFileNameForSymbol(oldSymbol);
        if (symbolQualifiedName is null)
            return null;
        
        var symbolParts = symbolQualifiedName.Split('.');
        
        // Match file parts against symbol parts to find where the renamed symbol appears
        var newParts = new List<string>(oldFileParts);
        
        // Find the position in the file name where the symbol's outermost type appears
        // For example, if the symbol is "Outer.Inner" and file is "Outer.Inner.cs", we match from position 0
        // If the symbol is "Outer" and file is "Outer.Inner.cs", we match from position 0 but only update "Outer"
        for (int fileIdx = 0; fileIdx < oldFileParts.Length; fileIdx++)
        {
            // Check if the symbol chain matches starting from this position
            bool matches = true;
            for (int symIdx = 0; symIdx < symbolParts.Length && (fileIdx + symIdx) < oldFileParts.Length; symIdx++)
            {
                if (!oldFileParts[fileIdx + symIdx].Equals(symbolParts[symIdx], StringComparison.OrdinalIgnoreCase))
                {
                    matches = false;
                    break;
                }
            }
            
            if (matches)
            {
                // Found the position where the symbol appears
                // The last part of the symbol name is what we're renaming
                var renameIdx = fileIdx + symbolParts.Length - 1;
                if (renameIdx < newParts.Count)
                {
                    newParts[renameIdx] = newSymbolName;
                }
                break;
            }
        }

        var newFileName = string.Join(".", newParts);
        return newFileName + extension;
    }
}
