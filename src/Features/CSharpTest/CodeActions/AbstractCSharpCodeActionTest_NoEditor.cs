// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;

public abstract class AbstractCSharpCodeActionTest_NoEditor : AbstractCodeActionTest_NoEditor<
    TestHostDocument,
    TestHostProject,
    TestHostSolution,
    TestWorkspace>
{
    protected override ParseOptions GetScriptOptions() => TestOptions.Script;

    protected internal override string GetLanguage() => LanguageNames.CSharp;

    internal new Task TestInRegularAndScriptAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expectedMarkup,
        int index = 0,
        TestParameters? parameters = null)
    {
        return base.TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, index, parameters);
    }

    internal new Task TestInRegularAndScriptAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expectedMarkup,
        TestParameters parameters)
    {
        return base.TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, parameters);
    }
}
