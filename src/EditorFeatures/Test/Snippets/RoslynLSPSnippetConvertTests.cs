﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Snippets;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.RoslynLSPSnippetConverter)]
public class RoslynLSPSnippetConvertTests
{
    #region Edgecase extend TextChange tests

    [Fact]
    public Task TestExtendSnippetTextChangeForwardsForCaret()
    {
        return TestAsync("""
            [|if ({|placeholder:true|})
            {
            }|] $$
            """, """
            if (${1:true})
            {
            } $0
            """);
    }

    [Fact]
    public Task TestExtendSnippetTextChangeBackwardsForCaret()
    {
        return TestAsync("""
            $$ [|if ({|placeholder:true|})
            {
            }|]
            """, """
            $0 if (${1:true})
            {
            }
            """);
    }

    [Fact]
    public Task TestExtendSnippetTextChangeForwardsForPlaceholder()
    {
        return TestAsync("""
            [|if (true)
            {$$
            }|] {|placeholder:test|}
            """, """
            if (true)
            {$0
            } ${1:test}
            """);
    }

    [Fact]
    public Task TestExtendSnippetTextChangeBackwardsForPlaceholder()
    {
        return TestAsync("""
            {|placeholder:test|} [|if (true)
            {$$
            }|]
            """, """
            ${1:test} if (true)
            {$0
            }
            """);
    }

    [Fact]
    public Task TestExtendSnippetTextChangeForwardsForPlaceholderThenCaret()
    {
        return TestAsync("""
            [|if (true)
            {
            }|] {|placeholder:test|} $$
            """, """
            if (true)
            {
            } ${1:test} $0
            """);
    }

    [Fact]
    public Task TestExtendSnippetTextChangeForwardsForCaretThenPlaceholder()
    {
        return TestAsync("""
            [|if (true)
            {
            }|] $$ {|placeholder:test|}
            """, """
            if (true)
            {
            } $0 ${1:test}
            """);
    }

    [Fact]
    public Task TestExtendSnippetTextChangeBackwardsForPlaceholderThenCaret()
    {
        return TestAsync("""
            {|placeholder:test|} $$ [|if (true)
            {
            }|]
            """, """
            ${1:test} $0 if (true)
            {
            }
            """);
    }

    [Fact]
    public Task TestExtendSnippetTextChangeBackwardsForCaretThenPlaceholder()
    {
        return TestAsync("""
            $$ {|placeholder:test|} [|if (true)
            {
            }|]
            """, """
            $0 ${1:test} if (true)
            {
            }
            """);
    }

    [Fact]
    public Task TestExtendSnippetTextChangeBackwardsForCaretForwardsForPlaceholder()
    {
        return TestAsync("""
            $$ [|if (true)
            {
            }|] {|placeholder:test|}
            """, """
            $0 if (true)
            {
            } ${1:test}
            """);
    }

    [Fact]
    public Task TestExtendSnippetTextChangeBackwardsForPlaceholderForwardsForCaret()
    {
        return TestAsync("""
            {|placeholder:test|} [|if (true)
            {
            }|] $$
            """, """
            ${1:test} if (true)
            {
            } $0
            """);
    }

    [Fact]
    public Task TestExtendSnippetTextChangeInMethodForwardsForCaret()
    {
        return TestAsync("""
            public void Method()
            {
                [|if ({|placeholder:true|})
                {
                }|] $$
            }
            """, """
            if (${1:true})
                {
                } $0
            """);
    }

    [Fact]
    public Task TestExtendSnippetTextChangeInMethodBackwardsForCaret()
    {
        return TestAsync("""
            public void Method()
            {
                $$ [|if ({|placeholder:true|})
                {
                }|]
            }
            """, """
            $0 if (${1:true})
                {
                }
            """);
    }

    [Fact]
    public Task TestExtendSnippetTextChangeInMethodForwardsForPlaceholder()
    {
        return TestAsync("""
            public void Method()
            {
                [|if (true)
                 {$$
                 }|] {|placeholder:test|}
            }
            """, """
            if (true)
                 {$0
                 } ${1:test}
            """);
    }

    [Fact]
    public Task TestExtendSnippetTextChangeInMethodBackwardsForPlaceholder()
    {
        return TestAsync("""
            public void Method()
            {
                {|placeholder:test|} [|if (true)
                {$$
                }|]
            """, """
            ${1:test} if (true)
                {$0
                }
            """);
    }

    [Fact]
    public Task TestExtendSnippetTextChangeInMethodForwardsForPlaceholderThenCaret()
    {
        return TestAsync("""
            public void Method()
            {
                [|if (true)
                {
                }|] {|placeholder:test|} $$
            }
            """, """
            if (true)
                {
                } ${1:test} $0
            """);
    }

    [Fact]
    public Task TestExtendSnippetTextChangeInMethodForwardsForCaretThenPlaceholder()
    {
        return TestAsync("""
            public void Method()
            {
                [|if (true)
                {
                }|] $$ {|placeholder:test|}
            }
            """, """
            if (true)
                {
                } $0 ${1:test}
            """);
    }

    [Fact]
    public Task TestExtendSnippetTextChangeInMethodBackwardsForPlaceholderThenCaret()
    {
        return TestAsync("""
            public void Method()
            {
                {|placeholder:test|} $$ [|if (true)
                {
                }|]
            }
            """, """
            ${1:test} $0 if (true)
                {
                }
            """);
    }

