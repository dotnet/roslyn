// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.MoveType;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.MoveType
{
    public abstract class CSharpMoveTypeTestsBase : AbstractMoveTypeTest
    {
        protected override ParseOptions GetScriptOptions() => Options.Script;

        protected override string GetLanguage() => LanguageNames.CSharp;

        protected override TestWorkspace CreateWorkspaceFromFile(string initialMarkup, TestParameters parameters)
        {
            return TestWorkspace.IsWorkspaceElement(initialMarkup)
                ? TestWorkspace.Create(initialMarkup)
                : TestWorkspace.CreateCSharp(initialMarkup, parameters.parseOptions, parameters.compilationOptions);
        }
    }
}
