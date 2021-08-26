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

                    _typeTask = _typeTask.ContinueWith(_ => PopulateTypeImportCompletionCacheAsync(this.Workspace.CurrentSolution.GetDocument(documentId)), TaskScheduler.Default);
                    _extensionMethodTask = _extensionMethodTask.ContinueWith(_ => PopulateExtensionMethodImportCompletionCacheAsync(this.Workspace.CurrentSolution.GetDocument(documentId)), TaskScheduler.Default);
                }

                return Task.WhenAll(_typeTask, _extensionMethodTask);
            }

            // Since we are running in the background, intentionally not use a frozen document
            // with partial semantic in the local functions below so we will get cache up-to-date sooner.

            static Task PopulateTypeImportCompletionCacheAsync(Document? document)
            {
                if (document is null)
                    return Task.CompletedTask;

                var service = document.GetRequiredLanguageService<ITypeImportCompletionService>();
                return service.WarmUpCacheAsync(document.Project, CancellationToken.None);
            }

            static Task PopulateExtensionMethodImportCompletionCacheAsync(Document? document)
                => ExtensionMethodImportCompletionHelper.WarmUpCacheAsync(document, CancellationToken.None);
        }
    }
}
