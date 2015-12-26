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

using static Microsoft.CodeAnalysis.CommandLine.CompilerServerLogger;

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

        public abstract bool CheckAnalyzers(string baseDirectory, ImmutableArray<CommandLineAnalyzerReference> analyzers);

        public bool TryCreateCompiler(RunRequest request, out CommonCompiler compiler)
        {
            switch (request.Language)
            {
                case LanguageNames.CSharp:
                    compiler = new CSharpCompilerServer(
                        AssemblyReferenceProvider,
                        args: request.Arguments,
                        clientDirectory: ClientDirectory,
                        baseDirectory: request.CurrentDirectory,
                        sdkDirectory: SdkDirectory,
                        libDirectory: request.LibDirectory,
                        analyzerLoader: AnalyzerAssemblyLoader);
                    return true;
                case LanguageNames.VisualBasic:
                    compiler = new VisualBasicCompilerServer(
                        AssemblyReferenceProvider,
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

        public BuildResponse RunCompilation(RunRequest request, CancellationToken cancellationToken)
        {
            Log($"CurrentDirectory = '{request.CurrentDirectory}'");
            Log($"LIB = '{request.LibDirectory}'");
            for (int i = 0; i < request.Arguments.Length; ++i)
            {
                Log($"Argument[{i}] = '{request.Arguments[i]}'");
            }

            CommonCompiler compiler;
            if (!TryCreateCompiler(request, out compiler))
            {
                // TODO: is this the right option?  Right now it will cause the fall back in the command line 
                // to fail because it's a valid response.  Probably should add a "server failed" response
                // for command line to deal with.

                // We can't do anything with a request we don't know about. 
                Log($"Got request with id '{request.Language}'");
                return new CompletedBuildResponse(returnCode:  -1, utf8output: false, output:  "", errorOutput: "");
            }

            bool utf8output = compiler.Arguments.Utf8Output;
            if (!CheckAnalyzers(request.CurrentDirectory, compiler.Arguments.AnalyzerReferences))
            {
                return new AnalyzerInconsistencyBuildResponse();
            }

            Log($"****Running {request.Language} compiler...");
            TextWriter output = new StringWriter(CultureInfo.InvariantCulture);
            int returnCode = compiler.Run(output, cancellationToken);
            Log($"****{request.Language} Compilation complete.\r\n****Return code: {returnCode}\r\n****Output:\r\n{output.ToString()}\r\n");
            return new CompletedBuildResponse(returnCode, utf8output, output.ToString(), "");
        }
    }
}
