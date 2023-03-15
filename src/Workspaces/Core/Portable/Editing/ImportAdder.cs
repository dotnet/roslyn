// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editing
{
    public static class ImportAdder
    {
        private static async ValueTask<IEnumerable<TextSpan>> GetSpansAsync(Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return SpecializedCollections.SingletonEnumerable(root.FullSpan);
        }

        private static async ValueTask<IEnumerable<TextSpan>> GetSpansAsync(Document document, SyntaxAnnotation annotation, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return root.GetAnnotatedNodesAndTokens(annotation).Select(t => t.FullSpan);
        }

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document.
        /// </summary>
        public static async Task<Document> AddImportsAsync(Document document, OptionSet? options = null, CancellationToken cancellationToken = default)
            => await AddImportsFromSyntaxesAsync(document, await GetSpansAsync(document, cancellationToken).ConfigureAwait(false), options, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the span specified.
        /// </summary>
        public static Task<Document> AddImportsAsync(Document document, TextSpan span, OptionSet? options = null, CancellationToken cancellationToken = default)
            => AddImportsFromSyntaxesAsync(document, new[] { span }, options, cancellationToken);

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the sub-trees annotated with the <see cref="SyntaxAnnotation"/>.
        /// </summary>
        public static async Task<Document> AddImportsAsync(Document document, SyntaxAnnotation annotation, OptionSet? options = null, CancellationToken cancellationToken = default)
            => await AddImportsFromSyntaxesAsync(document, await GetSpansAsync(document, annotation, cancellationToken).ConfigureAwait(false), options, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the spans specified.
        /// </summary>
        public static Task<Document> AddImportsAsync(Document document, IEnumerable<TextSpan> spans, OptionSet? options = null, CancellationToken cancellationToken = default)
            => AddImportsFromSyntaxesAsync(document, spans, options, cancellationToken);

        private static async Task<Document> AddImportsFromSyntaxesAsync(Document document, IEnumerable<TextSpan> spans, OptionSet? _, CancellationToken cancellationToken)
        {
            var service = document.GetLanguageService<ImportAdderService>();
            if (service == null)
            {
                return document;
            }

            // Since no public options affect the behavior we can ignore options parameter and pass no fallback options:
            var addImportOptions = await document.GetAddImportPlacementOptionsAsync(CodeActionOptions.DefaultProvider, cancellationToken).ConfigureAwait(false);
            return await service.AddImportsAsync(document, spans, ImportAdderService.Strategy.AddImportsFromSyntaxes, addImportOptions, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document.
        /// </summary>
        internal static async Task<Document> AddImportsFromSyntaxesAsync(Document document, AddImportPlacementOptions options, CancellationToken cancellationToken)
            => await AddImportsFromSyntaxesAsync(document, await GetSpansAsync(document, cancellationToken).ConfigureAwait(false), options, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the sub-trees annotated with the <see cref="SyntaxAnnotation"/>.
        /// </summary>
        internal static async Task<Document> AddImportsFromSyntaxesAsync(Document document, SyntaxAnnotation annotation, AddImportPlacementOptions options, CancellationToken cancellationToken)
            => await AddImportsFromSyntaxesAsync(document, await GetSpansAsync(document, annotation, cancellationToken).ConfigureAwait(false), options, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the spans specified.
        /// </summary>
        internal static Task<Document> AddImportsFromSyntaxesAsync(Document document, IEnumerable<TextSpan> spans, AddImportPlacementOptions options, CancellationToken cancellationToken)
            => document.GetRequiredLanguageService<ImportAdderService>().AddImportsAsync(document, spans, ImportAdderService.Strategy.AddImportsFromSyntaxes, options, cancellationToken);

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document.
        /// </summary>
        internal static async Task<Document> AddImportsFromSymbolAnnotationAsync(Document document, AddImportPlacementOptions options, CancellationToken cancellationToken)
            => await AddImportsFromSymbolAnnotationAsync(document, await GetSpansAsync(document, cancellationToken).ConfigureAwait(false), options, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the sub-trees annotated with the <see cref="SyntaxAnnotation"/>.
        /// </summary>
        internal static async Task<Document> AddImportsFromSymbolAnnotationAsync(Document document, SyntaxAnnotation annotation, AddImportPlacementOptions options, CancellationToken cancellationToken)
            => await AddImportsFromSymbolAnnotationAsync(document, await GetSpansAsync(document, annotation, cancellationToken).ConfigureAwait(false), options, cancellationToken).ConfigureAwait(false);

        internal static Task<Document> AddImportsFromSymbolAnnotationAsync(Document document, IEnumerable<TextSpan> spans, AddImportPlacementOptions options, CancellationToken cancellationToken)
            => document.GetRequiredLanguageService<ImportAdderService>().AddImportsAsync(document, spans, ImportAdderService.Strategy.AddImportsFromSymbolAnnotations, options, cancellationToken);
    }
}
