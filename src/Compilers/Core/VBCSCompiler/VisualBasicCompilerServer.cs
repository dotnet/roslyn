// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class VisualBasicCompilerServer : VisualBasicCompiler
    {
        internal VisualBasicCompilerServer(string[] args, string clientDirectory, string baseDirectory, string sdkDirectory, string libDirectory, IAnalyzerAssemblyLoader analyzerLoader)
            : base(VisualBasicCommandLineParser.Default, clientDirectory != null ? Path.Combine(clientDirectory, ResponseFileName) : null, args, clientDirectory, baseDirectory, sdkDirectory, libDirectory, analyzerLoader)
        {
        }

        public static int RunCompiler(
            string clientDirectory,
            string[] args,
            string baseDirectory,
            string sdkDirectory,
            string libDirectory,
            IAnalyzerAssemblyLoader analyzerLoader,
            TextWriter output,
            CancellationToken cancellationToken,
            out bool utf8output)
        {
            var compiler = new VisualBasicCompilerServer(args, clientDirectory, baseDirectory, sdkDirectory, libDirectory, analyzerLoader);
            utf8output = compiler.Arguments.Utf8Output;

            foreach (var analyzer in compiler.Arguments.AnalyzerReferences)
            {
                CompilerServerFileWatcher.AddPath(analyzer.FilePath);
            }

            return compiler.Run(output, cancellationToken);
        }

        public override int Run(TextWriter consoleOutput, CancellationToken cancellationToken)
        {
            int runResult;
            CompilerServerLogger.Log("****Running VB compiler...");
            runResult = base.Run(consoleOutput, cancellationToken);
            CompilerServerLogger.Log("****VB Compilation complete.\r\n****Return code: {0}\r\n****Output:\r\n{1}\r\n", runResult, consoleOutput.ToString());
            return runResult;
        }

        internal override MetadataFileReferenceProvider GetMetadataProvider()
        {
            return CompilerRequestHandler.AssemblyReferenceProvider;
        }

        protected override uint GetSqmAppID()
        {
            return SqmServiceProvider.BASIC_APPID;
        }

        protected override void CompilerSpecificSqm(IVsSqmMulti sqm, uint sqmSession)
        {
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_COMPILERTYPE, (uint)SqmServiceProvider.CompilerType.CompilerServer);
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_WARNINGLEVEL, (uint)Arguments.CompilationOptions.WarningLevel);
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_LANGUAGEVERSION, (uint)Arguments.ParseOptions.LanguageVersion);
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_WARNINGLEVEL, Arguments.CompilationOptions.GeneralDiagnosticOption == ReportDiagnostic.Suppress ? 1u : 0u);
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_EMBEDVBCORE, Arguments.CompilationOptions.EmbedVbCoreRuntime ? 1u : 0u);

            //Project complexity # of source files, # of references
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_SOURCES, (uint)Arguments.SourceFiles.Count());
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_REFERENCES, (uint)Arguments.ReferencePaths.Count());
        }
    }
}
