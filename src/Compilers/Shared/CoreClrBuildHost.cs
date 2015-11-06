// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CompilerServer;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CompilerServer.BuildProtocolConstants;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// Client class that handles communication to the server.
    /// </summary>
    internal sealed class CoreClrBuildHost : IBuildHost
    {
        private readonly IEnumerable<string> _originalArguments;
        private readonly string _clientDir;
        private readonly Func<string, string, string[], IAnalyzerAssemblyLoader, int> _compileFunc;
        private readonly RequestLanguage _language;

        public IEnumerable<string> OriginalArguments => _originalArguments;

        internal CoreClrBuildHost(IEnumerable<string> arguments, RequestLanguage language, Func<string, string, string[], IAnalyzerAssemblyLoader, int> compileFunc)
        {
            _language = language;
            // BTODO: Should be using BuildClient.GetCommandLineArgs(arguments) here.  But the native invoke 
            // ends up giving us both CoreRun and the exe file.  Need to find a good way to remove the host 
            // as well as the EXE argument. 
            _originalArguments = arguments;
            _clientDir = AppContext.BaseDirectory;
            _compileFunc = compileFunc;
        }

        public int RunCompilation(List<string> arguments)
        {
            return _compileFunc(_clientDir, null, arguments.ToArray(), CoreClrAnalyzerAssemblyLoader.CreateAndSetDefault());
        }

        public async Task<BuildResponse> TryRunServerCompilation(List<string> arguments, string keepAlive, string libEnvVariable, CancellationToken cancellationToken)
        {
            try
            {
                var client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", port: 12000).ConfigureAwait(true);

                // BTODO: Should be passing BuildRequest as a parameter.
                var request = BuildRequest.Create(_language,
                                                  _clientDir,
                                                  arguments,
                                                  keepAlive,
                                                  libEnvVariable);
                await request.WriteAsync(client.GetStream(), cancellationToken).ConfigureAwait(true);
                return await BuildResponse.ReadAsync(client.GetStream(), cancellationToken).ConfigureAwait(true);
            }
            catch
            {
                return null;
            }
        }
    }
}