    [Fact]
    public Task TestExtendSnippetTextChangeInMethodBackwardsForCaretThenPlaceholder()
    {
        return TestAsync("""
            public void Method()
            {
                $$ {|placeholder:test|} [|if (true)
                {
                }|]
            }
            """, """
            $0 ${1:test} if (true)
                {
                }
            """);
    }

    [Fact]
    public Task TestExtendSnippetTextChangeInMethodBackwardsForCaretForwardsForPlaceholder()
    {
        return TestAsync("""
            public void Method()
            {
                $$ [|if (true)
                {
                }|] {|placeholder:test|}
            }
            """, """
            $0 if (true)
                {
                } ${1:test}
            """);
    }

    [Fact]
    public Task TestExtendSnippetTextChangeInMethodBackwardsForPlaceholderForwardsForCaret()
    {
        return TestAsync("""
            public void Method()
            {
                {|placeholder:test|} [|if (true)
                {
                }|] $$
            }
            """, """
            ${1:test} if (true)
                {
                } $0
            """);
    }

    [Fact]
    public Task TestExtendSnippetTextChangeInMethodWithCodeBeforeAndAfterBackwardsForPlaceholderForwardsForCaret()
    {
        return TestAsync("""
            public void Method()
            {
                var x = 5;
                {|placeholder:test|} [|if (true)
                {
                }|] $$

                x = 3;
            }
            """, """
            ${1:test} if (true)
                {
                } $0
            """);
    }

    [Fact]
    public void TestExtendTextChangeInsertion()
    {
        var testString = "foo bar quux baz";
        using var workspace = CreateWorkspaceFromCode(testString);
        var document = workspace.CurrentSolution.GetRequiredDocument(workspace.Documents.First().Id);
        var lspSnippetString = RoslynLSPSnippetConverter.GenerateLSPSnippetAsync(document, caretPosition: 12,
            [], new TextChange(new TextSpan(8, 0), "quux"), triggerLocation: 12, CancellationToken.None).Result;
        AssertEx.EqualOrDiff("quux$0", lspSnippetString);
    }

    [Fact]
    public void TestExtendTextChangeReplacement()
    {
        var testString = "foo bar quux baz";
        using var workspace = CreateWorkspaceFromCode(testString);
        var document = workspace.CurrentSolution.GetRequiredDocument(workspace.Documents.First().Id);
        var lspSnippetString = RoslynLSPSnippetConverter.GenerateLSPSnippetAsync(document, caretPosition: 12,
            [], new TextChange(new TextSpan(4, 4), "bar quux"), triggerLocation: 12, CancellationToken.None).Result;
        AssertEx.EqualOrDiff("bar quux$0", lspSnippetString);
    }

    #endregion

    #region LSP Snippet generation tests

    [Fact]
    public Task TestForLoopSnippet()
    {
        return TestAsync("""
            [|for (var {|placeholder1:i|} = 0; {|placeholder1:i|} < {|placeholder2:length|}; {|placeholder1:i|}++)
            {$$
            }|]
            """, """
            for (var ${1:i} = 0; ${1:i} < ${2:length}; ${1:i}++)
            {$0
            }
            """);
    }

    [Fact]
    public Task TestIfSnippetSamePlaceholderCursorLocation()
    {
        return TestAsync("""
            public void Method()
            {
                var x = 5;
                [|if ({|placeholder:true|}$$)
                {
                }|]

                x = 3;
            }
            """, """
            if (${1:true}$0)
                {
                }
            """);
    }

    [Fact]
    public Task TestIfSnippetSameCursorPlaceholderLocation()
    {
        return TestAsync("""
            public void Method()
            {
                var x = 5;
                [|if ($${|placeholder:true|})
                {
                }|]

                x = 3;
            }
            """, """
            if ($0${1:true})
                {
                }
            """);
    }

    #endregion

    protected static TestWorkspace CreateWorkspaceFromCode(string code)
     => TestWorkspace.CreateCSharp(code);

    private static async Task TestAsync(string markup, string output)
    {
        MarkupTestFile.GetPositionAndSpans(markup, out var text, out var cursorPosition, out IDictionary<string, ImmutableArray<TextSpan>> placeholderDictionary);
        var stringSpan = placeholderDictionary[""].First();
        var textChange = new TextChange(new TextSpan(stringSpan.Start, 0), text.Substring(stringSpan.Start, stringSpan.Length));
        var placeholders = GetSnippetPlaceholders(text, placeholderDictionary);
        using var workspace = CreateWorkspaceFromCode(markup);
        var document = workspace.CurrentSolution.GetRequiredDocument(workspace.Documents.First().Id);

        var lspSnippetString = await RoslynLSPSnippetConverter.GenerateLSPSnippetAsync(document, cursorPosition!.Value, placeholders, textChange, stringSpan.Start, CancellationToken.None).ConfigureAwait(false);
        AssertEx.EqualOrDiff(output, lspSnippetString);
    }

    private static ImmutableArray<SnippetPlaceholder> GetSnippetPlaceholders(string text, IDictionary<string, ImmutableArray<TextSpan>> placeholderDictionary)
    {
        using var _ = ArrayBuilder<SnippetPlaceholder>.GetInstance(out var arrayBuilder);
        foreach (var kvp in placeholderDictionary)
        {
            if (kvp.Key.Length > 0)
            {
                var spans = kvp.Value;
                var placeholderText = text.Substring(spans[0].Start, spans[0].Length);
                var placeholders = spans.Select(span => span.Start).ToImmutableArray();
                arrayBuilder.Add(new SnippetPlaceholder(placeholderText, placeholders));
            }
        }

        return arrayBuilder.ToImmutableAndClear();
    }
}
