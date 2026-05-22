// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class RemoveUnnecessaryDirectiveTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task RemoveUnusedUsingDirective()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]System.Text

                <div>
                    Hello World
                </div>
                """,
            expected: """

                <div>
                    Hello World
                </div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task NotOfferedWithoutRZ0005()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]System.Text

                <div>
                    Hello World
                </div>

                @{ var x = new StringBuilder(); }

                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task NotOfferedWhenCursorIsNotOnDirective()
    {
        await VerifyCodeActionAsync(
            input: """
                @using System.Text

                <div>
                    Hello [||]World
                </div>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task NotOfferedWhenCursorIsInCodeBlock()
    {
        await VerifyCodeActionAsync(
            input: """
                @using System.Text

                <div>
                    Hello World
                </div>

                @code {
                    private [||]int _x;
                }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task MultipleUnusedDirectives_RemovesAll()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]System.Text
                @using System.Buffers

                <div>
                    Hello World
                </div>
                """,
            expected: """

                <div>
                    Hello World
                </div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task MultipleDirectives_OnlyUnusedRemoved()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]System.Text
                @using System.Buffers
                @using System

                <div>
                    Hello World
                </div>

                @{ var x = Console.WriteLine(""); }

                """,
            expected: """
                @using System

                <div>
                    Hello World
                </div>

                @{ var x = Console.WriteLine(""); }

                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task CursorOnUsedDirective_StillOfferedWhenUnusedExist()
    {
        await VerifyCodeActionAsync(
            input: """
                @using System.Text
                @using [||]System

                <div>
                    Hello World
                </div>

                @{ var x = Console.WriteLine(""); }

                """,
            expected: """
                @using System

                <div>
                    Hello World
                </div>

                @{ var x = Console.WriteLine(""); }

                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task SelectionSpanningDirective()
    {
        await VerifyCodeActionAsync(
            input: """
                [|@using System.Text|]

                <div>
                    Hello World
                </div>
                """,
            expected: """

                <div>
                    Hello World
                </div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task SelectionStartOnDirective_EndOutside()
    {
        await VerifyCodeActionAsync(
            input: """
                [|@using System.Text

                <div>
                    Hello|] World
                </div>
                """,
            expected: """

                <div>
                    Hello World
                </div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task SelectionEndOnDirective_StartOutside()
    {
        await VerifyCodeActionAsync(
            input: """
                @using System.Text
                @using System.Buffers

                <div>
                    [|Hello World
                </div>

                @using Sy|]stem.IO
                """,
            expected: """

                <div>
                    Hello World
                </div>


                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task CursorBeforeAtSign()
    {
        await VerifyCodeActionAsync(
            input: """
                [||]@using System.Text

                <div>
                    Hello World
                </div>
                """,
            expected: """

                <div>
                    Hello World
                </div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task CursorAfterAtSign()
    {
        await VerifyCodeActionAsync(
            input: """
                @[||]using System.Text

                <div>
                    Hello World
                </div>
                """,
            expected: """

                <div>
                    Hello World
                </div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task CursorAtEndOfDirectiveLine()
    {
        await VerifyCodeActionAsync(
            input: """
                @using System.Text[||]

                <div>
                    Hello World
                </div>
                """,
            expected: """

                <div>
                    Hello World
                </div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task SelectionAcrossTwoUsings()
    {
        await VerifyCodeActionAsync(
            input: """
                [|@using System.Text
                @using System.Buffers|]

                <div>
                    Hello World
                </div>
                """,
            expected: """

                <div>
                    Hello World
                </div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task UnusedDirectiveWithSemicolon()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]System.Text;

                <div>
                    Hello World
                </div>
                """,
            expected: """

                <div>
                    Hello World
                </div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task UnusedDirectiveWithExtraContent()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]System.Text;hello there

                <div>
                    Hello World
                </div>
                """,
            expected: """
                hello there

                <div>
                    Hello World
                </div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task MultipleUnusedDirectives_OneWithExtraContent()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]System.Text;extra stuff
                @using System.Buffers

                <div>
                    Hello World
                </div>
                """,
            expected: """
                extra stuff

                <div>
                    Hello World
                </div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task UnusedDirective_MultipleUsings1()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]System.Text;hello there @using System

                @{ Console.WriteLine("hi"); }
                """,
            expected: """
                hello there @using System

                @{ Console.WriteLine("hi"); }
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task UnusedDirective_MultipleUsings2()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]System;hello there @using System.Text

                @{ Console.WriteLine("hi"); }
                """,
            expected: """
                @using System;hello there 

                @{ Console.WriteLine("hi"); }
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task UnusedDirective_MultipleUsings3()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]System @using System.Text

                @{ Console.WriteLine("hi"); }
                """,
            expected: """
                @using System 

                @{ Console.WriteLine("hi"); }
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task Legacy_UnusedAddTagHelper()
    {
        await VerifyCodeActionAsync(
            input: """
                [||]@addTagHelper *, SomeProject

                <div></div>
                """,
            expected: """

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles:
            [
                (FilePath("AboutBoxTagHelper.cs"), """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("dw:about-box")]
                    public class AboutBoxTagHelper : TagHelper
                    {
                    }
                    """)
            ],
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task Legacy_UnusedAddTagHelper_CursorInMiddle()
    {
        await VerifyCodeActionAsync(
            input: """
                @addTagHelper [||]*, SomeProject

                <div></div>
                """,
            expected: """

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles:
            [
                (FilePath("AboutBoxTagHelper.cs"), """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("dw:about-box")]
                    public class AboutBoxTagHelper : TagHelper
                    {
                    }
                    """)
            ],
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12846")]
    public async Task Legacy_UnusedAddTagHelper_CursorInAssemblyName()
    {
        await VerifyCodeActionAsync(
            input: """
                @addTagHelper *, SomePr[||]oject

                <div></div>
                """,
            expected: """

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles:
            [
                (FilePath("AboutBoxTagHelper.cs"), """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("dw:about-box")]
                    public class AboutBoxTagHelper : TagHelper
                    {
                    }
                    """)
            ],
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task Legacy_UsedAddTagHelper_NotOffered()
    {
        await VerifyCodeActionAsync(
            input: """
                @addTagHelper [||]*, SomeProject

                <dw:about-box />
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles:
            [
                (FilePath("AboutBoxTagHelper.cs"), """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("dw:about-box")]
                    public class AboutBoxTagHelper : TagHelper
                    {
                    }
                    """)
            ],
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task Legacy_MixedDirectives_OnlyUnusedRemoved()
    {
        await VerifyCodeActionAsync(
            input: """
                @addTagHelper AboutBoxTagHelper, SomeProject
                @addTagHelper [||]FancyBoxTagHelper, SomeProject

                <dw:about-box />
                """,
            expected: """
                @addTagHelper AboutBoxTagHelper, SomeProject

                <dw:about-box />
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles:
            [
                (FilePath("AboutBoxTagHelper.cs"), """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("dw:about-box")]
                    public class AboutBoxTagHelper : TagHelper
                    {
                    }
                    """),
                (FilePath("FancyBoxTagHelper.cs"), """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("dw:fancy-box")]
                    public class FancyBoxTagHelper : TagHelper
                    {
                    }
                    """)
            ],
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task Legacy_CursorOnUsedDirective_StillOfferedWhenUnusedExist()
    {
        await VerifyCodeActionAsync(
            input: """
                @addTagHelper [||]AboutBoxTagHelper, SomeProject
                @addTagHelper FancyBoxTagHelper, SomeProject

                <dw:about-box />
                """,
            expected: """
                @addTagHelper AboutBoxTagHelper, SomeProject

                <dw:about-box />
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles:
            [
                (FilePath("AboutBoxTagHelper.cs"), """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("dw:about-box")]
                    public class AboutBoxTagHelper : TagHelper
                    {
                    }
                    """),
                (FilePath("FancyBoxTagHelper.cs"), """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("dw:fancy-box")]
                    public class FancyBoxTagHelper : TagHelper
                    {
                    }
                    """)
            ],
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task Legacy_UnusedAddTagHelperAndUsing_RemovesBoth()
    {
        await VerifyCodeActionAsync(
            input: """
                @addTagHelper [||]*, SomeProject
                @using System.Text

                <div></div>
                """,
            expected: """

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles:
            [
                (FilePath("AboutBoxTagHelper.cs"), """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("dw:about-box")]
                    public class AboutBoxTagHelper : TagHelper
                    {
                    }
                    """)
            ],
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task Legacy_MixedUsedAndUnused_OnlyUnusedRemoved()
    {
        await VerifyCodeActionAsync(
            input: """
                @addTagHelper AboutBoxTagHelper, SomeProject
                @addTagHelper [||]FancyBoxTagHelper, SomeProject
                @using System.Text
                @using System

                <dw:about-box />
                @{ var x = Console.WriteLine(""); }
                """,
            expected: """
                @addTagHelper AboutBoxTagHelper, SomeProject
                @using System

                <dw:about-box />
                @{ var x = Console.WriteLine(""); }
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles:
            [
                (FilePath("AboutBoxTagHelper.cs"), """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("dw:about-box")]
                    public class AboutBoxTagHelper : TagHelper
                    {
                    }
                    """),
                (FilePath("FancyBoxTagHelper.cs"), """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("dw:fancy-box")]
                    public class FancyBoxTagHelper : TagHelper
                    {
                    }
                    """)
            ],
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task Legacy_SelectionAcrossAddTagHelperDirectives()
    {
        await VerifyCodeActionAsync(
            input: """
                [|@addTagHelper *, SomeProject
                @using System.Text|]

                <div></div>
                """,
            expected: """

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles:
            [
                (FilePath("AboutBoxTagHelper.cs"), """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("dw:about-box")]
                    public class AboutBoxTagHelper : TagHelper
                    {
                    }
                    """)
            ],
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task Legacy_NotOfferedWhenCursorNotOnDirective()
    {
        await VerifyCodeActionAsync(
            input: """
                @addTagHelper *, SomeProject
                @using System.Text

                <div>[||]</div>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles:
            [
                (FilePath("AboutBoxTagHelper.cs"), """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("dw:about-box")]
                    public class AboutBoxTagHelper : TagHelper
                    {
                    }
                    """)
            ],
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }
}
