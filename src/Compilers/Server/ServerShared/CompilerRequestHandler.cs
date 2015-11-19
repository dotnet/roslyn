﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal struct RunRequest
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

    internal abstract class CompilerServerHost : ICompilerServerHost
    {
        public abstract IAnalyzerAssemblyLoader AnalyzerAssemblyLoader { get; }

        public abstract Func<string, MetadataReferenceProperties, PortableExecutableReference> AssemblyReferenceProvider { get; }

        /// <summary>
        /// Directory that contains the compiler executables and the response files. 
        /// </summary>
        public string ClientDirectory { get; }

        /// <summary>
        /// Directory that contains mscorlib.  Can be null when the host is executing in a CoreCLR context.
        /// </summary>
        public string SdkDirectory { get; }

        protected CompilerServerHost(string clientDirectory, string sdkDirectory)
        {
            ClientDirectory = clientDirectory;
            SdkDirectory = sdkDirectory;
        }

        public abstract bool CheckAnalyzers(string baseDirectory, ImmutableArray<CommandLineAnalyzerReference> analyzers);

        public bool TryCreateCompiler(RunRequest request, out CommonCompiler compiler)
        {
            switch (request.Language)
            {
                case LanguageNames.CSharp:
                    compiler = new CSharpCompilerServer(
                        this,
                        args: request.Arguments,
                        clientDirectory: ClientDirectory,
                        baseDirectory: request.CurrentDirectory,
                        sdkDirectory: SdkDirectory,
                        libDirectory: request.LibDirectory,
                        analyzerLoader: AnalyzerAssemblyLoader);
                    return true;
                case LanguageNames.VisualBasic:
                    compiler = new VisualBasicCompilerServer(
                        this,
                        args: request.Arguments,
                        clientDirectory: ClientDirectory,
                        baseDirectory: request.CurrentDirectory,
                        sdkDirectory: SdkDirectory,
                        libDirectory: request.LibDirectory,
                        analyzerLoader: AnalyzerAssemblyLoader);
                    return true;
                default:
                    compiler = null;
                    return false;
            }
        }
    }
}
