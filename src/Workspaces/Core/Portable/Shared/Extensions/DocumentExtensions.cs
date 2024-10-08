// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class DocumentExtensions
{
    public static async ValueTask<SyntaxTreeIndex> GetSyntaxTreeIndexAsync(this Document document, CancellationToken cancellationToken)
    {
        var result = await SyntaxTreeIndex.GetIndexAsync(document, loadOnly: false, cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfNull(result);
        return result;
    }

    public static ValueTask<SyntaxTreeIndex?> GetSyntaxTreeIndexAsync(this Document document, bool loadOnly, CancellationToken cancellationToken)
        => SyntaxTreeIndex.GetIndexAsync(document, loadOnly, cancellationToken);

    /// <summary>
    /// Returns the semantic model for this document that may be produced from partial semantics. The semantic model
    /// is only guaranteed to contain the syntax tree for <paramref name="document"/> and nothing else.
    /// </summary>
    public static async Task<(Document document, SemanticModel? semanticModel)> GetFullOrPartialSemanticModelAsync(this Document document, CancellationToken cancellationToken)
    {
        if (document.Project.TryGetCompilation(out var compilation))
        {
            // We already have a compilation, so at this point it's fastest to just get a SemanticModel
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Make sure the compilation is kept alive so that GetSemanticModelAsync() doesn't become expensive
            GC.KeepAlive(compilation);
            return (document, semanticModel);
        }
        else
        {
            var frozenDocument = document.WithFrozenPartialSemantics(cancellationToken);
            return (frozenDocument, await frozenDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false));
        }
    }

    internal static Document WithSolutionOptions(this Document document, OptionSet options)
        => document.Project.Solution.WithOptions(options).GetRequiredDocument(document.Id);
}
