// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageService;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

internal static class PathMetadataUtilities
{
    private static readonly char[] NamespaceSeparatorArray = ['.'];

    /// <summary>
    /// Given a set of folders from build the namespace that would match
    /// the folder structure. If a document is located in { "Bat" , "Bar", "Baz" } then the namespace could be 
    /// "Bat.Bar.Baz". If a rootNamespace is provided, it is prepended to the generated namespace.
    /// 
    /// Returns null if the folders contain parts that are invalid identifiers for a namespace.
    /// </summary>
    public static string? TryBuildNamespaceFromFolders(IEnumerable<string> folders, ISyntaxFacts syntaxFacts, string? rootNamespace = null)
    {
        var parts = folders.SelectMany(folder => folder.Split(NamespaceSeparatorArray)).SelectAsArray(syntaxFacts.EscapeIdentifier);

        // The root namespace can come directly from the project file name and/or
        // editor config file, so if its not valid we don't want to use it.
        if (rootNamespace is { Length: > 0 } &&
            !rootNamespace.Split(NamespaceSeparatorArray).All(syntaxFacts.IsValidIdentifier))
        {
            rootNamespace = null;
        }

        if (parts.IsDefaultOrEmpty)
        {
            return rootNamespace;
        }

        var constructedNamespace = parts.All(syntaxFacts.IsValidIdentifier)
            ? string.Join(".", parts)
            : null;

        if (constructedNamespace is null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(rootNamespace))
        {
            return constructedNamespace;
        }

        return $"{rootNamespace}.{constructedNamespace}";
    }

    public static ImmutableArray<string> BuildFoldersFromNamespace(string? @namespace, string? rootNamespace = null)
    {
        if (@namespace is null || @namespace == rootNamespace)
        {
            return [];
        }

        if (rootNamespace is not null && @namespace.StartsWith(rootNamespace + ".", StringComparison.OrdinalIgnoreCase))
        {
            // Add 1 to get rid of the starting "." as well
            @namespace = @namespace[(rootNamespace.Length + 1)..];
        }

        var parts = @namespace.Split(NamespaceSeparatorArray, options: StringSplitOptions.RemoveEmptyEntries);
        return [.. parts];
    }
}
