// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine
{
    internal sealed class Csc : CSharpCompiler
    {
        internal Csc(string responseFile, string baseDirectory, string[] args)
            : base(CSharpCommandLineParser.Default, responseFile, args, baseDirectory, Environment.GetEnvironmentVariable("LIB"))
        {
        }

        internal static int Run(string clientDir, string[] args)
        {
            FatalError.Handler = FailFast.OnFatalException;

            var responseFile = Path.Combine(clientDir, CSharpCompiler.ResponseFileName);
            Csc compiler = new Csc(responseFile, Directory.GetCurrentDirectory(), args);

            // We store original encoding and restore it later to revert 
            // the changes that might be done by /utf8output options
            // NOTE: original encoding may not be restored if process terminated 
            Encoding origEncoding = Console.OutputEncoding;
            try
            {
                if (compiler.Arguments.Utf8Output && Console.IsOutputRedirected)
                {
                    Console.OutputEncoding = Encoding.UTF8;
                }
                return compiler.Run(Console.Out);
            }
            finally
            {
                try
                {
                    Console.OutputEncoding = origEncoding;
                }
                catch
                { // Try to reset the output encoding, ignore if we can't
                }
            }
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
