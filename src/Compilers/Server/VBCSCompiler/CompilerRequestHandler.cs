// Licensed to the .NET Foundation under one or more agreements.
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
        public string RequestId { get; }
        public string Language { get; }
        public string? WorkingDirectory { get; }
        public string? TempDirectory { get; }
        public string? LibDirectory { get; }
        public string[] Arguments { get; }

        public RunRequest(string requestId, string language, string? workingDirectory, string? tempDirectory, string? libDirectory, string[] arguments)
        {
            RequestId = requestId;
            Language = language;
            WorkingDirectory = workingDirectory;
            TempDirectory = tempDirectory;
            LibDirectory = libDirectory;
            Arguments = arguments;
        }
    }

    internal sealed class CompilerServerHost : ICompilerServerHost
    {
        public IAnalyzerAssemblyLoaderInternal AnalyzerAssemblyLoader { get; }

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
        private string? SdkDirectory { get; }

        public ICompilerServerLogger Logger { get; }

        /// <summary>
        /// A cache that can store generator drivers in order to enable incrementalism across builds for the lifetime of the server.
        /// </summary>
        private readonly GeneratorDriverCache _driverCache = new GeneratorDriverCache();

        internal CompilerServerHost(string clientDirectory, string? sdkDirectory, ICompilerServerLogger logger)
        {
            ClientDirectory = clientDirectory;
            SdkDirectory = sdkDirectory;
            Logger = logger;

            var path = Path.Combine(Path.GetTempPath(), "VBCSCompiler", "AnalyzerAssemblyLoader");
#if NET
            AnalyzerAssemblyLoader = DefaultAnalyzerAssemblyLoader.CreateNonLockingLoader(loadContext: null, path);
#else
            AnalyzerAssemblyLoader = DefaultAnalyzerAssemblyLoader.CreateNonLockingLoader(path);
#endif
        }

        public bool TryCreateCompiler(in RunRequest request, BuildPaths buildPaths, [NotNullWhen(true)] out CommonCompiler? compiler)
        {
            switch (request.Language)
            {
                case LanguageNames.CSharp:
                    compiler = new CSharpCompilerServer(
                        AssemblyReferenceProvider,
                        args: request.Arguments,
                        buildPaths: buildPaths,
                        libDirectory: request.LibDirectory,
                        analyzerLoader: AnalyzerAssemblyLoader,
                        _driverCache);
                    return true;
                case LanguageNames.VisualBasic:
                    compiler = new VisualBasicCompilerServer(
                        AssemblyReferenceProvider,
                        args: request.Arguments,
                        buildPaths: buildPaths,
                        libDirectory: request.LibDirectory,
                        analyzerLoader: AnalyzerAssemblyLoader,
                        _driverCache);
                    return true;
                default:
                    compiler = null;
                    return false;
            }
        }

        public BuildResponse RunCompilation(in RunRequest request, CancellationToken cancellationToken)
        {
            Logger.Log($@"
Run Compilation for {request.RequestId}
  Language = {request.Language}
  CurrentDirectory = '{request.WorkingDirectory}
  LIB = '{request.LibDirectory}'");

            // Compiler server must be provided with a valid current directory in order to correctly 
            // resolve files in the compilation
            if (string.IsNullOrEmpty(request.WorkingDirectory))
            {
                var message = "Missing working directory";
                Logger.Log($"Rejected: {request.RequestId}: {message}");
                return new RejectedBuildResponse(message);
            }

            // Compiler server must be provided with a valid temporary directory in order to correctly
            // isolate signing between compilations.
            if (string.IsNullOrEmpty(request.TempDirectory))
            {
                var message = "Missing temp directory";
                Logger.Log($"Rejected: {request.RequestId}: {message}");
                return new RejectedBuildResponse(message);
            }

            var buildPaths = new BuildPaths(ClientDirectory, request.WorkingDirectory, SdkDirectory, request.TempDirectory);
            if (!TryCreateCompiler(request, buildPaths, out CommonCompiler? compiler))
            {
                var message = $"Cannot create compiler for language id {request.Language}";
                Logger.Log($"Rejected: {request.RequestId}: {message}");
                return new RejectedBuildResponse(message);
            }

            if (!AnalyzerConsistencyChecker.Check(request.WorkingDirectory, compiler.Arguments.AnalyzerReferences, AnalyzerAssemblyLoader, Logger, out List<string>? errorMessages))
            {
                Logger.Log($"Rejected: {request.RequestId}: for analyzer load issues {string.Join(";", errorMessages)}");
                return new AnalyzerInconsistencyBuildResponse(new ReadOnlyCollection<string>(errorMessages));
            }

            Logger.Log($"Begin {request.RequestId} {request.Language} compiler run");
            try
            {
                CodeAnalysisEventSource.Log.StartServerCompilation(request.RequestId);
                bool utf8output = compiler.Arguments.Utf8Output;
                TextWriter output = new StringWriter(CultureInfo.InvariantCulture);
                int returnCode = compiler.Run(output, cancellationToken);
                var outputString = output.ToString();
                Logger.Log(@$"End {request.RequestId} {request.Language} compiler run
Return code: {returnCode}
Output:
{outputString}");
                return new CompletedBuildResponse(returnCode, utf8output, outputString);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Running compilation for {request.RequestId}");
                throw;
            }
            finally
            {
                CodeAnalysisEventSource.Log.StopServerCompilation(request.RequestId);
            }
        }
    }
}
