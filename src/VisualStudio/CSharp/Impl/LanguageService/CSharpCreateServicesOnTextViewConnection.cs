// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
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
        private readonly object _object = new();
        private Task _unimportedTypeTask = Task.CompletedTask;
        private Task _unimportedExtensionMethodTask = Task.CompletedTask;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpCreateServicesOnTextViewConnection(
            VisualStudioWorkspace workspace,
            IAsynchronousOperationListenerProvider listenerProvider,
            IThreadingContext threadingContext)
            : base(workspace, listenerProvider, threadingContext, LanguageNames.CSharp)
        {
        }

        protected override Task InitializeServiceForOpenedDocumentAsync(Document document)
        {
            var documentId = document.Id;
            lock (_object)
            {
                _unimportedTypeTask = _unimportedTypeTask.ContinueWith(async _
                    => await PopulateTypeImportCompletionCacheAsync(this.Workspace.CurrentSolution.GetDocument(documentId)).ConfigureAwait(false), TaskScheduler.Default);

                _unimportedExtensionMethodTask = _unimportedExtensionMethodTask.ContinueWith(async _
                    => await PopulateExtensionMethodImportCompletionCacheAsync(this.Workspace.CurrentSolution.GetDocument(documentId)).ConfigureAwait(false), TaskScheduler.Default);

                return Task.WhenAll(_unimportedTypeTask, _unimportedExtensionMethodTask);
            }

            static Task PopulateTypeImportCompletionCacheAsync(Document? document)
            {
                if (document is null)
                    return Task.CompletedTask;

                var service = document.GetRequiredLanguageService<ITypeImportCompletionService>();
                // Since we are running in background, intentionally not use a frozen document
                // with partial semantic so we will get cache up-to-date sooner.
                return service.WarmUpCacheAsync(document.Project, CancellationToken.None);
            }

            static Task PopulateExtensionMethodImportCompletionCacheAsync(Document? document)
            {
                if (document is null)
                    return Task.CompletedTask;

                return ExtensionMethodImportCompletionHelper.WarmUpCacheAsync(document, CancellationToken.None);
            }
        }
    }
}
