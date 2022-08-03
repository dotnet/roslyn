// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
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
            var (clientStream, serverStream) = FullDuplexStream.CreatePair();
            var server = Create(clientStream, serverStream, logger);

            var request = new InitializeParams
            {
                Capabilities = new ClientCapabilities
                {

                },
            };

            var result = await server.ExecuteRequestAsync<InitializeParams, InitializeResult>(Methods.InitializeName, request, CancellationToken.None);

            var expected = new InitializeResult
            {
                Capabilities = new ServerCapabilities{ },
            };

            Assert.Equal(expected, result);
        }

        private static ILspLogger GetLogger()
        {
            return NoOpLspLogger.Instance;
        }

        private static TestExampleLanguageServer Create(Stream input, Stream output, ILspLogger logger)
        {
            var jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(output, input));

            var server = new TestExampleLanguageServer(input, jsonRpc, logger);

            return server;
        }

        private class TestExampleLanguageServer : ExampleLanguageServer
        {
            private readonly JsonRpc _clientRpc;

            public TestExampleLanguageServer(Stream clientSteam, JsonRpc jsonRpc, ILspLogger logger) : base(jsonRpc, logger)
            {
                _clientRpc = new JsonRpc(new HeaderDelimitedMessageHandler(clientSteam, clientSteam));

                _clientRpc.StartListening();
            }

            public async Task<ResponseType?> ExecuteRequestAsync<RequestType, ResponseType>(string methodName, RequestType request, CancellationToken cancellationToken)
            {
                var result = await _clientRpc.InvokeWithParameterObjectAsync<ResponseType>(methodName, request, cancellationToken);

                return result;
            }
        }

        private class NoOpLspLogger : ILspLogger
        {
            public static NoOpLspLogger Instance = new NoOpLspLogger();

            public void TraceError(string message)
            {
            }

            public void TraceException(Exception exception)
            {
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
