// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Razor.Workspaces.Resources;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Text.Adornments;
using Xunit;
using Xunit.Abstractions;
using WorkItemAttribute = Roslyn.Test.Utilities.WorkItemAttribute;

#if !VSCODE
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Razor.Snippets;
#endif

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public partial class CohostDocumentCompletionEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task NotWhenAutoShowCompletionIsOff()
    {
        var settings = ClientSettingsManager.GetClientSettings();
        ClientSettingsManager.Update(settings.ClientCompletionSettings with { AutoShowCompletion = false });

        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                @$$

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "@",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: null);
    }

    [Fact]
    public async Task CSharpInEmptyExplicitStatement()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                @{
                    $$
                }

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["var", "char", "DateTime", "Exception"]);
    }

    [Fact]
    public async Task CSharpClassesAtTransition()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <div>@$$</div>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "@",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["char", "DateTime", "Exception"],
            itemToResolve: "DateTime",
            expectedResolvedItemDescription: "readonly struct System.DateTime");
    }

    [Fact]
    public async Task CSharpClassesBeforeTag()
    {
        await VerifyCompletionListAsync(
            input: """
                @page
                @model IndexModel
                @{
                    ViewData["Title"] = "Home page";
                }
                @da$$
                <div class="text-center">
                    <h1 class="display-4">Welcome</h1>
                    <p>Learn about <a href="https://learn.microsoft.com/aspnet/core">building Web apps with ASP.NET Core</a>.</p>
                </div>
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "@",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["char", "DateTime", "Exception"],
            itemToResolve: "DateTime",
            expectedResolvedItemDescription: "readonly struct System.DateTime",
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task CSharpClassMembersAtProvisionalCompletion()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <div>@DateTime.$$</div>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = ".",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["DaysInMonth", "IsLeapYear", "Now"],
            itemToResolve: "Now",
            expectedResolvedItemDescription: "DateTime DateTime.Now { get; }",
            expected: """
            This is a Razor document.
            
            <div>@DateTime.Now</div>
            
            The end.
            """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/8442")]
    public async Task CSharpClassMembersInComponentParameterWithoutLeadingAt()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <EditForm Model="DateTime.$$"></EditForm>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = ".",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["DaysInMonth", "IsLeapYear", "Now"]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/8442")]
    public async Task CSharpClassMembersInComponentParameterWithLeadingAt()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <EditForm Model="@DateTime.$$"></EditForm>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = ".",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["DaysInMonth", "IsLeapYear", "Now"]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/8442")]
    public async Task CSharpClassMembersInComponentParameterWithLeadingAt_Incomplete()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <EditForm Model="@DateTime.$$

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = ".",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["DaysInMonth", "IsLeapYear", "Now"]);
    }

    [Fact]
    public async Task CSharpClassesInCodeBlock()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <div></div>

                @code{ $$ }

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["char", "DateTime", "Exception"]);
    }

    [Fact]
    public async Task CSharpClassMembersInCodeBlock()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <div></div>

                @code{
                    void foo()
                    {
                        DateTime.$$
                    }
                }

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = ".",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["DaysInMonth", "IsLeapYear", "Now"]);
    }

    [Fact]
    public async Task CSharpOverrideMethods()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <div></div>

                @code {
                    public override $$
                }

                The end.
                """,
            expected: """
                @using System.Threading.Tasks
                This is a Razor document.
            
                <div></div>
            
                @code {
                    public override Task SetParametersAsync(ParameterView parameters)
                    {
                        return base.SetParametersAsync(parameters);
                    }
                }
            
                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["Equals(object? obj)", "GetHashCode()", "SetParametersAsync(ParameterView parameters)", "ToString()"],
            itemToResolve: "SetParametersAsync(ParameterView parameters)",
            expectedResolvedItemDescription: "(awaitable) Task ComponentBase.SetParametersAsync(ParameterView parameters)");
    }

    // Tests MarkupTransitionCompletionItemProvider
    [Fact]
    public async Task CSharpMarkupTransitionAndTagHelpersInCodeBlock()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <div></div>

                @code{
                    void foo()
                    {
                        <$$
                    }
                }

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "<",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["text", "EditForm", "InputDate", "div"],
            htmlItemLabels: ["div"]);
    }

    [Fact]
    public async Task RazorDirectives()
    {
        var expectedDirectiveLabels = new string[]
            {
                "attribute", "implements", "inherits", "inject", "layout", "namespace", "page",
                "preservewhitespace", "typeparam", "using"
            };
        var expectedDirectiveSnippetLabels = expectedDirectiveLabels.Select(label => $"{label} directive ...");
        var expectedCSharpLabels = new string[] { "char", "DateTime", "Exception" };
        var expectedLabels = expectedDirectiveLabels
            .Concat(expectedDirectiveSnippetLabels)
            .Concat(expectedCSharpLabels)
            .ToArray();

        await VerifyCompletionListAsync(
            input: """
                @$$
                This is a Razor document.

                <div></div>

                @code{
                    void foo()
                    {
                        
                    }
                }

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "@",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: expectedLabels,
            itemToResolve: "page",
            expectedResolvedItemDescription: "Mark the page as a routable component.");
    }

    [Fact]
    public async Task ElementNameTagHelpersCompletion()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <$$

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "<",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["LayoutView", "EditForm", "ValidationMessage", "div"],
            htmlItemLabels: ["div"],
            itemToResolve: "EditForm",
            expectedResolvedItemDescription: "Microsoft.AspNetCore.Components.Forms.EditForm");
    }

    [Fact]
    public async Task HtmlElementNamesAndTagHelpersCompletion()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <$$

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "<",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["div", "h1", "LayoutView", "EditForm", "ValidationMessage"],
            htmlItemLabels: ["div", "h1"]);
    }

    [Fact]
    public async Task HtmlCompletionFailure_ReturnsIncompleteEmptyList()
    {
        // When the HTML language server fails to respond (e.g., not yet initialized on first document open),
        // we should return an empty IsIncomplete list so the client retries, rather than showing partial
        // Razor-only results that could cause the user to accidentally commit a wrong item.
        var input = new TestCode("""
            This is a Razor document.

            <$$

            The end.
            """);

        var document = CreateProjectAndRazorDocument(input.Text);
        var sourceText = await document.GetTextAsync(DisposalToken);

        // Use a TestHtmlRequestInvoker that returns null (simulating HTML server not ready)
        var requestInvoker = new TestHtmlRequestInvoker((Methods.TextDocumentCompletionName, (object?)null));

#if VSCODE
        ISnippetCompletionItemProvider? snippetCompletionItemProvider = null;
#else
        var snippetCompletionItemProvider = new SnippetCompletionItemProvider(new SnippetCache());
#endif

        var completionListCache = new CompletionListCache();
        var endpoint = new CohostDocumentCompletionEndpoint(
            IncompatibleProjectService,
            RemoteServiceInvoker,
            ClientSettingsManager,
            ClientCapabilitiesService,
            snippetCompletionItemProvider,
            requestInvoker,
            completionListCache,
            NoOpTelemetryReporter.Instance,
            LoggerFactory);

        var request = new RazorVSInternalCompletionParams()
        {
            TextDocument = new TextDocumentIdentifier()
            {
                DocumentUri = document.CreateDocumentUri()
            },
            Position = sourceText.GetPosition(input.Position),
            Context = new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "<",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            }
        };

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken);

        Assert.NotNull(result);
        Assert.True(result.IsIncomplete);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Component_FullyQualified()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <$$

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "<",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["EditForm", "SectionOutlet - @using Microsoft.AspNetCore.Components.Sections", "Microsoft.AspNetCore.Components.Sections.SectionOutlet"],
            htmlItemLabels: ["div", "h1"],
            itemToResolve: "Microsoft.AspNetCore.Components.Sections.SectionOutlet",
            expectedResolvedItemDescription: "Microsoft.AspNetCore.Components.Sections.SectionOutlet",
            expected: """
            This is a Razor document.
            
            <Microsoft.AspNetCore.Components.Sections.SectionOutlet
            
            The end.
            """);
    }

    [Fact]
    public async Task Completion_WithUsing()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <$$

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "<",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["EditForm", "SectionOutlet - @using Microsoft.AspNetCore.Components.Sections", "Microsoft.AspNetCore.Components.Sections.SectionOutlet"],
            htmlItemLabels: ["div", "h1"],
            itemToResolve: "SectionOutlet - @using Microsoft.AspNetCore.Components.Sections",
            expectedResolvedItemDescription: "Microsoft.AspNetCore.Components.Sections.SectionOutlet",
            expected: """
            @using Microsoft.AspNetCore.Components.Sections
            This is a Razor document.
            
            <SectionOutlet
            
            The end.
            """);
    }

    [Fact]
    public async Task HtmlElementNamesAndTagHelpersCompletion_EndOfDocument()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <$$
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "<",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["div", "h1", "LayoutView", "EditForm", "ValidationMessage"],
            htmlItemLabels: ["div", "h1"],
            unexpectedItemLabels: ["snippet1", "snippet2"]);
    }

    [Fact]
    public async Task HtmlElementNamesCompletion_UsesRazorVSInternalCompletionParams()
    {
        await VerifyCompletionListParamsTypeAsync(
            input: """
                This is a Razor document.

                <$$1
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "<",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            });
    }

    [Fact]
    public async Task HtmlElementDoNotCommitWithSpace()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <$$

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "<",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["div", "h1", "LayoutView", "EditForm", "ValidationMessage"],
            htmlItemLabels: ["div", "h1"],
            htmlItemCommitCharacters: [" ", ">"],
            commitElementsWithSpace: false);
    }

    // Tests HTML attributes and DirectiveAttributeTransitionCompletionItemProvider
    [Fact]
    public async Task HtmlAndDirectiveAttributeTransitionNamesCompletion()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <div $$></div>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["style", "dir", "@..."],
            htmlItemLabels: ["style", "dir"]);
    }

    // Tests HTML attributes and DirectiveAttributeCompletionItemProvider
    [Fact]
    public async Task HtmlAndDirectiveAttributeNamesCompletion()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <div @$$></div>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "@",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["style", "dir", "@rendermode", "@bind-..."],
            htmlItemLabels: ["style", "dir"],
            itemToResolve: "@rendermode",
#if VSCODE
            expectedResolvedItemDescription: """
                IComponentRenderMode RenderMode.RenderMode

                Specifies the render mode for a component.
                """);
#else
            expectedResolvedItemDescription: """
                IComponentRenderMode Microsoft.AspNetCore.Components.RenderMode.RenderMode
                Specifies the render mode for a component.
                """);
#endif
    }

    // Tests HTML attributes and DirectiveAttributeParameterCompletionItemProvider
    [Fact]
    public async Task HtmlAndDirectiveAttributeParameterNamesCompletion()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <input @bind:f$$></div>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["style", "dir", "culture", "event", "format", "get", "set", "after"],
            htmlItemLabels: ["style", "dir"]);
    }

    [Fact]
    public async Task HtmlAndDirectiveAttributeEventParameterEmptyNoSuffixHtmlEventNamesCompletion()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <input @bind="str" @bind:event="$$ />

                The end.

                @code {
                    private string? str;
                }
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["oninput", "onchange", "onblur"],
            htmlItemLabels: [],
            commitElementsWithSpace: true);
    }

    [Fact]
    public async Task HtmlAndDirectiveAttributeEventParameterEmptyHtmlEventNamesCompletion()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <input @bind="str" @bind:event="$$" />

                The end.

                @code {
                    private string? str;
                }
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["oninput", "onchange", "onblur"],
            htmlItemLabels: [],
            commitElementsWithSpace: true);
    }

    [Fact]
    public async Task HtmlAndDirectiveAttributeEventParameterNonEmptyHtmlEventNamesCompletion()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <input @bind="str" @bind:event="on$$" />

                The end.

                @code {
                    private string? str;
                }
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["oninput", "onchange", "onblur"],
            htmlItemLabels: [],
            commitElementsWithSpace: true);
    }

    [Fact]
    public async Task HtmlAttributeNamesAndTagHelpersCompletion()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <EditForm $$></EditForm>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["style", "dir", "FormName", "OnValidSubmit", "@..."],
            htmlItemLabels: ["style", "dir"],
            itemToResolve: "FormName",
#if VSCODE
            expectedResolvedItemDescription: "string EditForm.FormName");
#else
            expectedResolvedItemDescription: "string Microsoft.AspNetCore.Components.Forms.EditForm.FormName");
#endif
    }

    [Fact]
    public async Task HtmlAttributeNamesAndTagHelpersCompletion_SelfClosing()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <EditForm $$/>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["style", "dir", "FormName", "OnValidSubmit", "@..."],
            htmlItemLabels: ["style", "dir"],
            itemToResolve: "FormName",
#if VSCODE
            expectedResolvedItemDescription: "string EditForm.FormName");
#else
            expectedResolvedItemDescription: "string Microsoft.AspNetCore.Components.Forms.EditForm.FormName");
#endif
    }

    [Fact]
    public async Task HtmlAttributeNamesAndTagHelpersCompletion_EndOfDocument()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <EditForm $$
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["style", "dir", "FormName", "OnValidSubmit", "@..."],
            htmlItemLabels: ["style", "dir"]);
    }

    [Fact]
    public async Task TagHelperAttributes_NoAutoInsertQuotes_Completion()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <EditForm $$></EditForm>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["FormName", "OnValidSubmit", "@...", "style"],
            htmlItemLabels: ["style"],
            autoInsertAttributeQuotes: false);
    }

#if VSCODE
    [Fact]
    public async Task TagHelperAttributes_NoCommitChars_VSCode()
    {
        var list = await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <EditForm $$></EditForm>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["FormName", "OnValidSubmit", "@...", "style"],
            htmlItemLabels: ["style"],
            autoInsertAttributeQuotes: false);

        Assert.NotNull(list);
        Assert.All(list.Items, item => Assert.DoesNotContain("=", item.CommitCharacters ?? []));
    }

    [Fact]
    public async Task CSharp_WithUsing()
    {
        // Roslyn won't send unimported types if SupportsVisualStudioExtensions is true

        await VerifyCompletionListAsync(
            input: """
                @{
                    void Foo()
                    {
                        String$$
                    }
                }
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["char", "DateTime", "StringBuilder"],
            itemToResolve: "StringBuilder",
            expectedResolvedItemDescription: "class System.Text.StringBuilder",
            expected: """
                @using System.Text
                @{
                    void Foo()
                    {
                        StringBuilder
                    }
                }
                """,
            // Completion from unimported types is computed in the background
            retryTimeout: TimeSpan.FromSeconds(5));
    }
