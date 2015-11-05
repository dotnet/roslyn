// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class CSharpCompilerServer : CSharpCompiler
    {
        internal CSharpCompilerServer(string[] args, string clientDirectory, string baseDirectory, string sdkDirectory, string libDirectory, IAnalyzerAssemblyLoader analyzerLoader)
            : base(CSharpCommandLineParser.Default, clientDirectory != null ? Path.Combine(clientDirectory, ResponseFileName) : null, args, clientDirectory, baseDirectory, sdkDirectory, libDirectory, analyzerLoader)
        {
        }

        public static BuildResponse RunCompiler(
            string clientDirectory,
            string[] args,
            string baseDirectory,
            string sdkDirectory,
            string libDirectory,
            IAnalyzerAssemblyLoader analyzerLoader,
            CancellationToken cancellationToken)
        {
            var compiler = new CSharpCompilerServer(args, clientDirectory, baseDirectory, sdkDirectory, libDirectory, analyzerLoader);
            bool utf8output = compiler.Arguments.Utf8Output;

            if (!AnalyzerConsistencyChecker.Check(baseDirectory, compiler.Arguments.AnalyzerReferences, analyzerLoader))
            {
                return new AnalyzerInconsistencyBuildResponse();
            }

            TextWriter output = new StringWriter(CultureInfo.InvariantCulture);
            int returnCode = compiler.Run(output, cancellationToken);

            return new CompletedBuildResponse(returnCode, utf8output, output.ToString(), string.Empty);
        }

        public override int Run(TextWriter consoleOutput, CancellationToken cancellationToken = default(CancellationToken))
        {
            int returnCode;

            CompilerServerLogger.Log("****Running C# compiler...");
            returnCode = base.Run(consoleOutput, cancellationToken);
            CompilerServerLogger.Log("****C# Compilation complete.\r\n****Return code: {0}\r\n****Output:\r\n{1}\r\n", returnCode, consoleOutput.ToString());
            return returnCode;
        }

        internal override Func<string, MetadataReferenceProperties, PortableExecutableReference> GetMetadataProvider()
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
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_SOURCES, (uint)Arguments.SourceFiles.Length);
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_REFERENCES, (uint)Arguments.ReferencePaths.Length);
        }
    }
}
