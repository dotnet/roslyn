// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine
{
    internal sealed class Vbc : VisualBasicCompiler
    {
        internal Vbc(string responseFile, string baseDirectory, string sdkDirectory, string[] args)
            : base(VisualBasicCommandLineParser.Default, responseFile, args, baseDirectory, sdkDirectory, Environment.GetEnvironmentVariable("LIB"))
        {
        }

        internal static int Run(string clientDir, string sdkDirectory, string[] args)
        {
            FatalError.Handler = FailFast.OnFatalException;

            var responseFile = Path.Combine(clientDir, VisualBasicCompiler.ResponseFileName);
            Vbc compiler = new Vbc(responseFile, Directory.GetCurrentDirectory(), sdkDirectory, args);

            return ConsoleUtil.RunWithUtf8Output(compiler.Arguments.Utf8Output, (textWriterOut, _) => compiler.Run(textWriterOut));
        }

        public override Assembly LoadAssembly(string fullPath)
        {
            return Assembly.LoadFrom(fullPath);
        }

        protected override uint GetSqmAppID()
        {
            return SqmServiceProvider.CSHARP_APPID;
        }

        protected override void CompilerSpecificSqm(IVsSqmMulti sqm, uint sqmSession)
        {
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_COMPILERTYPE, (uint)SqmServiceProvider.CompilerType.Compiler);
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_LANGUAGEVERSION, (uint)Arguments.ParseOptions.LanguageVersion);
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_WARNINGLEVEL, (uint)Arguments.CompilationOptions.WarningLevel);

            //Project complexity # of source files, # of references
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_SOURCES, (uint)Arguments.SourceFiles.Count());
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_REFERENCES, (uint)Arguments.ReferencePaths.Count());
        }
    }
}

