// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CompilerServer;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.BuildTasks.NativeMethods;
using static Microsoft.CodeAnalysis.CompilerServer.BuildProtocolConstants;
using static Microsoft.CodeAnalysis.CompilerServer.CompilerServerLogger;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    internal interface IBuildHost
    {
        IEnumerable<string> OriginalArguments { get; }

        int RunCompilation(List<string> arguments);

        Task<BuildResponse> TryRunServerCompilation(List<string> arguments, string keepAlive, string libEnvVariable, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Client class that handles communication to the server.
    /// </summary>
    internal static class BuildClient
    {
        private static bool IsRunningOnWindows => Path.DirectorySeparatorChar == '\\';

        /// <summary>
        /// Run a compilation through the compiler server and print the output
        /// to the console. If the compiler server fails, run the fallback
        /// compiler.
        /// </summary>
        public static int RunWithConsoleOutput(IBuildHost buildHost)
        {
            var args = buildHost.OriginalArguments.Select(arg => arg.Trim()).ToArray();

            bool hasShared;
            string keepAlive;
            string errorMessage;
            List<string> parsedArgs;
            if (!CommandLineParser.TryParseClientArgs(
                    args,
                    out parsedArgs,
                    out hasShared,
                    out keepAlive,
                    out errorMessage))
            {
                Console.Out.WriteLine(errorMessage);
                return CommonCompiler.Failed;
            }

            if (hasShared)
            {
                var responseTask = buildHost.TryRunServerCompilation(
                    parsedArgs,
                    keepAlive: keepAlive,
                    libEnvVariable: Environment.GetEnvironmentVariable("LIB"),
                    cancellationToken: default(CancellationToken));

                var response = responseTask.Result;
                if (response != null)
                {
                    return HandleResponse(buildHost, response, parsedArgs);
                }
            }

            return buildHost.RunCompilation(parsedArgs);
        }

        public static IEnumerable<string> GetCommandLineArgs(IEnumerable<string> args)
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

        private static int HandleResponse(
            IBuildHost buildHost,
            BuildResponse response,
            List<string> parsedArgs)
        {
            switch (response.Type)
            {
                case BuildResponse.ResponseType.MismatchedVersion:
                    Console.Error.WriteLine(CommandLineParser.MismatchedVersionErrorText);
                    return CommonCompiler.Failed;

                case BuildResponse.ResponseType.Completed:
                    var completedResponse = (CompletedBuildResponse)response;
                    return ConsoleUtil.RunWithOutput(
                        completedResponse.Utf8Output,
                        (outWriter, errorWriter) =>
                        {
                            outWriter.Write(completedResponse.Output);
                            errorWriter.Write(completedResponse.ErrorOutput);
                            return completedResponse.ReturnCode;
                        });

                case BuildResponse.ResponseType.AnalyzerInconsistency:
                    return buildHost.RunCompilation(parsedArgs);

                default:
                    throw new InvalidOperationException("Encountered unknown response type");
            }
        }
    }
}
