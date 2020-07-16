// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class TypeParameterSymbolReferenceFinder : AbstractReferenceFinder<ITypeParameterSymbol>
    {
        protected override bool CanFind(ITypeParameterSymbol symbol)
            => symbol.TypeParameterKind != TypeParameterKind.Method;

        protected override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            ITypeParameterSymbol symbol,
            Project project,
            IImmutableSet<Document> documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            // Type parameters are only found in documents that have both their name, and the
            // name of its owning type.  NOTE(cyrusn): We have to check in multiple files because
            // of partial types.  A type parameter can be referenced across all the parts.
            // NOTE(cyrusn): We look for type parameters by name.  This means if the same type
            // parameter has a different name in different parts that we won't find it.  However,
            // this only happens in error situations.  It is not legal in C# to use a different
            // name for a type parameter in different parts.
            return FindDocumentsAsync(project, documents, findInGlobalSuppressions: false, cancellationToken, symbol.Name, symbol.ContainingType.Name);
        }

        protected override Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            ITypeParameterSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentUsingSymbolNameAsync(symbol, document, semanticModel, cancellationToken);
        }
    }
}
