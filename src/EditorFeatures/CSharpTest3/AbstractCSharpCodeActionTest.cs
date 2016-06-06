// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings
{
    public abstract class AbstractCSharpCodeActionTest : AbstractCodeActionTest
    {
        protected override ParseOptions GetScriptOptions()
        {
            return Options.Script;
        }

        protected override Task<TestWorkspace> CreateWorkspaceFromFileAsync(string definition, ParseOptions parseOptions, CompilationOptions compilationOptions)
        {
            return TestWorkspace.CreateCSharpAsync(definition, (CSharpParseOptions)parseOptions, (CSharpCompilationOptions)compilationOptions);
        }

        protected override string GetLanguage()
        {
            return LanguageNames.CSharp;
        }
    }
}
