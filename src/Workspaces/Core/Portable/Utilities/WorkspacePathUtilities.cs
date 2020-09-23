// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Utilities
{
    internal static class WorkspacePathUtilities
    {
        /// <summary>
        /// Given a set of folders from a <see cref="Document"/> build the namespace that would match
        /// the folder structure. If a document is located in "Bat/Bar/Baz" then the namespace could be 
        /// "Bat.Bar.Baz"
        /// 
        /// Returns null if the folders contain parts that are invalid identifiers for a namespace.
        /// </summary>
        public static string? TryBuildNamespaceFromFolders(IEnumerable<string> folders, ISyntaxFacts syntaxFacts)
        {
            var parts = folders.SelectMany(folder => folder.Split(new[] { '.' })).SelectAsArray(syntaxFacts.EscapeIdentifier);

            return parts.All(syntaxFacts.IsValidIdentifier)
                ? string.Join(".", parts)
                : null;
        }

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
    }
}
