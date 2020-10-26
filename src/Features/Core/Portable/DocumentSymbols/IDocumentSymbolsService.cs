// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.DocumentSymbols
{
    public interface IDocumentSymbolsService : ILanguageService
    {
        /// <summary>
        /// Gets the symbols defined in this document.
        /// </summary>
        Task<ImmutableArray<DocumentSymbolInfo>> GetSymbolsInDocumentAsync(Document document, DocumentSymbolsOptions options, CancellationToken cancellationToken);
    }

    public enum DocumentSymbolsOptions
    {
        /// <summary>
        /// Computes all types and nested types as part of the main array returned by
        /// <see cref="IDocumentSymbolsService.GetSymbolsInDocumentAsync(Document, DocumentSymbolsOptions, CancellationToken)"/>,
        /// with their top-level members returned as the <see cref="DocumentSymbolInfo.ChildrenSymbols"/> of those types.
        /// No info on locals or local functions is returned. Members not in the given document can be returned. This format
        /// matches the behavior of the Visual Studio toolbars.
        /// </summary>
        TypesAndMethodsOnly,
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
    public abstract class DocumentSymbolInfo
    {
        protected DocumentSymbolInfo(ISymbol symbol, ImmutableArray<DocumentSymbolInfo> childrenSymbols)
        {
            Symbol = symbol;
            ChildrenSymbols = childrenSymbols;
        }

        /// <summary>
        /// The symbol in the document
        /// </summary>
        public ISymbol Symbol { get; }
        /// <summary>
        /// Any nested symbols from the document.
        /// </summary>
        public ImmutableArray<DocumentSymbolInfo> ChildrenSymbols { get; }

        public void Deconstruct(out ISymbol symbol, out ImmutableArray<DocumentSymbolInfo> childrenSymbols)
            => (symbol, childrenSymbols) = (Symbol, ChildrenSymbols);

        protected abstract string FormatSymbol();

        private string? _text;
        /// <summary>
        /// Textual representation of the symbol.
        /// </summary>
        public string Text => _text ??= FormatSymbol();
    }
}
