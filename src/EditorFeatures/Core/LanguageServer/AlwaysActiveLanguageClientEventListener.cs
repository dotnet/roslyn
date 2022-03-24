// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient
{
    // unfortunately, we can't implement this on LanguageServerClient since this uses MEF v2 and
    // ILanguageClient requires MEF v1 and two can't be mixed exported in 1 class.
    [Export]
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host), Shared]
    internal class AlwaysActiveLanguageClientEventListener : IEventListener<object>
    {
        private readonly AlwaysActivateInProcLanguageClient _languageClient;
        private readonly Lazy<ILanguageClientBroker> _languageClientBroker;

        private readonly IAsynchronousOperationListener _asynchronousOperationListener;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AlwaysActiveLanguageClientEventListener(
            AlwaysActivateInProcLanguageClient languageClient,
            Lazy<ILanguageClientBroker> languageClientBroker,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _languageClient = languageClient;
            _languageClientBroker = languageClientBroker;
            _asynchronousOperationListener = listenerProvider.GetListener(FeatureAttribute.LanguageServer);
        }

        /// <summary>
        /// LSP clients do not necessarily know which language servers (and when) to activate as they are language
        /// agnostic.  We know we can provide <see cref="AlwaysActivateInProcLanguageClient"/> as soon as the
        /// workspace is started, so tell the <see cref="ILanguageClientBroker"/> to start loading it.
        /// </summary>
        public void StartListening(Workspace workspace, object serviceOpt)
        {
            // Trigger a fire and forget request to the VS LSP client to load our ILanguageClient.
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                using var token = _asynchronousOperationListener.BeginAsyncOperation(nameof(LoadAsync));

                // Explicitly switch to the bg so that if this causes any expensive work (like mef loads) it 
                // doesn't block the UI thread.
                await TaskScheduler.Default;

                await _languageClientBroker.Value.LoadAsync(new LanguageClientMetadata(new[]
                {
                        ContentTypeNames.CSharpContentType,
                        ContentTypeNames.VisualBasicContentType,
                        ContentTypeNames.FSharpContentType
                }), _languageClient).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
            }
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
