// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
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
        private readonly IAnalyzerAssemblyLoader _analyzerAssemblyLoader;

        private CoreClrBuildClient(RequestLanguage language, CompileFunc compileFunc, IAnalyzerAssemblyLoader analyzerAssemblyLoader)
        {
            _language = language;
            _compileFunc = compileFunc;
            _analyzerAssemblyLoader = analyzerAssemblyLoader;
        }

        internal static int Run(IEnumerable<string> arguments, RequestLanguage language, CompileFunc compileFunc, IAnalyzerAssemblyLoader analyzerAssemblyLoader)
        {
            // Register encodings for console
            // https://github.com/dotnet/roslyn/issues/10785 
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Should be using BuildClient.GetCommandLineArgs(arguments) here.  But the native invoke
            // ends up giving us both CoreRun and the exe file.  Need to find a good way to remove the host
            // as well as the EXE argument.
            // https://github.com/dotnet/roslyn/issues/6677
            var client = new CoreClrBuildClient(language, compileFunc, analyzerAssemblyLoader);
            var clientDir = AppContext.BaseDirectory;
            var workingDir = Directory.GetCurrentDirectory();
            var tempDir = Path.GetTempPath();
            var buildPaths = new BuildPaths(clientDir: clientDir, workingDir: workingDir, sdkDir: null, tempDir: tempDir);
            return client.RunCompilation(arguments, buildPaths).ExitCode;
        }

        protected override int RunLocalCompilation(string[] arguments, BuildPaths buildPaths, TextWriter textWriter)
        {
            return _compileFunc(arguments, buildPaths, textWriter, _analyzerAssemblyLoader);
        }

        protected override string GetSessionKey(BuildPaths buildPaths)
        {
            return string.Empty;
        }

        protected override async Task<BuildResponse> RunServerCompilation(List<string> arguments, BuildPaths buildPaths, string pipeName, string keepAlive, string libDirectory, CancellationToken cancellationToken)
        {
            var client = new TcpClient();
            var port = int.Parse(pipeName);
            await client.ConnectAsync("127.0.0.1", port: port).ConfigureAwait(true);

            var request = BuildRequest.Create(_language, buildPaths.WorkingDirectory, buildPaths.TempDirectory, arguments, keepAlive, libDirectory);
            await request.WriteAsync(client.GetStream(), cancellationToken).ConfigureAwait(true);
            var ret = await BuildResponse.ReadAsync(client.GetStream(), cancellationToken).ConfigureAwait(true);
            return ret;
        }
    }
}
