// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure;

[Trait(Traits.Feature, Traits.Features.Outlining)]
public sealed class DocumentationCommentStructureTests : AbstractCSharpSyntaxNodeStructureTests<DocumentationCommentTriviaSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new DocumentationCommentStructureProvider();

    [Fact]
    public async Task TestDocumentationCommentWithoutSummaryTag1()
    {
        await VerifyBlockSpansAsync("""
                {|span:/// $$XML doc comment
                /// some description
                /// of
                /// the comment|}
                class Class3
                {
                }
                """,
            Region("span", "/// XML doc comment ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestDocumentationCommentWithoutSummaryTag2()
    {
        await VerifyBlockSpansAsync("""
                {|span:/** $$Block comment
                * some description
                * of
                * the comment
                */|}
                class Class3
                {
                }
                """,
            Region("span", "/** Block comment ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestDocumentationCommentWithoutSummaryTag3()
    {
        await VerifyBlockSpansAsync("""
                {|span:/// $$<param name="tree"></param>|}
                class Class3
                {
                }
                """,
            Region("span", "/// <param name=\"tree\"></param> ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestDocumentationComment()
    {
        await VerifyBlockSpansAsync("""
                {|span:/// <summary>
                /// $$Hello C#!
                /// </summary>|}
                class Class3
                {
                }
                """,
            Region("span", "/// <summary> Hello C#!", autoCollapse: true));
    }

    [Fact]
    public async Task TestDocumentationCommentWithLongBannerText()
    {
        var code = """
                {|span:/// $$<summary>
                ///
                """ + new string('x', 240) + """
                /// </summary>|}
                class Class3
                {
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "/// <summary> " + new string('x', 106) + " ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestMultilineDocumentationComment()
    {
        await VerifyBlockSpansAsync("""
                {|span:/** <summary>
                $$Hello C#!
                </summary> */|}
                class Class3
                {
                }
                """,
            Region("span", "/** <summary> Hello C#!", autoCollapse: true));
    }

    [Fact]
    public async Task TestIndentedDocumentationComment()
    {
        await VerifyBlockSpansAsync("""
                {|span:/// <summary>
                /// $$Hello C#!
                /// </summary>|}
                class Class3
                {
                }
                """,
            Region("span", "/// <summary> Hello C#!", autoCollapse: true));
    }

    [Fact]
    public async Task TestIndentedMultilineDocumentationComment()
    {
        await VerifyBlockSpansAsync("""
                {|span:/** <summary>
                $$Hello C#!
                </summary> */|}
                class Class3
                {
                }
                """,
            Region("span", "/** <summary> Hello C#!", autoCollapse: true));
    }

    [Fact]
    public async Task TestDocumentationCommentOnASingleLine()
    {
        await VerifyBlockSpansAsync("""
                {|span:/// <summary>$$Hello C#!</summary>|}
                class Class3
                {
                }
                """,
            Region("span", "/// <summary>Hello C#!", autoCollapse: true));
    }

    [Fact]
    public async Task TestMultilineDocumentationCommentOnASingleLine()
    {
        await VerifyBlockSpansAsync("""
                {|span:/** <summary>$$Hello C#!</summary> */|}
                class Class3
                {
                }
                """,
            Region("span", "/** <summary>Hello C#!", autoCollapse: true));
    }

    [Fact]
    public async Task TestIndentedDocumentationCommentOnASingleLine()
    {
        await VerifyBlockSpansAsync("""
                {|span:/// <summary>$$Hello C#!</summary>|}
                class Class3
                {
                }
                """,
            Region("span", "/// <summary>Hello C#!", autoCollapse: true));
    }

    [Fact]
    public async Task TestIndentedMultilineDocumentationCommentOnASingleLine()
    {
        await VerifyBlockSpansAsync("""
                {|span:/** <summary>$$Hello C#!</summary> */|}
                class Class3
                {
                }
                """,
            Region("span", "/** <summary>Hello C#!", autoCollapse: true));
    }

    [Fact]
    public async Task TestMultilineSummaryInDocumentationComment1()
    {
        await VerifyBlockSpansAsync("""
                {|span:/// <summary>
                /// $$Hello
                /// C#!
                /// </summary>|}
                class Class3
                {
                }
                """,
            Region("span", "/// <summary> Hello C#!", autoCollapse: true));
    }

    [Fact]
    public async Task TestMultilineSummaryInDocumentationComment2()
    {
        await VerifyBlockSpansAsync("""
                {|span:/// <summary>
                /// $$Hello
                /// 
                /// C#!
                /// </summary>|}
                class Class3
                {
                }
                """,
            Region("span", "/// <summary> Hello C#!", autoCollapse: true));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2129")]
    public async Task CrefInSummary()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    {|span:/// $$<summary>
                    /// Summary with <see cref="SeeClass" />, <seealso cref="SeeAlsoClass" />, 
                    /// <see langword="null" />, <typeparamref name="T" />, <paramref name="t" />, and <see unsupported-attribute="not-supported" />.
                    /// </summary>|}
                    public void M<T>(T t) { }
                }
                """,
            Region("span", "/// <summary> Summary with SeeClass, SeeAlsoClass, null, T, t, and not-supported.", autoCollapse: true));
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=402822")]
    public async Task TestSummaryWithPunctuation()
    {
        await VerifyBlockSpansAsync("""
                class C
                {
                    {|span:/// $$<summary>
                    /// The main entrypoint for <see cref="Program"/>.
                    /// </summary>
                    /// <param name="args"></param>|}
                    void Main()
                    {
                    }
                }
                """,
            Region("span", "/// <summary> The main entrypoint for Program.", autoCollapse: true));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20679")]
    public async Task TestSummaryWithAdditionalTags()
    {
        await VerifyBlockSpansAsync("""
                public class Class1
                {
                    {|span:/// $$<summary>
                    /// Initializes a <c>new</c> instance of the <see cref="Class1" /> class.
                    /// </summary>|}
                    public Class1()
                    {

                    }
                }
                """,
            Region("span", "/// <summary> Initializes a new instance of the Class1 class.", autoCollapse: true));
    }
}
