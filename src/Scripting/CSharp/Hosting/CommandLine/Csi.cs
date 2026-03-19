// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.Hosting
{
    internal sealed class CSharpInteractiveCompiler : CSharpCompiler
    {
        private readonly Func<string, PEStreamOptions, MetadataReferenceProperties, MetadataImageReference> _createFromFileFunc;

        internal CSharpInteractiveCompiler(
            string? responseFile,
            BuildPaths buildPaths,
            string[] args,
            IAnalyzerAssemblyLoader analyzerLoader,
            Func<string, PEStreamOptions, MetadataReferenceProperties, MetadataImageReference>? createFromFileFunc = null)
            // Unlike C# compiler we do not use LIB environment variable. It's only supported for historical reasons.
            : base(CSharpCommandLineParser.Script, responseFile, args, buildPaths, additionalReferenceDirectories: null, analyzerLoader)
        {
            _createFromFileFunc = createFromFileFunc ?? Script.CreateFromFile;
        }

        internal override Type Type => typeof(CSharpInteractiveCompiler);

        internal override MetadataReferenceResolver GetCommandLineMetadataReferenceResolver(TouchedFileLogger? loggerOpt) =>
           CommandLineRunner.GetMetadataReferenceResolver(Arguments, loggerOpt, _createFromFileFunc);

        public override void PrintLogo(TextWriter consoleOutput)
        {
            consoleOutput.WriteLine(CSharpScriptingResources.LogoLine1, GetCompilerVersion());
            consoleOutput.WriteLine(CSharpScriptingResources.LogoLine2);
            consoleOutput.WriteLine();
        }

        public override void PrintHelp(TextWriter consoleOutput)
        {
            consoleOutput.Write(CSharpScriptingResources.InteractiveHelp);
        }
    }
}
