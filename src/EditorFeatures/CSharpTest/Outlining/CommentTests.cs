// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Editor.UnitTests.Outlining;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class CommentTests : AbstractSyntaxOutlinerTests
    {
        protected override string LanguageName => LanguageNames.CSharp;

        internal override Task<OutliningSpan[]> GetRegionsAsync(Document document, int position)
        {
            var root = document.GetSyntaxRootAsync(CancellationToken.None).Result;
            var trivia = root.FindTrivia(position, findInsideTrivia: true);

            var token = trivia.Token;

            if (token.LeadingTrivia.Contains(trivia))
            {
                return Task.FromResult(CSharpOutliningHelpers.CreateCommentRegions(token.LeadingTrivia).ToArray());
            }
            else if (token.TrailingTrivia.Contains(trivia))
            {
                return Task.FromResult(CSharpOutliningHelpers.CreateCommentRegions(token.TrailingTrivia).ToArray());
            }
            else
            {
                return Task.FromResult(Contract.FailWithReturn<OutliningSpan[]>());
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

            await VerifyRegionsAsync(code,
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

            await VerifyRegionsAsync(code,
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

            await VerifyRegionsAsync(code,
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

            await VerifyRegionsAsync(code,
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

            await VerifyRegionsAsync(code,
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

            await VerifyRegionsAsync(code,
                Region("span", "/* Hello C# ...", autoCollapse: true));
        }

        [WorkItem(791)]
        [WorkItem(1108049)]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestIncompleteMultilineCommentZeroSpace()
        {
            const string code = @"
{|span:$$/*|}";

            await VerifyRegionsAsync(code,
                Region("span", "/*  ...", autoCollapse: true));
        }

        [WorkItem(791)]
        [WorkItem(1108049)]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestIncompleteMultilineCommentSingleSpace()
        {
            const string code = @"
{|span:$$/* |}";

            await VerifyRegionsAsync(code,
                Region("span", "/*  ...", autoCollapse: true));
        }
    }
}
