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
        private readonly DesktopCompilerServerHost _desktopCompilerServerHost;
        private readonly CompilerRunHandler _compilerRunHandler;

        internal CompilerRequestHandler(string clientDirectory)
        {
            _desktopCompilerServerHost = new DesktopCompilerServerHost();
            _compilerRunHandler = new CompilerRunHandler(_desktopCompilerServerHost, clientDirectory);
        }

        /// <summary>
        /// An incoming request as occurred. This is called on a new thread to handle
        /// the request.
        /// </summary>
        public BuildResponse HandleRequest(BuildRequest req, CancellationToken cancellationToken)
        {
            var request = GetRunRequest(req);
            var result = _compilerRunHandler.HandleRequest(request, cancellationToken);
            switch (result.Kind)
            {
                case RunResultKind.BadAnalyzer:
                    return new AnalyzerInconsistencyBuildResponse();
                case RunResultKind.BadLanguage:
                    return new CompletedBuildResponse(-1, utf8output: false, output: "", errorOutput: "");
                case RunResultKind.Run:
                    return new CompletedBuildResponse(result.ReturnCode, result.Utf8Output, result.Output, errorOutput: "");
                default:
                    Debug.Assert(false);
                    throw new Exception($"Bad enum value {result.Kind}");
            }
        }

        private static RunRequest GetRunRequest(BuildRequest req)
        {
            string currentDirectory;
            string libDirectory;
            string[] arguments = GetCommandLineArguments(req, out currentDirectory, out libDirectory);
            string language = "";
            switch (req.Language)
            {
                case BuildProtocolConstants.RequestLanguage.CSharpCompile:
                    language = LanguageNames.CSharp;
                    break;
                case BuildProtocolConstants.RequestLanguage.VisualBasicCompile:
                    language = LanguageNames.VisualBasic;
                    break;
            }

            return new RunRequest(language, currentDirectory, libDirectory, arguments);
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
    }
}
