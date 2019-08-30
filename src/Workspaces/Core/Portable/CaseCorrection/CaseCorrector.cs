// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CaseCorrection
{
    internal static class CaseCorrector
    {
        /// <summary>
        /// The annotation normally used on nodes to request case correction.
        /// </summary>
        public static readonly SyntaxAnnotation Annotation = new SyntaxAnnotation();

        /// <summary>
        /// Case corrects all names found in the provided document.
        /// </summary>
        public static async Task<Document> CaseCorrectAsync(Document document, CancellationToken cancellationToken = default)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return await CaseCorrectAsync(document, root.FullSpan, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Case corrects all names found in the spans of any nodes annotated with the provided
        /// annotation.
        /// </summary>
        public static async Task<Document> CaseCorrectAsync(Document document, SyntaxAnnotation annotation, CancellationToken cancellationToken = default)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return await CaseCorrectAsync(document, root.GetAnnotatedNodesAndTokens(annotation).Select(n => n.Span).ToImmutableArray(), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Case corrects all names found in the span.
        /// </summary>
        public static async Task<Document> CaseCorrectAsync(Document document, TextSpan span, CancellationToken cancellationToken = default)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return await CaseCorrectAsync(document, ImmutableArray.Create(span), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Case corrects all names found in the provided spans.
        /// </summary>
        public static Task<Document> CaseCorrectAsync(Document document, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken = default)
            => document.GetLanguageService<ICaseCorrectionService>().CaseCorrectAsync(document, spans, cancellationToken);

        /// <summary>
        /// Case correct only things that don't require semantic information
        /// </summary>
        internal static SyntaxNode CaseCorrect(SyntaxNode root, ImmutableArray<TextSpan> spans, Workspace workspace, CancellationToken cancellationToken = default)
            => workspace.Services.GetLanguageServices(root.Language).GetService<ICaseCorrectionService>().CaseCorrect(root, spans, workspace, cancellationToken);
    }
}
