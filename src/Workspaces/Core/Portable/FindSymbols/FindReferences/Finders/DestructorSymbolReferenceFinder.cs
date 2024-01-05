// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal sealed class DestructorSymbolReferenceFinder : AbstractReferenceFinder<IMethodSymbol>
    {
        protected override bool CanFind(IMethodSymbol symbol)
            => symbol.MethodKind == MethodKind.Destructor;

        protected override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IMethodSymbol symbol,
            HashSet<string>? globalAliases,
            Project project,
            IImmutableSet<Document>? documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<Document>();
        }

        protected override ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            IMethodSymbol methodSymbol,
            FindReferencesDocumentState state,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            return new ValueTask<ImmutableArray<FinderLocation>>(ImmutableArray<FinderLocation>.Empty);
        }
    }
}
