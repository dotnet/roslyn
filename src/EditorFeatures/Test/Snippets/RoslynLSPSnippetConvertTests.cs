// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Snippets
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.RoslynLSPSnippetConverter)]
    public class RoslynLSPSnippetConvertTests
    {
        #region Edgecase extend TextChange tests

        [Fact]
        public Task TestExtendSnippetTextChangeForwardsForCaret()
        {
            var markup =
@"[|if ({|placeholder:true|})
{
}|] $$";

            var expectedLSPSnippet =
@"if (${1:true})
{
} $0";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestExtendSnippetTextChangeBackwardsForCaret()
        {
            var markup =
@"$$ [|if ({|placeholder:true|})
{
}|]";

            var expectedLSPSnippet =
@"$0 if (${1:true})
{
}";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestExtendSnippetTextChangeForwardsForPlaceholder()
        {
            var markup =
@"[|if (true)
{$$
}|] {|placeholder:test|}";

            var expectedLSPSnippet =
@"if (true)
{$0
} ${1:test}";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestExtendSnippetTextChangeBackwardsForPlaceholder()
        {
            var markup =
@"{|placeholder:test|} [|if (true)
{$$
}|]";

            var expectedLSPSnippet =
@"${1:test} if (true)
{$0
}";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestExtendSnippetTextChangeForwardsForPlaceholderThenCaret()
        {
            var markup =
@"[|if (true)
{
}|] {|placeholder:test|} $$";

            var expectedLSPSnippet =
@"if (true)
{
} ${1:test} $0";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestExtendSnippetTextChangeForwardsForCaretThenPlaceholder()
        {
            var markup =
@"[|if (true)
{
}|] $$ {|placeholder:test|}";

            var expectedLSPSnippet =
@"if (true)
{
} $0 ${1:test}";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestExtendSnippetTextChangeBackwardsForPlaceholderThenCaret()
        {
            var markup =
@"{|placeholder:test|} $$ [|if (true)
{
}|]";

            var expectedLSPSnippet =
@"${1:test} $0 if (true)
{
}";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestExtendSnippetTextChangeBackwardsForCaretThenPlaceholder()
        {
            var markup =
@"$$ {|placeholder:test|} [|if (true)
{
}|]";

            var expectedLSPSnippet =
@"$0 ${1:test} if (true)
{
}";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestExtendSnippetTextChangeBackwardsForCaretForwardsForPlaceholder()
        {
            var markup =
@"$$ [|if (true)
{
}|] {|placeholder:test|}";

            var expectedLSPSnippet =
@"$0 if (true)
{
} ${1:test}";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestExtendSnippetTextChangeBackwardsForPlaceholderForwardsForCaret()
        {
            var markup =
@"{|placeholder:test|} [|if (true)
{
}|] $$";

            var expectedLSPSnippet =
@"${1:test} if (true)
{
} $0";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestExtendSnippetTextChangeInMethodForwardsForCaret()
        {
            var markup =
@"public void Method()
{
    [|if ({|placeholder:true|})
    {
    }|] $$
}";

            var expectedLSPSnippet =
@"if (${1:true})
    {
    } $0";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestExtendSnippetTextChangeInMethodBackwardsForCaret()
        {
            var markup =
@"public void Method()
{
    $$ [|if ({|placeholder:true|})
    {
    }|]
}";

            var expectedLSPSnippet =
@"$0 if (${1:true})
    {
    }";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestExtendSnippetTextChangeInMethodForwardsForPlaceholder()
        {
            var markup =
@"public void Method()
{
    [|if (true)
     {$$
     }|] {|placeholder:test|}
}";

            var expectedLSPSnippet =
@"if (true)
     {$0
     } ${1:test}";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestExtendSnippetTextChangeInMethodBackwardsForPlaceholder()
        {
            var markup =
@"public void Method()
{
    {|placeholder:test|} [|if (true)
    {$$
    }|]";

            var expectedLSPSnippet =
@"${1:test} if (true)
    {$0
    }";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestExtendSnippetTextChangeInMethodForwardsForPlaceholderThenCaret()
        {
            var markup =
@"public void Method()
{
    [|if (true)
    {
    }|] {|placeholder:test|} $$
}";

            var expectedLSPSnippet =
@"if (true)
    {
    } ${1:test} $0";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestExtendSnippetTextChangeInMethodForwardsForCaretThenPlaceholder()
        {
            var markup =
@"public void Method()
{
    [|if (true)
    {
    }|] $$ {|placeholder:test|}
}";

            var expectedLSPSnippet =
@"if (true)
    {
    } $0 ${1:test}";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestExtendSnippetTextChangeInMethodBackwardsForPlaceholderThenCaret()
        {
            var markup =
@"public void Method()
{
    {|placeholder:test|} $$ [|if (true)
    {
    }|]
}";

            var expectedLSPSnippet =
@"${1:test} $0 if (true)
    {
    }";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestExtendSnippetTextChangeInMethodBackwardsForCaretThenPlaceholder()
        {
            var markup =
@"public void Method()
{
    $$ {|placeholder:test|} [|if (true)
    {
    }|]
}";

            var expectedLSPSnippet =
@"$0 ${1:test} if (true)
    {
    }";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestExtendSnippetTextChangeInMethodBackwardsForCaretForwardsForPlaceholder()
        {
            var markup =
@"public void Method()
{
    $$ [|if (true)
    {
    }|] {|placeholder:test|}
}";

            var expectedLSPSnippet =
@"$0 if (true)
    {
    } ${1:test}";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestExtendSnippetTextChangeInMethodBackwardsForPlaceholderForwardsForCaret()
        {
            var markup =
@"public void Method()
{
    {|placeholder:test|} [|if (true)
    {
    }|] $$
}";

            var expectedLSPSnippet =
@"${1:test} if (true)
    {
    } $0";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestExtendSnippetTextChangeInMethodWithCodeBeforeAndAfterBackwardsForPlaceholderForwardsForCaret()
        {
            var markup =
@"public void Method()
{
    var x = 5;
    {|placeholder:test|} [|if (true)
    {
    }|] $$
    
    x = 3;
}";

            var expectedLSPSnippet =
@"${1:test} if (true)
    {
    } $0";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public void TestExtendTextChangeInsertion()
        {
            var testString = "foo bar quux baz";
            using var workspace = CreateWorkspaceFromCode(testString);
            var document = workspace.CurrentSolution.GetRequiredDocument(workspace.Documents.First().Id);
            var lspSnippetString = RoslynLSPSnippetConverter.GenerateLSPSnippetAsync(document, caretPosition: 12,
                ImmutableArray<SnippetPlaceholder>.Empty, new TextChange(new TextSpan(8, 0), "quux"), triggerLocation: 12, CancellationToken.None).Result;
            AssertEx.EqualOrDiff("quux$0", lspSnippetString);
        }

        [Fact]
        public void TestExtendTextChangeReplacement()
        {
            var testString = "foo bar quux baz";
            using var workspace = CreateWorkspaceFromCode(testString);
            var document = workspace.CurrentSolution.GetRequiredDocument(workspace.Documents.First().Id);
            var lspSnippetString = RoslynLSPSnippetConverter.GenerateLSPSnippetAsync(document, caretPosition: 12,
                ImmutableArray<SnippetPlaceholder>.Empty, new TextChange(new TextSpan(4, 4), "bar quux"), triggerLocation: 12, CancellationToken.None).Result;
            AssertEx.EqualOrDiff("bar quux$0", lspSnippetString);
        }

        #endregion

        #region LSP Snippet generation tests

        [Fact]
        public Task TestForLoopSnippet()
        {
            var markup =
@"[|for (var {|placeholder1:i|} = 0; {|placeholder1:i|} < {|placeholder2:length|}; {|placeholder1:i|}++)
{$$
}|]";

            var expectedLSPSnippet =
@"for (var ${1:i} = 0; ${1:i} < ${2:length}; ${1:i}++)
{$0
}";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestIfSnippetSamePlaceholderCursorLocation()
        {
            var markup =
@"public void Method()
{
    var x = 5;
    [|if ({|placeholder:true|}$$)
    {
    }|]
    
    x = 3;
}";

            var expectedLSPSnippet =
@"if (${1:true}$0)
    {
    }";

            return TestAsync(markup, expectedLSPSnippet);
        }

        [Fact]
        public Task TestIfSnippetSameCursorPlaceholderLocation()
        {
            var markup =
@"public void Method()
{
    var x = 5;
    [|if ($${|placeholder:true|})
    {
    }|]
    
    x = 3;
}";

            var expectedLSPSnippet =
@"if ($0${1:true})
    {
    }";

            return TestAsync(markup, expectedLSPSnippet);
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
                    var identifier = text.Substring(spans[0].Start, spans[0].Length);
                    var placeholders = spans.Select(span => span.Start).ToImmutableArray();
                    arrayBuilder.Add(new SnippetPlaceholder(identifier, placeholders));
                }
            }

            return arrayBuilder.ToImmutable();
        }
    }
}
