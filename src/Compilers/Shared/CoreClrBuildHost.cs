// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CompilerServer;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

        public IEnumerable<string> OriginalArguments => _originalArguments;

        internal CoreClrBuildHost(IEnumerable<string> arguments, Func<string, string, string[], IAnalyzerAssemblyLoader, int> compileFunc)
        {
            _originalArguments = BuildClient.GetCommandLineArgs(arguments);
            _clientDir = AppContext.BaseDirectory;
            _compileFunc = compileFunc;
        }

        public int RunCompilation(List<string> arguments)
        {
            return _compileFunc(_clientDir, null, arguments.ToArray(), CoreClrAnalyzerAssemblyLoader.CreateAndSetDefault());
        }

        public Task<BuildResponse> TryRunServerCompilation(List<string> arguments, string keepAlive, string libEnvVariable, CancellationToken cancellationToken)
        {
            return Task.FromResult<BuildResponse>(null);
        }
    }
}