#endif

    [Fact]
    public async Task ComponentWithEditorRequiredAttributes()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <$$

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "<",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["LayoutView", "EditForm", "ValidationMessage", "div", "Router", SR.FormatComponentCompletionWithRequiredAttributesLabel("Router")],
            htmlItemLabels: ["div"],
            itemToResolve: SR.FormatComponentCompletionWithRequiredAttributesLabel("Router"),
            expectedResolvedItemDescription: "Microsoft.AspNetCore.Components.Routing.Router",
            expected: $"""
                This is a Razor document.

                <Router AppAssembly="$1" Found="$2">$0</Router>

                The end.
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9378")]
    public async Task BlazorDataEnhanceAttributeCompletion_OnFormElement()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <form $$></form>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["data-enhance", "data-enhance-nav", "data-permanent", "dir", "@..."],
            htmlItemLabels: ["dir"]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9378")]
    public async Task BlazorDataEnhanceNavAttributeCompletion_OnAnyElement()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <div $$></div>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["data-enhance-nav", "data-permanent", "dir", "@..."],
            unexpectedItemLabels: ["data-enhance"],
            htmlItemLabels: ["dir"]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9378")]
    public async Task BlazorDataPermanentAttributeCompletion_OnAnchorElement()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <a $$></a>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["data-enhance-nav", "data-permanent", "dir", "@..."],
            unexpectedItemLabels: ["data-enhance"],
            htmlItemLabels: ["dir"]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9378")]
    public async Task BlazorDataAttributeCompletion_DoesNotDuplicateExistingAttribute()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <form data-enhance $$></form>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["data-enhance-nav", "data-permanent", "dir", "@..."],
            unexpectedItemLabels: ["data-enhance"],
            htmlItemLabels: ["dir"]);
    }

    [Fact]
    public async Task RazorCSharpKeywordCompletion_ReturnsKeywords()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                @$$

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "@",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: [.. CSharpRazorKeywordCompletionItemProvider.CSharpRazorKeywords]);
    }

    [Fact]
    public async Task CSharp_AwaitKeyword()
    {
        await VerifyCompletionListAsync(
            input: """
                @{
                    Task FooAsync()
                    {
                    awai$$
                    }
                }
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["char", "DateTime", "await"],
            itemToResolve: "await",
            expectedResolvedItemDescription: "await Keyword\r\nAsynchronously waits for the task to finish.",
            expected: """
                @{
                    async Task FooAsync()
                    {
                        await
                    }
                }
                """);
    }

    [Fact]
    public async Task RazorHelpersFilteredOut()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                @{ var __helper = 3; }

                @$$

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked,
            },
            expectedItemLabels: ["char", "DateTime", "Exception"],
            unexpectedItemLabels: ["__builder", "__helper"]);
    }

    [Fact]
    public async Task RazorHelpersNotFilteredIfTyping()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                @{ var __helper = 3; }

                @__$$

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked,
            },
            expectedItemLabels: ["char", "DateTime", "Exception", "__helper"],
            unexpectedItemLabels: ["__builder"]);
    }

    [Fact]
    public async Task IndexerAttributes1()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <a $$
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expected: """
                This is a Razor document.

                <a asp-route="$0"
                """,
            expectedItemLabels: ["asp-route", "asp-route-..."],
            htmlItemLabels: ["style", "dir"],
            itemToResolve: "asp-route",
