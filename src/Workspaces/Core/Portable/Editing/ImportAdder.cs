// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editing
{
    public static class ImportAdder
    {
        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document.
        /// </summary>
        public static Task<Document> AddImportsAsync(Document document, OptionSet? options = null, CancellationToken cancellationToken = default)
            => AddImportsFromSyntaxesAsync(document, options, cancellationToken);

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the span specified.
        /// </summary>
        public static Task<Document> AddImportsAsync(Document document, TextSpan span, OptionSet? options = null, CancellationToken cancellationToken = default)
            => AddImportsFromSyntaxesAsync(document, new[] { span }, options, cancellationToken);

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the sub-trees annotated with the <see cref="SyntaxAnnotation"/>.
        /// </summary>
        public static Task<Document> AddImportsAsync(Document document, SyntaxAnnotation annotation, OptionSet? options = null, CancellationToken cancellationToken = default)
            => AddImportsFromSyntaxesAsync(document, annotation, options, cancellationToken);

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the spans specified.
        /// </summary>
        public static Task<Document> AddImportsAsync(Document document, IEnumerable<TextSpan> spans, OptionSet? options = null, CancellationToken cancellationToken = default)
            => AddImportsFromSyntaxesAsync(document, spans, options, cancellationToken);

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document.
        /// </summary>
        internal static async Task<Document> AddImportsFromSyntaxesAsync(Document document, OptionSet? options = null, CancellationToken cancellationToken = default)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(root);
            return await AddImportsFromSyntaxesAsync(document, root.FullSpan, options, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the span specified.
        /// </summary>
        internal static Task<Document> AddImportsFromSyntaxesAsync(Document document, TextSpan span, OptionSet? options = null, CancellationToken cancellationToken = default)
            => AddImportsFromSyntaxesAsync(document, new[] { span }, options, cancellationToken);

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the sub-trees annotated with the <see cref="SyntaxAnnotation"/>.
        /// </summary>
        internal static async Task<Document> AddImportsFromSyntaxesAsync(Document document, SyntaxAnnotation annotation, OptionSet? options = null, CancellationToken cancellationToken = default)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(root);
            return await AddImportsFromSyntaxesAsync(document, root.GetAnnotatedNodesAndTokens(annotation).Select(t => t.FullSpan), options, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the spans specified.
        /// </summary>
        internal static Task<Document> AddImportsFromSyntaxesAsync(Document document, IEnumerable<TextSpan> spans, OptionSet? options = null, CancellationToken cancellationToken = default)
        {
            var service = document.GetLanguageService<ImportAdderService>();
            if (service != null)
            {
                return service.AddImportsAsync(document, spans, ImportAdderService.Strategy.AddImportsFromSyntaxes, options, cancellationToken);
            }
            else
            {
                return Task.FromResult(document);
            }
        }

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document.
        /// </summary>
        internal static async Task<Document> AddImportsFromSymbolAnnotationAsync(Document document, OptionSet? options = null, CancellationToken cancellationToken = default)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(root);
            return await AddImportsFromSymbolAnnotationAsync(document, root.FullSpan, options, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the span specified.
        /// </summary>
        internal static Task<Document> AddImportsFromSymbolAnnotationAsync(Document document, TextSpan span, OptionSet? options = null, CancellationToken cancellationToken = default)
            => AddImportsFromSymbolAnnotationAsync(document, new[] { span }, options, cancellationToken);

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the sub-trees annotated with the <see cref="SyntaxAnnotation"/>.
        /// </summary>
        internal static async Task<Document> AddImportsFromSymbolAnnotationAsync(Document document, SyntaxAnnotation annotation, OptionSet? options = null, CancellationToken cancellationToken = default)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(root);
            return await AddImportsFromSymbolAnnotationAsync(document, root.GetAnnotatedNodesAndTokens(annotation).Select(t => t.FullSpan), options, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the spans specified.
        /// </summary>
        internal static Task<Document> AddImportsFromSymbolAnnotationAsync(Document document, IEnumerable<TextSpan> spans, OptionSet? options = null, CancellationToken cancellationToken = default)
        {
            var service = document.GetLanguageService<ImportAdderService>();
            if (service != null)
            {
                return service.AddImportsAsync(document, spans, ImportAdderService.Strategy.AddImportsFromSymbolAnnotations, options, cancellationToken);
            }
            else
            {
                return Task.FromResult(document);
            }
        }
    }
}
