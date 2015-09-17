// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting.CSharp;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.Scripting.Hosting.CSharp
{
    internal sealed class CSharpInteractiveCompiler : CSharpCompiler
    {
        internal CSharpInteractiveCompiler(string responseFile, string baseDirectory, string[] args, IAnalyzerAssemblyLoader analyzerLoader)
            : base(CSharpCommandLineParser.Interactive, responseFile, args, AppContext.BaseDirectory, baseDirectory, null, null, analyzerLoader)
        {
        }

        internal override MetadataReferenceResolver GetCommandLineMetadataReferenceResolver(TouchedFileLogger loggerOpt)
        {
            return new RuntimeMetadataReferenceResolver(
                new RelativePathResolver(Arguments.ReferencePaths, Arguments.BaseDirectory),
                null,
                new GacFileResolver(GacFileResolver.Default.Architectures, CultureInfo.CurrentCulture),
                (path, properties) =>
                {
                    loggerOpt?.AddRead(path);
                    return MetadataReference.CreateFromFile(path);
                });
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
