// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.Editor.UnitTests.Structure;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure
{
    public class CommentTests : AbstractSyntaxStructureProviderTests
    {
        protected override string LanguageName => LanguageNames.CSharp;

        internal override async Task<ImmutableArray<BlockSpan>> GetBlockSpansWorkerAsync(Document document, int position)
        {
            var root = await document.GetSyntaxRootAsync();
            var trivia = root.FindTrivia(position, findInsideTrivia: true);

            var token = trivia.Token;

            if (token.LeadingTrivia.Contains(trivia))
            {
                return CSharpStructureHelpers.CreateCommentBlockSpan(token.LeadingTrivia);
            }
            else if (token.TrailingTrivia.Contains(trivia))
            {
                return CSharpStructureHelpers.CreateCommentBlockSpan(token.TrailingTrivia);
            }
            else
            {
                return Contract.FailWithReturn<ImmutableArray<BlockSpan>>();
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestSimpleComment1()
        {
            const string code = @"
{|span:// Hello
// $$C#|}
class C
{
}
";

            await VerifyBlockSpansAsync(code,
                Region("span", "// Hello ...", autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestSimpleComment2()
        {
            const string code = @"
{|span:// Hello
//
// $$C#!|}
class C
{
}
";

            await VerifyBlockSpansAsync(code,
                Region("span", "// Hello ...", autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestSimpleComment3()
        {
            const string code = @"
{|span:// Hello

// $$C#!|}
class C
{
}
";

            await VerifyBlockSpansAsync(code,
                Region("span", "// Hello ...", autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestSingleLineCommentGroupFollowedByDocumentationComment()
        {
            const string code = @"
{|span:// Hello

// $$C#!|}
/// <summary></summary>
class C
{
}
";

            await VerifyBlockSpansAsync(code,
                Region("span", "// Hello ...", autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestMultilineComment1()
        {
            const string code = @"
{|span:/* Hello
$$C# */|}
class C
{
}
";

            await VerifyBlockSpansAsync(code,
                Region("span", "/* Hello ...", autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestMultilineCommentOnOneLine()
        {
            const string code = @"
{|span:/* Hello $$C# */|}
class C
{
}
";

            await VerifyBlockSpansAsync(code,
                Region("span", "/* Hello C# ...", autoCollapse: true));
        }

        [WorkItem(791, "https://github.com/dotnet/roslyn/issues/791")]
        [WorkItem(1108049, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1108049")]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestIncompleteMultilineCommentZeroSpace()
        {
            const string code = @"
{|span:$$/*|}";

            await VerifyBlockSpansAsync(code,
                Region("span", "/*  ...", autoCollapse: true));
        }

        [WorkItem(791, "https://github.com/dotnet/roslyn/issues/791")]
        [WorkItem(1108049, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1108049")]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestIncompleteMultilineCommentSingleSpace()
        {
            const string code = @"
{|span:$$/* |}";

            await VerifyBlockSpansAsync(code,
                Region("span", "/*  ...", autoCollapse: true));
        }
    }
}
