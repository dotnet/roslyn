// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup.Providers;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CodeCleanup
{
    /// <summary>
    /// Static CodeCleaner class that provides default code cleaning behavior.
    /// </summary>
    internal static class CodeCleaner
    {
        /// <summary>
        /// Return default code cleaners for a given document.
        /// 
        /// This can be modified and given to the Cleanup method to provide different cleaners.
        /// </summary>
        public static ImmutableArray<ICodeCleanupProvider> GetDefaultProviders(Document document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var service = document.GetLanguageService<ICodeCleanerService>();
            if (service != null)
            {
                return service.GetDefaultProviders();
            }
            else
            {
                return ImmutableArray<ICodeCleanupProvider>.Empty;
            }
        }

        /// <summary>
        /// Cleans up the whole document.
        /// Optionally you can provide your own options and code cleaners. Otherwise, the default will be used.
        /// </summary>
        public static async Task<Document> CleanupAsync(Document document, ImmutableArray<ICodeCleanupProvider> providers = default, CancellationToken cancellationToken = default)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return await CleanupAsync(document, new TextSpan(0, text.Length), providers, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Cleans up the document marked with the provided annotation.
        /// Optionally you can provide your own options and code cleaners. Otherwise, the default will be used.
        /// </summary>
        public static async Task<Document> CleanupAsync(Document document, SyntaxAnnotation annotation, ImmutableArray<ICodeCleanupProvider> providers = default, CancellationToken cancellationToken = default)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return await CleanupAsync(document, root.GetAnnotatedNodesAndTokens(annotation).Select(n => n.Span).ToImmutableArray(), providers, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Clean up the provided span in the document.
        /// Optionally you can provide your own options and code cleaners. Otherwise, the default will be used.
        /// </summary>
        public static Task<Document> CleanupAsync(Document document, TextSpan span, ImmutableArray<ICodeCleanupProvider> providers = default, CancellationToken cancellationToken = default)
            => CleanupAsync(document, ImmutableArray.Create(span), providers, cancellationToken);

        /// <summary>
        /// Clean up the provided spans in the document.
        /// Optionally you can provide your own options and code cleaners. Otherwise, the default will be used.
        /// </summary>
        public static async Task<Document> CleanupAsync(Document document, ImmutableArray<TextSpan> spans, ImmutableArray<ICodeCleanupProvider> providers = default, CancellationToken cancellationToken = default)
        {
            var cleanupService = document.GetRequiredLanguageService<ICodeCleanerService>();
            var options = await SyntaxFormattingOptions.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false);
            return await cleanupService.CleanupAsync(document, spans, options, providers, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Clean up the provided span in the node.
        /// This will only cleanup stuff that doesn't require semantic information.
        /// </summary>
        public static Task<SyntaxNode> CleanupAsync(SyntaxNode root, TextSpan span, OptionSet options, HostWorkspaceServices services, ImmutableArray<ICodeCleanupProvider> providers = default, CancellationToken cancellationToken = default)
            => CleanupAsync(root, ImmutableArray.Create(span), options, services, providers, cancellationToken);

        /// <summary>
        /// Clean up the provided spans in the node.
        /// This will only cleanup stuff that doesn't require semantic information.
        /// </summary>
        public static Task<SyntaxNode> CleanupAsync(SyntaxNode root, ImmutableArray<TextSpan> spans, OptionSet options, HostWorkspaceServices services, ImmutableArray<ICodeCleanupProvider> providers = default, CancellationToken cancellationToken = default)
        {
            var cleanupService = services.GetLanguageServices(root.Language).GetRequiredService<ICodeCleanerService>();
            var formattingOptions = SyntaxFormattingOptions.Create(options, services, root.Language);
            return cleanupService.CleanupAsync(root, spans, formattingOptions, services, providers, cancellationToken);
        }
    }
}
