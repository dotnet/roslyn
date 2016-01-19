// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CommandLine.CompilerServerLogger;

namespace Microsoft.CodeAnalysis.CommandLine
{
    internal delegate int CompileFunc(string[] arguments, BuildPaths buildPaths, TextWriter textWriter, IAnalyzerAssemblyLoader analyzerAssemblyLoader);

    internal struct BuildPaths
    {
        /// <summary>
        /// The path which containts the compiler binaries and response files.
        /// </summary>
        internal string ClientDirectory { get; }

        /// <summary>
        /// The path in which the compilation takes place.
        /// </summary>
        internal string WorkingDirectory { get; }

        /// <summary>
        /// The path which contains mscorlib.  This can be null when specified by the user or running in a 
        /// CoreClr environment.
        /// </summary>
        internal string SdkDirectory { get; }

        internal BuildPaths(string clientDir, string workingDir, string sdkDir)
        {
            ClientDirectory = clientDir;
            WorkingDirectory = workingDir;
            SdkDirectory = sdkDir;
        }
    }

    internal struct RunCompilationResult
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
    internal abstract class BuildClient
    {
        protected static bool IsRunningOnWindows => Path.DirectorySeparatorChar == '\\';

        /// <summary>
        /// Run a compilation through the compiler server and print the output
        /// to the console. If the compiler server fails, run the fallback
        /// compiler.
        /// </summary>
        internal RunCompilationResult RunCompilation(IEnumerable<string> originalArguments, BuildPaths buildPaths, TextWriter textWriter = null)
        {
            var utf8Output = originalArguments.Any(CommandLineParser.IsUtf8Option);
            if (utf8Output)
            {
                // The utf8output option is only valid when we are sending the compiler output to Console.Out.
                if (textWriter != null && textWriter != Console.Out)
                {
                    throw new InvalidOperationException("Cannot provide a custom TextWriter when using /utf8output");
                }

                return ConsoleUtil.RunWithUtf8Output(utf8Writer => RunCompilationCore(originalArguments, buildPaths, utf8Writer));
            }

            textWriter = textWriter ?? Console.Out;
            return RunCompilationCore(originalArguments, buildPaths, textWriter);
        }

        internal RunCompilationResult RunCompilationCore(IEnumerable<string> originalArguments, BuildPaths buildPaths, TextWriter textWriter)
        {
            var args = originalArguments.Select(arg => arg.Trim()).ToArray();

            bool hasShared;
            string keepAlive;
            string errorMessage;
            string sessionKey;
            List<string> parsedArgs;
            if (!CommandLineParser.TryParseClientArgs(
                    args,
                    out parsedArgs,
                    out hasShared,
                    out keepAlive,
                    out sessionKey,
                    out errorMessage))
            {
                Console.Out.WriteLine(errorMessage);
                return RunCompilationResult.Failed;
            }

            if (hasShared)
            {
                var libDirectory = Environment.GetEnvironmentVariable("LIB");
                try
                {
                    sessionKey = sessionKey ?? GetSessionKey(buildPaths);
                    var buildResponseTask = RunServerCompilation(
                        parsedArgs,
                        buildPaths,
                        sessionKey,
                        keepAlive,
                        libDirectory,
                        CancellationToken.None);
                    var buildResponse = buildResponseTask.Result;
                    if (buildResponse != null)
                    {
                        return HandleResponse(buildResponse, parsedArgs.ToArray(), buildPaths, textWriter);
                    }
                }
                catch (OperationCanceledException)
                {
                    // #7866 tracks cleaning up this code. 
                    return RunCompilationResult.Succeeded;
                }
            }

            // It's okay, and expected, for the server compilation to fail.  In that case just fall 
            // back to normal compilation. 
            var exitCode = RunLocalCompilation(parsedArgs.ToArray(), buildPaths, textWriter);
            return new RunCompilationResult(exitCode);
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

        protected abstract int RunLocalCompilation(string[] arguments, BuildPaths buildPaths, TextWriter textWriter);

        protected abstract Task<BuildResponse> RunServerCompilation(List<string> arguments, BuildPaths buildPaths, string sessionName, string keepAlive, string libDirectory, CancellationToken cancellationToken);

        protected abstract string GetSessionKey(BuildPaths buildPaths);

        protected virtual RunCompilationResult HandleResponse(BuildResponse response, string[] arguments, BuildPaths buildPaths, TextWriter textWriter)
        {
            switch (response.Type)
            {
                case BuildResponse.ResponseType.MismatchedVersion:
                    Console.Error.WriteLine(CommandLineParser.MismatchedVersionErrorText);
                    return RunCompilationResult.Failed;

                case BuildResponse.ResponseType.Completed:
                    var completedResponse = (CompletedBuildResponse)response;
                    textWriter.Write(completedResponse.Output);
                    return new RunCompilationResult(completedResponse.ReturnCode, ranOnServer: true);

                case BuildResponse.ResponseType.Rejected:
                case BuildResponse.ResponseType.AnalyzerInconsistency:
                    var exitCode = RunLocalCompilation(arguments, buildPaths, textWriter);
                    return new RunCompilationResult(exitCode);

                default:
                    throw new InvalidOperationException("Encountered unknown response type");
            }
        }

        protected static IEnumerable<string> GetCommandLineArgs(IEnumerable<string> args)
        {
            if (IsRunningOnWindows)
            {
                return GetCommandLineWindows(args);
            }

            return args;
        }

        /// <summary>
        /// When running on Windows we can't take the commmand line which was provided to the 
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
