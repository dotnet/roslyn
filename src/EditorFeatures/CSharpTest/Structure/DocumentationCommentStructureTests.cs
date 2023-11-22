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
public class DocumentationCommentStructureTests : AbstractCSharpSyntaxNodeStructureTests<DocumentationCommentTriviaSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new DocumentationCommentStructureProvider();

    [Fact]
    public async Task TestDocumentationCommentWithoutSummaryTag1()
    {
        var code = """
                {|span:/// $$XML doc comment
                /// some description
                /// of
                /// the comment|}
                class Class3
                {
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "/// XML doc comment ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestDocumentationCommentWithoutSummaryTag2()
    {
        var code = """
                {|span:/** $$Block comment
                * some description
                * of
                * the comment
                */|}
                class Class3
                {
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "/** Block comment ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestDocumentationCommentWithoutSummaryTag3()
    {
        var code = """
                {|span:/// $$<param name="tree"></param>|}
                class Class3
                {
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "/// <param name=\"tree\"></param> ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestDocumentationComment()
    {
        var code = """
                {|span:/// <summary>
                /// $$Hello C#!
                /// </summary>|}
                class Class3
                {
                }
                """;

        await VerifyBlockSpansAsync(code,
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
        var code = """
                {|span:/** <summary>
                $$Hello C#!
                </summary> */|}
                class Class3
                {
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "/** <summary> Hello C#!", autoCollapse: true));
    }

    [Fact]
    public async Task TestIndentedDocumentationComment()
    {
        var code = """
                {|span:/// <summary>
                /// $$Hello C#!
                /// </summary>|}
                class Class3
                {
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "/// <summary> Hello C#!", autoCollapse: true));
    }

    [Fact]
    public async Task TestIndentedMultilineDocumentationComment()
    {
        var code = """
                {|span:/** <summary>
                $$Hello C#!
                </summary> */|}
                class Class3
                {
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "/** <summary> Hello C#!", autoCollapse: true));
    }

    [Fact]
    public async Task TestDocumentationCommentOnASingleLine()
    {
        var code = """
                {|span:/// <summary>$$Hello C#!</summary>|}
                class Class3
                {
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "/// <summary>Hello C#!", autoCollapse: true));
    }

    [Fact]
    public async Task TestMultilineDocumentationCommentOnASingleLine()
    {
        var code = """
                {|span:/** <summary>$$Hello C#!</summary> */|}
                class Class3
                {
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "/** <summary>Hello C#!", autoCollapse: true));
    }

    [Fact]
    public async Task TestIndentedDocumentationCommentOnASingleLine()
    {
        var code = """
                {|span:/// <summary>$$Hello C#!</summary>|}
                class Class3
                {
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "/// <summary>Hello C#!", autoCollapse: true));
    }

    [Fact]
    public async Task TestIndentedMultilineDocumentationCommentOnASingleLine()
    {
        var code = """
                {|span:/** <summary>$$Hello C#!</summary> */|}
                class Class3
                {
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "/** <summary>Hello C#!", autoCollapse: true));
    }

    [Fact]
    public async Task TestMultilineSummaryInDocumentationComment1()
    {
        var code = """
                {|span:/// <summary>
                /// $$Hello
                /// C#!
                /// </summary>|}
                class Class3
                {
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "/// <summary> Hello C#!", autoCollapse: true));
    }

    [Fact]
    public async Task TestMultilineSummaryInDocumentationComment2()
    {
        var code = """
                {|span:/// <summary>
                /// $$Hello
                /// 
                /// C#!
                /// </summary>|}
                class Class3
                {
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "/// <summary> Hello C#!", autoCollapse: true));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2129")]
    public async Task CrefInSummary()
    {
        var code = """
                class C
                {
                    {|span:/// $$<summary>
                    /// Summary with <see cref="SeeClass" />, <seealso cref="SeeAlsoClass" />, 
                    /// <see langword="null" />, <typeparamref name="T" />, <paramref name="t" />, and <see unsupported-attribute="not-supported" />.
                    /// </summary>|}
                    public void M<T>(T t) { }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "/// <summary> Summary with SeeClass, SeeAlsoClass, null, T, t, and not-supported.", autoCollapse: true));
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=402822")]
    public async Task TestSummaryWithPunctuation()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "/// <summary> The main entrypoint for Program.", autoCollapse: true));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20679")]
    public async Task TestSummaryWithAdditionalTags()
    {
        var code = """
                public class Class1
                {
                    {|span:/// $$<summary>
                    /// Initializes a <c>new</c> instance of the <see cref="Class1" /> class.
                    /// </summary>|}
                    public Class1()
                    {

                    }
                }
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", "/// <summary> Initializes a new instance of the Class1 class.", autoCollapse: true));
    }
}
