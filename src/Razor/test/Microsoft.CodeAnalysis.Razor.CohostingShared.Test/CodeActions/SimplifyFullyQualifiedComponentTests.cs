// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class SimplifyFullyQualifiedComponentTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task SimplifyFullyQualifiedComponent_NoExistingUsing()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Microsoft.AspNetCore.Components.Sections.Section[||]Outlet />

                <div></div>
                """,
            expected: """
                @using Microsoft.AspNetCore.Components.Sections
                <div></div>

                <SectionOutlet />

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyFullyQualifiedComponent);
    }

    [Fact]
    public async Task SimplifyFullyQualifiedComponent_WithExistingUsing()
    {
        await VerifyCodeActionAsync(
            input: """
                @using Microsoft.AspNetCore.Components.Sections

                <div></div>

                <Microsoft.AspNetCore.Components.Sections.Section[||]Outlet />

                <div></div>
                """,
            expected: """
                @using Microsoft.AspNetCore.Components.Sections

                <div></div>

                <SectionOutlet />

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyFullyQualifiedComponent);
    }

    [Fact]
    public async Task SimplifyFullyQualifiedComponent_WithStartAndEndTags()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Microsoft.AspNetCore.Components.Sections.Section[||]Outlet>
                    <p>Content</p>
                </Microsoft.AspNetCore.Components.Sections.SectionOutlet>

                <div></div>
                """,
            expected: """
                @using Microsoft.AspNetCore.Components.Sections
                <div></div>

                <SectionOutlet>
                    <p>Content</p>
                </SectionOutlet>

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyFullyQualifiedComponent);
    }

    [Fact]
    public async Task SimplifyFullyQualifiedComponent_WithAttributes()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Microsoft.AspNetCore.Components.Sections.Section[||]Outlet SectionName="test" />

                <div></div>
                """,
            expected: """
                @using Microsoft.AspNetCore.Components.Sections
                <div></div>

                <SectionOutlet SectionName="test" />

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyFullyQualifiedComponent);
    }

    [Fact]
    public async Task DoNotOfferOnSimpleComponent()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Compo[||]nent />

                <div></div>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyFullyQualifiedComponent,
            additionalFiles: [
                (FilePath("Component.razor"), """
                    <div>
                        Hello World
                    </div>
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
            codeActionName: LanguageServerConstants.CodeActions.SimplifyFullyQualifiedComponent);
    }

    [Fact]
    public async Task DoNotOfferWhenDiagnosticPresent()
    {
        // When there are diagnostics on the start tag, we shouldn't offer the code action
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <{|RZ10012:NotACompo$$nent|} />

                <div></div>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyFullyQualifiedComponent);
    }

    [Fact]
    public async Task SimplifyFullyQualifiedComponent_NestedNamespace()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <MyCompany.MyApp.Custom[||]Component />

                <div></div>
                """,
            expected: """
                @using MyCompany.MyApp
                <div></div>

                <CustomComponent />

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyFullyQualifiedComponent,
            additionalFiles: [
                (FilePath("MyCompany/MyApp/CustomComponent.razor"), """
                    @namespace MyCompany.MyApp
                    
                    <div>
                        Custom Component
                    </div>
                    """)]);
    }

    [Fact]
    public async Task SimplifyFullyQualifiedComponent_MultipleOccurrences()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Microsoft.AspNetCore.Components.Sections.Section[||]Outlet />
                <Microsoft.AspNetCore.Components.Sections.SectionOutlet />

                <div></div>
                """,
            expected: """
                @using Microsoft.AspNetCore.Components.Sections
                <div></div>

                <SectionOutlet />
                <Microsoft.AspNetCore.Components.Sections.SectionOutlet />

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyFullyQualifiedComponent);
    }

    [Fact]
    public async Task SimplifyFullyQualifiedComponent_SelfClosingNoSpace()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Microsoft.AspNetCore.Components.Sections.Section[||]Outlet/>

                <div></div>
                """,
            expected: """
                @using Microsoft.AspNetCore.Components.Sections
                <div></div>

                <SectionOutlet/>

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyFullyQualifiedComponent);
    }

    [Fact]
    public async Task SimplifyFullyQualifiedComponent_MultilineWithAttributes()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Microsoft.AspNetCore.Components.Sections.Section[||]Outlet
                    SectionName="test"
                    class="goo">
                    content
                </Microsoft.AspNetCore.Components.Sections.SectionOutlet>

                <div></div>
                """,
            expected: """
                @using Microsoft.AspNetCore.Components.Sections
                <div></div>

                <SectionOutlet
                    SectionName="test"
                    class="goo">
                    content
                </SectionOutlet>

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyFullyQualifiedComponent);
    }

    [Fact]
    public async Task SimplifyFullyQualifiedComponent_NamespaceAlreadyInScope()
    {
        // Microsoft.AspNetCore.Components.Forms is automatically in scope, so no using directive should be added
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Microsoft.AspNetCore.Components.Forms.Input[||]Text />

                <div></div>
                """,
            expected: """
                <div></div>

                <InputText />

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyFullyQualifiedComponent);
    }

    [Fact]
    public async Task DoNotOfferWhenCursorInsideTagContent()
    {
        // Code action should not be offered when cursor is positioned within element content
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Microsoft.AspNetCore.Components.Authorization.AuthorizeRouteView>
                    [||]<p>Content inside</p>
                </Microsoft.AspNetCore.Components.Authorization.AuthorizeRouteView>

                <div></div>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyFullyQualifiedComponent);
    }

    [Fact]
    public async Task DoNotOfferOnLegacyRazorFile()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Microsoft.AspNetCore.Components.Forms.Input[||]Text />

                <div></div>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyFullyQualifiedComponent,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task DoNotOfferOnLegacyRazorFile2()
    {
        await VerifyCodeActionAsync(
            input: """
                @addTagHelper *, SomeProject

                <lab[||]}el foo="Dave" />
                """,
            additionalFiles:
                [(FilePath("FooTagHelper.cs"),
                    """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("*", Attributes = FooAttributeName)]
                    public class FooTaghelper : TagHelper
                    {
                        private const string FooAttributeName = "foo";

                        public override void Process(TagHelperContext context, TagHelperOutput output)
                        {
                            output.Attributes.Add("foo", "bar");
                        }
                    }
                    """)],
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyFullyQualifiedComponent,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task DoNotOfferInCSharpCode()
    {
        await VerifyCodeActionAsync(
            input: """
                <Microsoft.AspNetCore.Components.Forms.InputText Value="@va[||]lue"></Microsoft.AspNetCore.Components.Forms.InputText>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyFullyQualifiedComponent);
    }

    [Fact]
    public async Task DoNotOfferInAttribute()
    {
        await VerifyCodeActionAsync(
            input: """
                <Microsoft.AspNetCore.Components.Forms.InputText class="bo[||]ld"></Microsoft.AspNetCore.Components.Forms.InputText>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyFullyQualifiedComponent);
    }

    [Fact]
    public async Task SimplifyFullyQualifiedComponent_EndTag()
    {
        // Microsoft.AspNetCore.Components.Forms is automatically in scope, so no using directive should be added
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Microsoft.AspNetCore.Components.Forms.InputText>
                </Microsoft.AspNetCore.Components.Forms.Input[||]Text>

                <div></div>
                """,
            expected: """
                <div></div>

                <InputText>
                </InputText>

                <div></div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SimplifyFullyQualifiedComponent);
    }
}
