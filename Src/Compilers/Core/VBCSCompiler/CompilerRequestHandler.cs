// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    /// <summary>
    /// This class handles incoming requests from the client, and invokes the compiler to actually
    /// do the compilation. We also handle the caching of assembly bytes and assembly objects here.
    /// </summary>
    internal class CompilerRequestHandler : IRequestHandler
    {
        // Caches are used by C# and VB compilers, and shared here.
        public static readonly ReferenceProvider AssemblyReferenceProvider = new ReferenceProvider();

        private static void LogAbnormalExit(string msg)
        {
            string roslynTempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "RoslynCompilerServerCrash");
            if (!Directory.Exists(roslynTempDir))
            {
                Directory.CreateDirectory(roslynTempDir);
            }
            string path = Path.Combine(roslynTempDir, DateTime.Now.ToString());

            using (var writer = File.AppendText(path))
            {
                writer.WriteLine(msg);
            }
        }

        /// <summary>
        /// An incoming request as occurred. This is called on a new thread to handle
        /// the request.
        /// </summary>
        public BuildResponse HandleRequest(BuildRequest req, CancellationToken cancellationToken)
        {
            switch (req.Id)
            {
                case BuildProtocolConstants.RequestId_CSharpCompile:
                    CompilerServerLogger.Log("Request to compile C#");
                    return CSharpCompile(req, cancellationToken);

                case BuildProtocolConstants.RequestId_VisualBasicCompile:
                    CompilerServerLogger.Log("Request to compile VB");
                    return BasicCompile(req, cancellationToken);

                case BuildProtocolConstants.RequestId_Analyze:
                    CompilerServerLogger.Log("Request to analyze managed code");
                    return Analyze(req, cancellationToken);

                default:
                    CompilerServerLogger.Log("Got request with id '{0}'", req.Id);
                    for (int i = 0; i < req.Arguments.Length; ++i)
                    {
                        CompilerServerLogger.Log("Request argument '{0}[{1}]' = '{2}'", req.Arguments[i].ArgumentId, req.Arguments[i].ArgumentIndex, req.Arguments[i].Value);
                    }

                    // We can't do anything with a request we don't know about. 
                    return new BuildResponse(0, "", "");
            }
        }

        private static string[] GetCommandLineArguments(BuildRequest req, out string currentDirectory, out string libDirectory)
        {
            currentDirectory = null;
            libDirectory = null;
            List<string> commandLineArguments = new List<string>();

            foreach (BuildRequest.Argument arg in req.Arguments)
            {
                if (arg.ArgumentId == BuildProtocolConstants.ArgumentId_CurrentDirectory)
                {
                    currentDirectory = arg.Value;
                }
                else if (arg.ArgumentId == BuildProtocolConstants.ArgumentId_LibEnvVariable)
                {
                    libDirectory = arg.Value;
                }
                else if (arg.ArgumentId == BuildProtocolConstants.ArgumentId_CommandLineArgument)
                {
                    uint argIndex = arg.ArgumentIndex;
                    while (argIndex >= commandLineArguments.Count)
                        commandLineArguments.Add("");
                    commandLineArguments[(int)argIndex] = arg.Value;
                }
            }

            return commandLineArguments.ToArray();
        }

        /// <summary>
        /// A request to compile C# files. Unpack the arguments and current directory and invoke
        /// the compiler, then create a response with the result of compilation.
        /// </summary>
        private BuildResponse CSharpCompile(BuildRequest req, CancellationToken cancellationToken)
        {
            string currentDirectory;
            string libDirectory;
            var commandLineArguments = GetCommandLineArguments(req, out currentDirectory, out libDirectory);

            if (currentDirectory == null)
            {
                // If we don't have a current directory, compilation can't proceed. This shouldn't ever happen,
                // because our clients always send the current directory.
                Debug.Assert(false, "Client did not send current directory; this is required.");
                return new BuildResponse(-1, "", "");
            }

            TextWriter output = new StringWriter(CultureInfo.InvariantCulture);
            int returnCode = CSharpCompile(currentDirectory, libDirectory, commandLineArguments, output, cancellationToken);

            return new BuildResponse(returnCode, output.ToString(), "");
        }

        /// <summary>
        /// Invoke the C# compiler with the given arguments and current directory, and send output and error
        /// to the given TextWriters.
        /// </summary>
        private int CSharpCompile(string currentDirectory, string libDirectory, string[] commandLineArguments, TextWriter output, CancellationToken cancellationToken)
        {
            CompilerServerLogger.Log("CurrentDirectory = '{0}'", currentDirectory);
            CompilerServerLogger.Log("LIB = '{0}'", libDirectory);
            for (int i = 0; i < commandLineArguments.Length; ++i)
            {
                CompilerServerLogger.Log("Argument[{0}] = '{1}'", i, commandLineArguments[i]);
            }

            return CSharpCompilerServer.RunCompiler(commandLineArguments, currentDirectory, libDirectory, output, cancellationToken);
        }

        /// <summary>
        /// A request to compile VB files. Unpack the arguments and current directory and invoke
        /// the compiler, then create a response with the result of compilation.
        /// </summary>
        private BuildResponse BasicCompile(BuildRequest req, CancellationToken cancellationToken)
        {
            string currentDirectory;
            string libDirectory;
            var commandLineArguments = GetCommandLineArguments(req, out currentDirectory, out libDirectory);

            if (currentDirectory == null)
            {
                // If we don't have a current directory, compilation can't proceed. This shouldn't ever happen,
                // because our clients always send the current directory.
                Debug.Assert(false, "Client did not send current directory; this is required.");
                return new BuildResponse(-1, "", "");
            }

            TextWriter output = new StringWriter(CultureInfo.InvariantCulture);
            int returnCode = BasicCompile(currentDirectory, libDirectory, commandLineArguments, output, cancellationToken);

            return new BuildResponse(returnCode, output.ToString(), "");
        }

        /// <summary>
        /// Invoke the VB compiler with the given arguments and current directory, and send output and error
        /// to the given TextWriters.
        /// </summary>
        private int BasicCompile(string currentDirectory, string libDirectory, string[] commandLineArguments, TextWriter output, CancellationToken cancellationToken)
        {
            CompilerServerLogger.Log("CurrentDirectory = '{0}'", currentDirectory);
            CompilerServerLogger.Log("LIB = '{0}'", libDirectory);
            for (int i = 0; i < commandLineArguments.Length; ++i)
            {
                CompilerServerLogger.Log("Argument[{0}] = '{1}'", i, commandLineArguments[i]);
            }

            return VisualBasicCompilerServer.RunCompiler(commandLineArguments, currentDirectory, libDirectory, output, cancellationToken);
        }

        /// <summary>
        /// A request to analyze managed source code files. Unpack the arguments and current directory and invoke
        /// the analyzer, then create a response with the result of compilation.
        /// </summary>
        private BuildResponse Analyze(BuildRequest req, CancellationToken cancellationToken)
        {
            string currentDirectory;
            string libDirectory;
            var commandLineArguments = GetCommandLineArguments(req, out currentDirectory, out libDirectory);

            // Server based execution of Roslyn Diagnostic Providers (Disabled for now).
            throw new NotImplementedException("Server based execution of Roslyn Diagnostic Providers NYI.");

            // TextWriter output = new StringWriter(CultureInfo.InvariantCulture);
            // var task = Microsoft.CodeAnalysis.Diagnostics.CommandLineDiagnosticService.ComputeAndWriteDiagnosticsAsync(commandLineArguments, output, cancellationToken);
            // task.Wait(cancellationToken);
            // int returnCode = task.Result;
            // return new BuildResponse(returnCode, "", output.ToString());
        }
    }
}
