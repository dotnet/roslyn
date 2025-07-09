// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.BraceHighlighting;

[Trait(Traits.Feature, Traits.Features.BraceHighlighting)]
public sealed class MultiCharacterBraceHighlightingTests : AbstractBraceHighlightingTests
{
    protected override EditorTestWorkspace CreateWorkspace(string markup, ParseOptions options)
        => EditorTestWorkspace.Create(
            NoCompilationConstants.LanguageName, compilationOptions: null, parseOptions: options, content: markup);

    internal override IBraceMatchingService GetBraceMatchingService(EditorTestWorkspace workspace)
        => new TestBraceMatchingService();

    private sealed class TestBraceMatchingService : IBraceMatchingService
    {
        public async Task<BraceMatchingResult?> GetMatchingBracesAsync(
            Document document, int position, BraceMatchingOptions options, CancellationToken cancellationToken)
        {
            var text = (await document.GetTextAsync(cancellationToken)).ToString();
            var braces = GetMatchingBraces(text, position);
            if (braces.HasValue)
            {
                Debug.Assert(text.Substring(braces.Value.LeftSpan.Start, braces.Value.LeftSpan.Length) == "<@");
                Debug.Assert(text.Substring(braces.Value.RightSpan.Start, braces.Value.RightSpan.Length) == "@>");
            }

            return braces;
        }

        public static BraceMatchingResult? GetMatchingBraces(
            string text, int position)
        {
            if (position < text.Length)
            {
                var ch = text[position];

                // Look for   <@   @>  depending on where the caret is.

                //      ^<@     @>
                if (ch == '<')
                {
                    Debug.Assert(text[position + 1] == '@');
                    var secondAt = text.IndexOf('@', position + 2);
                    return new BraceMatchingResult(new TextSpan(position, 2), new TextSpan(secondAt, 2));
                }

                //  <^@    @>     or   <@    ^@>
                if (ch == '@')
                {
                    if (text[position - 1] == '<')
                    {
                        var secondAt = text.IndexOf('@', position + 1);
                        return new BraceMatchingResult(new TextSpan(position - 1, 2), new TextSpan(secondAt, 2));
                    }
                    else
                    {
                        Debug.Assert(text[position + 1] == '>');
                        var lessThan = text.LastIndexOf('<', position);
                        return new BraceMatchingResult(new TextSpan(lessThan, 2), new TextSpan(position, 2));
                    }
                }

                // <@    @^>
                if (ch == '>')
                {
                    Debug.Assert(text[position - 1] == '@');
                    var lessThan = text.LastIndexOf('<', position);
                    return new BraceMatchingResult(new TextSpan(lessThan, 2), new TextSpan(position - 1, 2));
                }
            }

            return null;
        }
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/18050")]
    public Task TestNotOnBrace()
        => TestBraceHighlightingAsync(
"$$ <@    @>");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/18050")]
    public Task TestOnLeftOfStartBrace()
        => TestBraceHighlightingAsync(
"$$[|<@|]    [|@>|]");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/18050")]
    public Task TestInsideStartBrace()
        => TestBraceHighlightingAsync(
"[|<$$@|]    [|@>|]");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/18050")]
    public Task TestNotOnRightOfStartBrace()
        => TestBraceHighlightingAsync(
"<@$$    @>");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/18050")]
    public Task TestNotOnLeftOfCloseBrace()
        => TestBraceHighlightingAsync(
"<@    $$@>");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/18050")]
    public Task TestInsideCloseBrace()
        => TestBraceHighlightingAsync(
"[|<@|]    [|@$$>|]");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/18050")]
    public Task TestOnRightOfCloseBrace()
        => TestBraceHighlightingAsync(
"[|<@|]    [|@>$$|]");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/18050")]
    public Task TestNotAfterBrace()
        => TestBraceHighlightingAsync(
"<@    @> $$");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/18050")]
    public Task TestNotOnBrace2()
        => TestBraceHighlightingAsync(
"$$ <@    @><@    @>");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/18050")]
    public Task TestOnLeftOfStartBrace2()
        => TestBraceHighlightingAsync(
"$$[|<@|]    [|@>|]<@    @>");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/18050")]
    public Task TestInsideStartBrace2()
        => TestBraceHighlightingAsync(
"[|<$$@|]    [|@>|]<@    @>");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/18050")]
    public Task TestNotOnRightOfStartBrace2()
        => TestBraceHighlightingAsync(
"<@$$    @><@    @>");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/18050")]
    public Task TestNotOnLeftOfCloseBrace2()
        => TestBraceHighlightingAsync(
"<@    $$@><@    @>");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/18050")]
    public Task TestInsideCloseBrace3()
        => TestBraceHighlightingAsync(
"[|<@|]    [|@$$>|]<@    @>");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/18050")]
    public Task TestOnRightOfCloseBrace2()
        => TestBraceHighlightingAsync(
"[|<@|]    [|@>|]$$[|<@|]    [|@>|]");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/18050")]
    public Task TestInSecondBracePair()
        => TestBraceHighlightingAsync(
"<@    @>[|<$$@|]    [|@>|]");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/18050")]
    public Task TestNotAfterSecondBracePairStart()
        => TestBraceHighlightingAsync(
"<@    @><@$$    @>");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/18050")]
    public Task TestNotBeforeSecondBracePairEnd()
        => TestBraceHighlightingAsync(
"<@    @><@    $$@>");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/18050")]
    public Task TestInSecondBracePairEnd()
        => TestBraceHighlightingAsync(
"<@    @>[|<@|]    [|@$$>|]");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/18050")]
    public Task TestAtSecondBracePairEnd()
        => TestBraceHighlightingAsync(
"<@    @>[|<@|]    [|@>|]$$");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/18050")]
    public Task TestNotAfterSecondBracePairEnd()
        => TestBraceHighlightingAsync(
"<@    @><@    @>  $$");
}
