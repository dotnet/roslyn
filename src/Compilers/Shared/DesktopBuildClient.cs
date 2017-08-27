// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

namespace Microsoft.CodeAnalysis.CommandLine
{
    internal class DesktopBuildClient : BuildClient
    {
        private readonly RequestLanguage _language;
        private readonly CompileFunc _compileFunc;
        private readonly IAnalyzerAssemblyLoader _analyzerAssemblyLoader;

        /// <summary>
        /// When set it overrides all timeout values in milliseconds when communicating with the server.
        /// </summary>
        internal int? TimeoutOverride { get; set; }

        internal DesktopBuildClient(RequestLanguage language, CompileFunc compileFunc, IAnalyzerAssemblyLoader analyzerAssemblyLoader)
        {
            _language = language;
            _compileFunc = compileFunc;
            _analyzerAssemblyLoader = analyzerAssemblyLoader;
        }

        internal static int Run(IEnumerable<string> arguments, RequestLanguage language, CompileFunc compileFunc, IAnalyzerAssemblyLoader analyzerAssemblyLoader)
        {
            var client = new DesktopBuildClient(language, compileFunc, analyzerAssemblyLoader);
            var clientDir = AppContext.BaseDirectory;
            var sdkDir = GetRuntimeDirectoryOpt();
            var workingDir = Directory.GetCurrentDirectory();
            var tempDir = BuildServerConnection.GetTempPath(workingDir);
            var buildPaths = new BuildPaths(clientDir: clientDir, workingDir: workingDir, sdkDir: sdkDir, tempDir: tempDir);
            var originalArguments = GetCommandLineArgs(arguments).ToArray();
            return client.RunCompilation(originalArguments, buildPaths).ExitCode;
        }

        internal static string GetRuntimeDirectoryOpt()
        {
            Type runtimeEnvironmentType = Roslyn.Utilities.ReflectionUtilities.TryGetType(
                "System.Runtime.InteropServices.RuntimeEnvironment, " +
                "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");

            return (string)runtimeEnvironmentType
                ?.GetTypeInfo()
                .GetDeclaredMethod("GetRuntimeDirectory")
                ?.Invoke(obj: null, parameters: null);
        }

        protected override int RunLocalCompilation(string[] arguments, BuildPaths buildPaths, TextWriter textWriter)
        {
            return _compileFunc(arguments, buildPaths, textWriter, _analyzerAssemblyLoader);
        }

        protected override Task<BuildResponse> RunServerCompilation(
            List<string> arguments,
            BuildPaths buildPaths,
            string sessionKey,
            string keepAlive,
            string libDirectory,
            CancellationToken cancellationToken)
        {
            return RunServerCompilationCore(_language, arguments, buildPaths, sessionKey, keepAlive, libDirectory, TimeoutOverride, TryCreateServer, cancellationToken);
        }

        public static Task<BuildResponse> RunServerCompilation(
            RequestLanguage language,
            List<string> arguments,
            BuildPaths buildPaths,
            string keepAlive,
            string libEnvVariable,
            CancellationToken cancellationToken)
        {
            var pipeNameOpt = BuildServerConnection.GetPipeNameForPathOpt(buildPaths.ClientDirectory);

            return RunServerCompilationCore(
                language,
                arguments,
                buildPaths,
                pipeNameOpt,
                keepAlive,
                libEnvVariable,
                timeoutOverride: null,
                tryCreateServerFunc: BuildServerConnection.TryCreateServerCore,
                cancellationToken: cancellationToken);
        }

        private static Task<BuildResponse> RunServerCompilationCore(
            RequestLanguage language,
            List<string> arguments,
            BuildPaths buildPaths,
            string pipeName,
            string keepAlive,
            string libEnvVariable,
            int? timeoutOverride,
            Func<string, string, bool> tryCreateServerFunc,
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
                tryCreateServerFunc,
                cancellationToken);
        }

        /// <summary>
        /// Create a new instance of the server process, returning true on success
        /// and false otherwise.
        /// </summary>
        protected virtual bool TryCreateServer(string clientDir, string pipeName)
        {
            return BuildServerConnection.TryCreateServerCore(clientDir, pipeName);
        }

        /// <summary>
        /// Given the full path to the directory containing the compiler exes,
        /// retrieves the name of the pipe for client/server communication on
        /// that instance of the compiler.
        /// </summary>
        protected override string GetSessionKey(BuildPaths buildPaths)
        {
            return BuildServerConnection.GetPipeNameForPathOpt(buildPaths.ClientDirectory);
        }
    }
}
