// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Interactive;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.CSharp;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Interactive
{
    internal class CSharpRepl : IRepl
    {
        public CSharpRepl()
        {
        }

        public ObjectFormatter CreateObjectFormatter()
        {
            return CSharpObjectFormatter.Instance;
        }

        public Script CreateScript(string code)
        {
            return CSharpScript.Create(code);
        }

        public string GetLogo()
        {
            return string.Format(CSharpInteractiveEditorResources.MicrosoftRoslynCSharpCompiler,
                    FileVersionInfo.GetVersionInfo(typeof(CSharpCommandLineArguments).Assembly.Location).FileVersion);
        }

        public CommandLineParser GetCommandLineParser()
        {
#if SCRIPTING
            return CSharpCommandLineParser.Interactive;
#else
            return CSharpCommandLineParser.Default;
#endif
        }

        public DiagnosticFormatter GetDiagnosticFormatter()
        {
            return CSharpDiagnosticFormatter.Instance;
        }
    }
}
