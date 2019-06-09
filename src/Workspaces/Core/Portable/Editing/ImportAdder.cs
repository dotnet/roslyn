// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editing
{
    public static class ImportAdder
    {
        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document.
        /// </summary>
        public static async Task<Document> AddImportsAsync(Document document, OptionSet options = null, CancellationToken cancellationToken = default)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return await AddImportsAsync(document, root.FullSpan, options, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the span specified.
        /// </summary>
        public static Task<Document> AddImportsAsync(Document document, TextSpan span, OptionSet options = null, CancellationToken cancellationToken = default)
        {
            return AddImportsAsync(document, new[] { span }, options, cancellationToken);
        }

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the sub-trees annotated with the <see cref="SyntaxAnnotation"/>.
        /// </summary>
        public static async Task<Document> AddImportsAsync(Document document, SyntaxAnnotation annotation, OptionSet options = null, CancellationToken cancellationToken = default)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return await AddImportsAsync(document, root.GetAnnotatedNodesAndTokens(annotation).Select(t => t.FullSpan), options, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds namespace imports / using directives for namespace references found in the document within the spans specified.
        /// </summary>
        public static Task<Document> AddImportsAsync(Document document, IEnumerable<TextSpan> spans, OptionSet options = null, CancellationToken cancellationToken = default)
        {
            var service = document.GetLanguageService<ImportAdderService>();
            if (service != null)
            {
                return service.AddImportsAsync(document, spans, options, cancellationToken);
            }
            else
            {
                return Task.FromResult(document);
            }
        }
    }
}