#if VSCODE
            expectedResolvedItemDescription: "string AnchorTagHelper.Route",
#else
            expectedResolvedItemDescription: "string Microsoft.AspNetCore.Mvc.TagHelpers.AnchorTagHelper.Route",
#endif
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task IndexerAttributes2()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <a $$
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expected: """
                This is a Razor document.

                <a asp-route-
                """,
            expectedItemLabels: ["asp-route", "asp-route-..."],
            htmlItemLabels: ["style", "dir"],
            itemToResolve: "asp-route-...",
#if VSCODE
            expectedResolvedItemDescription: "string AnchorTagHelper.RouteValues",
#else
            expectedResolvedItemDescription: "string System.String> Microsoft.AspNetCore.Mvc.TagHelpers.AnchorTagHelper.RouteValues",
#endif
            fileKind: RazorFileKind.Legacy);
    }

    private async Task<RazorVSInternalCompletionList?> VerifyCompletionListAsync(
        TestCode input,
        VSInternalCompletionContext completionContext,
        string[]? expectedItemLabels,
        string[]? unexpectedItemLabels = null,
        string[]? htmlItemLabels = null,
        string[]? htmlItemCommitCharacters = null,
        string[]? snippetLabels = null,
        string? itemToResolve = null,
        string? expected = null,
        string? expectedResolvedItemDescription = null,
        bool autoInsertAttributeQuotes = true,
        bool commitElementsWithSpace = true,
        RazorFileKind? fileKind = null,
        TimeSpan? retryTimeout = null)
    {
        var document = CreateProjectAndRazorDocument(input.Text, fileKind);
        var sourceText = await document.GetTextAsync(DisposalToken);

        ClientSettingsManager.Update(ClientAdvancedSettings.Default with { AutoInsertAttributeQuotes = autoInsertAttributeQuotes, CommitElementsWithSpace = commitElementsWithSpace });

        const string InvalidLabel = "_INVALID_";

        // If delegatedItemLabels wasn't supplied, supply our own to ensure delegation isn't happening and causing a false positive result
        htmlItemLabels ??= [InvalidLabel];
        var response = new RazorVSInternalCompletionList()
        {
            Items = [.. htmlItemLabels.Select((label) => new VSInternalCompletionItem()
            {
                Label = label,
                CommitCharacters = htmlItemCommitCharacters,
                // If test specifies not to commit with space, set kind to element since we remove space
                // commit from elements only. Otherwise test doesn't care, so set to None
                Kind = !commitElementsWithSpace ? CompletionItemKind.Element : CompletionItemKind.None,
            })],
            IsIncomplete = true
        };

        var requestInvoker = new TestHtmlRequestInvoker([(Methods.TextDocumentCompletionName, response)]);

#if VSCODE
        ISnippetCompletionItemProvider? snippetCompletionItemProvider = null;
#else
        var snippetCompletionItemProvider = new SnippetCompletionItemProvider(new SnippetCache());
        // If snippetLabels wasn't supplied, supply our own to ensure snippets aren't being requested and causing a false positive result
        snippetLabels ??= [InvalidLabel];
        var snippetInfos = snippetLabels.Select(label => new SnippetInfo(label, label, label, string.Empty, SnippetLanguage.Html)).ToImmutableArray();
        snippetCompletionItemProvider.SnippetCache.Update(SnippetLanguage.Html, snippetInfos);
#endif

        var completionListCache = new CompletionListCache();
        var endpoint = new CohostDocumentCompletionEndpoint(
            IncompatibleProjectService,
            RemoteServiceInvoker,
            ClientSettingsManager,
            ClientCapabilitiesService,
            snippetCompletionItemProvider,
            requestInvoker,
            completionListCache,
            NoOpTelemetryReporter.Instance,
            LoggerFactory);

        var request = new RazorVSInternalCompletionParams()
        {
            TextDocument = new TextDocumentIdentifier()
            {
                DocumentUri = document.CreateDocumentUri()
            },
            Position = sourceText.GetPosition(input.Position),
            Context = completionContext
        };

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken);

        if (result is null)
        {
            Assert.Null(expectedItemLabels);
            return null;
        }

        Assert.NotNull(expectedItemLabels);

        if (retryTimeout is not null && itemToResolve is not null)
        {
            var sw = Stopwatch.StartNew();
            while (result.Items.FirstOrDefault(i => i.Label == itemToResolve) == null)
            {
                Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), "Failed to resolve unimported completion item after 5 second.");

                // Roslyn only computes unimported types in the background, and we have no access to its internal workings to wait for it to be
                // finished, so we just have to delay and ask for completion items again.
                await Task.Delay(100, DisposalToken);
                result = await endpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken);
                Assert.NotNull(result);
            }
        }

        using var _ = HashSetPool<string>.GetPooledObject(out var labelSet);
        labelSet.AddRange(result.Items.SelectAsArray((item) => item.Label));

        Assert.DoesNotContain(InvalidLabel, labelSet);

        foreach (var expectedItemLabel in expectedItemLabels)
        {
            Assert.Contains(expectedItemLabel, labelSet);
        }

        if (unexpectedItemLabels is not null)
        {
            foreach (var unexpectedItemLabel in unexpectedItemLabels)
            {
                Assert.DoesNotContain(unexpectedItemLabel, labelSet);
            }
        }

        if (!commitElementsWithSpace)
        {
            Assert.False(result.Items.Any(item => item.CommitCharacters?.First().Contains(' ') ?? false));
        }

        if (!autoInsertAttributeQuotes)
        {
            // Tag helper attributes create InsertText that looks something like
            // "OnValidSubmit=\"$0\"" (for OnValidSubmit attribute). Make sure the value
            // placeholder $0 is not surrounded with quotes if we set AutoInsertAttributeQuotes
            // to false
            Assert.False(result.Items.Any(item => item.InsertText?.Contains("\"$0\"") ?? false));
        }

        if (itemToResolve is not null)
        {
            // In the real world the client will send us back the data for the item to resolve, but in tests its easier if we just set it here.
            // We clone the item first though, to ensure us setting the data doesn't hide a bug in our caching logic, around wrapping" the data.
            var item = Assert.Single(result.Items.Where(i => i.Label == itemToResolve));
            item = JsonSerializer.Deserialize<VSInternalCompletionItem>(JsonSerializer.SerializeToElement(item, JsonHelpers.JsonSerializerOptions), JsonHelpers.JsonSerializerOptions)!;
            item.Data ??= result.Data ?? result.ItemDefaults?.Data;

            Assert.NotNull(item);
            Assert.NotNull(expectedResolvedItemDescription);

            await VerifyCompletionResolveAsync(document, completionListCache, item, expected, expectedResolvedItemDescription, request.Position);
        }

        return result;
    }

    private async Task VerifyCompletionListParamsTypeAsync(
        TestCode input,
        VSInternalCompletionContext completionContext,
        RazorFileKind? fileKind = null)
    {
        var document = CreateProjectAndRazorDocument(input.Text, fileKind);
        var sourceText = await document.GetTextAsync(DisposalToken);

        // Assert the request invoker is passed a RazorVSInternalCompletionParams
        var requestInvoker = new TestHtmlRequestInvoker((Methods.TextDocumentCompletionName, ValidateArgType));

        var completionListCache = new CompletionListCache();
        var endpoint = new CohostDocumentCompletionEndpoint(
            IncompatibleProjectService,
            RemoteServiceInvoker,
            ClientSettingsManager,
            ClientCapabilitiesService,
            new ThrowingSnippetCompletionItemResolveProvider(),
            requestInvoker,
            completionListCache,
            NoOpTelemetryReporter.Instance,
            LoggerFactory);

        var request = new RazorVSInternalCompletionParams()
        {
            TextDocument = new TextDocumentIdentifier()
            {
                DocumentUri = document.CreateDocumentUri()
            },
            Position = sourceText.GetPosition(input.Position),
            Context = completionContext
        };

        await endpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken);

        static RazorVSInternalCompletionList? ValidateArgType(object arg)
        {
            Assert.Equal(typeof(RazorVSInternalCompletionParams), arg.GetType());
            return default;
        }
    }

    private async Task VerifyCompletionResolveAsync(CodeAnalysis.TextDocument document, CompletionListCache completionListCache, VSInternalCompletionItem item, string? expected, string expectedResolvedItemDescription, Position position)
    {
        // We expect data to be a JsonElement, so for tests we have to _not_ strongly type
        item.Data = JsonSerializer.SerializeToElement(item.Data, JsonHelpers.JsonSerializerOptions);

        var endpoint = new CohostDocumentCompletionResolveEndpoint(
            IncompatibleProjectService,
            completionListCache,
            RemoteServiceInvoker,
            new TestHtmlRequestInvoker(),
            ClientCapabilitiesService,
            new ThrowingSnippetCompletionItemResolveProvider(),
            LoggerFactory);

        var tdi = endpoint.GetTestAccessor().GetRazorTextDocumentIdentifier(item);
        Assert.NotNull(tdi);
        Assert.Equal(document.CreateUri(), tdi.Value.Uri);

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(item, document, DisposalToken);

        Assert.NotNull(result);

        SourceText? changedText = null;
        if (result.TextEdit is { Value: TextEdit edit })
        {
            Assert.NotNull(expected);

            var text = await document.GetTextAsync(DisposalToken).ConfigureAwait(false);
            changedText = text.WithChanges(text.GetTextChange(edit));
        }
        else if (result.Command is { Arguments: [_, TextEdit textEdit, ..] })
        {
            Assert.NotNull(expected);

            var text = await document.GetTextAsync(DisposalToken).ConfigureAwait(false);
            changedText = text.WithChanges(text.GetTextChange(textEdit));
        }
        else if (result.InsertText is { } insertText)
        {
            // We'll let expected be null here, since its just simple text insertion

            var text = await document.GetTextAsync(DisposalToken).ConfigureAwait(false);
            var insertIndex = text.GetRequiredAbsoluteIndex(position);
            changedText = text.WithChanges(new TextChange(new TextSpan(insertIndex, 0), insertText));
        }
        else if (result.Label is { } label)
        {
            // We'll let expected be null here, since its just simple text insertion

            var text = await document.GetTextAsync(DisposalToken).ConfigureAwait(false);
            var insertIndex = text.GetRequiredAbsoluteIndex(position);
            var startIndex = insertIndex;
            // If there is already a word in the document, replace it
            for (var i = insertIndex - 1; i > 0; i--)
            {
                if (!char.IsLetter(text[i]))
                {
                    startIndex = i + 1;
                    break;
                }
            }

            changedText = text.WithChanges(new TextChange(TextSpan.FromBounds(startIndex, insertIndex), label));
        }
        else if (expected is not null)
        {
            Assert.Fail("Expected a TextEdit or Command with TextEdit, or InsertText, or Label, but got none. Presumably resolve failed. Result: " + JsonSerializer.SerializeToElement(result).ToString());
        }

        if (result.AdditionalTextEdits is not null)
        {
            // Can't be only additional texts. They're additional!
            Assert.NotNull(changedText);
            Assert.NotNull(expected);

            changedText = changedText.WithChanges(result.AdditionalTextEdits.Select(changedText.GetTextChange));
        }

        if (expected is not null)
        {
            Assert.NotNull(changedText);
            AssertEx.EqualOrDiff(expected, changedText.ToString());
        }

        if (result.Description is not null)
        {
            AssertEx.EqualOrDiff(expectedResolvedItemDescription, FlattenDescription(result.Description));
        }
        else if (result.Documentation is { Value: string description })
        {
            AssertEx.EqualOrDiff(expectedResolvedItemDescription, description);
        }
        else if (result.Documentation is { Value: MarkupContent { Kind.Value: "plaintext" } content })
        {
            AssertEx.EqualOrDiff(expectedResolvedItemDescription, content.Value);
        }
        else
        {
            Assert.Fail("Unhandled description type: " + JsonSerializer.SerializeToElement(result).ToString());
        }
    }

    private static string? FlattenDescription(ClassifiedTextElement? description)
    {
        if (description is null)
        {
            return null;
        }

        var sb = new StringBuilder();
        foreach (var run in description.Runs)
        {
            sb.Append(run.Text);
        }

        return sb.ToString();
    }
}
