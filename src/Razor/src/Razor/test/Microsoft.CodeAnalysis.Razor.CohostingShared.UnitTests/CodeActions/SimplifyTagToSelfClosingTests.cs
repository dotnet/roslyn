// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class SimplifyTagToSelfClosingTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task NoRenderFragment()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Compo[||]nent>
                    
                </Component>

                <div></div>
                """,
            expected: """
                <div></div>

                <Component />

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyTagToSelfClosing,
            additionalFiles: [
                (FilePath("Component.razor"), """
                    <div>
                        Hello World
                    </div>
                    """)]);
    }

    [Fact]
    public async Task HasAttributesButNoRenderFragment()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Compo[||]nent Attribute="Value">
                    
                </Component>

                <div></div>
                """,
            expected: """
                <div></div>

                <Component Attribute="Value" />

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyTagToSelfClosing,
            additionalFiles: [
                (FilePath("Component.razor"), """
                    <div>
                        Hello World
                    </div>
                    
                    @code {
                        [Parameter]
                        public string Attribute { get; set; } = null!;
                    }
                    """)]);
    }

    [Fact]
    public async Task HasRenderFragmentButNotEditorRequired()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Compo[||]nent>
                    
                </Component>

                <div></div>
                """,
            expected: """
                <div></div>

                <Component />

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyTagToSelfClosing,
            additionalFiles: [
                (FilePath("Component.razor"), """
                    <div>
                        Hello World
                    </div>

                    @code {
                        [Parameter]
                        public RenderFragment ChildContent { get; set; } = null!;
                    }
                    """)]);
    }

    [Fact]
    public async Task DoNotOfferOnHtmlTag()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <d[||]iv>
                    Hello World
                </div>

                <div></div>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyTagToSelfClosing);
    }

    [Fact]
    public async Task DoNotOfferOnNonExistentComponent()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <div>
                    Hello World
                </div>

                <{|RZ10012:Not$$AComponent|}></AComponent>

                <div></div>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyTagToSelfClosing);
    }

    [Fact]
    public async Task DoNotOfferIfComponentHasNonWhiteSpaceBody()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Compo[||]nent>
                    Hello world
                </Component>

                <div></div>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyTagToSelfClosing,
            additionalFiles: [
                (FilePath("Component.razor"), """
                    <div>
                        Hello world
                    </div>
                    """)]);
    }

    [Fact]
    public async Task DoNotOfferIfComponentIsSelfClosing()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Comp[||]onent />

                <div></div>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyTagToSelfClosing,
            additionalFiles: [
                (FilePath("Component.razor"), """
                    <div>
                        Hello world
                    </div>
                    """)]);
    }

    [Fact]
    public async Task DoNotOfferIfHasAnyEditorRequiredRenderFragmentAttribute()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Compo[||]nent>
                    
                </Component>

                <div></div>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyTagToSelfClosing,
            additionalFiles: [
                (FilePath("Component.razor"), """
                    <div>
                        @ChildContent
                    </div>
                    
                    @code {
                        [Parameter, EditorRequired]
                        public RenderFragment ChildContent { get; set; } = null!;
                    }
                    """)]);
    }

    [Fact]
    public async Task DoNotOfferIfHasAnyEditorRequiredGenericRenderFragmentAttribute()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Compo[||]nent>
                    
                </Component>

                <div></div>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyTagToSelfClosing,
            additionalFiles: [
                (FilePath("Component.razor"), """
                    <div>
                        @ItemContent("Test")
                    </div>
                    
                    @code {
                        [Parameter, EditorRequired]
                        public RenderFragment<string> ItemContent { get; set; } = null!;
                    }
                    """)]);
    }

    [Fact]
    public async Task AllEditorRequiredRenderFragmentAttributesAreSetAsAttributesButBodyIsWhiteSpace()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Compo[||]nent ChildContent="null">
                    
                </Component>

                <div></div>
                """,
            expected: """
                <div></div>

                <Component ChildContent="null" />

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyTagToSelfClosing,
            additionalFiles: [
                (FilePath("Component.razor"), """
                    <div>
                        @if (ChildContent is { } cc)
                        {
                            @cc
                        }
                    </div>
                    
                    @code {
                        [Parameter, EditorRequired]
                        public RenderFragment? ChildContent { get; set; }
                    }
                    """)]);
    }

    [Fact]
    public async Task AllEditorRequiredRenderFragmentAttributesAreSetOrBoundAsDirectiveAttributesButBodyIsWhiteSpace()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Compo[||]nent @bind-ChildContent="LocalChildContent">
                    
                </Component>

                <div></div>

                @code {
                    private RenderFragment? LocalChildContent { get; set; }
                }
                """,
            expected: """
                <div></div>

                <Component @bind-ChildContent="LocalChildContent" />

                <div></div>
                
                @code {
                    private RenderFragment? LocalChildContent { get; set; }
                }
                """,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyTagToSelfClosing,
            additionalFiles: [
                (FilePath("Component.razor"), """
                    <div>
                        @if (ChildContent is { } cc)
                        {
                            @cc
                        }
                    </div>
                    
                    @code {
                        [Parameter, EditorRequired]
                        public RenderFragment? ChildContent { get; set; }
                    }
                    """)]);
    }

    [Fact]
    public async Task AllEditorRequiredRenderFragmentAttributesAreSetOrBoundWithGetSetAsDirectiveAttributesButBodyIsWhiteSpace()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Compo[||]nent @bind-ChildContent:get="LocalChildContent" @bind-ChildContent:set="_ => { }">
                    
                </Component>

                <div></div>

                @code {
                    private RenderFragment? LocalChildContent { get; set; }
                }
                """,
            expected: """
                <div></div>

                <Component @bind-ChildContent:get="LocalChildContent" @bind-ChildContent:set="_ => { }" />

                <div></div>
                
                @code {
                    private RenderFragment? LocalChildContent { get; set; }
                }
                """,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyTagToSelfClosing,
            additionalFiles: [
                (FilePath("Component.razor"), """
                    <div>
                        @if (ChildContent is { } cc)
                        {
                            @cc
                        }
                    </div>
                    
                    @code {
                        [Parameter, EditorRequired]
                        public RenderFragment? ChildContent { get; set; }
                    }
                    """)]);
    }
}
