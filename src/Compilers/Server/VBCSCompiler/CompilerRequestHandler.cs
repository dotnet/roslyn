// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
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
        public static readonly Func<string, MetadataReferenceProperties, PortableExecutableReference> AssemblyReferenceProvider =
            (path, properties) => new CachingMetadataReference(path, properties);

        public static readonly IAnalyzerAssemblyLoader AnalyzerLoader = new ShadowCopyAnalyzerAssemblyLoader(Path.Combine(Path.GetTempPath(), "VBCSCompiler", "AnalyzerAssemblyLoader"));

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

        private readonly string _responseFileDirectory;

        internal CompilerRequestHandler(string responseFileDirectory)
        {
            _responseFileDirectory = responseFileDirectory;
        }

        /// <summary>
        /// An incoming request as occurred. This is called on a new thread to handle
        /// the request.
        /// </summary>
        public BuildResponse HandleRequest(BuildRequest req, CancellationToken cancellationToken)
        {
            switch (req.Language)
            {
                case BuildProtocolConstants.RequestLanguage.CSharpCompile:
                    CompilerServerLogger.Log("Request to compile C#");
                    return CSharpCompile(req, cancellationToken);

                case BuildProtocolConstants.RequestLanguage.VisualBasicCompile:
                    CompilerServerLogger.Log("Request to compile VB");
                    return BasicCompile(req, cancellationToken);

                default:
                    CompilerServerLogger.Log("Got request with id '{0}'", req.Language);
                    for (int i = 0; i < req.Arguments.Length; ++i)
                    {
                        CompilerServerLogger.Log("Request argument '{0}[{1}]' = '{2}'", req.Arguments[i].ArgumentId, req.Arguments[i].ArgumentIndex, req.Arguments[i].Value);
                    }

                    // We can't do anything with a request we don't know about. 
                    return new CompletedBuildResponse(-1,
                        utf8output: false,
                        output: "",
                        errorOutput: "");
            }
        }

        private static string[] GetCommandLineArguments(BuildRequest req, out string currentDirectory, out string libDirectory)
        {
            currentDirectory = null;
            libDirectory = null;
            List<string> commandLineArguments = new List<string>();

            foreach (BuildRequest.Argument arg in req.Arguments)
            {
                if (arg.ArgumentId == BuildProtocolConstants.ArgumentId.CurrentDirectory)
                {
                    currentDirectory = arg.Value;
                }
                else if (arg.ArgumentId == BuildProtocolConstants.ArgumentId.LibEnvVariable)
                {
                    libDirectory = arg.Value;
                }
                else if (arg.ArgumentId == BuildProtocolConstants.ArgumentId.CommandLineArgument)
                {
                    int argIndex = arg.ArgumentIndex;
                    while (argIndex >= commandLineArguments.Count)
                        commandLineArguments.Add("");
                    commandLineArguments[argIndex] = arg.Value;
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
                return new CompletedBuildResponse(-1,
                    utf8output: false,
                    output: "",
                    errorOutput: "");
            }

            return CSharpCompile(
                currentDirectory,
                libDirectory,
                _responseFileDirectory,
                commandLineArguments,
                cancellationToken);
        }

        /// <summary>
        /// Invoke the C# compiler with the given arguments and current directory, and send output and error
        /// to the given TextWriters.
        /// </summary>
        private BuildResponse CSharpCompile(
            string currentDirectory,
            string libDirectory,
            string responseFileDirectory,
            string[] commandLineArguments,
            CancellationToken cancellationToken)
        {
            CompilerServerLogger.Log("CurrentDirectory = '{0}'", currentDirectory);
            CompilerServerLogger.Log("LIB = '{0}'", libDirectory);
            for (int i = 0; i < commandLineArguments.Length; ++i)
            {
                CompilerServerLogger.Log("Argument[{0}] = '{1}'", i, commandLineArguments[i]);
            }

            return CSharpCompilerServer.RunCompiler(
                responseFileDirectory,
                commandLineArguments,
                currentDirectory,
                RuntimeEnvironment.GetRuntimeDirectory(),
                libDirectory,
                AnalyzerLoader,
                cancellationToken);
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
                return new CompletedBuildResponse(-1, utf8output: false, output: "", errorOutput: "");
            }

            return BasicCompile(
                _responseFileDirectory,
                currentDirectory,
                libDirectory,
                commandLineArguments,
                cancellationToken);
        }

        /// <summary>
        /// Invoke the VB compiler with the given arguments and current directory, and send output and error
        /// to the given TextWriters.
        /// </summary>
        private BuildResponse BasicCompile(
            string responseFileDirectory,
            string currentDirectory,
            string libDirectory,
            string[] commandLineArguments,
            CancellationToken cancellationToken)
        {
            CompilerServerLogger.Log("CurrentDirectory = '{0}'", currentDirectory);
            CompilerServerLogger.Log("LIB = '{0}'", libDirectory);
            for (int i = 0; i < commandLineArguments.Length; ++i)
            {
                CompilerServerLogger.Log("Argument[{0}] = '{1}'", i, commandLineArguments[i]);
            }

            return VisualBasicCompilerServer.RunCompiler(
                responseFileDirectory,
                commandLineArguments,
                currentDirectory,
                RuntimeEnvironment.GetRuntimeDirectory(),
                libDirectory,
                AnalyzerLoader,
                cancellationToken);
        }
    }
}
