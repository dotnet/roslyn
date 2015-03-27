// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class CSharpCompilerServer : CSharpCompiler
    {
        internal CSharpCompilerServer(string responseFile, string[] args, string baseDirectory, string libDirectory)
            : base(CSharpCommandLineParser.Default, responseFile, args, baseDirectory, libDirectory)
        {
        }

        public static int RunCompiler(
            string responseFileDirectory,
            string[] args,
            string baseDirectory,
            string libDirectory,
            TextWriter output,
            CancellationToken cancellationToken,
            out bool utf8output)
        {
            var responseFile = Path.Combine(responseFileDirectory, CSharpCompiler.ResponseFileName);
            var compiler = new CSharpCompilerServer(responseFile, args, baseDirectory, libDirectory);
            utf8output = compiler.Arguments.Utf8Output;
            return compiler.Run(output, cancellationToken);
        }

        public override Assembly LoadAssembly(string fullPath)
        {
            return InMemoryAssemblyProvider.GetAssembly(fullPath);
        }

        public override int Run(TextWriter consoleOutput, CancellationToken cancellationToken = default(CancellationToken))
        {
            int returnCode;

            CompilerServerLogger.Log("****Running C# compiler...");
            returnCode = base.Run(consoleOutput, cancellationToken);
            CompilerServerLogger.Log("****C# Compilation complete.\r\n****Return code: {0}\r\n****Output:\r\n{1}\r\n", returnCode, consoleOutput.ToString());
            return returnCode;
        }

        internal override MetadataFileReferenceProvider GetMetadataProvider()
        {
            return CompilerRequestHandler.AssemblyReferenceProvider;
        }

        protected override uint GetSqmAppID()
        {
            return SqmServiceProvider.CSHARP_APPID;
        }

        protected override void CompilerSpecificSqm(IVsSqmMulti sqm, uint sqmSession)
        {
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_COMPILERTYPE, (uint)SqmServiceProvider.CompilerType.CompilerServer);
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_LANGUAGEVERSION, (uint)Arguments.ParseOptions.LanguageVersion);
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_WARNINGLEVEL, (uint)Arguments.CompilationOptions.WarningLevel);

            //Project complexity # of source files, # of references
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_SOURCES, (uint)Arguments.SourceFiles.Count());
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_REFERENCES, (uint)Arguments.ReferencePaths.Count());
        }
    }
}
