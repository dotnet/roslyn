﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        public string? WorkingDirectory { get; }
        public string? TempDirectory { get; }
        public string? LibDirectory { get; }
        public string[] Arguments { get; }

        public RunRequest(string language, string? workingDirectory, string? tempDirectory, string? libDirectory, string[] arguments)
        {
            Language = language;
            WorkingDirectory = workingDirectory;
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

        public ICompilerServerLogger Logger { get; }

        internal CompilerServerHost(string clientDirectory, string sdkDirectory, ICompilerServerLogger logger)
        {
            ClientDirectory = clientDirectory;
            SdkDirectory = sdkDirectory;
            Logger = logger;
        }

        private bool CheckAnalyzers(string baseDirectory, ImmutableArray<CommandLineAnalyzerReference> analyzers, [NotNullWhen(false)] out List<string>? errorMessages)
        {
            return AnalyzerConsistencyChecker.Check(baseDirectory, analyzers, AnalyzerAssemblyLoader, Logger, out errorMessages);
        }

        public bool TryCreateCompiler(RunRequest request, BuildPaths buildPaths, [NotNullWhen(true)] out CommonCompiler? compiler)
        {
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
            Logger.Log($@"
Run Compilation
  CurrentDirectory = '{request.WorkingDirectory}
  LIB = '{request.LibDirectory}'");

            // Compiler server must be provided with a valid current directory in order to correctly 
            // resolve files in the compilation
            if (string.IsNullOrEmpty(request.WorkingDirectory))
            {
                return new RejectedBuildResponse("Missing temp directory");
            }

            // Compiler server must be provided with a valid temporary directory in order to correctly
            // isolate signing between compilations.
            if (string.IsNullOrEmpty(request.TempDirectory))
            {
                return new RejectedBuildResponse("Missing temp directory");
            }

            var buildPaths = new BuildPaths(ClientDirectory, request.WorkingDirectory, SdkDirectory, request.TempDirectory);
            CommonCompiler? compiler;
            if (!TryCreateCompiler(request, buildPaths, out compiler))
            {
                return new RejectedBuildResponse($"Cannot create compiler for language id {request.Language}");
            }

            bool utf8output = compiler.Arguments.Utf8Output;
            if (!CheckAnalyzers(request.WorkingDirectory, compiler.Arguments.AnalyzerReferences, out List<string>? errorMessages))
            {
                return new AnalyzerInconsistencyBuildResponse(new ReadOnlyCollection<string>(errorMessages));
            }

            Logger.Log($"Begin {request.Language} compiler run");
            TextWriter output = new StringWriter(CultureInfo.InvariantCulture);
            int returnCode = compiler.Run(output, cancellationToken);
            var outputString = output.ToString();
            Logger.Log(@$"
End {request.Language} Compilation complete.
Return code: {returnCode}
Output:
{outputString}");
            return new CompletedBuildResponse(returnCode, utf8output, outputString);
        }
    }
}
