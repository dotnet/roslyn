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
    public struct RunRequest
    {
        public string Language { get; }
        public string CurrentDirectory { get; }
        public string LibDirectory { get; }
        public string[] Arguments { get; }

        public RunRequest(string language, string currentDirectory, string libDirectory, string[] arguments)
        {
            Language = language;
            CurrentDirectory = currentDirectory;
            LibDirectory = libDirectory;
            Arguments = arguments;
        }
    }

    public enum RunResultKind
    {
        BadLanguage,
        BadAnalyzer,
        Run
    }

    public struct RunResult
    {
        public RunResultKind Kind { get; }
        public int ReturnCode { get; }
        public bool Utf8Output { get; }
        public string Output { get; }

        public RunResult(RunResultKind kind)
        {
            Debug.Assert(kind != RunResultKind.Run);
            Kind = kind;
            ReturnCode = 0;
            Utf8Output = false;
            Output = string.Empty;
        }

        public RunResult(int returnCode, bool utf8Output, string output)
        {
            Kind = RunResultKind.Run;
            ReturnCode = returnCode;
            Utf8Output = utf8Output;
            Output = output;
        }
    }

    public sealed class CompilerRunHandler
    {
        private readonly ICompilerServerHost _compilerServerHost;

        /// <summary>
        /// Directory holding the command line executable.  It will be the same directory as the
        /// response file.
        /// </summary>
        private readonly string _clientDirectory;

        public CompilerRunHandler(ICompilerServerHost compilerServerHost, string clientDirectory)
        {
            _compilerServerHost = compilerServerHost;
            _clientDirectory = clientDirectory;
        }

        /// <summary>
        /// An incoming request as occurred. This is called on a new thread to handle
        /// the request.
        /// </summary>
        public RunResult HandleRequest(RunRequest req, CancellationToken cancellationToken)
        {
            switch (req.Language)
            {
                case LanguageNames.CSharp:
                    _compilerServerHost.Log("Request to compile C#");
                    return RunCompile(req, CreateCSharpCompiler, cancellationToken);

                case LanguageNames.VisualBasic:
                    _compilerServerHost.Log("Request to compile VB");
                    return RunCompile(req, CreateBasicCompiler, cancellationToken);

                default:
                    // We can't do anything with a request we don't know about. 
                    _compilerServerHost.Log($"Got request with id '{req.Language}'");
                    return new RunResult(RunResultKind.BadLanguage);
            }
        }

        /// <summary>
        /// A request to compile C# files. Unpack the arguments and current directory and invoke
        /// the compiler, then create a response with the result of compilation.
        /// </summary>
        private RunResult RunCompile(RunRequest request, Func<RunRequest, CommonCompiler> func,  CancellationToken cancellationToken)
        {
            _compilerServerHost.Log($"CurrentDirectory = '{request.CurrentDirectory}'");
            _compilerServerHost.Log($"LIB = '{request.LibDirectory}'");
            for (int i = 0; i < request.Arguments.Length; ++i)
            {
                _compilerServerHost.Log($"Argument[{i}] = '{request.Arguments[i]}'");
            }

            var compiler = func(request);
            bool utf8output = compiler.Arguments.Utf8Output;
            if (!_compilerServerHost.CheckAnalyzers(request.CurrentDirectory, compiler.Arguments.AnalyzerReferences))
            {
                return new RunResult(RunResultKind.BadAnalyzer);
            }

            _compilerServerHost.Log($"****Running {request.Language} compiler...");
            TextWriter output = new StringWriter(CultureInfo.InvariantCulture);
            int returnCode = compiler.Run(output, cancellationToken);
            _compilerServerHost.Log($"****{request.Language} Compilation complete.\r\n****Return code: {returnCode}\r\n****Output:\r\n{output.ToString()}\r\n");
            return new RunResult(returnCode, utf8output, output.ToString());
        }

        private CommonCompiler CreateCSharpCompiler(RunRequest request)
        {
            return new CSharpCompilerServer(
                _compilerServerHost,
                request.Arguments,
                _clientDirectory,
                request.CurrentDirectory,
                _compilerServerHost.GetSdkDirectory(),
                request.LibDirectory,
                _compilerServerHost.AnalyzerAssemblyLoader);
        }

        private CommonCompiler CreateBasicCompiler(RunRequest request)
        {
            return new VisualBasicCompilerServer(
                _compilerServerHost,
                request.Arguments,
                _clientDirectory,
                request.CurrentDirectory,
                _compilerServerHost.GetSdkDirectory(),
                request.LibDirectory,
                _compilerServerHost.AnalyzerAssemblyLoader);
        }
    }
}
