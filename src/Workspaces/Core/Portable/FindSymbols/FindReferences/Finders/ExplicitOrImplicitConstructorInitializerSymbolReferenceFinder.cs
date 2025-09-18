// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal abstract class ExplicitOrImplicitConstructorInitializerSymbolReferenceFinder : AbstractReferenceFinder<IMethodSymbol>
{
    protected abstract bool CheckIndex(Document document, string name, SyntaxTreeIndex index);

    protected override bool CanFind(IMethodSymbol symbol)
        => symbol.MethodKind == MethodKind.Constructor;

    protected sealed override Task DetermineDocumentsToSearchAsync<TData>(
        IMethodSymbol symbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        Action<Document, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        return FindDocumentsAsync(project, documents, static async (document, tuple, cancellationToken) =>
        {
            var (@this, name) = tuple;
            var index = await SyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
            return @this.CheckIndex(document, name, index);
        }, (this, symbol.ContainingType.Name), processResult, processResultData, cancellationToken);
    }
}
