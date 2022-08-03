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
    public class ExampleTests
    {
        [Fact]
        public async Task InitializeServer()
        {
            var logger = GetLogger();
            var server = CreateLanguageServer(logger);

            var request = new InitializeParams
            {
                Capabilities = new ClientCapabilities
                {

                },
            };

            var result = await server.ExecuteRequestAsync<InitializeParams, InitializeResult>(Methods.InitializeName, request, CancellationToken.None);

            Assert.True(result.Capabilities.SemanticTokensOptions.Range.Value.First);
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

            public async Task<ResponseType?> ExecuteRequestAsync<RequestType, ResponseType>(string methodName, RequestType request, CancellationToken cancellationToken)
            {
                var result = await _clientRpc.InvokeWithParameterObjectAsync<ResponseType>(methodName, request, cancellationToken);

                return result;
            }

            private void _clientRpc_Disconnected(object sender, JsonRpcDisconnectedEventArgs e)
            {
                throw new NotImplementedException();
            }

            public void InitializeTest()
            {
                _clientRpc.StartListening();
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

        private class NoOpLspLogger : ILspLogger
        {
            public static NoOpLspLogger Instance = new NoOpLspLogger();

            public void TraceError(string message)
            {
            }

            public void TraceException(Exception exception)
            {
                throw exception;
            }

            public void TraceInformation(string message)
            {
            }

            public void TraceStart(string message)
            {
            }

            public void TraceStop(string message)
            {
            }

            public void TraceWarning(string message)
            {
            }
        }
    }
}
