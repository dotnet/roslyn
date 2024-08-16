// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports;

internal abstract class AbstractRemoveUnnecessaryImportsService<T> :
    IRemoveUnnecessaryImportsService,
    IEqualityComparer<T> where T : SyntaxNode
{
    protected abstract IUnnecessaryImportsProvider<T> UnnecessaryImportsProvider { get; }

    public Task<Document> RemoveUnnecessaryImportsAsync(Document document, CancellationToken cancellationToken)
        => RemoveUnnecessaryImportsAsync(document, predicate: null, cancellationToken: cancellationToken);

    public abstract Task<Document> RemoveUnnecessaryImportsAsync(Document fromDocument, Func<SyntaxNode, bool>? predicate, CancellationToken cancellationToken);

    protected async Task<HashSet<T>> GetCommonUnnecessaryImportsOfAllContextAsync(
        Document document, Func<SyntaxNode, bool> predicate, CancellationToken cancellationToken)
    {
        var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var unnecessaryImports = new HashSet<T>(this);
        unnecessaryImports.AddRange(UnnecessaryImportsProvider.GetUnnecessaryImports(
            model, span: null, predicate, cancellationToken));
        foreach (var current in document.GetLinkedDocuments())
        {
            var currentModel = await current.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            unnecessaryImports.IntersectWith(UnnecessaryImportsProvider.GetUnnecessaryImports(
                currentModel, span: null, predicate, cancellationToken));
        }

        return unnecessaryImports;
    }

    bool IEqualityComparer<T>.Equals(T? x, T? y)
        => x?.Span == y?.Span;

    int IEqualityComparer<T>.GetHashCode(T obj)
        => obj.Span.GetHashCode();
}
