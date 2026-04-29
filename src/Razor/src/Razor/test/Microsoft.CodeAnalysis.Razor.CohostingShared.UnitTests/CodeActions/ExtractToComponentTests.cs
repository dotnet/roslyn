// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class ExtractToComponentTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task NotOfferedInLegacy()
    {
        await VerifyCodeActionAsync(
            input: """
                @co[||]de
                {
                    private int x = 1;
                }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToCodeBehind,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task ExtractToComponent()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                [|<div>
                    Hello World
                </div>|]

                <div></div>
                """,
            expected: """
                <div></div>

                <Component />

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                    <div>
                        Hello World
                    </div>
                    """)]);
    }

    [Fact]
    public async Task ExtractToComponent_SinglePointSelection()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                $$<div>
                    Hello World
                </div>

                <div></div>
                """,
            expected: """
                <div></div>

                <Component />

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                    <div>
                        Hello World
                    </div>
                    """)]);
    }

    [Fact]
    public async Task DontOfferOnSinglePointSelectionInElement()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <div>
                    Hello $$World
                </div>

                <div></div>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent);
    }

    [Fact]
    public async Task DontOfferInCodeBlock()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                @code
                {
                    [|public int I { get; set; }
                    public void M()
                    {
                    }|]
                }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent);
    }

    [Fact]
    public async Task DontOfferOnNonExistentComponent()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <div>
                    Hello World
                </div>

                <{|RZ10012:Not$$AComponent|} />

                <div></div>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent);
    }

    [Fact]
    public async Task ExtractNamespace()
    {
        await VerifyCodeActionAsync(
            input: """
                @namespace ILoveYou

                <div></div>

                [|<div>
                    Hello World
                </div>|]

                <div></div>
                """,
            expected: """
                @namespace ILoveYou

                <div></div>

                <Component />

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                    @namespace ILoveYou

                    <div>
                        Hello World
                    </div>
                    """)]);
    }

    [Fact]
    public async Task ExtractNamespace_Pathological()
    {
        await VerifyCodeActionAsync(
            input: """
                @namespace DidYouEverKnow
                @namespace ThatYoure
                @namespace MyHero

                <div></div>

                [|<div>
                    Hello World
                </div>|]

                <div></div>
                """,
            expected: """
                @namespace DidYouEverKnow
                @namespace ThatYoure
                @namespace MyHero

                <div></div>

                <Component />

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                    @namespace MyHero

                    <div>
                        Hello World
                    </div>
                    """)]);
    }

    [Fact]
    public async Task SelectionEndAfterElement()
    {
        await VerifyCodeActionAsync(
            input: """
                @namespace MarketApp.Pages.Product.Home
                
                <PageTitle>Home</PageTitle>
                
                <div id="parent">
                    [|<div>
                        <h1>Div a title</h1>
                        <p>Div a par</p>
                    </div>
                    <div>
                        <h1>Div b title</h1>
                        <p>Div b par</p>
                    </div>|]
                </div>
                
                <h1>Hello, world!</h1>
                
                Welcome to your new app.
                """,
            expected: """
                @namespace MarketApp.Pages.Product.Home
                
                <PageTitle>Home</PageTitle>
                
                <div id="parent">
                    <Component />
                </div>
                
                <h1>Hello, world!</h1>
                
                Welcome to your new app.
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                    @namespace MarketApp.Pages.Product.Home

                    <div>
                        <h1>Div a title</h1>
                        <p>Div a par</p>
                    </div>
                    <div>
                        <h1>Div b title</h1>
                        <p>Div b par</p>
                    </div>
                    """)]);
    }

    [Fact]
    public async Task SelectionEndInsideSiblingElement1()
    {
        await VerifyCodeActionAsync(
            input: """
                @namespace MarketApp.Pages.Product.Home
                
                <PageTitle>Home</PageTitle>
                
                <div id="parent">
                    [|<div>
                        <h1>Div a title</h1>
                        <p>Div a par</p>
                    </div>
                    <div>
                        <h1>Div b title</h1>|]
                        <p>Div b par</p>
                    </div>
                </div>
                
                <h1>Hello, world!</h1>
                
                Welcome to your new app.
                """,
            expected: """
                @namespace MarketApp.Pages.Product.Home
                
                <PageTitle>Home</PageTitle>
                
                <div id="parent">
                    <Component />
                </div>
                
                <h1>Hello, world!</h1>
                
                Welcome to your new app.
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                    @namespace MarketApp.Pages.Product.Home

                    <div>
                        <h1>Div a title</h1>
                        <p>Div a par</p>
                    </div>
                    <div>
                        <h1>Div b title</h1>
                        <p>Div b par</p>
                    </div>
                    """)]);
    }

    [Fact]
    public async Task SelectionEndInsideSiblingElement2()
    {
        await VerifyCodeActionAsync(
            input: """
                @namespace MarketApp.Pages.Product.Home
                
                <PageTitle>Home</PageTitle>
                
                <div id="parent">
                    [|<div>
                        <h1>Div a title</h1>
                        <p>Div a par</p>
                    </div>
                    <div>
                        <h1>Div b title|]</h1>
                        <p>Div b par</p>
                    </div>
                </div>
                
                <h1>Hello, world!</h1>
                
                Welcome to your new app.
                """,
            expected: """
                @namespace MarketApp.Pages.Product.Home
                
                <PageTitle>Home</PageTitle>
                
                <div id="parent">
                    <Component />
                </div>
                
                <h1>Hello, world!</h1>
                
                Welcome to your new app.
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                    @namespace MarketApp.Pages.Product.Home

                    <div>
                        <h1>Div a title</h1>
                        <p>Div a par</p>
                    </div>
                    <div>
                        <h1>Div b title</h1>
                        <p>Div b par</p>
                    </div>
                    """)]);
    }

    [Fact]
    public async Task SelectionStartInsideSiblingElement()
    {
        await VerifyCodeActionAsync(
            input: """
                @namespace MarketApp.Pages.Product.Home
                
                <PageTitle>Home</PageTitle>
                
                <div id="parent">
                    <div>
                        <h1>[|Div a title</h1>
                        <p>Div a par</p>
                    </div>
                    <div>
                        <h1>Div b title</h1>
                        <p>Div b par</p>
                    </div>|]
                </div>
                
                <h1>Hello, world!</h1>
                
                Welcome to your new app.
                """,
            expected: """
                @namespace MarketApp.Pages.Product.Home
                
                <PageTitle>Home</PageTitle>
                
                <div id="parent">
                    <Component />
                </div>
                
                <h1>Hello, world!</h1>
                
                Welcome to your new app.
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                    @namespace MarketApp.Pages.Product.Home

                    <div>
                        <h1>Div a title</h1>
                        <p>Div a par</p>
                    </div>
                    <div>
                        <h1>Div b title</h1>
                        <p>Div b par</p>
                    </div>
                    """)]);
    }

    [Fact]
    public async Task SelectionStartAndEndInsideSiblingElement()
    {
        await VerifyCodeActionAsync(
            input: """
                @namespace MarketApp.Pages.Product.Home
                
                <PageTitle>Home</PageTitle>
                
                <div id="parent">
                    <div>
                        <h1>[|Div a title</h1>
                        <p>Div a par</p>
                    </div>
                    <div>
                        <h1>Div b title</h1>
                        <p>Div b par|]</p>
                    </div>
                </div>
                
                <h1>Hello, world!</h1>
                
                Welcome to your new app.
                """,
            expected: """
                @namespace MarketApp.Pages.Product.Home
                
                <PageTitle>Home</PageTitle>
                
                <div id="parent">
                    <Component />
                </div>
                
                <h1>Hello, world!</h1>
                
                Welcome to your new app.
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                    @namespace MarketApp.Pages.Product.Home

                    <div>
                        <h1>Div a title</h1>
                        <p>Div a par</p>
                    </div>
                    <div>
                        <h1>Div b title</h1>
                        <p>Div b par</p>
                    </div>
                    """)]);
    }

    [Fact]
    public async Task SelectionEndInsideElement()
    {
        await VerifyCodeActionAsync(
            input: """
                @namespace MarketApp.Pages.Product.Home
                
                <PageTitle>Home</PageTitle>
                
                <div id="parent">
                    [|<div>
                        <h1>Div a title</h1>
                        <p>Div a par</p>|]
                    </div>
                    <div>
                        <h1>Div b title</h1>
                        <p>Div b par</p>
                    </div>
                </div>
                
                <h1>Hello, world!</h1>
                
                Welcome to your new app.
                """,
            expected: """
                @namespace MarketApp.Pages.Product.Home
                
                <PageTitle>Home</PageTitle>
                
                <div id="parent">
                    <Component />
                    <div>
                        <h1>Div b title</h1>
                        <p>Div b par</p>
                    </div>
                </div>
                
                <h1>Hello, world!</h1>
                
                Welcome to your new app.
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                    @namespace MarketApp.Pages.Product.Home

                    <div>
                        <h1>Div a title</h1>
                        <p>Div a par</p>
                    </div>
                    """)]);
    }

    [Fact]
    public async Task SelectionStartWithSelfClosing()
    {
        await VerifyCodeActionAsync(
            input: """
                @namespace MarketApp.Pages.Product.Home
                
                <PageTitle>Home</PageTitle>
                
                <div id="parent">
                    [|<img src="/myimg.png" />
                    <div>
                        <h1>Div a title</h1>
                        <p>Div a par</p>
                    </div>|]
                    <div>
                        <h1>Div b title</h1>
                        <p>Div b par</p>
                    </div>
                </div>
                
                <h1>Hello, world!</h1>
                
                Welcome to your new app.
                """,
            expected: """
                @namespace MarketApp.Pages.Product.Home
                
                <PageTitle>Home</PageTitle>
                
                <div id="parent">
                    <Component />
                    <div>
                        <h1>Div b title</h1>
                        <p>Div b par</p>
                    </div>
                </div>
                
                <h1>Hello, world!</h1>
                
                Welcome to your new app.
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                    @namespace MarketApp.Pages.Product.Home

                    <img src="/myimg.png" />
                    <div>
                        <h1>Div a title</h1>
                        <p>Div a par</p>
                    </div>
                    """)]);
    }

    [Fact]
    public async Task IncludeCodeBlock()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                [|<div>
                    Hello World
                </div>

                @code {
                |]
                }
                """,
            expected: """
                <div></div>

                <Component />
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                    <div>
                        Hello World
                    </div>

                    @code {

                    }
                    """)]);
    }

    [Fact]
    public async Task IncludeIfBlock()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                [|<div>
                    Hello World
                </div>

                @if (true)
                {
                |]
                }
                """,
            expected: """
                <div></div>

                <Component />
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                    <div>
                        Hello World
                    </div>

                    @if (true)
                    {

                    }
                    """)]);
    }

    [Fact]
    public async Task IncludeNestedIfBlock()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                [|<div>
                    Hello World
                </div>

                <div>
                    <div>
                        @if (true) {
                          |]
                        }
                    </div>
                </div>
                """,
            expected: """
                <div></div>

                <Component />
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                    <div>
                        Hello World
                    </div>

                    <div>
                        <div>
                            @if (true) {
                              
                            }
                        </div>
                    </div>
                    """)]);
    }

    [Fact]
    public async Task ExplicitStatement()
    {
        await VerifyCodeActionAsync(
            input: """
            <div></div>

            [|<div>
                Hello World
            </div>

            @{
                RenderFragment fragment = @<Component1 Id="Comp1" Caption="Title">|] </Component1>;
            }
            """,
            expected: """
            <div></div>

            <Component />
            """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                <div>
                    Hello World
                </div>

                @{
                    RenderFragment fragment = @<Component1 Id="Comp1" Caption="Title"> </Component1>;
                }
                """)]);
    }

    [Fact]
    public async Task SingleLineElement()
    {
        await VerifyCodeActionAsync(
            input: """
              <div></div>

              [|<div>Hello World</div>|]

              <div></div>
              """,
            expected: """
              <div></div>

              <Component />

              <div></div>
              """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                  <div>Hello World</div>
                  """)]);
    }

    [Fact]
    public async Task TextOnly()
    {
        await VerifyCodeActionAsync(
            input: """
              <div></div>

              [|Hello World|]

              <div></div>
              """,
            expected: """
              <div></div>

              <Component />

              <div></div>
              """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                  Hello World
                  """)]);
    }

    [Fact]
    public async Task PartialTextOnly()
    {
        await VerifyCodeActionAsync(
            input: """
              <div></div>

              Hello [|World|]

              <div></div>
              """,
            expected: """
              <div></div>

              Hello <Component />

              <div></div>
              """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                  World
                  """)]);
    }

    [Fact]
    public async Task SelectionStartInText()
    {
        await VerifyCodeActionAsync(
            input: """
              <div></div>

              Hello [|World

              <div>
                  Hello|] World
              </div>
              """,
            expected: """
              <div></div>

              Hello <Component />
              """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                  World

                  <div>
                      Hello World
                  </div>
                  """)]);
    }

    [Fact]
    public async Task SelectionEndInText()
    {
        await VerifyCodeActionAsync(
            input: """
              <div></div>

              <div>
                  Hello [|World
              </div>

              Hello |]World
              """,
            expected: """
              <div></div>

              <Component />World
              """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                  <div>
                      Hello World
                  </div>
                  
                  Hello 
                  """)]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/11261")]
    public async Task InsideElseBlock1()
    {
        await VerifyCodeActionAsync(
            input: """
              <div></div>

              @if (true)
              {
                  <div>
                      Hello World
                  </div>
              }
              else
              {
                  [|<div>
                      Hello World
                  </div>|]
              }
              """,
            expected: """
              <div></div>

              @if (true)
              {
                  <div>
                      Hello World
                  </div>
              }
              else
              {
                  <Component />
              }
              """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                  <div>
                      Hello World
                  </div>
                  """)]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/11261")]
    public async Task InsideElseBlock2()
    {
        await VerifyCodeActionAsync(
            input: """
              <div></div>

              @if (true)
              {
                  <div>
                      Hello World
                  </div>
              }
              else
              {
              $$    <div>
                      Hello World
                  </div>
              }
              """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent);
    }

    [Fact]
    public async Task ExtractToComponent_WithUsings()
    {
        await VerifyCodeActionAsync(
            input: """
                @using MyApp.Data
                @using MyApp.Models
                
                <[|div id="parent">
                    <div>
                        <div>
                            <div>
                                <p>Deeply nested par</p|]>
                            </div>
                        </div>
                    </div>
                </div>
                """,
            expected: """
                @using MyApp.Data
                @using MyApp.Models
                
                <Component />
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                    @using MyApp.Data
                    @using MyApp.Models
                    
                    <div id="parent">
                        <div>
                            <div>
                                <div>
                                    <p>Deeply nested par</p>
                                </div>
                            </div>
                        </div>
                    </div>
                    """)]);
    }

    [Fact]
    public async Task SelectStartWithinElement()
    {
        await VerifyCodeActionAsync(
            input: """
                <[|div id="parent">
                    <div>
                        <div>
                            <div>
                                <p>Deeply nested par</p|]>
                            </div>
                        </div>
                    </div>
                </div>
                """,
            expected: """
                <Component />
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToNewComponent,
            additionalExpectedFiles: [
                (FileUri("Component.razor"), """
                    <div id="parent">
                        <div>
                            <div>
                                <div>
                                    <p>Deeply nested par</p>
                                </div>
                            </div>
                        </div>
                    </div>
                    """)]);
    }
}
