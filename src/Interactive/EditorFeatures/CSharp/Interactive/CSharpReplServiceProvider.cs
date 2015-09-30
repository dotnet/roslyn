// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Interactive;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.CSharp;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Interactive
{
    internal sealed class CSharpReplServiceProvider : ReplServiceProvider
    {
        public CSharpReplServiceProvider()
        {
        }

        public override ObjectFormatter ObjectFormatter => CSharpObjectFormatter.Instance;
        public override CommandLineParser CommandLineParser => CSharpCommandLineParser.Interactive;
        public override DiagnosticFormatter DiagnosticFormatter => CSharpDiagnosticFormatter.Instance;

        public override string Logo
        {
            get
            {
                return string.Format(CSharpInteractiveEditorResources.MicrosoftRoslynCSharpCompiler,
                FileVersionInfo.GetVersionInfo(typeof(CSharpCommandLineArguments).Assembly.Location).FileVersion);
            }
        }

        public override Script<T> CreateScript<T>(string code, ScriptOptions options, Type globalsTypeOpt, InteractiveAssemblyLoader assemblyLoader)
        {
            return CSharpScript.Create<T>(code, options, globalsTypeOpt, assemblyLoader);
        }
    }
}
