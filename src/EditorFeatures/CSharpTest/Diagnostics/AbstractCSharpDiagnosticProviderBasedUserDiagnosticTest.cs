// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics
{
    public abstract class AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest : AbstractDiagnosticProviderBasedUserDiagnosticTest
    {
        protected override ParseOptions GetScriptOptions()
        {
            return Options.Script;
        }

        protected override Task<TestWorkspace> CreateWorkspaceFromFileAsync(string definition, ParseOptions parseOptions, CompilationOptions compilationOptions)
        {
            return TestWorkspaceFactory.CreateCSharpWorkspaceAsync(definition, (CSharpParseOptions)parseOptions, (CSharpCompilationOptions)compilationOptions);
        }

        protected override string GetLanguage()
        {
            return LanguageNames.CSharp;
        }
    }
}
