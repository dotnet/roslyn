// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// The documentation provider used to lookup xml docs for any metadata reference we pass out.  They'll
    /// all get the same xml doc comment provider (as different references to the same compilation don't
    /// see the xml docs any differently).  This provider does root a Compilation around.  However, this should
    /// not be an issue in practice as the compilation we are rooting is a clone of the acutal compilation of
    /// project, and not the compilation itself.  This clone doesn't share any symbols/semantics with the main
    /// compilation, and it can dump syntax trees whenever necessary.  What is does store is the compact
    /// decl-table which is safe and cheap to hold onto long term.  When some downstream consumer of this
    /// metadata-reference then needs to get xml-doc comments, it will resolve a doc-comment-id against this
    /// decl-only-compilation.  Resolution is very cheap, only causing the necessary symbols referenced directly
    /// in the ID to be created.  As downstream consumers are only likely to resolve a small handful of these 
    /// symbols in practice, this should not be expensive to hold onto.  Importantly, semantic models and 
    /// complex method binding/caching should never really happen with this compilation.
    /// </summary>
    internal class DeferredDocumentationProvider(Compilation compilation) : DocumentationProvider
    {
        private readonly Compilation _compilation = compilation.Clone();

        protected override string? GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default)
        {
            var symbol = DocumentationCommentId.GetFirstSymbolForDeclarationId(documentationMemberID, _compilation);

            if (symbol != null)
            {
                return symbol.GetDocumentationCommentXml(preferredCulture, cancellationToken: cancellationToken);
            }

            return string.Empty;
        }

        public override bool Equals(object? obj)
            => object.ReferenceEquals(this, obj);

        public override int GetHashCode()
            => _compilation.GetHashCode();
    }
}
