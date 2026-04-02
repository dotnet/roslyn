// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Wrapping;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Wrapping;

public abstract class AbstractWrappingTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpWrappingCodeRefactoringProvider();

    protected sealed override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
        => FlattenActions(actions);

    // Wrapping tests need CRLF consistency so that IsEquivalentTo comparisons in the
    // wrapping code work correctly across platforms. The wrapping code uses NewLine from
    // formatting options to generate candidates, and IsEquivalentTo compares green nodes
    // including trivia — so source and formatting options must use the same line endings.
    protected override string NormalizeMarkup(string markup)
        => markup.Replace("\r\n", "\n").Replace("\n", "\r\n");

    // Ensure FormattingOptions2.NewLine is always "\r\n" for wrapping tests. This is
    // called by CreateWorkspaceFromOptions, ensuring ALL test paths (TestAsync,
    // TestActionCountAsync, TestMissingAsync, etc.) use consistent CRLF formatting.
    protected override TestParameters SetParameterDefaults(TestParameters parameters)
    {
        var options = new OptionsCollection(GetLanguage());
        if (parameters.options is OptionsCollection existing)
            options.Add(existing);
        options.Set(FormattingOptions2.NewLine, "\r\n");
        return parameters.WithOptions(options);
    }

    private protected TestParameters GetIndentionColumn(int column)
        => new(options: Option(FormattingOptions2.WrappingColumn, column));

    protected Task TestAllWrappingCasesAsync(
        string input,
        params string[] outputs)
    {
        return TestAllWrappingCasesAsync(input, parameters: null, outputs);
    }

    private protected Task TestAllWrappingCasesAsync(
        string input,
        TestParameters parameters,
        params string[] outputs)
    {
        return TestAllInRegularAndScriptAsync(input, parameters, outputs);
    }
}
