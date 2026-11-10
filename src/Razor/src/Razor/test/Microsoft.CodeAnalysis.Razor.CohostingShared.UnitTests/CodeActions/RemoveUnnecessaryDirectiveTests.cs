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
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public async Task NamespaceAlias_OnlyOrdinaryImportRemoved()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]Library
                @using UI = global::Library
                <UI.Button />
                """,
            expected: """
                @using UI = global::Library
                <UI.Button />
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles: [
                (FilePath("Button.cs"), """
                    namespace Library;
                    public class Button : Microsoft.AspNetCore.Components.ComponentBase { }
                    """)
            ],
            makeDiagnosticsRequest: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public async Task SameNameTypeAlias_PreferredOverOrdinaryImport()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]Library
                @using Button = global::Library.Button
                <Button />
                """,
            expected: """
                @using Button = global::Library.Button
                <Button />
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles: [
                (FilePath("Button.cs"), """
                    namespace Library;
                    public class Button : Microsoft.AspNetCore.Components.ComponentBase { }
                    """)
            ],
            makeDiagnosticsRequest: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public async Task NamespaceAlias_OrdinaryAndAliasedTagsKeepBothDirectives()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]Library
                @using UI = global::Library
                <UI.Button />
                <Button />
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles: [
                (FilePath("Button.cs"), """
                    namespace Library;
                    public class Button : Microsoft.AspNetCore.Components.ComponentBase { }
                    """)
            ],
            makeDiagnosticsRequest: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public async Task NamespaceAlias_UnusedAliasRemovedDespiteOrdinaryTag()
    {
        await VerifyCodeActionAsync(
            input: """
                @using Library
                @using [||]UI = global::Library
                <Button />
                """,
            expected: """
                @using Library
                <Button />
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles: [
                (FilePath("Button.cs"), """
                    namespace Library;
                    public class Button : Microsoft.AspNetCore.Components.ComponentBase { }
                    """)
            ],
            makeDiagnosticsRequest: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public async Task NamespaceAlias_ChildContentDoesNotKeepOrdinaryImport()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]Library
                @using UI = global::Library
                <UI.Button><ChildContent>Hello</ChildContent></UI.Button>
                """,
            expected: """
                @using UI = global::Library
                <UI.Button><ChildContent>Hello</ChildContent></UI.Button>
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles: [
                (FilePath("Button.cs"), """
                    namespace Library;
                    public class Button : Microsoft.AspNetCore.Components.ComponentBase
                    {
                        [Microsoft.AspNetCore.Components.Parameter]
                        public Microsoft.AspNetCore.Components.RenderFragment ChildContent { get; set; }
                    }
                    """)
            ],
            makeDiagnosticsRequest: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public async Task TypeAlias_MatchingChildContentNameDoesNotKeepUnusedAlias()
    {
        await VerifyCodeActionAsync(
            input: """
                @using Library
                @using [||]Header = global::Library.Modal
                <Modal><Header>Title</Header></Modal>
                """,
            expected: """
                @using Library
                <Modal><Header>Title</Header></Modal>
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles: [
                (FilePath("Modal.cs"), """
                    namespace Library;
                    public class Modal : Microsoft.AspNetCore.Components.ComponentBase
                    {
                        [Microsoft.AspNetCore.Components.Parameter]
                        public Microsoft.AspNetCore.Components.RenderFragment Header { get; set; }
                    }
                    """)
            ],
            makeDiagnosticsRequest: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public async Task TypeAlias_ChildContentDoesNotKeepOrdinaryImport()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]Library
                @using Button = global::Library.Button
                <Button><ChildContent>Hello</ChildContent></Button>
                """,
            expected: """
                @using Button = global::Library.Button
                <Button><ChildContent>Hello</ChildContent></Button>
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles: [
                (FilePath("Button.cs"), """
                    namespace Library;
                    public class Button : Microsoft.AspNetCore.Components.ComponentBase
                    {
                        [Microsoft.AspNetCore.Components.Parameter]
                        public Microsoft.AspNetCore.Components.RenderFragment ChildContent { get; set; }
                    }
                    """)
            ],
            makeDiagnosticsRequest: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public async Task NamespaceAlias_EventHandlerImportKept()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]CustomEvents
                @using UI = global::Library
                <UI.Button @oncustom="() => { }" />
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles: [
                (FilePath("Button.cs"), """
                    namespace Library;
                    public class Button : Microsoft.AspNetCore.Components.ComponentBase { }
                    """),
                (FilePath("EventHandlers.cs"), """
                    namespace CustomEvents;
                    [Microsoft.AspNetCore.Components.EventHandler("oncustom", typeof(System.EventArgs), true, true)]
                    public static class EventHandlers { }
                    """)
            ],
            makeDiagnosticsRequest: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public async Task NamespaceAlias_OrdinaryImportUsedInCSharpKept()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]Library
                @using UI = global::Library
                <UI.Button />
                @code { private Button CreateButton() => new Button(); }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles: [
                (FilePath("Button.cs"), """
                    namespace Library;
                    public class Button : Microsoft.AspNetCore.Components.ComponentBase { }
                    """)
            ],
            makeDiagnosticsRequest: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public async Task NamespaceAlias_UsedInCSharpKeptWithoutAliasedTag()
    {
        await VerifyCodeActionAsync(
            input: """
                @using Library
                @using [||]UI = global::Library
                <Button />
                @code { private UI.Button CreateButton() => new UI.Button(); }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles: [
                (FilePath("Button.cs"), """
                    namespace Library;
                    public class Button : Microsoft.AspNetCore.Components.ComponentBase { }
                    """)
            ],
            makeDiagnosticsRequest: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public async Task NamespaceAlias_FromImportsAllowsOrdinaryImportRemoval()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]Library
                <UI.Button />
                """,
            expected: """
                <UI.Button />
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles: [
                (FilePath("_Imports.razor"), "@using UI = global::Library"),
                (FilePath("Button.cs"), """
                    namespace Library;
                    public class Button : Microsoft.AspNetCore.Components.ComponentBase { }
                    """)
            ],
            addDefaultImports: false,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public async Task TypeAlias_OtherComponentKeepsOrdinaryImport()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]Library
                @using Button = global::Library.Button
                <Button />
                <OtherComponent />
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles: [
                (FilePath("Components.cs"), """
                    namespace Library;
                    public class Button : Microsoft.AspNetCore.Components.ComponentBase { }
                    public class OtherComponent : Microsoft.AspNetCore.Components.ComponentBase { }
                    """)
            ],
            makeDiagnosticsRequest: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public async Task TypeAlias_ComponentBindDoesNotKeepOrdinaryImport()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]Library
                @using Button = global::Library.Button
                <Button @bind-Value="value" />
                @code { private string value = ""; }
                """,
            expected: """
                @using Button = global::Library.Button
                <Button @bind-Value="value" />
                @code { private string value = ""; }
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles: [
                (FilePath("Button.cs"), """
                    namespace Library;
                    public class Button : Microsoft.AspNetCore.Components.ComponentBase
                    {
                        [Microsoft.AspNetCore.Components.Parameter]
                        public string Value { get; set; }
                        [Microsoft.AspNetCore.Components.Parameter]
                        public Microsoft.AspNetCore.Components.EventCallback<string> ValueChanged { get; set; }
                    }
                    """)
            ],
            makeDiagnosticsRequest: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public async Task NamespaceAlias_EventHandlerInComponentNamespaceKeepsOrdinaryImport()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]Library
                @using UI = global::Library
                <UI.Button @oncustom="() => { }" />
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles: [
                (FilePath("Components.cs"), """
                    namespace Library;
                    public class Button : Microsoft.AspNetCore.Components.ComponentBase { }
                    [Microsoft.AspNetCore.Components.EventHandler("oncustom", typeof(System.EventArgs), true, true)]
                    public static class EventHandlers { }
                    """)
            ],
            makeDiagnosticsRequest: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public async Task NamespaceAlias_NestedNamespaceDoesNotKeepOrdinaryImport()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]Library.Controls
                @using UI = global::Library
                <UI.Controls.Button />
                """,
            expected: """
                @using UI = global::Library
                <UI.Controls.Button />
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles: [
                (FilePath("Button.cs"), """
                    namespace Library.Controls;
                    public class Button : Microsoft.AspNetCore.Components.ComponentBase { }
                    """)
            ],
            makeDiagnosticsRequest: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public async Task EscapedNamespaceAlias_OnlyOrdinaryImportRemoved()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]Library
                @using @UI = global::Library
                <UI.Button />
                """,
            expected: """
                @using @UI = global::Library
                <UI.Button />
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles: [
                (FilePath("Button.cs"), """
                    namespace Library;
                    public class Button : Microsoft.AspNetCore.Components.ComponentBase { }
                    """)
            ],
            makeDiagnosticsRequest: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public async Task NamespaceAlias_UnusedAliasAndOrdinaryImportRemoved()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]Library
                @using UI = global::Library
                <div>Hello</div>
                """,
            expected: """
                <div>Hello</div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles: [
                (FilePath("Button.cs"), """
                    namespace Library;
                    public class Button : Microsoft.AspNetCore.Components.ComponentBase { }
                    """)
            ],
            makeDiagnosticsRequest: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public async Task NamespaceAlias_OrdinaryChildContentKeepsOrdinaryImport()
    {
        await VerifyCodeActionAsync(
            input: """
                @using Library
                @using [||]UI = global::Library
                <Button><ChildContent>Hello</ChildContent></Button>
                """,
            expected: """
                @using Library
                <Button><ChildContent>Hello</ChildContent></Button>
                """,
            codeActionName: LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            additionalFiles: [
                (FilePath("Button.cs"), """
                    namespace Library;
                    public class Button : Microsoft.AspNetCore.Components.ComponentBase
                    {
                        [Microsoft.AspNetCore.Components.Parameter]
                        public Microsoft.AspNetCore.Components.RenderFragment ChildContent { get; set; }
                    }
                    """)
            ],
            makeDiagnosticsRequest: true);
    }

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
