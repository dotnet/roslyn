// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings
{
    public abstract class AbstractCSharpCodeActionTest : AbstractCodeActionTest
    {
        protected override ParseOptions GetScriptOptions()
        {
            return Options.Script;
        }

        protected override TestWorkspace CreateWorkspaceFromFile(string definition, ParseOptions parseOptions, CompilationOptions compilationOptions)
        {
            return CSharpWorkspaceFactory.CreateWorkspaceFromFile(definition, (CSharpParseOptions)parseOptions, (CSharpCompilationOptions)compilationOptions);
        }

        protected override string GetLanguage()
        {
            return LanguageNames.CSharp;
        }
    }
}
