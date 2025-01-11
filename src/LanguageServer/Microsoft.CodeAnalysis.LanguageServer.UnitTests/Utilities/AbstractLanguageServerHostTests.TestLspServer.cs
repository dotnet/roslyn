// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using Nerdbank.Streams;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public partial class AbstractLanguageServerHostTests
{
    internal sealed class TestLspServer : IAsyncDisposable
    {
        private readonly Dictionary<Uri, SourceText> _documents;
        private readonly Dictionary<string, IList<LSP.Location>> _locations;
        private readonly Task _languageServerHostCompletionTask;
        private readonly JsonRpc _clientRpc;

        private ServerCapabilities? _serverCapabilities;

        internal static async Task<TestLspServer> CreateAsync(
            ClientCapabilities clientCapabilities,
            TestOutputLogger logger,
            string cacheDirectory,
            bool includeDevKitComponents = true,
            string[]? extensionPaths = null,
            Dictionary<Uri, SourceText>? documents = null,
            Dictionary<string, IList<LSP.Location>>? locations = null)
        {
            var exportProvider = await LanguageServerTestComposition.CreateExportProviderAsync(
                logger.Factory, includeDevKitComponents, cacheDirectory, extensionPaths, out var serverConfiguration, out var assemblyLoader);
            exportProvider.GetExportedValue<ServerConfigurationFactory>().InitializeConfiguration(serverConfiguration);

            var testLspServer = new TestLspServer(exportProvider, logger, assemblyLoader, documents ?? [], locations ?? []);
            var initializeResponse = await testLspServer.Initialize(clientCapabilities);
            Assert.NotNull(initializeResponse?.Capabilities);
            testLspServer._serverCapabilities = initializeResponse!.Capabilities;

            await testLspServer.Initialized();

            return testLspServer;
        }

        internal LanguageServerHost LanguageServerHost { get; }
        public ExportProvider ExportProvider { get; }

        internal ServerCapabilities ServerCapabilities => _serverCapabilities ?? throw new InvalidOperationException("Initialize has not been called");

        private TestLspServer(ExportProvider exportProvider, TestOutputLogger logger, IAssemblyLoader assemblyLoader, Dictionary<Uri, SourceText> documents, Dictionary<string, IList<LSP.Location>> locations)
        {
            _documents = documents;
            _locations = locations;

            var typeRefResolver = new ExtensionTypeRefResolver(assemblyLoader, logger.Factory);

            var (clientStream, serverStream) = FullDuplexStream.CreatePair();
            LanguageServerHost = new LanguageServerHost(serverStream, serverStream, exportProvider, logger, typeRefResolver);

            var messageFormatter = RoslynLanguageServer.CreateJsonMessageFormatter();
            _clientRpc = new JsonRpc(new HeaderDelimitedMessageHandler(clientStream, clientStream, messageFormatter))
            {
                AllowModificationWhileListening = true,
                ExceptionStrategy = ExceptionProcessing.ISerializable,
            };

            _clientRpc.StartListening();

            // This task completes when the server shuts down.  We store it so that we can wait for completion
            // when we dispose of the test server.
            LanguageServerHost.Start();

            _languageServerHostCompletionTask = LanguageServerHost.WaitForExitAsync();
            ExportProvider = exportProvider;
        }

        public async Task<TResponseType?> ExecuteRequestAsync<TRequestType, TResponseType>(string methodName, TRequestType request, CancellationToken cancellationToken) where TRequestType : class
        {
            var result = await _clientRpc.InvokeWithParameterObjectAsync<TResponseType>(methodName, request, cancellationToken: cancellationToken);
            return result;
        }

        public Task ExecuteNotificationAsync<RequestType>(string methodName, RequestType request) where RequestType : class
        {
            return _clientRpc.NotifyWithParameterObjectAsync(methodName, request);
        }

        public Task ExecuteNotification0Async(string methodName)
        {
            return _clientRpc.NotifyWithParameterObjectAsync(methodName);
        }

        public void AddClientLocalRpcTarget(object target)
        {
            _clientRpc.AddLocalRpcTarget(target);
        }

        public void AddClientLocalRpcTarget(string methodName, Delegate handler)
        {
            _clientRpc.AddLocalRpcMethod(methodName, handler);
        }

        public void ApplyWorkspaceEdit(WorkspaceEdit? workspaceEdit)
        {
            Assert.NotNull(workspaceEdit);

            // We do not support applying the following edits
            Assert.Null(workspaceEdit.Changes);
            Assert.Null(workspaceEdit.ChangeAnnotations);

            // Currently we only support applying TextDocumentEdits
            var textDocumentEdits = (TextDocumentEdit[]?)workspaceEdit.DocumentChanges?.Value;
            Assert.NotNull(textDocumentEdits);

            foreach (var documentEdit in textDocumentEdits)
            {
                var uri = documentEdit.TextDocument.Uri;
                var document = _documents[uri];

                var changes = documentEdit.Edits
                    .Select(edit => edit.Value)
                    .Cast<TextEdit>()
                    .SelectAsArray(edit => ProtocolConversions.TextEditToTextChange(edit, document));

                var updatedDocument = document.WithChanges(changes);
                _documents[uri] = updatedDocument;
            }
        }

        public string GetDocumentText(Uri uri) => _documents[uri].ToString();

        public IList<LSP.Location> GetLocations(string locationName) => _locations[locationName];

        public async ValueTask DisposeAsync()
        {
            await _clientRpc.InvokeAsync(Methods.ShutdownName);
            await _clientRpc.NotifyAsync(Methods.ExitName);

            // The language server host task should complete once shutdown and exit are called.
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
            await _languageServerHostCompletionTask;
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks

            _clientRpc.Dispose();
        }
    }
}
