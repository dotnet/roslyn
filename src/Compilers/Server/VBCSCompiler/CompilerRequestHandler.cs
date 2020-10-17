﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    internal readonly struct RunRequest
    {
        public string Language { get; }
        public string CurrentDirectory { get; }
        public string TempDirectory { get; }
        public string LibDirectory { get; }
        public string[] Arguments { get; }

        public RunRequest(string language, string currentDirectory, string tempDirectory, string libDirectory, string[] arguments)
        {
            Language = language;
            CurrentDirectory = currentDirectory;
            TempDirectory = tempDirectory;
            LibDirectory = libDirectory;
            Arguments = arguments;
        }
    }

    internal sealed class CompilerServerHost : ICompilerServerHost
    {
        public IAnalyzerAssemblyLoader AnalyzerAssemblyLoader { get; } = new ShadowCopyAnalyzerAssemblyLoader(Path.Combine(Path.GetTempPath(), "VBCSCompiler", "AnalyzerAssemblyLoader"));

        public static Func<string, MetadataReferenceProperties, PortableExecutableReference> SharedAssemblyReferenceProvider { get; } = (path, properties) => new CachingMetadataReference(path, properties);

        /// <summary>
        /// The caching metadata provider used by the C# and VB compilers
        /// </summary>
        private Func<string, MetadataReferenceProperties, PortableExecutableReference> AssemblyReferenceProvider { get; } = SharedAssemblyReferenceProvider;

        /// <summary>
        /// Directory that contains the compiler executables and the response files. 
        /// </summary>
        private string ClientDirectory { get; }

        /// <summary>
        /// Directory that contains mscorlib.  Can be null when the host is executing in a CoreCLR context.
        /// </summary>
        private string SdkDirectory { get; }

        internal CompilerServerHost(string clientDirectory, string sdkDirectory)
        {
            ClientDirectory = clientDirectory;
            SdkDirectory = sdkDirectory;
        }

        private bool CheckAnalyzers(string baseDirectory, ImmutableArray<CommandLineAnalyzerReference> analyzers)
        {
            return AnalyzerConsistencyChecker.Check(baseDirectory, analyzers, AnalyzerAssemblyLoader);
        }

        public bool TryCreateCompiler(RunRequest request, out CommonCompiler compiler)
        {
            var buildPaths = new BuildPaths(ClientDirectory, request.CurrentDirectory, SdkDirectory, request.TempDirectory);
            switch (request.Language)
            {
                case LanguageNames.CSharp:
                    compiler = new CSharpCompilerServer(
                        AssemblyReferenceProvider,
                        args: request.Arguments,
                        buildPaths: buildPaths,
                        libDirectory: request.LibDirectory,
                        analyzerLoader: AnalyzerAssemblyLoader);
                    return true;
                case LanguageNames.VisualBasic:
                    compiler = new VisualBasicCompilerServer(
                        AssemblyReferenceProvider,
                        args: request.Arguments,
                        buildPaths: buildPaths,
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
            Log($@"
Run Compilation
  CurrentDirectory = '{request.CurrentDirectory}
  LIB = '{request.LibDirectory}'");

            // Compiler server must be provided with a valid temporary directory in order to correctly
            // isolate signing between compilations.
            if (string.IsNullOrEmpty(request.TempDirectory))
            {
                return new RejectedBuildResponse("Missing temp directory");
            }

            CommonCompiler compiler;
            if (!TryCreateCompiler(request, out compiler))
            {
                return new RejectedBuildResponse($"Cannot create compiler for language id {request.Language}");
            }

            bool utf8output = compiler.Arguments.Utf8Output;
            if (!CheckAnalyzers(request.CurrentDirectory, compiler.Arguments.AnalyzerReferences))
            {
                return new AnalyzerInconsistencyBuildResponse();
            }

            Log($"Begin {request.Language} compiler run");
            TextWriter output = new StringWriter(CultureInfo.InvariantCulture);
            int returnCode = compiler.Run(output, cancellationToken);
            var outputString = output.ToString();
            Log(@$"
End {request.Language} Compilation complete.
Return code: {returnCode}
Output:
{outputString}");
            return new CompletedBuildResponse(returnCode, utf8output, outputString);
        }
    }
}
