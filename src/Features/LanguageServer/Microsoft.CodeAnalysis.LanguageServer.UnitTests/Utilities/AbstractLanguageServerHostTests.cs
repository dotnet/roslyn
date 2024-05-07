// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Nerdbank.Streams;
using Roslyn.LanguageServer.Protocol;
using StreamJsonRpc;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public abstract class AbstractLanguageServerHostTests
{
    protected TestOutputLogger TestOutputLogger { get; }

    protected AbstractLanguageServerHostTests(ITestOutputHelper testOutputHelper)
    {
        TestOutputLogger = new TestOutputLogger(testOutputHelper);
    }

    protected Task<TestLspServer> CreateLanguageServerAsync(bool includeDevKitComponents = true)
    {
        return TestLspServer.CreateAsync(new ClientCapabilities(), TestOutputLogger, includeDevKitComponents);
    }

    protected sealed class TestLspServer : IAsyncDisposable
    {
        private readonly Task _languageServerHostCompletionTask;
        private readonly JsonRpc _clientRpc;

        internal static async Task<TestLspServer> CreateAsync(ClientCapabilities clientCapabilities, TestOutputLogger logger, bool includeDevKitComponents = true)
        {
            var exportProvider = await LanguageServerTestComposition.CreateExportProviderAsync(logger.Factory, includeDevKitComponents);
            var testLspServer = new TestLspServer(exportProvider, logger);
            var initializeResponse = await testLspServer.ExecuteRequestAsync<InitializeParams, InitializeResult>(Methods.InitializeName, new InitializeParams { Capabilities = clientCapabilities }, CancellationToken.None);
            Assert.NotNull(initializeResponse?.Capabilities);

            await testLspServer.ExecuteRequestAsync<InitializedParams, object>(Methods.InitializedName, new InitializedParams(), CancellationToken.None);

            return testLspServer;
        }

        internal LanguageServerHost LanguageServerHost { get; }
        public ExportProvider ExportProvider { get; }

        private TestLspServer(ExportProvider exportProvider, ILogger logger)
        {
            var (clientStream, serverStream) = FullDuplexStream.CreatePair();
            LanguageServerHost = new LanguageServerHost(serverStream, serverStream, exportProvider, logger);

            _clientRpc = new JsonRpc(new HeaderDelimitedMessageHandler(clientStream, clientStream, new JsonMessageFormatter()))
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

        public void AddClientLocalRpcTarget(object target)
        {
            _clientRpc.AddLocalRpcTarget(target);
        }

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
