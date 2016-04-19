// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.Hosting
{
    internal sealed class CSharpInteractiveCompiler : CSharpCompiler
    {
        internal CSharpInteractiveCompiler(string responseFile, string baseDirectory, string sdkDirectoryOpt, string clientDirectory, string[] args, IAnalyzerAssemblyLoader analyzerLoader)
            // Unlike C# compiler we do not use LIB environment variable. It's only supported for historical reasons.
            : base(CSharpCommandLineParser.ScriptRunner, responseFile, args, clientDirectory, baseDirectory, sdkDirectoryOpt, null, analyzerLoader)
        {
        }

        internal override MetadataReferenceResolver GetCommandLineMetadataReferenceResolver(TouchedFileLogger loggerOpt)
        {
            return CommandLineRunner.GetMetadataReferenceResolver(Arguments, loggerOpt);
        }

        public override void PrintLogo(TextWriter consoleOutput)
        {
            var version = typeof(CSharpInteractiveCompiler).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
            consoleOutput.WriteLine(CSharpScriptingResources.LogoLine1, version);
            consoleOutput.WriteLine(CSharpScriptingResources.LogoLine2);
            consoleOutput.WriteLine();
        }

        public override void PrintHelp(TextWriter consoleOutput)
        {
            consoleOutput.Write(CSharpScriptingResources.InteractiveHelp);
        }

        protected override uint GetSqmAppID()
        {
            return SqmServiceProvider.CSHARP_APPID;
        }

        protected override void CompilerSpecificSqm(IVsSqmMulti sqm, uint sqmSession)
        {
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_COMPILERTYPE, (uint)SqmServiceProvider.CompilerType.Interactive);
        }
    }
}
