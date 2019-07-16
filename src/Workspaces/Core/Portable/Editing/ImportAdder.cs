// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editing
{
    public static class ImportAdder
    {
        /// <summary>
        /// The annotation <see cref="CodeAction.CleanupDocumentAsync"/> uses to identify sub trees to look for symbol annotations on.
        /// It will then add import directives for these symbol annotations.
        /// </summary>
        public static SyntaxAnnotation Annotation = new SyntaxAnnotation();

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document.
        /// </summary>
        public static Task<Document> AddImportsAsync(Document document, OptionSet options = null, CancellationToken cancellationToken = default)
        {
            return AddImportsFromSyntaxesAsync(document, safe: false, options, cancellationToken);
        }

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the span specified.
        /// </summary>
        public static Task<Document> AddImportsAsync(Document document, TextSpan span, OptionSet options = null, CancellationToken cancellationToken = default)
        {
            return AddImportsFromSyntaxesAsync(document, new[] { span }, safe: false, options, cancellationToken);
        }

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the sub-trees annotated with the <see cref="SyntaxAnnotation"/>.
        /// </summary>
        public static Task<Document> AddImportsAsync(Document document, SyntaxAnnotation annotation, OptionSet options = null, CancellationToken cancellationToken = default)
        {
            return AddImportsFromSyntaxesAsync(document, annotation, safe: false, options, cancellationToken);
        }

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the spans specified.
        /// </summary>
        public static Task<Document> AddImportsAsync(Document document, IEnumerable<TextSpan> spans, OptionSet options = null, CancellationToken cancellationToken = default)
        {
            return AddImportsFromSyntaxesAsync(document, spans, safe: false, options, cancellationToken);
        }

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document.
        /// </summary>
        public static async Task<Document> AddImportsFromSyntaxesAsync(Document document, bool safe = true, OptionSet options = null, CancellationToken cancellationToken = default)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return await AddImportsFromSyntaxesAsync(document, root.FullSpan, safe, options, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the span specified.
        /// </summary>
        public static Task<Document> AddImportsFromSyntaxesAsync(Document document, TextSpan span, bool safe = true, OptionSet options = null, CancellationToken cancellationToken = default)
        {
            return AddImportsFromSyntaxesAsync(document, new[] { span }, safe, options, cancellationToken);
        }

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the sub-trees annotated with the <see cref="SyntaxAnnotation"/>.
        /// </summary>
        public static async Task<Document> AddImportsFromSyntaxesAsync(Document document, SyntaxAnnotation annotation, bool safe = true, OptionSet options = null, CancellationToken cancellationToken = default)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return await AddImportsFromSyntaxesAsync(document, root.GetAnnotatedNodesAndTokens(annotation).Select(t => t.FullSpan), safe, options, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the spans specified.
        /// </summary>
        public static Task<Document> AddImportsFromSyntaxesAsync(Document document, IEnumerable<TextSpan> spans, bool safe = true, OptionSet options = null, CancellationToken cancellationToken = default)
        {
            var service = document.GetLanguageService<ImportAdderService>();
            if (service != null)
            {
                return service.AddImportsAsync(document, spans, ImportAdderService.Strategy.AddImportsFromSyntaxes, safe, options, cancellationToken);
            }
            else
            {
                return Task.FromResult(document);
            }
        }

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document.
        /// </summary>
        public static async Task<Document> AddImportsFromSymbolAnnotationAsync(Document document, bool safe = true, OptionSet options = null, CancellationToken cancellationToken = default)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return await AddImportsFromSymbolAnnotationAsync(document, root.FullSpan, safe, options, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the span specified.
        /// </summary>
        public static Task<Document> AddImportsFromSymbolAnnotationAsync(Document document, TextSpan span, bool safe = true, OptionSet options = null, CancellationToken cancellationToken = default)
        {
            return AddImportsFromSymbolAnnotationAsync(document, new[] { span }, safe, options, cancellationToken);
        }

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the sub-trees annotated with the <see cref="SyntaxAnnotation"/>.
        /// </summary>
        public static async Task<Document> AddImportsFromSymbolAnnotationAsync(Document document, SyntaxAnnotation annotation, bool safe = true, OptionSet options = null, CancellationToken cancellationToken = default)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return await AddImportsFromSymbolAnnotationAsync(document, root.GetAnnotatedNodesAndTokens(annotation).Select(t => t.FullSpan), safe, options, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the spans specified.
        /// </summary>
        public static Task<Document> AddImportsFromSymbolAnnotationAsync(Document document, IEnumerable<TextSpan> spans, bool safe = true, OptionSet options = null, CancellationToken cancellationToken = default)
        {
            var service = document.GetLanguageService<ImportAdderService>();
            if (service != null)
            {
                return service.AddImportsAsync(document, spans, ImportAdderService.Strategy.AddImportsFromSymbolAnnotations, safe, options, cancellationToken);
            }
            else
            {
                return Task.FromResult(document);
            }
        }
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
    }
}
