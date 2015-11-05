// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CompilerServer;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.BuildTasks.NativeMethods;
using static Microsoft.CodeAnalysis.CompilerServer.BuildProtocolConstants;
using static Microsoft.CodeAnalysis.CompilerServer.CompilerServerLogger;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// Client class that handles communication to the server.
    /// </summary>
    internal sealed class DesktopBuildHost : IBuildHost
    {
        private const string s_serverName = "VBCSCompiler.exe";
        // Spend up to 1s connecting to existing process (existing processes should be always responsive).
        private const int TimeOutMsExistingProcess = 1000;
        // Spend up to 20s connecting to a new process, to allow time for it to start.
        private const int TimeOutMsNewProcess = 20000;

        private readonly string _sdkDir;
        private readonly string _clientDir;
        private readonly string _workingDir;
        private readonly string[] _originalArguments;
        private readonly RequestLanguage _language;
        private readonly Func<string, string, string[], IAnalyzerAssemblyLoader, int> _compileFunc;

        public IEnumerable<string> OriginalArguments => _originalArguments;

        internal DesktopBuildHost(IEnumerable<string> arguments, IEnumerable<string> extraArguments, RequestLanguage language, Func<string, string, string[], IAnalyzerAssemblyLoader, int> compileFunc)
        {
            _clientDir = AppDomain.CurrentDomain.BaseDirectory;
            _workingDir = Directory.GetCurrentDirectory();
            _sdkDir = RuntimeEnvironment.GetRuntimeDirectory();
            _language = RequestLanguage.VisualBasicCompile;

            _originalArguments = BuildClient.GetCommandLineArgs(arguments).Concat(extraArguments).ToArray();
            _compileFunc = compileFunc;
        }

        public int RunCompilation(List<string> arguments) => _compileFunc(_clientDir, _sdkDir, arguments.ToArray(), new SimpleAnalyzerAssemblyLoader());

        /// <summary>
        /// Returns a Task with a null BuildResponse if no server
        /// response was received.
        /// </summary>
        public Task<BuildResponse> TryRunServerCompilation(
            List<string> arguments,
            string keepAlive,
            string libEnvVariable,
            CancellationToken cancellationToken)
        {
            return DesktopBuildClient.TryRunServerCompilation(
                _language,
                _clientDir,
                _workingDir,
                arguments,
                keepAlive,
                libEnvVariable,
                cancellationToken);
        }
    }
}
