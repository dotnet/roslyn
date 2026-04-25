// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.AutoInsert;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.LanguageServer.AutoInsert;

public class AutoClosingTagOnAutoInsertProviderTest(ITestOutputHelper testOutput) : RazorOnAutoInsertProviderTestBase(testOutput)
{
    private protected override IOnAutoInsertProvider CreateProvider()
        => new AutoClosingTagOnAutoInsertProvider();

    private static readonly TagHelperDescriptor s_catchAllTagHelper =
        TagHelperDescriptorBuilder.CreateTagHelper("CatchAllTagHelper", "TestAssembly")
            .TypeName("TestNamespace.CatchAllTagHelper")
            .TagMatchingRuleDescriptor(builder => builder
                .RequireTagName("*")
                .RequireTagStructure(TagStructure.Unspecified))
            .Build();

    private static readonly TagHelperDescriptor s_unspecifiedInputMirroringTagHelper =
        TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "TestAssembly")
            .TypeName("TestNamespace.TestTagHelper")
            .TagMatchingRuleDescriptor(builder => builder
                .RequireTagName("Input")
                .RequireTagStructure(TagStructure.Unspecified))
            .Build();

    private static readonly TagHelperDescriptor s_unspecifiedTagHelper =
        TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "TestAssembly")
            .TypeName("TestNamespace.TestTagHelper")
            .TagMatchingRuleDescriptor(builder => builder
                .RequireTagName("test")
            .RequireTagStructure(TagStructure.Unspecified))
            .Build();

    private static readonly TagHelperDescriptor s_unspecifiedInputTagHelper =
        TagHelperDescriptorBuilder.CreateTagHelper("TestInputTagHelper", "TestAssembly")
            .TypeName("TestNamespace.TestInputTagHelper")
            .TagMatchingRuleDescriptor(builder => builder
                .RequireTagName("input")
                .RequireTagStructure(TagStructure.Unspecified))
            .Build();

    private static readonly TagHelperDescriptor s_normalOrSelfclosingInputTagHelper =
        TagHelperDescriptorBuilder.CreateTagHelper("TestInputTagHelper", "TestAssembly")
            .TypeName("TestNamespace.TestInputTagHelper")
            .TagMatchingRuleDescriptor(builder => builder
                .RequireTagName("input")
                .RequireTagStructure(TagStructure.NormalOrSelfClosing))
            .Build();

    private static readonly TagHelperDescriptor s_normalOrSelfClosingTagHelper =
        TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper2", "TestAssembly")
            .TypeName("TestNamespace.TestTagHelper2")
            .TagMatchingRuleDescriptor(builder => builder
                .RequireTagName("test")
                .RequireTagStructure(TagStructure.NormalOrSelfClosing))
            .Build();

    private static readonly TagHelperDescriptor s_withoutEndTagTagHelper =
        TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper3", "TestAssembly")
            .TypeName("TestNamespace.TestTagHelper3")
            .TagMatchingRuleDescriptor(builder => builder
                .RequireTagName("test")
                .RequireTagStructure(TagStructure.WithoutEndTag))
            .Build();

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/6217")]
    public void OnTypeCloseAngle_ConflictingAutoClosingBehaviorsChoosesMostSpecific()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                <test>$$
                """,
            expected: """
                @addTagHelper *, TestAssembly

                <test />
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_withoutEndTagTagHelper, s_catchAllTagHelper]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/36125")]
    public void OnTypeCloseAngle_TagHelperAlreadyHasEndTag()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                <test>$$<test></test></test>
                """,
            expected: """
                @addTagHelper *, TestAssembly

                <test><test></test></test>
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_normalOrSelfClosingTagHelper]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/36125")]
    public void OnTypeCloseAngle_VoidTagHelperHasEndTag_ShouldStillAutoClose()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                <input>$$<input></input></input>
                """,
            expected: """
                @addTagHelper *, TestAssembly

                <input /><input></input></input>
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_unspecifiedInputTagHelper]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/36125")]
    public void OnTypeCloseAngle_TagAlreadyHasEndTag()
    {
        RunAutoInsertTest(
            input: """
                <div>$$<div></div></div>
                """,
            expected: """
                <div><div></div></div>
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/36125")]
    public void OnTypeCloseAngle_TagDoesAutoCloseOutOfScope()
    {
        RunAutoInsertTest(
            input: """
                <div>
                    @if (true)
                    {
                        <div>$$</div>
                    }
                """,
            expected: """
                <div>
                    @if (true)
                    {
                        <div>$0</div></div>
                    }
                """);
    }

    [Fact]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2251322")]
    public void OnTypeCloseAngle_TagDoesAutoCloseInsideCSharpStatement()
    {
        RunAutoInsertTest(
            input: """
                <div>
                    @if (true)
                    {
                        <div>$$
                    }
                </div>
                """,
            expected: """
                <div>
                    @if (true)
                    {
                        <div>$0</div>
                    }
                </div>
                """);
    }

    [Fact]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2251322")]
    public void OnTypeCloseAngle_TagDoesAutoCloseInsideDifferentTag()
    {
        RunAutoInsertTest(
            input: """
                <div>
                    <blockquote>
                        @if (true)
                        {
                            <div>$$
                        }
                    </blockquote>
                </div>
                """,
            expected: """
                <div>
                    <blockquote>
                        @if (true)
                        {
                            <div>$0</div>
                        }
                    </blockquote>
                </div>
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/36125")]
    public void OnTypeCloseAngle_VoidTagHasEndTag_ShouldStillClose()
    {
        RunAutoInsertTest(
            input: """
                <input>$$<input></input></input>
                """,
            expected: """
                <input /><input></input></input>
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/36568")]
    public void OnTypeCloseAngle_VoidElementMirroringTagHelper()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                <Input>$$
                """,
            expected: """
                @addTagHelper *, TestAssembly

                <Input>$0</Input>
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_unspecifiedInputMirroringTagHelper]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/36568")]
    public void OnTypeCloseAngle_VoidHtmlElementCapitalized_SelfCloses()
    {
        RunAutoInsertTest(
            input: "<Input>$$",
            expected: "<Input />",
            fileKind: RazorFileKind.Legacy,
            tagHelpers: []);
    }

    [Fact]
    public void OnTypeCloseAngle_NormalOrSelfClosingStructureOverridesVoidTagBehavior()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                <input>$$
                """,
            expected: """
                @addTagHelper *, TestAssembly

                <input>$0</input>
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_normalOrSelfclosingInputTagHelper]);
    }

    [Fact]
    public void OnTypeCloseAngle_UnspeccifiedStructureInheritsVoidTagBehavior()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                <input>$$
                """,
            expected: """
                @addTagHelper *, TestAssembly

                <input />
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_unspecifiedInputTagHelper]);
    }

    [Fact]
    public void OnTypeCloseAngle_UnspeccifiedTagHelperTagStructure()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                <test>$$
                """,
            expected: """
                @addTagHelper *, TestAssembly

                <test>$0</test>
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_unspecifiedTagHelper]);
    }

    [Fact]
    public void OnTypeCloseAngle_NormalOrSelfClosingTagHelperTagStructure()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                <test>$$
                """,
            expected: """
                @addTagHelper *, TestAssembly

                <test>$0</test>
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_normalOrSelfClosingTagHelper]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/33930")]
    public void OnTypeCloseAngle_TagHelperInHtml_NestedStatement()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <div><test>$$</div>
                }
                """,
            expected: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <div><test>$0</test></div>
                }
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_normalOrSelfClosingTagHelper]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/5694")]
    public void OnTypeCloseAngle_HtmlTagInHtml_NestedStatement_WithAttribute()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <div><a target=""_blank"">$$</div>
                }
                """,
            expected: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <div><a target=""_blank"">$0</a></div>
                }
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_normalOrSelfClosingTagHelper]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/5694")]
    public void OnTypeCloseAngle_HtmlTagInHtml_NestedStatement_WithAttribute_SpaceBetweenClosingAngleAndAttributeClosingQuote()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <div><a target=""_blank"" >$$</div>
                }
                """,
            expected: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <div><a target=""_blank"" >$0</a></div>
                }
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_normalOrSelfClosingTagHelper]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/5694")]
    public void OnTypeCloseAngle_HtmlTagInHtml_NestedStatement_WithMinimalizedAttribute()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <div><form novalidate>$$</div>
                }
                """,
            expected: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <div><form novalidate>$0</form></div>
                }
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_normalOrSelfClosingTagHelper]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/5694")]
    public void OnTypeCloseAngle_HtmlTagInHtml_NestedStatement_WithMinimalizedAttribute_SpaceBetweenClosingAngleAndAttributeClosingQuote()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <div><form novalidate >$$</div>
                }
                """,
            expected: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <div><form novalidate >$0</form></div>
                }
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_normalOrSelfClosingTagHelper]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/5694")]
    public void OnTypeCloseAngle_TagHelperInHtml_NestedStatement_WithAttribute()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <div><test attribute=""value"">$$</div>
                }
                """,
            expected: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <div><test attribute=""value"">$0</test></div>
                }
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_normalOrSelfClosingTagHelper]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/5694")]
    public void OnTypeCloseAngle_TagHelperInHtml_NestedStatement_WithAttribute_SpaceBetweenClosingAngleAndAttributeClosingQuote()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <div><test attribute=""value"" >$$</div>
                }
                """,
            expected: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <div><test attribute=""value"" >$0</test></div>
                }
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_normalOrSelfClosingTagHelper]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/5694")]
    public void OnTypeCloseAngle_TagHelperInHtml_NestedStatement_WithMinimalizedAttribute()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <div><test bool-val>$$</div>
                }
                """,
            expected: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <div><test bool-val>$0</test></div>
                }
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_normalOrSelfClosingTagHelper]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/5694")]
    public void OnTypeCloseAngle_TagHelperInHtml_NestedStatement_WithMinimalizedAttribute_SpaceBetweenClosingAngleAndAttributeClosingQuote()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <div><test bool-val >$$</div>
                }
                """,
            expected: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <div><test bool-val >$0</test></div>
                }
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_normalOrSelfClosingTagHelper]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/33930")]
    public void OnTypeCloseAngle_TagHelperInTagHelper_NestedStatement()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <test><input>$$</test>
                }
                """,
            expected: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <test><input /></test>
                }
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_normalOrSelfClosingTagHelper, s_unspecifiedInputTagHelper]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/36906")]
    public void OnTypeCloseAngle_TagHelperNextToVoidTagHelper_NestedStatement()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <test>$$<input />
                }
                """,
            expected: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <test>$0</test><input />
                }
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_normalOrSelfClosingTagHelper, s_unspecifiedInputTagHelper]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/36906")]
    public void OnTypeCloseAngle_TagHelperNextToTagHelper_NestedStatement()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <test>$$<input></input>
                }
                """,
            expected: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <test>$0</test><input></input>
                }
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_normalOrSelfClosingTagHelper, s_normalOrSelfclosingInputTagHelper]);
    }

    [Fact]
    public void OnTypeCloseAngle_NormalOrSelfClosingTagHelperTagStructure_CodeBlock()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                @{
                    <test>$$
                }
                """,
            expected: """
                @addTagHelper *, TestAssembly

                @{
                    <test>$0</test>
                }
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_normalOrSelfClosingTagHelper]);
    }

    [Fact]
    public void OnTypeCloseAngle_WithSlash_WithoutEndTagTagHelperTagStructure()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                <test />$$
                """,
            expected: """
                @addTagHelper *, TestAssembly

                <test />
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_withoutEndTagTagHelper]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/33930")]
    public void OnTypeCloseAngle_NestedStatement()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <div><test />$$</div>
                }
                """,
            expected: """
                @addTagHelper *, TestAssembly

                @if (true)
                {
                <div><test /></div>
                }
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_withoutEndTagTagHelper]);
    }

    [Fact]
    public void OnTypeCloseAngle_WithSpace_WithoutEndTagTagHelperTagStructure()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                <test >$$
                """,
            expected: """
                @addTagHelper *, TestAssembly

                <test />
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_withoutEndTagTagHelper]);
    }

    [Fact]
    public void OnTypeCloseAngle_WithoutEndTagTagHelperTagStructure()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                <test>$$
                """,
            expected: """
                @addTagHelper *, TestAssembly

                <test />
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_withoutEndTagTagHelper]);
    }

    [Fact]
    public void OnTypeCloseAngle_WithoutEndTagTagHelperTagStructure_CodeBlock()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                @{
                    <test>$$
                }
                """,
            expected: """
                @addTagHelper *, TestAssembly

                @{
                    <test />
                }
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_withoutEndTagTagHelper]);
    }

    [Fact]
    public void OnTypeCloseAngle_MultipleApplicableTagHelperTagStructures()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                <test>$$
                """,
            expected: """
                @addTagHelper *, TestAssembly

                <test>$0</test>
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_unspecifiedTagHelper, s_normalOrSelfClosingTagHelper, s_withoutEndTagTagHelper]);
    }

    [Fact]
    public void OnTypeCloseAngle_EscapedTagTagHelperAutoCompletesWithEscape()
    {
        RunAutoInsertTest(
            input: """
                @addTagHelper *, TestAssembly

                <!test>$$
                """,
            expected: """
                @addTagHelper *, TestAssembly

                <!test>$0</!test>
                """,
            fileKind: RazorFileKind.Legacy,
            tagHelpers: [s_normalOrSelfClosingTagHelper]);
    }

    [Fact]
    public void OnTypeCloseAngle_AlwaysClosesStandardHTMLTag()
    {
        RunAutoInsertTest(
            input: """
                   <div><div>$$</div>
                   """,
            expected: """
                    <div><div>$0</div></div>
                    """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/33930")]
    public void OnTypeCloseAngle_ClosesStandardHTMLTag_NestedStatement()
    {
        RunAutoInsertTest(
            input: """
                @if (true)
                {
                    <div><p>$$</div>
                }
                """,
            expected: """
                @if (true)
                {
                    <div><p>$0</p></div>
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/36906")]
    public void OnTypeCloseAngle_TagNextToTag_NestedStatement()
    {
        RunAutoInsertTest(
            input: """
                @if (true)
                {
                    <p>$$<div></div>
                }
                """,
            expected: """
                @if (true)
                {
                    <p>$0</p><div></div>
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/36906")]
    public void OnTypeCloseAngle_TagNextToVoidTag_NestedStatement()
    {
        RunAutoInsertTest(
            input: """
                @if (true)
                {
                    <p>$$<input />
                }
                """,
            expected: """
                @if (true)
                {
                    <p>$0</p><input />
                }
                """);
    }

    [Fact]
    public void OnTypeCloseAngle_ClosesStandardHTMLTag()
    {
        RunAutoInsertTest(
            input: """
                    <div>$$
                    """,
            expected: """
                    <div>$0</div>
                    """);
    }

    [Fact]
    public void OnTypeCloseAngle_ClosesStandardHTMLTag_CodeBlock()
    {
        RunAutoInsertTest(
            input: """
                @{
                    <div>$$
                }
                """,
            expected: """
                @{
                    <div>$0</div>
                }
                """);
    }

    [Fact]
    public void OnTypeCloseAngle_ClosesVoidHTMLTag()
    {
        RunAutoInsertTest(
            input: """
                   <input>$$
                   """,
            expected: """
                    <input />
                    """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/33930")]
    public void OnTypeCloseAngle_ClosesVoidHTMLTag_NestedStatement()
    {
        RunAutoInsertTest(
            input: """
                @if (true)
                {
                    <strong><input>$$</strong>
                }
                """,
            expected: """
                @if (true)
                {
                    <strong><input /></strong>
                }
                """);
    }

    [Fact]
    public void OnTypeCloseAngle_ClosesVoidHTMLTag_CodeBlock()
    {
        RunAutoInsertTest(
            input: """
                @{
                    <input>$$
                }
                """,
            expected: """
                @{
                    <input />
                }
                """);
    }

    [Fact]
    public void OnTypeCloseAngle_WithSlash_ClosesVoidHTMLTag()
    {
        RunAutoInsertTest(
            input: """
                <input />$$
                """,
            expected: """
                <input />
                """);
    }

    [Fact]
    public void OnTypeCloseAngle_WithSpace_ClosesVoidHTMLTag()
    {
        RunAutoInsertTest(
            input: """
                <input >$$
                """,
            expected: """
                <input />
                """);
    }

    [Fact]
    public void OnTypeCloseAngle_AutoInsertDisabled_Noops()
    {
        RunAutoInsertTest(
            input: """
                <div>$$
                """,
            expected: """
                <div>
                """,
            enableAutoClosingTags: false);
    }
}
