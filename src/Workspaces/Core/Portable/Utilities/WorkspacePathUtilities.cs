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
    /// Checks if a symbol (potentially nested) matches a document name using the Outer.Inner.cs convention.
    /// For example, a nested type "Inner" within "Outer" would match a document named "Outer.Inner.cs".
    /// </summary>
    /// <param name="document">The document to check against</param>
    /// <param name="symbol">The type symbol to match</param>
    /// <returns>True if the symbol's fully qualified name matches the document name pattern</returns>
    public static bool SymbolMatchesDocumentName(Document document, ISymbol symbol)
    {
        if (symbol is not INamedTypeSymbol typeSymbol)
            return false;

        var documentTypeName = GetTypeNameFromDocumentName(document);
        if (documentTypeName is null)
            return false;

        // Get the type hierarchy (e.g., [Outer, Inner] for Outer.Inner)
        var typeHierarchy = GetTypeHierarchy(typeSymbol);
        
        // Join with dots to create the expected pattern
        var fullTypeName = string.Join(".", typeHierarchy.Select(t => t.Name));
        
        return fullTypeName.Equals(documentTypeName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the hierarchy of types from outermost to innermost for a nested type.
    /// For example, for A.B.C, returns [A, B, C].
    /// </summary>
    private static List<INamedTypeSymbol> GetTypeHierarchy(INamedTypeSymbol typeSymbol)
    {
        var hierarchy = new List<INamedTypeSymbol>();
        var current = typeSymbol;
        
        while (current is not null)
        {
            hierarchy.Insert(0, current);
            current = current.ContainingType;
        }
        
        return hierarchy;
    }

    /// <summary>
    /// Gets the new document name when renaming a type that follows the Outer.Inner.cs convention.
    /// </summary>
    /// <param name="document">The current document</param>
    /// <param name="symbol">The type symbol being renamed</param>
    /// <param name="newName">The new name for the symbol</param>
    /// <returns>The new document name, or null if the document doesn't follow the convention</returns>
    public static string? GetNewDocumentNameForSymbolRename(Document document, ISymbol symbol, string newName)
    {
        if (symbol is not INamedTypeSymbol typeSymbol)
            return null;

        var documentTypeName = GetTypeNameFromDocumentName(document);
        if (documentTypeName is null)
            return null;

        var typeHierarchy = GetTypeHierarchy(typeSymbol);
        
        // Build the new type hierarchy by replacing the renamed symbol's name
        var newTypeHierarchy = typeHierarchy.Select(t => SymbolEqualityComparer.Default.Equals(t, typeSymbol) ? newName : t.Name);
        var newDocumentTypeName = string.Join(".", newTypeHierarchy);
        
        // Get the file extension from the original document
        var extension = IOUtilities.PerformIO(() => Path.GetExtension(document.Name));
        if (extension is null)
            return null;

        return newDocumentTypeName + extension;
    }
}
