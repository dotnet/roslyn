// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Debugging;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Debugging;

/// <summary>
/// Tests for <see cref="CSharpDebuggerSplicer"/>
/// </summary>
[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)]
public sealed class CSharpDebuggerSplicerTests
{
    /// <param name="source">Should include a selection</param>
    /// <param name="expression">Should include a cursor position</param>
    /// <param name="expected">Should include a cursor position</param>
    private static async Task VerifySpliceAsync(string source, string expression, string expected)
    {
        // Parse cursor position from expression
        var cursorIndex = expression.IndexOf('$');
        Assert.True(cursorIndex >= 0, "Expression must contain '$' to mark cursor position");
        var expressionWithoutCursor = expression.Remove(cursorIndex, 1);
        var cursorOffset = cursorIndex;

        // Parse expected completion position
        var expectedCompletionIndex = expected.IndexOf('$');
        Assert.True(expectedCompletionIndex >= 0, "Expected must contain '$' to mark completion position");
        var expectedWithoutCursor = expected.Remove(expectedCompletionIndex, 1);

        // Run the splicer
        using var workspace = EditorTestWorkspace.CreateCSharp(source);
        var document = workspace.CurrentSolution.Projects.First().Documents.First();
        var testDocument = workspace.Documents.Single();

        var statementSpan = testDocument.SelectedSpans.Single();
        var contextPoint = statementSpan.End;

        var splicer = document.Project.Services.GetRequiredService<IDebuggerSplicer>();
        Assert.IsType<CSharpDebuggerSplicer>(splicer);
        var result = await splicer.SpliceAsync(document, contextPoint, expressionWithoutCursor, cursorOffset, CancellationToken.None);

        var actualText = result.Text.ToString();
        var actualCompletionPosition = result.CompletionPosition;

        // Verify
        Assert.Equal(expectedWithoutCursor, actualText);
        Assert.True(
            actualCompletionPosition == expectedCompletionIndex,
            $"Expected completion at position {expectedCompletionIndex}, but was at {actualCompletionPosition}. " +
            $"Character at actual position: '{(actualCompletionPosition < actualText.Length ? actualText[actualCompletionPosition] : "EOF")}'");
    }

    // Corresponds to VS test: CSharpDebuggerIntellisenseTests.LocalsInBlockAfterInstructionPointer
    [Fact]
    public async Task SpliceAtEndOfStatement_InsertsAtStatementStart()
    {
        await VerifySpliceAsync(
            source: """
                class C
                {
                    void M()
                    {
                        [|int x = 1;|]
                    }
                }
                """,
            expression: "x.$ToString()",
            expected: """
                class C
                {
                    void M()
                    {
                         x.$ToString();int x = 1;
                    }
                }
                """);
    }

    // Corresponds to VS test: CSharpDebuggerIntellisenseTests.Locals2
    [Fact]
    public async Task SpliceAtCloseBrace_InsertsInsideBlock()
    {
        await VerifySpliceAsync(
            source: """
                class C
                {
                    void M()
                    {
                        int x = 1;
                    [|}|]
                }
                """,
            expression: "x$",
            expected: """
                class C
                {
                    void M()
                    {
                        int x = 1;
                    ;x$;}
                }
                """);
    }

    [Fact]
    public async Task SpliceInMethodBody_UsesSpaceSeparator()
    {
        await VerifySpliceAsync(
            source: """
                class C
                {
                    void M()
                    {
                        [|int x = 1;|]
                        int y = 2;
                    }
                }
                """,
            expression: "x$",
            expected: """
                class C
                {
                    void M()
                    {
                         x$;int x = 1;
                        int y = 2;
                    }
                }
                """);
    }

    // Corresponds to VS test: CSharpDebuggerIntellisenseTests.SingleStatementBlock
    [Fact]
    public async Task SpliceInForLoop_StaysInLoopScope()
    {
        await VerifySpliceAsync(
            source: """
                class C
                {
                    void M()
                    {
                        for (int i = 0; i < 10; i++)
                            [|System.Console.WriteLine(i);|]
                    }
                }
                """,
            expression: "i$",
            expected: """
                class C
                {
                    void M()
                    {
                        for (int i = 0; i < 10; i++)
                             i$;System.Console.WriteLine(i);
                    }
                }
                """);
    }

