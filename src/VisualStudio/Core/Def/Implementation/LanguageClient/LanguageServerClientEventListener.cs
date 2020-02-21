// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    // unfortunately, we can't implement this on LanguageServerClient since this uses MEF v2 and
    // ILanguageClient requires MEF v1 and two can't be mixed exported in 1 class.
    [Export]
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host), Shared]
    internal class LanguageServerClientEventListener : IEventListener<object>
    {
        private readonly LanguageServerClient _languageServerClient;
        private readonly Lazy<ILanguageClientBroker> _languageClientBroker;

        private readonly IAsynchronousOperationListener _asynchronousOperationListener;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LanguageServerClientEventListener(LanguageServerClient languageServerClient, Lazy<ILanguageClientBroker> languageClientBroker,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            this._languageServerClient = languageServerClient;
            this._languageClientBroker = languageClientBroker;
            this._asynchronousOperationListener = listenerProvider.GetListener(FeatureAttribute.LanguageServer);
        }

        /// <summary>
        /// LSP clients do not necessarily know which language servers (and when) to activate as they are language agnostic.
        /// We know we can provide <see cref="LanguageServerClient"/> as soon as the workspace is started, so tell the
        /// <see cref="ILanguageClientBroker"/> to start loading it.
        /// </summary>
        public void StartListening(Workspace workspace, object serviceOpt)
        {
            var token = this._asynchronousOperationListener.BeginAsyncOperation("LoadAsync");
            // Trigger a fire and forget request to the VS LSP client to load our ILanguageClient.
            // This needs to be done with .Forget() as the LoadAsync (VS LSP client) synchronously stores the result task of OnLoadedAsync.
            // The synchronous execution happens under the sln load threaded wait dialog, so user actions cannot be made in between triggering LoadAsync and storing the result task from OnLoadedAsync.
            // The result task from OnLoadedAsync is waited on before invoking LSP requests to the ILanguageClient.
            this._languageClientBroker.Value.LoadAsync(new LanguageClientMetadata(new string[] { ContentTypeNames.CSharpContentType, ContentTypeNames.VisualBasicContentType }), this._languageServerClient)
                .CompletesAsyncOperation(token).Forget();
        }

        /// <summary>
        /// The <see cref="ILanguageClientBroker.LoadAsync(ILanguageClientMetadata, ILanguageClient)"/> 
        /// requires that we pass the <see cref="ILanguageClientMetadata"/> along with the language client instance.
        /// The implementation of <see cref="ILanguageClientMetadata"/> is not public, so have to re-implement.
        /// https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1043922 tracking to remove this.
        /// </summary>
        private class LanguageClientMetadata : ILanguageClientMetadata
        {
            public LanguageClientMetadata(string[] contentTypes, string clientName = null)
            {
                this.ContentTypes = contentTypes;
                this.ClientName = clientName;
            }

            public string ClientName { get; }

            public IEnumerable<string> ContentTypes { get; }
        }
    }
}
