// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CommandLine
{
    internal delegate bool CreateServerFunc(string clientDir, string pipeName);

    internal class DesktopBuildClient : BuildClient
    {
        private readonly RequestLanguage _language;
        private readonly CompileFunc _compileFunc;
        private readonly CreateServerFunc _createServerFunc;
        private readonly int? _timeoutOverride;

        /// <summary>
        /// When set it overrides all timeout values in milliseconds when communicating with the server.
        /// </summary>

        internal DesktopBuildClient(RequestLanguage language, CompileFunc compileFunc, CreateServerFunc createServerFunc = null, int? timeoutOverride = null)
        {
            _language = language;
            _compileFunc = compileFunc;
            _createServerFunc = createServerFunc ?? BuildServerConnection.TryCreateServerCore;
            _timeoutOverride = timeoutOverride;
        }

        internal static int Run(IEnumerable<string> arguments, RequestLanguage language, CompileFunc compileFunc)
        {
            var sdkDir = GetSystemSdkDirectory();
            if (RuntimeHostInfo.IsCoreClrRuntime)
            {
                // Register encodings for console
                // https://github.com/dotnet/roslyn/issues/10785
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            }

            var client = new DesktopBuildClient(language, compileFunc);
            var clientDir = AppContext.BaseDirectory;
            var workingDir = Directory.GetCurrentDirectory();
            var tempDir = BuildServerConnection.GetTempPath(workingDir);
            var buildPaths = new BuildPaths(clientDir: clientDir, workingDir: workingDir, sdkDir: sdkDir, tempDir: tempDir);
            var originalArguments = GetCommandLineArgs(arguments);
            return client.RunCompilation(originalArguments, buildPaths).ExitCode;
        }

        protected override int RunLocalCompilation(string[] arguments, BuildPaths buildPaths, TextWriter textWriter)
        {
            var loader = CreateAnalyzerAssemblyLoader();
            return _compileFunc(arguments, buildPaths, textWriter, loader);
        }

        protected override Task<BuildResponse> RunServerCompilation(
            List<string> arguments,
            BuildPaths buildPaths,
            string sessionKey,
            string keepAlive,
            string libDirectory,
            CancellationToken cancellationToken)
        {
            return RunServerCompilationCore(_language, arguments, buildPaths, sessionKey, keepAlive, libDirectory, _timeoutOverride, _createServerFunc, cancellationToken);
        }

        private static Task<BuildResponse> RunServerCompilationCore(
            RequestLanguage language,
            List<string> arguments,
            BuildPaths buildPaths,
            string pipeName,
            string keepAlive,
            string libEnvVariable,
            int? timeoutOverride,
            CreateServerFunc createServerFunc,
            CancellationToken cancellationToken)
        {
            var alt = new BuildPathsAlt(
                buildPaths.ClientDirectory,
                buildPaths.WorkingDirectory,
                buildPaths.SdkDirectory,
                buildPaths.TempDirectory);

            return BuildServerConnection.RunServerCompilationCore(
                language,
                arguments,
                alt,
                pipeName,
                keepAlive,
                libEnvVariable,
                timeoutOverride,
                createServerFunc,
                cancellationToken);
        }

        /// <summary>
        /// Given the full path to the directory containing the compiler exes,
        /// retrieves the name of the pipe for client/server communication on
        /// that instance of the compiler.
        /// </summary>
        protected override string GetPipeName(BuildPaths buildPaths)
        {
            return BuildServerConnection.GetPipeNameForPathOpt(buildPaths.ClientDirectory);
        }
    }
}
