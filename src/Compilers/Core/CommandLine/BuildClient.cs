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
    internal delegate int CompileFunc(string clientDir, string sdkDir, string[] arguments, IAnalyzerAssemblyLoader analyzerAssemblyLoader);

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
        protected int RunCompilation(IEnumerable<string> originalArguments, BuildPaths buildPaths)
        {
            var args = originalArguments.Select(arg => arg.Trim()).ToArray();

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
                var libDirectory = Environment.GetEnvironmentVariable("LIB");
                try
                {
                    var buildResponseTask = RunServerCompilation(
                        parsedArgs,
                        buildPaths,
                        keepAlive,
                        libDirectory,
                        CancellationToken.None);
                    var buildResponse = buildResponseTask.Result;
                    if (buildResponse != null)
                    {
                        return HandleResponse(buildResponse, parsedArgs, buildPaths);
                    }
                }
                catch
                {
                    // It's okay, and expected, for the server compilation to fail.  In that case just fall 
                    // back to normal compilation. 
                }
            }

            return RunLocalCompilation(parsedArgs, buildPaths.ClientDirectory, buildPaths.SdkDirectory);
        }

        protected abstract int RunLocalCompilation(List<string> arguments, string clientDir, string sdkDir);

        protected abstract Task<BuildResponse> RunServerCompilation(List<string> arguments, BuildPaths buildPaths, string keepAlive, string libDirectory, CancellationToken cancellationToken);

        private int HandleResponse(BuildResponse response, List<string> arguments, BuildPaths buildPaths)
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
                    return RunLocalCompilation(arguments, buildPaths.ClientDirectory, buildPaths.SdkDirectory);

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
