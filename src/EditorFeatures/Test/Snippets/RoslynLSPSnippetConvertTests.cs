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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Snippets
{
    [UseExportProvider]
    public class RoslynLSPSnippetConvertTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.RoslynLSPSnippetConverter)]
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
            MarkupTestFile.GetPositionAndSpans(markup, out var outString, out var cursorPosition, out IDictionary<string, ImmutableArray<TextSpan>> dictionary);
            var stringSpan = dictionary[""].First();
            var textChange = new TextChange(new TextSpan(stringSpan.Start, 0), outString[..stringSpan.Length]);
            var placeholders = dictionary["placeholder"].Select(span => span.Start).ToImmutableArray();
            return TestAsync(markup, expectedLSPSnippet, cursorPosition, ImmutableArray.Create(new SnippetPlaceholder("true", placeholders)), textChange);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RoslynLSPSnippetConverter)]
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
            MarkupTestFile.GetPositionAndSpans(markup, out var outString, out var cursorPosition, out IDictionary<string, ImmutableArray<TextSpan>> dictionary);
            var stringSpan = dictionary[""].First();
            var textChange = new TextChange(new TextSpan(stringSpan.Start, 0), outString[stringSpan.Start..]);
            var placeholders = dictionary["placeholder"].Select(span => span.Start).ToImmutableArray();
            return TestAsync(markup, expectedLSPSnippet, cursorPosition, ImmutableArray.Create(new SnippetPlaceholder("true", placeholders)), textChange);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RoslynLSPSnippetConverter)]
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
            MarkupTestFile.GetPositionAndSpans(markup, out var outString, out var cursorPosition, out IDictionary<string, ImmutableArray<TextSpan>> dictionary);
            var stringSpan = dictionary[""].First();
            var textChange = new TextChange(new TextSpan(stringSpan.Start, 0), outString[..stringSpan.Length]);
            var placeholders = dictionary["placeholder"].Select(span => span.Start).ToImmutableArray();
            return TestAsync(markup, expectedLSPSnippet, cursorPosition, ImmutableArray.Create(new SnippetPlaceholder("test", placeholders)), textChange);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RoslynLSPSnippetConverter)]
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
            MarkupTestFile.GetPositionAndSpans(markup, out var outString, out var cursorPosition, out IDictionary<string, ImmutableArray<TextSpan>> dictionary);
            var stringSpan = dictionary[""].First();
            var textChange = new TextChange(new TextSpan(stringSpan.Start, 0), outString[stringSpan.Start..]);
            var placeholders = dictionary["placeholder"].Select(span => span.Start).ToImmutableArray();
            return TestAsync(markup, expectedLSPSnippet, cursorPosition, ImmutableArray.Create(new SnippetPlaceholder("test", placeholders)), textChange);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RoslynLSPSnippetConverter)]
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
            MarkupTestFile.GetPositionAndSpans(markup, out var outString, out var cursorPosition, out IDictionary<string, ImmutableArray<TextSpan>> dictionary);
            var stringSpan = dictionary[""].First();
            var textChange = new TextChange(new TextSpan(stringSpan.Start, 0), outString[..stringSpan.Length]);
            var placeholders = dictionary["placeholder"].Select(span => span.Start).ToImmutableArray();
            return TestAsync(markup, expectedLSPSnippet, cursorPosition, ImmutableArray.Create(new SnippetPlaceholder("test", placeholders)), textChange);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RoslynLSPSnippetConverter)]
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
            MarkupTestFile.GetPositionAndSpans(markup, out var outString, out var cursorPosition, out IDictionary<string, ImmutableArray<TextSpan>> dictionary);
            var stringSpan = dictionary[""].First();
            var textChange = new TextChange(new TextSpan(stringSpan.Start, 0), outString[..stringSpan.Length]);
            var placeholders = dictionary["placeholder"].Select(span => span.Start).ToImmutableArray();
            return TestAsync(markup, expectedLSPSnippet, cursorPosition, ImmutableArray.Create(new SnippetPlaceholder("test", placeholders)), textChange);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RoslynLSPSnippetConverter)]
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
            MarkupTestFile.GetPositionAndSpans(markup, out var outString, out var cursorPosition, out IDictionary<string, ImmutableArray<TextSpan>> dictionary);
            var stringSpan = dictionary[""].First();
            var textChange = new TextChange(new TextSpan(stringSpan.Start, 0), outString[stringSpan.Start..]);
            var placeholders = dictionary["placeholder"].Select(span => span.Start).ToImmutableArray();
            return TestAsync(markup, expectedLSPSnippet, cursorPosition, ImmutableArray.Create(new SnippetPlaceholder("test", placeholders)), textChange);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RoslynLSPSnippetConverter)]
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
            MarkupTestFile.GetPositionAndSpans(markup, out var outString, out var cursorPosition, out IDictionary<string, ImmutableArray<TextSpan>> dictionary);
            var stringSpan = dictionary[""].First();
            var textChange = new TextChange(new TextSpan(stringSpan.Start, 0), outString[stringSpan.Start..]);
            var placeholders = dictionary["placeholder"].Select(span => span.Start).ToImmutableArray();
            return TestAsync(markup, expectedLSPSnippet, cursorPosition, ImmutableArray.Create(new SnippetPlaceholder("test", placeholders)), textChange);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RoslynLSPSnippetConverter)]
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
            MarkupTestFile.GetPositionAndSpans(markup, out var outString, out var cursorPosition, out IDictionary<string, ImmutableArray<TextSpan>> dictionary);
            var stringSpan = dictionary[""].First();
            var textChange = new TextChange(new TextSpan(stringSpan.Start, 0), outString.Substring(stringSpan.Start, stringSpan.Length));
            var placeholders = dictionary["placeholder"].Select(span => span.Start).ToImmutableArray();
            return TestAsync(markup, expectedLSPSnippet, cursorPosition, ImmutableArray.Create(new SnippetPlaceholder("test", placeholders)), textChange);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RoslynLSPSnippetConverter)]
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
            MarkupTestFile.GetPositionAndSpans(markup, out var outString, out var cursorPosition, out IDictionary<string, ImmutableArray<TextSpan>> dictionary);
            var stringSpan = dictionary[""].First();
            var textChange = new TextChange(new TextSpan(stringSpan.Start, 0), outString.Substring(stringSpan.Start, stringSpan.Length));
            var placeholders = dictionary["placeholder"].Select(span => span.Start).ToImmutableArray();
            return TestAsync(markup, expectedLSPSnippet, cursorPosition, ImmutableArray.Create(new SnippetPlaceholder("test", placeholders)), textChange);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RoslynLSPSnippetConverter)]
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
            MarkupTestFile.GetPositionAndSpans(markup, out var outString, out var cursorPosition, out IDictionary<string, ImmutableArray<TextSpan>> dictionary);
            var stringSpan = dictionary[""].First();
            var textChange = new TextChange(new TextSpan(stringSpan.Start, 0), outString);
            var placeholders1 = dictionary["placeholder1"].Select(span => span.Start).ToImmutableArray();
            var placeholders2 = dictionary["placeholder2"].Select(span => span.Start).ToImmutableArray();
            return TestAsync(markup, expectedLSPSnippet, cursorPosition, ImmutableArray.Create(
                new SnippetPlaceholder("i", placeholders1),
                new SnippetPlaceholder("length", placeholders2)), textChange);
        }

        protected static TestWorkspace CreateWorkspaceFromCode(string code)
         => TestWorkspace.CreateCSharp(code);

        private static async Task TestAsync(string markup, string expectedLSPSnippet, int? cursorPosition, ImmutableArray<SnippetPlaceholder> placeholders, TextChange textChange)
        {
            using var workspace = CreateWorkspaceFromCode(markup);
            var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);

            if (document is not null)
            {
                var lspSnippetString = await RoslynLSPSnippetConverter.GenerateLSPSnippetAsync(document, cursorPosition!.Value, placeholders, textChange).ConfigureAwait(false);
                AssertEx.EqualOrDiff(expectedLSPSnippet, lspSnippetString);
            }
        }
    }
}