    [Fact]
    public async Task SpliceWithCursorAtStart()
    {
        await VerifySpliceAsync(
            source: """
                class C
                {
                    void M()
                    {
                        [|int x = 1;|]
                    }
                }
                """,
            expression: "$x.ToString()",
            expected: """
                class C
                {
                    void M()
                    {
                         $x.ToString();int x = 1;
                    }
                }
                """);
    }

    [Fact]
    public async Task SpliceWithCursorAtEnd()
    {
        await VerifySpliceAsync(
            source: """
                class C
                {
                    void M()
                    {
                        [|int x = 1;|]
                    }
                }
                """,
            expression: "x.ToString$",
            expected: """
                class C
                {
                    void M()
                    {
                         x.ToString$;int x = 1;
                    }
                }
                """);
    }

    // Corresponds to VS test: CSharpDebuggerIntellisenseTests.Locals1
    [Fact]
    public async Task SpliceWithNestedBlocks()
    {
        await VerifySpliceAsync(
            source: """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                            [|int x = 1;|]
                        }
                    }
                }
                """,
            expression: "x$",
            expected: """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                             x$;int x = 1;
                        }
                    }
                }
                """);
    }

    [Fact]
    public async Task SpliceWithMemberCompletion()
    {
        await VerifySpliceAsync(
            source: """
                class C
                {
                    void M()
                    {
                        [|string s = "hello";|]
                    }
                }
                """,
            expression: "s.$",
            expected: """
                class C
                {
                    void M()
                    {
                         s.$;string s = "hello";
                    }
                }
                """);
    }

    // Corresponds to VS test: CSharpDebuggerIntellisenseTests.Locals2
    [Fact]
    public async Task SpliceAtEndOfBlock_WithCloseBrace()
    {
        await VerifySpliceAsync(
            source: """
                class C
                {
                    void M()
                    {
                        {
                            int x = 1;
                        [|}|]
                    }
                }
                """,
            expression: "x$",
            expected: """
                class C
                {
                    void M()
                    {
                        {
                            int x = 1;
                        ;x$;}
                    }
                }
                """);
    }

    [Fact]
    public async Task SpliceInSwitchCase()
    {
        await VerifySpliceAsync(
            source: """
                class C
                {
                    void M(int value)
                    {
                        switch (value)
                        {
                            case 1:
                                [|int x = 1;|]
                                break;
                        }
                    }
                }
                """,
            expression: "x$",
            expected: """
                class C
                {
                    void M(int value)
                    {
                        switch (value)
                        {
                            case 1:
                                 x$;int x = 1;
                                break;
                        }
                    }
                }
                """);
    }

    // Corresponds to VS test: CSharpDebuggerIntellisenseTests.Locals5
    [Fact]
    public async Task SpliceAtMethodClosingBrace()
    {
        await VerifySpliceAsync(
            source: """
                class C
                {
                    void M()
                    {
                        {
                            int variable1 = 0;
                        }
                        int variable2 = 0;
                    [|}|]
                }
                """,
            expression: "variable$",
            expected: """
                class C
                {
                    void M()
                    {
                        {
                            int variable1 = 0;
                        }
                        int variable2 = 0;
                    ;variable$;}
                }
                """);
    }

    // Corresponds to VS test: CSharpDebuggerIntellisenseTests.Locals3
    [Fact]
    public async Task SpliceAfterBlockEnds()
    {
        await VerifySpliceAsync(
            source: """
                class C
                {
                    void M()
                    {
                        {
                            int variable1 = 0;
                        }
                        [|System.Console.Write(0);|]
                        int variable2 = 0;
                    }
                }
                """,
            expression: "variable2$",
            expected: """
                class C
                {
                    void M()
                    {
                        {
                            int variable1 = 0;
                        }
                         variable2$;System.Console.Write(0);
                        int variable2 = 0;
                    }
                }
                """);
    }

    // Corresponds to VS test: CSharpDebuggerIntellisenseTests.Locals4
    [Fact]
    public async Task SpliceAtVariableDeclaration()
    {
        await VerifySpliceAsync(
            source: """
                class C
                {
                    void M()
                    {
                        {
                            int variable1 = 0;
                        }
                        System.Console.Write(0);
                        [|int variable2 = 0;|]
                    }
                }
                """,
            expression: "variable2$",
            expected: """
                class C
                {
                    void M()
                    {
                        {
                            int variable1 = 0;
                        }
                        System.Console.Write(0);
                         variable2$;int variable2 = 0;
                    }
                }
                """);
    }
}
