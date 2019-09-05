// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        private readonly RequestLanguage _language;
        private readonly CompileFunc _compileFunc;
        private readonly CreateServerFunc _createServerFunc;
        private readonly int? _timeoutOverride;

        /// <summary>
        /// When set it overrides all timeout values in milliseconds when communicating with the server.
        /// </summary>
        internal BuildClient(RequestLanguage language, CompileFunc compileFunc, CreateServerFunc createServerFunc = null, int? timeoutOverride = null)
        {
            _language = language;
            _compileFunc = compileFunc;
            _createServerFunc = createServerFunc ?? BuildServerConnection.TryCreateServerCore;
            _timeoutOverride = timeoutOverride;
        }

        /// <summary>
        /// Returns the directory that contains mscorlib, or null when running on CoreCLR.
        /// </summary>
        public static string GetSystemSdkDirectory()
        {
            return RuntimeHostInfo.IsCoreClrRuntime
                ? null
                : RuntimeEnvironment.GetRuntimeDirectory();
        }

        public static IAnalyzerAssemblyLoader CreateAnalyzerAssemblyLoader()
        {
#if NET472
            return new DesktopAnalyzerAssemblyLoader();
#else
            return new CoreClrAnalyzerAssemblyLoader();
#endif
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

            var client = new BuildClient(language, compileFunc);
            var clientDir = AppContext.BaseDirectory;
            var workingDir = Directory.GetCurrentDirectory();
            var tempDir = BuildServerConnection.GetTempPath(workingDir);
            var buildPaths = new BuildPaths(clientDir: clientDir, workingDir: workingDir, sdkDir: sdkDir, tempDir: tempDir);
            var originalArguments = GetCommandLineArgs(arguments);
            return client.RunCompilation(originalArguments, buildPaths).ExitCode;
        }


        /// <summary>
        /// Run a compilation through the compiler server and print the output
        /// to the console. If the compiler server fails, run the fallback
        /// compiler.
        /// </summary>
        internal RunCompilationResult RunCompilation(IEnumerable<string> originalArguments, BuildPaths buildPaths, TextWriter textWriter = null, string pipeName = null)
        {
            textWriter = textWriter ?? Console.Out;

            var args = originalArguments.Select(arg => arg.Trim()).ToArray();

            List<string> parsedArgs;
            bool hasShared;
            string keepAliveOpt;
            string errorMessageOpt;
            if (CommandLineParser.TryParseClientArgs(
                    args,
                    out parsedArgs,
                    out hasShared,
                    out keepAliveOpt,
                    out string commandLinePipeName,
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
                pipeName = pipeName ?? GetPipeName(buildPaths);
                var libDirectory = Environment.GetEnvironmentVariable("LIB");
                var serverResult = RunServerCompilation(textWriter, parsedArgs, buildPaths, libDirectory, pipeName, keepAliveOpt);
                if (serverResult.HasValue)
                {
                    Debug.Assert(serverResult.Value.RanOnServer);
                    return serverResult.Value;
                }
            }

            // It's okay, and expected, for the server compilation to fail.  In that case just fall 
            // back to normal compilation. 
            var exitCode = RunLocalCompilation(parsedArgs.ToArray(), buildPaths, textWriter);
            return new RunCompilationResult(exitCode);
        }

        private static bool TryEnableMulticoreJitting(out string errorMessage)
        {
            errorMessage = null;
            try
            {
                // Enable multi-core JITing
                // https://blogs.msdn.microsoft.com/dotnet/2012/10/18/an-easy-solution-for-improving-app-launch-performance/
                var profileRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RoslynCompiler",
                    "ProfileOptimization");
                var assemblyName = Assembly.GetExecutingAssembly().GetName();
                var profileName = assemblyName.Name + assemblyName.Version + ".profile";
                Directory.CreateDirectory(profileRoot);
#if NET472
                ProfileOptimization.SetProfileRoot(profileRoot);
                ProfileOptimization.StartProfile(profileName);
#else
                AssemblyLoadContext.Default.SetProfileOptimizationRoot(profileRoot);
                AssemblyLoadContext.Default.StartProfileOptimization(profileName);
#endif
            }
            catch (Exception e)
            {
                errorMessage = string.Format(CodeAnalysisResources.ExceptionEnablingMulticoreJit, e.Message);
                return false;
            }

            return true;
        }

        public Task<RunCompilationResult> RunCompilationAsync(IEnumerable<string> originalArguments, BuildPaths buildPaths, TextWriter textWriter = null)
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
            var loader = CreateAnalyzerAssemblyLoader();
            return _compileFunc(arguments, buildPaths, textWriter, loader);
        }

        /// <summary>
        /// Runs the provided compilation on the server.  If the compilation cannot be completed on the server then null
        /// will be returned.
        /// </summary>
        private RunCompilationResult? RunServerCompilation(TextWriter textWriter, List<string> arguments, BuildPaths buildPaths, string libDirectory, string sessionName, string keepAlive)
        {
            BuildResponse buildResponse;

            if (!AreNamedPipesSupported())
            {
                return null;
            }

            try
            {
                var buildResponseTask = RunServerCompilation(
                    arguments,
                    buildPaths,
                    sessionName,
                    keepAlive,
                    libDirectory,
                    CancellationToken.None);
                buildResponse = buildResponseTask.Result;

                Debug.Assert(buildResponse != null);
                if (buildResponse == null)
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }

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
                    // Build could not be completed on the server.
                    return null;
                default:
                    // Will not happen with our server but hypothetically could be sent by a rogue server.  Should
                    // not let that block compilation.
                    Debug.Assert(false);
                    return null;
            }
        }

        private Task<BuildResponse> RunServerCompilation(
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
        private static string GetPipeName(BuildPaths buildPaths)
        {
            return BuildServerConnection.GetPipeNameForPathOpt(buildPaths.ClientDirectory);
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

            IDisposable npcs = null;
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
            IntPtr ptr = NativeMethods.GetCommandLine();
            if (ptr == IntPtr.Zero)
            {
                return args;
            }

            // This memory is owned by the operating system hence we shouldn't (and can't)
            // free the memory.  
            var commandLine = Marshal.PtrToStringUni(ptr);

            // The first argument will be the executable name hence we skip it. 
            return CommandLineParser.SplitCommandLineIntoArguments(commandLine, removeHashComments: false).Skip(1);
        }
    }
}
