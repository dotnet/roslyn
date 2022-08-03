// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework.Example;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Nerdbank.Streams;
using StreamJsonRpc;
using Xunit;

namespace CommonLanguageServerProtocol.Framework.UnitTests
{
    public partial class ExampleTests
    {
        [Fact]
        public async Task InitializeServer_SerializesCorrectly()
        {
            var logger = GetLogger();
            var server = CreateLanguageServer(logger);

            var result = await InitializeServerAsync(server);
            Assert.True(result.Capabilities.SemanticTokensOptions.Range.Value.First);
        }

        [Fact]
        public async Task ShutdownServer_Succeeds()
        {
            var logger = GetLogger();
            var server = CreateLanguageServer(logger);

            _ = InitializeServerAsync(server);

            await ShutdownServerAsync(server);

            var result = await server.WaitForShutdown();
            Assert.True(0 == result, "Server failed to shut down properly");
        }

        private static async Task ShutdownServerAsync(TestExampleLanguageServer server)
        {
            await server.ExecuteNotificationAsync(Methods.ShutdownName, CancellationToken.None);
        }

        private static async Task<InitializeResult> InitializeServerAsync(TestExampleLanguageServer server)
        {
            var request = new InitializeParams
            {
                Capabilities = new ClientCapabilities
                {

                },
            };

            var result = await server.ExecuteRequestAsync<InitializeParams, InitializeResult>(Methods.InitializeName, request, CancellationToken.None);

            return result;
        }

        private static ILspLogger GetLogger()
        {
            return NoOpLspLogger.Instance;
        }

        private static TestExampleLanguageServer CreateLanguageServer(ILspLogger logger)
        {
            var (clientStream, serverStream) = FullDuplexStream.CreatePair();

            var jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(serverStream, serverStream, CreateJsonMessageFormatter()));

            var server = new TestExampleLanguageServer(clientStream, jsonRpc, logger);

            jsonRpc.StartListening();
            server.InitializeTest();
            return server;
        }

        private class TestExampleLanguageServer : ExampleLanguageServer
        {
            private readonly JsonRpc _clientRpc;

            public TestExampleLanguageServer(Stream clientSteam, JsonRpc jsonRpc, ILspLogger logger) : base(jsonRpc, logger)
            {
                _clientRpc = new JsonRpc(new HeaderDelimitedMessageHandler(clientSteam, clientSteam, CreateJsonMessageFormatter()))
                {
                    ExceptionStrategy = ExceptionProcessing.ISerializable,
                };

                _clientRpc.Disconnected += _clientRpc_Disconnected;
            }

            public async Task<ResponseType> ExecuteRequestAsync<RequestType, ResponseType>(string methodName, RequestType request, CancellationToken cancellationToken)
            {
                var result = await _clientRpc.InvokeWithParameterObjectAsync<ResponseType>(methodName, request, cancellationToken);

                return result;
            }

            internal async Task ExecuteNotificationAsync(string methodName, CancellationToken cancellationToken)
            {
                await _clientRpc.NotifyAsync(methodName);
            }

            private TaskCompletionSource<int> _shuttingDown = new TaskCompletionSource<int>();

            private void _clientRpc_Disconnected(object sender, JsonRpcDisconnectedEventArgs e)
            {
                throw new NotImplementedException();
            }

            public override void Shutdown()
            {
                base.Shutdown();
                _shuttingDown.SetResult(0);
            }

            public void InitializeTest()
            {
                _clientRpc.StartListening();
            }

            public async Task<int> WaitForShutdown()
            {
                return await _shuttingDown.Task;
            }

            public override ValueTask DisposeAsync()
            {
                _clientRpc.Dispose();
                return base.DisposeAsync();
            }

        }

        private static JsonMessageFormatter CreateJsonMessageFormatter()
        {
            var messageFormatter = new JsonMessageFormatter();
            VSInternalExtensionUtilities.AddVSInternalExtensionConverters(messageFormatter.JsonSerializer);
            return messageFormatter;
        }
    }
}
