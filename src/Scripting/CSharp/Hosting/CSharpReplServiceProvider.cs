// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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
