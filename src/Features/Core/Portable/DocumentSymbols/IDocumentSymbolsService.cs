// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.DocumentSymbols
{
    internal interface IDocumentSymbolsService : ILanguageService
    {
        /// <summary>
        /// Gets the symbols defined in this document.
        /// </summary>
        Task<ImmutableArray<DocumentSymbolInfo>> GetSymbolsInDocumentAsync(Document document, DocumentSymbolsOptions options, CancellationToken cancellationToken);
    }

    internal enum DocumentSymbolsOptions
    {
        /// <summary>
        /// Computes all types and nested types as part of the main array returned by
        /// <see cref="IDocumentSymbolsService.GetSymbolsInDocumentAsync(Document, DocumentSymbolsOptions, CancellationToken)"/>,
        /// with their top-level members returned as the <see cref="DocumentSymbolInfo.ChildrenSymbols"/> of those types.
        /// No info on locals or local functions is returned. Members not in the given document can be returned. This format
        /// matches the behavior of the Visual Studio toolbars.
        /// </summary>
        TypesAndMembersOnly,
        /// <summary>
        /// Computes all types as a hierarchy. Nested types are returned as <see cref="DocumentSymbolInfo.ChildrenSymbols"/> of their
        /// containing types. Information about locals and local functions is also returned. Members not in the given document are not
        /// returned.
        /// </summary>
        FullHierarchy
    }

    /// <summary>
    /// Information about the symbols in this document
    /// </summary>
    internal sealed class DocumentSymbolInfo
    {
        /// <summary>
        /// Textual representation of the symbol.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// The short name of this item.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The editor glyph that represents this item.
        /// </summary>
        public Glyph Glyph { get; }

        /// <summary>
        /// Whether this item is obsolete.
        /// </summary>
        public bool Obsolete { get; }

        /// <summary>
        /// Descriptive tags from <see cref="Tags.WellKnownTags"/>.
        /// These tags may influence how the item is displayed.
        /// </summary>
        public ImmutableArray<string> Tags { get; }

        /// <summary>
        /// Additional information attached to a completion item by it creator.
        /// </summary>
        public ImmutableDictionary<string, string> Properties { get; }

        /// <summary>
        /// The range enclosing this symbol not including leading/trailing whitespace
	    /// but everything else like comments.
        /// </summary>
        public ImmutableArray<TextSpan> EnclosingSpans { get; }

        /// <summary>
        /// The range that should be selected and revealed when this symbol is being picked.
        /// </summary>
        public ImmutableArray<TextSpan> DeclaringSpans { get; }

        /// <summary>
        /// Any nested items from the document.
        /// </summary>
        public ImmutableArray<DocumentSymbolInfo> ChildrenSymbols { get; }

        public DocumentSymbolInfo(
            string text,
            string name,
            Glyph glyph,
            bool obsolete,
            ImmutableArray<string> tags,
            ImmutableDictionary<string, string> properties,
            ImmutableArray<TextSpan> enclosingSpans,
            ImmutableArray<TextSpan> declaringSpans,
            ImmutableArray<DocumentSymbolInfo> childrenSymbols)
        {
            Text = text;
            Name = name;
            Glyph = glyph;
            Obsolete = obsolete;
            Tags = tags;
            Properties = properties;
            EnclosingSpans = enclosingSpans;
            ChildrenSymbols = childrenSymbols;
            DeclaringSpans = declaringSpans;
        }
    }
}
