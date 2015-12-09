// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CommandLine
{
    /// <summary>
    /// Client class that handles communication to the server.
    /// </summary>
    internal sealed class CoreClrBuildClient : BuildClient
    {
        private readonly RequestLanguage _language;
        private readonly CompileFunc _compileFunc;

        private CoreClrBuildClient(RequestLanguage language, CompileFunc compileFunc)
        {
            _language = language;
            _compileFunc = compileFunc;
        }

        internal static int Run(IEnumerable<string> arguments, RequestLanguage language, CompileFunc compileFunc)
        {
            // Should be using BuildClient.GetCommandLineArgs(arguments) here.  But the native invoke 
            // ends up giving us both CoreRun and the exe file.  Need to find a good way to remove the host 
            // as well as the EXE argument. 
            // https://github.com/dotnet/roslyn/issues/6677
            var client = new CoreClrBuildClient(language, compileFunc);
            var clientDir = AppContext.BaseDirectory;
            var workingDir = Directory.GetCurrentDirectory();
            var buildPaths = new BuildPaths(clientDir: clientDir, workingDir: workingDir, sdkDir: null);
            return client.RunCompilation(arguments, buildPaths);
        }

        protected override int RunLocalCompilation(List<string> arguments, string clientDir, string sdkDir)
        {
            return _compileFunc(clientDir, sdkDir, arguments.ToArray(), CoreClrAnalyzerAssemblyLoader.CreateAndSetDefault());
        }

        protected override async Task<BuildResponse> RunServerCompilation(List<string> arguments, BuildPaths buildPaths, string keepAlive, string libDirectory, CancellationToken cancellationToken)
        {
            var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", port: 12000).ConfigureAwait(true);

            var request = BuildRequest.Create(_language, buildPaths.WorkingDirectory, arguments, keepAlive, libDirectory);
            await request.WriteAsync(client.GetStream(), cancellationToken).ConfigureAwait(true);
            return await BuildResponse.ReadAsync(client.GetStream(), cancellationToken).ConfigureAwait(true);
        }
    }
}
