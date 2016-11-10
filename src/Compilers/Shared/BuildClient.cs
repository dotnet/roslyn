﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            textWriter = textWriter ?? Console.Out;

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
                sessionKey = sessionKey ?? GetSessionKey(buildPaths);
                var libDirectory = Environment.GetEnvironmentVariable("LIB");
                var serverResult = RunServerCompilation(textWriter, parsedArgs, buildPaths, libDirectory, sessionKey, keepAlive);
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

        /// <summary>
        /// Runs the provided compilation on the server.  If the compilation cannot be completed on the server then null
        /// will be returned.
        /// </summary>
        internal RunCompilationResult? RunServerCompilation(TextWriter textWriter, List<string> arguments, BuildPaths buildPaths, string libDirectory, string sessionName, string keepAlive)
        {
            BuildResponse buildResponse;

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
                case BuildResponse.ResponseType.Rejected:
                case BuildResponse.ResponseType.AnalyzerInconsistency:
                    // Build could not be completed on the server.
                    return null;
                default:
                    // Will not happen with our server but hypothetically could be sent by a rouge server.  Should
                    // not let that block compilation.
                    Debug.Assert(false);
                    return null;
            }
        }

        protected abstract Task<BuildResponse> RunServerCompilation(List<string> arguments, BuildPaths buildPaths, string sessionName, string keepAlive, string libDirectory, CancellationToken cancellationToken);

        protected abstract string GetSessionKey(BuildPaths buildPaths);

        protected static IEnumerable<string> GetCommandLineArgs(IEnumerable<string> args)
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

            if (Type.GetType("Mono.Runtime") != null)
            {
                return false;
            }

            return true;
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
