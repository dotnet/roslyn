// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.Editor.UnitTests.Structure;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure;

[Trait(Traits.Feature, Traits.Features.Outlining)]
public class CommentStructureTests : AbstractSyntaxStructureProviderTests
{
    protected override string LanguageName => LanguageNames.CSharp;

    private static ImmutableArray<BlockSpan> CreateCommentBlockSpan(
        SyntaxTriviaList triviaList)
    {
        using var _ = ArrayBuilder<BlockSpan>.GetInstance(out var result);
        CSharpStructureHelpers.CollectCommentBlockSpans(triviaList, result);
        return result.ToImmutableAndClear();
    }

    internal override async Task<ImmutableArray<BlockSpan>> GetBlockSpansWorkerAsync(Document document, BlockStructureOptions options, int position)
    {
        var root = await document.GetRequiredSyntaxRootAsync(CancellationToken.None);
        var trivia = root.FindTrivia(position, findInsideTrivia: true);

        var token = trivia.Token;

        if (token.LeadingTrivia.Contains(trivia))
        {
            return CreateCommentBlockSpan(token.LeadingTrivia);
        }
        else if (token.TrailingTrivia.Contains(trivia))
        {
            return CreateCommentBlockSpan(token.TrailingTrivia);
        }

        throw Roslyn.Utilities.ExceptionUtilities.Unreachable();
    }

    [Fact]
    public async Task TestSimpleComment1()
    {
        var code = """
                {|span:// Hello
                // $$C#|}
                class C
                {
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "// Hello ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestSimpleComment2()
    {
        var code = """
                {|span:// Hello
                //
                // $$C#!|}
                class C
                {
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "// Hello ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestSimpleComment3()
    {
        var code = """
                {|span:// Hello

                // $$C#!|}
                class C
                {
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "// Hello ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestSingleLineCommentGroupFollowedByDocumentationComment()
    {
        var code = """
                {|span:// Hello

                // $$C#!|}
                /// <summary></summary>
                class C
                {
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "// Hello ...", autoCollapse: true));
    }
}
