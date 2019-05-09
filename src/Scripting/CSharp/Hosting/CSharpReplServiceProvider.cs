// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.Hosting
{
    internal sealed class CSharpReplServiceProvider : ReplServiceProvider
    {
        public CSharpReplServiceProvider()
        {
        }

        public override ObjectFormatter ObjectFormatter { get; } = CSharpObjectFormatter.Instance;
        public override CommandLineParser CommandLineParser => CSharpCommandLineParser.Script;
        public override DiagnosticFormatter DiagnosticFormatter => CSharpDiagnosticFormatter.Instance;

        public override string Logo
            => string.Format(CSharpScriptingResources.LogoLine1, CommonCompiler.GetProductVersion(typeof(CSharpReplServiceProvider)));

        public override Script<T> CreateScript<T>(string code, ScriptOptions options, Type globalsTypeOpt, InteractiveAssemblyLoader assemblyLoader)
            => CSharpScript.Create<T>(code, options, globalsTypeOpt, assemblyLoader);
    }
}
