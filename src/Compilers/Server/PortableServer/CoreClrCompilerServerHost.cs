// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CommandLine;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class CoreClrCompilerServerHost : ICompilerServerHost
    {
        private readonly Func<string, MetadataReferenceProperties, PortableExecutableReference> _assemblyReferenceProvider = (path, properties) => new CachingMetadataReference(path, properties);
        private readonly IAnalyzerAssemblyLoader _analyzerAssemblyLoader = CoreClrAnalyzerAssemblyLoader.CreateAndSetDefault();
        private readonly TcpListener _listener;

        public IAnalyzerAssemblyLoader AnalyzerAssemblyLoader => _analyzerAssemblyLoader;

        public Func<string, MetadataReferenceProperties, PortableExecutableReference> AssemblyReferenceProvider => _assemblyReferenceProvider;

        internal CoreClrCompilerServerHost(IPEndPoint endPoint)
        {
            _listener = new TcpListener(endPoint);
            _listener.Start();
        }

        public bool CheckAnalyzers(string baseDirectory, ImmutableArray<CommandLineAnalyzerReference> analyzers)
        {
            // Analyzers not supported in the portable server yet.
            return analyzers.Length == 0;
        }

        public async Task<IClientConnection> CreateListenTask(CancellationToken cancellationToken)
        {
            var tcpClient = await _listener.AcceptTcpClientAsync().ConfigureAwait(true);
            return new TcpClientConnection(tcpClient);
        }

        public void Log(string message)
        {
            // BTODO: Do we need this anymore? 
        }

        private sealed class TcpClientConnection : IClientConnection
        {
            private readonly string _identifier = Guid.NewGuid().ToString();
            private readonly TcpClient _client;

            public string LoggingIdentifier => _identifier;

            internal TcpClientConnection(TcpClient client)
            {
                _client = client;
            }

            public Task<BuildRequest> ReadBuildRequest(CancellationToken cancellationToken)
            {
                return BuildRequest.ReadAsync(_client.GetStream(), cancellationToken);
            }

            public Task WriteBuildResponse(BuildResponse response, CancellationToken cancellationToken)
            {
                return response.WriteAsync(_client.GetStream(), cancellationToken);
            }

            public async Task CreateMonitorDisconnectTask(CancellationToken cancellationToken)
            {
                while (_client.Connected && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                }
            }

            public void Close()
            {
                _client.Dispose();
            }
        }
    }
}
