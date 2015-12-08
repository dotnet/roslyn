// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup.Providers;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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
        public static IEnumerable<ICodeCleanupProvider> GetDefaultProviders(Document document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var service = document.Project.LanguageServices.GetService<ICodeCleanerService>();
            if (service != null)
            {
                return service.GetDefaultProviders();
            }
            else
            {
                return SpecializedCollections.EmptyEnumerable<ICodeCleanupProvider>();
            }
        }

        /// <summary>
        /// Cleans up the whole document.
        /// Optionally you can provide your own options and code cleaners. Otherwise, the default will be used.
        /// </summary>
        public static async Task<Document> CleanupAsync(Document document, IEnumerable<ICodeCleanupProvider> providers = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return await CleanupAsync(document, new TextSpan(0, text.Length), providers, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Cleans up the document marked with the provided annotation.
        /// Optionally you can provide your own options and code cleaners. Otherwise, the default will be used.
        /// </summary>
        public static async Task<Document> CleanupAsync(Document document, SyntaxAnnotation annotation, IEnumerable<ICodeCleanupProvider> providers = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return await CleanupAsync(document, root.GetAnnotatedNodesAndTokens(annotation).Select(n => n.Span), providers, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Clean up the provided span in the document.
        /// Optionally you can provide your own options and code cleaners. Otherwise, the default will be used.
        /// </summary>
        public static Task<Document> CleanupAsync(Document document, TextSpan span, IEnumerable<ICodeCleanupProvider> providers = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return CleanupAsync(document, SpecializedCollections.SingletonEnumerable(span), providers, cancellationToken);
        }

        /// <summary>
        /// Clean up the provided spans in the document.
        /// Optionally you can provide your own options and code cleaners. Otherwise, the default will be used.
        /// </summary>
        public static Task<Document> CleanupAsync(Document document, IEnumerable<TextSpan> spans, IEnumerable<ICodeCleanupProvider> providers = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var cleanupService = document.GetLanguageService<ICodeCleanerService>();
            return cleanupService.CleanupAsync(document, spans, providers, cancellationToken);
        }

        /// <summary>
        /// Clean up the provided span in the node.
        /// This will only cleanup stuff that doesn't require semantic information.
        /// </summary>
        public static Task<SyntaxNode> CleanupAsync(SyntaxNode root, TextSpan span, Workspace workspace, IEnumerable<ICodeCleanupProvider> providers = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return CleanupAsync(root, SpecializedCollections.SingletonEnumerable(span), workspace, providers, cancellationToken);
        }

        /// <summary>
        /// Clean up the provided spans in the node.
        /// This will only cleanup stuff that doesn't require semantic information.
        /// </summary>
        public static Task<SyntaxNode> CleanupAsync(SyntaxNode root, IEnumerable<TextSpan> spans, Workspace workspace, IEnumerable<ICodeCleanupProvider> providers = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var cleanupService = workspace.Services.GetLanguageServices(root.Language).GetService<ICodeCleanerService>();
            return cleanupService.CleanupAsync(root, spans, workspace, providers, cancellationToken);
        }
    }
}
