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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Wrapping;

public abstract class AbstractWrappingTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpWrappingCodeRefactoringProvider();

    protected sealed override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
        => FlattenActions(actions);

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
        // Normalize to CRLF so that the wrapping code's "already existing style" detection
        // works consistently across platforms. The wrapping code generates CRLF trivia (from
        // end_of_line=crlf editorconfig) and needs the source to also have CRLF to correctly
        // identify that the existing code already matches a wrapping style.
        input = input.Replace("\r\n", "\n").Replace("\n", "\r\n");
        for (var i = 0; i < outputs.Length; i++)
            outputs[i] = outputs[i].Replace("\r\n", "\n").Replace("\n", "\r\n");

        return TestAllInRegularAndScriptAsync(input, parameters, outputs);
    }
}
