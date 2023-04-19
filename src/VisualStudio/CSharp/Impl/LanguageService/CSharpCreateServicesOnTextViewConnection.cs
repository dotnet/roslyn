// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Completion.Providers.ImportCompletion;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets
{
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal class CSharpCreateServicesOnTextViewConnection : AbstractCreateServicesOnTextViewConnection
    {
        private readonly object _gate = new();
        private readonly HashSet<ProjectId> _processedProjects = new();
        private Task _typeTask = Task.CompletedTask;
        private Task _extensionMethodTask = Task.CompletedTask;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpCreateServicesOnTextViewConnection(
            VisualStudioWorkspace workspace,
            IAsynchronousOperationListenerProvider listenerProvider,
            IThreadingContext threadingContext)
            : base(workspace, listenerProvider, threadingContext, LanguageNames.CSharp)
        {
        }

        protected override void OnSolutionRemoved()
        {
            lock (_gate)
            {
                _processedProjects.Clear();
            }
        }

        protected override Task InitializeServiceForOpenedDocumentAsync(Document document)
        {
            // Only pre-populate cache if import completion is enabled
            if (this.Workspace.Options.GetOption(CompletionOptions.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp) != true)
                return Task.CompletedTask;

            lock (_gate)
            {
                if (!_processedProjects.Contains(document.Project.Id))
                {
                    // Make sure we don't capture the entire snapshot
                    var documentId = document.Id;

                    _typeTask = _typeTask.ContinueWith(_ => PopulateTypeImportCompletionCacheAsync(this.Workspace, documentId), TaskScheduler.Default);
                    _extensionMethodTask = _extensionMethodTask.ContinueWith(_ => PopulateExtensionMethodImportCompletionCacheAsync(this.Workspace, documentId), TaskScheduler.Default);
                }

                return Task.WhenAll(_typeTask, _extensionMethodTask);
            }

            static async Task PopulateTypeImportCompletionCacheAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken = default)
            {
                var document = workspace.CurrentSolution.GetDocument(documentId);
                if (document is null)
                    return;

                var service = document.GetRequiredLanguageService<ITypeImportCompletionService>();

                // First use partial semantic to build mostly correct cache fast
                var partialDocument = document.WithFrozenPartialSemantics(cancellationToken);
                await service.WarmUpCacheAsync(partialDocument.Project, CancellationToken.None).ConfigureAwait(false);

                // Then try to update the cache with full semantic
                await service.WarmUpCacheAsync(document.Project, CancellationToken.None).ConfigureAwait(false);
            }

            static async Task PopulateExtensionMethodImportCompletionCacheAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken = default)
            {
                var document = workspace.CurrentSolution.GetDocument(documentId);
                if (document is null)
                    return;

                // First use partial semantic to build mostly correct cache fast
                var partialDocument = document.WithFrozenPartialSemantics(cancellationToken);
                await ExtensionMethodImportCompletionHelper.WarmUpCacheAsync(partialDocument, cancellationToken).ConfigureAwait(false);

                // Then try to update the cache with full semantic
                await ExtensionMethodImportCompletionHelper.WarmUpCacheAsync(document, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
