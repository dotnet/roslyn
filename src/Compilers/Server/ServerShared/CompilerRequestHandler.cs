// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal struct RunRequest
    {
        public string Language { get; }
        public string CurrentDirectory { get; }
        public string LibDirectory { get; }
        public string[] Arguments { get; }

        public RunRequest(string language, string currentDirectory, string libDirectory, string[] arguments)
        {
            Language = language;
            CurrentDirectory = currentDirectory;
            LibDirectory = libDirectory;
            Arguments = arguments;
        }
    }

    internal interface IRequestHandler
    {
        BuildResponse HandleRequest(BuildRequest req, CancellationToken cancellationToken);
    }

    internal sealed class CompilerRequestHandler : IRequestHandler
    {
        private readonly ICompilerServerHost _compilerServerHost;

        internal CompilerRequestHandler(ICompilerServerHost compilerServerHost)
        {
            _compilerServerHost = compilerServerHost;
        }

        /// <summary>
        /// An incoming request as occurred. This is called on a new thread to handle
        /// the request.
        /// </summary>
        public BuildResponse HandleRequest(BuildRequest buildRequest, CancellationToken cancellationToken)
        {
            var request = BuildProtocolUtil.GetRunRequest(buildRequest);
            CommonCompiler compiler;
            if (!_compilerServerHost.TryCreateCompiler(request, out compiler))
            {
                // We can't do anything with a request we don't know about. 
                _compilerServerHost.Log($"Got request with id '{request.Language}'");
                return new CompletedBuildResponse(-1, false, "", "");
            }

            _compilerServerHost.Log($"CurrentDirectory = '{request.CurrentDirectory}'");
            _compilerServerHost.Log($"LIB = '{request.LibDirectory}'");
            for (int i = 0; i < request.Arguments.Length; ++i)
            {
                _compilerServerHost.Log($"Argument[{i}] = '{request.Arguments[i]}'");
            }

            bool utf8output = compiler.Arguments.Utf8Output;
            if (!_compilerServerHost.CheckAnalyzers(request.CurrentDirectory, compiler.Arguments.AnalyzerReferences))
            {
                return new AnalyzerInconsistencyBuildResponse();
            }

            _compilerServerHost.Log($"****Running {request.Language} compiler...");
            TextWriter output = new StringWriter(CultureInfo.InvariantCulture);
            int returnCode = compiler.Run(output, cancellationToken);
            _compilerServerHost.Log($"****{request.Language} Compilation complete.\r\n****Return code: {returnCode}\r\n****Output:\r\n{output.ToString()}\r\n");
            return new CompletedBuildResponse(returnCode, utf8output, output.ToString(), "");
        }
    }

    internal abstract class CompilerServerHost : ICompilerServerHost
    {
        public abstract IAnalyzerAssemblyLoader AnalyzerAssemblyLoader { get; }

        public abstract Func<string, MetadataReferenceProperties, PortableExecutableReference> AssemblyReferenceProvider { get; }

        /// <summary>
        /// Directory that contains the compiler executables and the response files. 
        /// </summary>
        public string ClientDirectory { get; }

        /// <summary>
        /// Directory that contains mscorlib.  Can be null when the host is executing in a CoreCLR context.
        /// </summary>
        public string SdkDirectory { get; }

        protected CompilerServerHost(string clientDirectory, string sdkDirectory)
        {
            ClientDirectory = clientDirectory;
            SdkDirectory = sdkDirectory;
        }

        public abstract Task<IClientConnection> CreateListenTask(CancellationToken cancellationToken);

        public abstract bool CheckAnalyzers(string baseDirectory, ImmutableArray<CommandLineAnalyzerReference> analyzers);

        public abstract void Log(string message);

        public bool TryCreateCompiler(RunRequest request, out CommonCompiler compiler)
        {
            switch (request.Language)
            {
                case LanguageNames.CSharp:
                    compiler = new CSharpCompilerServer(
                        this,
                        args: request.Arguments,
                        clientDirectory: ClientDirectory,
                        baseDirectory: request.CurrentDirectory,
                        sdkDirectory: SdkDirectory,
                        libDirectory: request.LibDirectory,
                        analyzerLoader: AnalyzerAssemblyLoader);
                    return true;
                case LanguageNames.VisualBasic:
                    compiler = new VisualBasicCompilerServer(
                        this,
                        args: request.Arguments,
                        clientDirectory: ClientDirectory,
                        baseDirectory: request.CurrentDirectory,
                        sdkDirectory: SdkDirectory,
                        libDirectory: request.LibDirectory,
                        analyzerLoader: AnalyzerAssemblyLoader);
                    return true;
                default:
                    compiler = null;
                    return false;
            }
        }
    }
}
