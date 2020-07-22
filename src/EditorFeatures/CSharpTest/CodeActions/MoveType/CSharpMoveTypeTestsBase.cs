// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.MoveType;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.MoveType
{
    public abstract class CSharpMoveTypeTestsBase : AbstractMoveTypeTest
    {
        protected override ParseOptions GetScriptOptions() => Options.Script;

        protected internal override string GetLanguage() => LanguageNames.CSharp;

        protected override TestWorkspace CreateWorkspaceFromFile(string initialMarkup, TestParameters parameters)
        {
            // TODO: Requires WPF due to IInlineRenameService dependency (https://github.com/dotnet/roslyn/issues/46153)
            var composition = EditorTestCompositions.EditorFeaturesWpf;

            return TestWorkspace.IsWorkspaceElement(initialMarkup)
                ? TestWorkspace.Create(initialMarkup, composition: composition)
                : TestWorkspace.CreateCSharp(initialMarkup, parameters.parseOptions, parameters.compilationOptions, composition: composition);
        }
    }
}
