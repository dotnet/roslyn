// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class DocumentationCommentOutlinerTests : AbstractCSharpSyntaxNodeOutlinerTests<DocumentationCommentTriviaSyntax>
    {
        internal override AbstractSyntaxOutliner CreateOutliner() => new DocumentationCommentOutliner();

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestDocumentationCommentWithoutSummaryTag1()
        {
            const string code = @"
{|span:/// $$XML doc comment
/// some description
/// of
/// the comment|}
class Class3
{
}";

            await VerifyRegionsAsync(code,
                Region("span", "/// XML doc comment ...", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestDocumentationCommentWithoutSummaryTag2()
        {
            const string code = @"
{|span:/** $$Block comment
* some description
* of
* the comment
*/|}
class Class3
{
}";

            await VerifyRegionsAsync(code,
                Region("span", "/** Block comment ...", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestDocumentationCommentWithoutSummaryTag3()
        {
            const string code = @"
{|span:/// $$<param name=""tree""></param>|}
class Class3
{
}";

            await VerifyRegionsAsync(code,
                Region("span", "/// <param name=\"tree\"></param> ...", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestDocumentationComment()
        {
            const string code = @"
{|span:/// <summary>
/// $$Hello C#!
/// </summary>|}
class Class3
{
}";

            await VerifyRegionsAsync(code,
                Region("span", "/// <summary> Hello C#!", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestDocumentationCommentWithLongBannerText()
        {
            var code = @"
{|span:/// $$<summary>
/// " + new string('x', 240) + @"
/// </summary>|}
class Class3
{
}";

            await VerifyRegionsAsync(code,
                Region("span", "/// <summary> " + new string('x', 106) + " ...", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestMultilineDocumentationComment()
        {
            const string code = @"
{|span:/** <summary>
$$Hello C#!
</summary> */|}
class Class3
{
}";

            await VerifyRegionsAsync(code,
                Region("span", "/** <summary> Hello C#!", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestIndentedDocumentationComment()
        {
            const string code = @"
    {|span:/// <summary>
    /// $$Hello C#!
    /// </summary>|}
    class Class3
    {
    }";

            await VerifyRegionsAsync(code,
                Region("span", "/// <summary> Hello C#!", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestIndentedMultilineDocumentationComment()
        {
            const string code = @"
    {|span:/** <summary>
    $$Hello C#!
    </summary> */|}
    class Class3
    {
    }";

            await VerifyRegionsAsync(code,
                Region("span", "/** <summary> Hello C#!", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestDocumentationCommentOnASingleLine()
        {
            const string code = @"
{|span:/// <summary>$$Hello C#!</summary>|}
class Class3
{
}";

            await VerifyRegionsAsync(code,
                Region("span", "/// <summary> Hello C#!", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestMultilineDocumentationCommentOnASingleLine()
        {
            const string code = @"
{|span:/** <summary>$$Hello C#!</summary> */|}
class Class3
{
}";

            await VerifyRegionsAsync(code,
                Region("span", "/** <summary> Hello C#!", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestIndentedDocumentationCommentOnASingleLine()
        {
            const string code = @"
    {|span:/// <summary>$$Hello C#!</summary>|}
    class Class3
    {
    }";

            await VerifyRegionsAsync(code,
                Region("span", "/// <summary> Hello C#!", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestIndentedMultilineDocumentationCommentOnASingleLine()
        {
            const string code = @"
    {|span:/** <summary>$$Hello C#!</summary> */|}
    class Class3
    {
    }";

            await VerifyRegionsAsync(code,
                Region("span", "/** <summary> Hello C#!", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestMultilineSummaryInDocumentationComment1()
        {
            const string code = @"
{|span:/// <summary>
/// $$Hello
/// C#!
/// </summary>|}
class Class3
{
}";

            await VerifyRegionsAsync(code,
                Region("span", "/// <summary> Hello C#!", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestMultilineSummaryInDocumentationComment2()
        {
            const string code = @"
{|span:/// <summary>
/// $$Hello
/// 
/// C#!
/// </summary>|}
class Class3
{
}";

            await VerifyRegionsAsync(code,
                Region("span", "/// <summary> Hello C#!", autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        [WorkItem(2129, "https://github.com/dotnet/roslyn/issues/2129")]
        public async Task CrefInSummary()
        {
            const string code = @"
class C
{
    {|span:/// $$<summary>
    /// Summary with <see cref=""SeeClass"" />, <seealso cref=""SeeAlsoClass"" />, 
    /// <see langword=""null"" />, <typeparamref name=""T"" />, <paramref name=""t"" />, and <see unsupported-attribute=""not-supported"" />.
    /// </summary>|}
    public void M<T>(T t) { }
}";

            await VerifyRegionsAsync(code,
                Region("span", "/// <summary> Summary with SeeClass , SeeAlsoClass , null , T , t , and not-supported .", autoCollapse: true));
        }
    }
}
