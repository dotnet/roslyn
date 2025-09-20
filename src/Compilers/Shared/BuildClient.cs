// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
#if NET472
using System.Runtime;
#else
using System.Runtime.Loader;
#endif
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CommandLine
{
    internal delegate int CompileFunc(string[] arguments, BuildPaths buildPaths, TextWriter textWriter, IAnalyzerAssemblyLoader analyzerAssemblyLoader);
    internal delegate Task<BuildResponse> CompileOnServerFunc(BuildRequest buildRequest, string pipeName, CancellationToken cancellationToken);

    internal readonly struct RunCompilationResult
    {
        internal static readonly RunCompilationResult Succeeded = new RunCompilationResult(CommonCompiler.Succeeded);

        internal static readonly RunCompilationResult Failed = new RunCompilationResult(CommonCompiler.Failed);

        internal int ExitCode { get; }

        internal bool RanOnServer { get; }

        internal RunCompilationResult(int exitCode, bool ranOnServer = false)
        {
            ExitCode = exitCode;
            RanOnServer = ranOnServer;
        }
    }

    /// <summary>
    /// Client class that handles communication to the server.
    /// </summary>
    internal sealed class BuildClient
    {
        internal static bool IsRunningOnWindows => Path.DirectorySeparatorChar == '\\';

        private readonly ICompilerServerLogger _logger;
        private readonly RequestLanguage _language;
        private readonly CompileFunc _compileFunc;
        private readonly CompileOnServerFunc _compileOnServerFunc;

        /// <summary>
        /// When set it overrides all timeout values in milliseconds when communicating with the server.
        /// </summary>
        internal BuildClient(ICompilerServerLogger logger, RequestLanguage language, CompileFunc compileFunc, CompileOnServerFunc compileOnServerFunc)
        {
            _logger = logger;
            _language = language;
            _compileFunc = compileFunc;
            _compileOnServerFunc = compileOnServerFunc;
        }

        /// <summary>
        /// Get the directory which contains the csc, vbc and VBCSCompiler clients. 
        /// 
        /// Historically this is referred to as the "client" directory but maybe better if it was 
        /// called the "installation" directory.
        /// 
        /// It is important that this method exist here and not on <see cref="BuildServerConnection"/>. This
        /// can only reliably be called from our executable projects and this file is only linked into 
        /// those projects while <see cref="BuildServerConnection"/> is also included in the MSBuild 
        /// task.
        /// </summary>
        public static string GetClientDirectory() =>
            // VBCSCompiler is installed in the same directory as csc.exe and vbc.exe which is also the 
            // location of the response files.
            //
            // BaseDirectory was mistakenly marked as potentially null in 3.1
            // https://github.com/dotnet/runtime/pull/32486
            AppDomain.CurrentDomain.BaseDirectory!;

        /// <summary>
        /// Returns the directory that contains mscorlib, or null when running on CoreCLR.
        /// </summary>
        public static string? GetSystemSdkDirectory()
        {
            return RuntimeHostInfo.IsCoreClrRuntime
                ? null
                : RuntimeEnvironment.GetRuntimeDirectory();
        }

        internal static int Run(
            IEnumerable<string> arguments,
            RequestLanguage language,
            CompileFunc compileFunc,
            CompileOnServerFunc compileOnServerFunc,
            ICompilerServerLogger logger)
        {
            var sdkDir = GetSystemSdkDirectory();
            if (RuntimeHostInfo.IsCoreClrRuntime)
            {
                // Register encodings for console
                // https://github.com/dotnet/roslyn/issues/10785
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            }

            var client = new BuildClient(logger, language, compileFunc, compileOnServerFunc);
            var clientDir = GetClientDirectory();
            var workingDir = Directory.GetCurrentDirectory();
            var tempDir = Path.GetTempPath();
            var buildPaths = new BuildPaths(clientDir: clientDir, workingDir: workingDir, sdkDir: sdkDir, tempDir: tempDir);
            var originalArguments = GetCommandLineArgs(arguments);
            return client.RunCompilation(originalArguments, buildPaths).ExitCode;
        }

        /// <summary>
        /// Run a compilation through the compiler server and print the output
        /// to the console. If the compiler server fails, run the fallback
        /// compiler.
        /// </summary>
        internal RunCompilationResult RunCompilation(IEnumerable<string> originalArguments, BuildPaths buildPaths, TextWriter? textWriter = null, string? pipeName = null)
        {
            textWriter = textWriter ?? Console.Out;

            var args = originalArguments.Select(arg => arg.Trim()).ToArray();

            List<string>? parsedArgs;
            bool hasShared;
            string? keepAliveOpt;
            string? errorMessageOpt;
            if (CommandLineParser.TryParseClientArgs(
                    args,
                    out parsedArgs,
                    out hasShared,
                    out keepAliveOpt,
                    out string? commandLinePipeName,
                    out errorMessageOpt))
            {
                pipeName ??= commandLinePipeName;
            }
            else
            {
                textWriter.WriteLine(errorMessageOpt);
                return RunCompilationResult.Failed;
            }

            if (hasShared)
            {
                pipeName = pipeName ?? BuildServerConnection.GetPipeName(buildPaths.ClientDirectory);
                var libDirectory = Environment.GetEnvironmentVariable("LIB");
                var serverResult = RunServerCompilation(textWriter, parsedArgs, buildPaths, libDirectory, pipeName, keepAliveOpt);
                if (serverResult.HasValue)
                {
                    Debug.Assert(serverResult.Value.RanOnServer);
                    return serverResult.Value;
                }

                _logger.Log("Server build failed, falling back to local build");
            }

            // It's okay, and expected, for the server compilation to fail.  In that case just fall 
            // back to normal compilation. 
            var exitCode = RunLocalCompilation(parsedArgs.ToArray(), buildPaths, textWriter);
            return new RunCompilationResult(exitCode);
        }

        public Task<RunCompilationResult> RunCompilationAsync(IEnumerable<string> originalArguments, BuildPaths buildPaths, TextWriter? textWriter = null)
        {
            var tcs = new TaskCompletionSource<RunCompilationResult>();
            ThreadStart action = () =>
            {
                try
                {
                    var result = RunCompilation(originalArguments, buildPaths, textWriter);
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            };

            var thread = new Thread(action);
            thread.Start();

            return tcs.Task;
        }

        private int RunLocalCompilation(string[] arguments, BuildPaths buildPaths, TextWriter textWriter)
        {
            var loader = new AnalyzerAssemblyLoader();
            return _compileFunc(arguments, buildPaths, textWriter, loader);
        }

        public static CompileOnServerFunc GetCompileOnServerFunc(ICompilerServerLogger logger) => (buildRequest, pipeName, cancellationToken) =>
            BuildServerConnection.RunServerBuildRequestAsync(
                buildRequest,
                pipeName,
                GetClientDirectory(),
                logger,
                cancellationToken);

        /// <summary>
        /// Runs the provided compilation on the server.  If the compilation cannot be completed on the server then null
        /// will be returned.
        /// </summary>
        private RunCompilationResult? RunServerCompilation(TextWriter textWriter, List<string> arguments, BuildPaths buildPaths, string? libDirectory, string pipeName, string? keepAlive)
        {
            BuildResponse buildResponse;

            if (!AreNamedPipesSupported())
            {
                return null;
            }

            try
            {
                var requestId = Guid.NewGuid().ToString();
                var buildRequest = BuildServerConnection.CreateBuildRequest(
                    requestId,
                    _language,
                    arguments,
                    workingDirectory: buildPaths.WorkingDirectory,
                    tempDirectory: buildPaths.TempDirectory,
                    keepAlive: keepAlive,
                    libDirectory: libDirectory);

                var buildResponseTask = _compileOnServerFunc(
                    buildRequest,
                    pipeName,
                    cancellationToken: default);

                buildResponse = buildResponseTask.Result;

                Debug.Assert(buildResponse != null);
                if (buildResponse == null)
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Server compilation failed");
                return null;
            }

            _logger.Log($"Server compilation completed: {buildResponse.Type}");
            switch (buildResponse.Type)
            {
                case BuildResponse.ResponseType.Completed:
                    {
                        var completedResponse = (CompletedBuildResponse)buildResponse;
                        return ConsoleUtil.RunWithUtf8Output(completedResponse.Utf8Output, textWriter, tw =>
                        {
                            tw.Write(completedResponse.Output);
                            return new RunCompilationResult(completedResponse.ReturnCode, ranOnServer: true);
                        });
                    }

                case BuildResponse.ResponseType.MismatchedVersion:
                case BuildResponse.ResponseType.IncorrectHash:
                case BuildResponse.ResponseType.Rejected:
                case BuildResponse.ResponseType.AnalyzerInconsistency:
                case BuildResponse.ResponseType.CannotConnect:
                    // Build could not be completed on the server.
                    return null;
                default:
                    // Will not happen with our server but hypothetically could be sent by a rogue server.  Should
                    // not let that block compilation.
                    Debug.Assert(false);
                    return null;
            }
        }

        private static IEnumerable<string> GetCommandLineArgs(IEnumerable<string> args)
        {
            if (UseNativeArguments())
            {
                return GetCommandLineWindows(args);
            }

            return args;
        }

        private static bool UseNativeArguments()
        {
            if (!IsRunningOnWindows)
            {
                return false;
            }

            if (PlatformInformation.IsRunningOnMono)
            {
                return false;
            }

            if (RuntimeHostInfo.IsCoreClrRuntime)
            {
                // The native invoke ends up giving us both CoreRun and the exe file.
                // We've decided to ignore backcompat for CoreCLR,
                // and use the Main()-provided arguments
                // https://github.com/dotnet/roslyn/issues/6677
                return false;
            }

            return true;
        }

        private static bool AreNamedPipesSupported()
        {
            if (!PlatformInformation.IsRunningOnMono)
                return true;

            IDisposable? npcs = null;
            try
            {
                var testPipeName = $"mono-{Guid.NewGuid()}";
                // Mono configurations without named pipe support will throw a PNSE at some point in this process.
                npcs = new NamedPipeClientStream(".", testPipeName, PipeDirection.InOut);
                npcs.Dispose();
                return true;
            }
            catch (PlatformNotSupportedException)
            {
                if (npcs != null)
                {
                    // Compensate for broken finalizer in older builds of mono
                    // https://github.com/mono/mono/commit/2a731f29b065392ca9b44d6613abee2aa413a144
                    GC.SuppressFinalize(npcs);
                }
                return false;
            }
        }

        /// <summary>
        /// When running on Windows we can't take the command line which was provided to the 
        /// Main method of the application.  That will go through normal windows command line 
        /// parsing which eliminates artifacts like quotes.  This has the effect of normalizing
        /// the below command line options, which are semantically different, into the same
        /// value:
        ///
        ///     /reference:a,b
        ///     /reference:"a,b"
        ///
        /// To get the correct semantics here on Windows we parse the original command line 
        /// provided to the process. 
        /// </summary>
        private static IEnumerable<string> GetCommandLineWindows(IEnumerable<string> args)
        {
            Debug.Assert(PlatformInformation.IsWindows);

            IntPtr ptr = NativeMethods.GetCommandLine();
            if (ptr == IntPtr.Zero)
            {
                return args;
            }

            // This memory is owned by the operating system hence we shouldn't (and can't)
            // free the memory.  
            var commandLine = Marshal.PtrToStringUni(ptr)!;

            // The first argument will be the executable name hence we skip it. 
            return CommandLineParser.SplitCommandLineIntoArguments(commandLine, removeHashComments: false).Skip(1);
        }
    }
}
