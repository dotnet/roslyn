// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
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

        internal override OutliningSpan[] GetRegions(Document document, int position)
        {
            var root = document.GetSyntaxRootAsync(CancellationToken.None).Result;
            var trivia = root.FindTrivia(position, findInsideTrivia: true);

            var token = trivia.Token;

            if (token.LeadingTrivia.Contains(trivia))
            {
                return CSharpOutliningHelpers.CreateCommentRegions(token.LeadingTrivia).ToArray();
            }
            else if (token.TrailingTrivia.Contains(trivia))
            {
                return CSharpOutliningHelpers.CreateCommentRegions(token.TrailingTrivia).ToArray();
            }
            else
            {
                return Contract.FailWithReturn<OutliningSpan[]>();
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestSimpleComment1()
        {
            const string code = @"
{|span:// Hello
// $$C#|}
class C
{
}
";

            Regions(code,
                Region("span", "// Hello ...", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestSimpleComment2()
        {
            const string code = @"
{|span:// Hello
//
// $$C#!|}
class C
{
}
";

            Regions(code,
                Region("span", "// Hello ...", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestSimpleComment3()
        {
            const string code = @"
{|span:// Hello

// $$C#!|}
class C
{
}
";

            Regions(code,
                Region("span", "// Hello ...", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestSingleLineCommentGroupFollowedByDocumentationComment()
        {
            const string code = @"
{|span:// Hello

// $$C#!|}
/// <summary></summary>
class C
{
}
";

            Regions(code,
                Region("span", "// Hello ...", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestMultilineComment1()
        {
            const string code = @"
{|span:/* Hello
$$C# */|}
class C
{
}
";

            Regions(code,
                Region("span", "/* Hello ...", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestMultilineCommentOnOneLine()
        {
            const string code = @"
{|span:/* Hello $$C# */|}
class C
{
}
";

            Regions(code,
                Region("span", "/* Hello C# ...", autoCollapse: true));
        }

        [WorkItem(791)]
        [WorkItem(1108049)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestIncompleteMultilineCommentZeroSpace()
        {
            const string code = @"
{|span:$$/*|}";

            Regions(code,
                Region("span", "/*  ...", autoCollapse: true));
        }

        [WorkItem(791)]
        [WorkItem(1108049)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestIncompleteMultilineCommentSingleSpace()
        {
            const string code = @"
{|span:$$/* |}";

            Regions(code,
                Region("span", "/*  ...", autoCollapse: true));
        }
    }
}
