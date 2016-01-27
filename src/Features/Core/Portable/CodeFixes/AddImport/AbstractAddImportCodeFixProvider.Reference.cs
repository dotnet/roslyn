using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private abstract class Reference : IComparable<Reference>, IEquatable<Reference>
        {
            protected readonly AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider;
            public readonly SearchResult SearchResult;

            protected Reference(AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider, SearchResult searchResult)
            {
                this.provider = provider;
                this.SearchResult = searchResult;
            }

            public int CompareTo(Reference other)
            {
                // If references have different weights, order by the ones with lower weight (i.e.
                // they are better matches).
                if (this.SearchResult.Weight < other.SearchResult.Weight)
                {
                    return -1;
                }

                if (this.SearchResult.Weight > other.SearchResult.Weight)
                {
                    return 1;
                }

                // If the weight are the same, just order them based on their names.
                return INamespaceOrTypeSymbolExtensions.CompareNameParts(
                    this.SearchResult.NameParts, other.SearchResult.NameParts);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as Reference);
            }

            public bool Equals(Reference other)
            {
                return other?.SearchResult.NameParts != null &&
                    this.SearchResult.NameParts.SequenceEqual(other.SearchResult.NameParts);
            }

            public override int GetHashCode()
            {
                return Hash.CombineValues(this.SearchResult.NameParts);
            }

            public abstract Task<CodeAction> CreateCodeActionAsync(Document document, SyntaxNode node, bool placeSystemNamespaceFirst, CancellationToken cancellationToken);
        }
    }
}
