// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CaseCorrection;

internal static class CaseCorrector
{
    /// <summary>
    /// The annotation normally used on nodes to request case correction.
    /// </summary>
    public static readonly SyntaxAnnotation Annotation = new();

    /// <summary>
    /// Case corrects all names found in the provided document.
    /// </summary>
    public static async Task<Document> CaseCorrectAsync(Document document, CancellationToken cancellationToken = default)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            throw new NotSupportedException(WorkspacesResources.Document_does_not_support_syntax_trees);
        }

        return await CaseCorrectAsync(document, root.FullSpan, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Case corrects all names found in the spans of any nodes annotated with the provided
    /// annotation.
    /// </summary>
    public static async Task<Document> CaseCorrectAsync(Document document, SyntaxAnnotation annotation, CancellationToken cancellationToken = default)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            throw new NotSupportedException(WorkspacesResources.Document_does_not_support_syntax_trees);
        }

        return await CaseCorrectAsync(document, root.GetAnnotatedNodesAndTokens(annotation).Select(n => n.Span).ToImmutableArray(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Case corrects all names found in the span.
    /// </summary>
    public static async Task<Document> CaseCorrectAsync(Document document, TextSpan span, CancellationToken cancellationToken = default)
    {
        return await CaseCorrectAsync(document, [span], cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Case corrects all names found in the provided spans.
    /// </summary>
    public static Task<Document> CaseCorrectAsync(Document document, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken = default)
        => document.GetRequiredLanguageService<ICaseCorrectionService>().CaseCorrectAsync(document, spans, cancellationToken);

    /// <summary>
    /// Case correct only things that don't require semantic information
    /// </summary>
    internal static SyntaxNode CaseCorrect(SyntaxNode root, ImmutableArray<TextSpan> spans, SolutionServices services, CancellationToken cancellationToken = default)
        => services.GetRequiredLanguageService<ICaseCorrectionService>(root.Language).CaseCorrect(root, spans, cancellationToken);
}
