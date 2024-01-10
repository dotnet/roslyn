// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings
{
    public abstract class AbstractCSharpCodeActionTest_NoEditor : AbstractCodeActionTest_NoEditor
    {
        protected override ParseOptions GetScriptOptions() => Options.Script;

        protected internal override string GetLanguage() => LanguageNames.CSharp;
    }
}
