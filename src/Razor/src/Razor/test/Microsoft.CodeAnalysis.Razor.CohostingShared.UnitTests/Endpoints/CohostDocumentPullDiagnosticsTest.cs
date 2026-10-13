// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;
using AssertEx = Roslyn.Test.Utilities.AssertEx;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public partial class CohostDocumentPullDiagnosticsTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public Task NoDiagnostics()
        => VerifyDiagnosticsAsync("""
            <div></div>

            @code
            {
                public void IJustMetYou()
                {
                }
            }
            """);

    [Fact]
    public Task CSharp()
        => VerifyDiagnosticsAsync("""
            <div></div>

            @code
            {
                public void IJustMetYou()
                {
                    {|CS0103:CallMeMaybe|}();
                }
            }
            """);

    [Fact]
    public Task CSharp_ImplicitExpression()
        => VerifyDiagnosticsAsync("""
            <div></div>

            @{|CS0103:CallMeMaybe|}()
            """);

    [Fact]
    public Task Razor()
        => VerifyDiagnosticsAsync("""
            <div>

            {|RZ10012:<NonExistentComponent />|}

            </div>
            """);

    [Fact]
    public Task Razor_CodeBlock()
        => VerifyDiagnosticsAsync("""
            @code
            {
                public void M()
                {
                    RenderFragment x = @{|RZ10012:<NonExistentComponent />|};
                }
            }
            """);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirective_CommentTerminator(bool isComponent)
        => VerifyDiagnosticsAsync("""
            @documentation {
                <summary>Cannot contain {|RZ1047:*/|}.</summary>
            }
            <p>After</p>
            """,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            projectConfigure: static builder => builder.RazorLanguageVersion = RazorLanguageVersion.Version_12_0);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirective_CompatibilityWarning(bool isComponent)
        => VerifyDiagnosticsAsync("""
            @{|RZ1048:documentation|}

            @functions {
                private string documentation => "Summary";
            }
            """,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            projectConfigure: builder =>
            {
                builder.RazorLanguageVersion = RazorLanguageVersion.Version_11_0;
                builder.AddAnalyzerConfigDocument(
                    FilePath("Warnings.globalconfig"),
                    SourceText.From("""
                        is_global = true
                        build_property.RazorWarningLevel = 11
                        """));
            });

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirective_PlainText(bool isComponent)
        => VerifyDiagnosticsAsync("""
            @documentation {
                {|RZ1049:T|}his is the summary
            }
            <p>After</p>
            """,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            projectConfigure: static builder => builder.RazorLanguageVersion = RazorLanguageVersion.Version_12_0);

    [Fact]
    public Task CSharpAndRazor_MiscellaneousFile()
        => VerifyDiagnosticsAsync("""
            <div>

            {|RZ10012:<NonExistentComponent />|}

            </div>

            @code
            {
                public void IJustMetYou()
                {
                    {|CS0103:CallMeMaybe|}();
                }
            }
            """,
            miscellaneousFile: true);

    [Fact]
    public Task CombinedAndNestedDiagnostics()
        => VerifyDiagnosticsAsync("""
            @using System.Threading.Tasks;

            <div>

            {|RZ10012:<NonExistentComponent />|}

            @code
            {
                public void IJustMetYou()
                {
                    {|CS0103:CallMeMaybe|}();
                }
            }

            <div>
                @{
                    {|CS4033:await Task.{|CS1501:Delay|}()|};
                }

                {|RZ9980:<p>|}
            </div>

            </div>
            """);

    [Fact]
    public Task CSharpUnusedUsings()
       => VerifyDiagnosticsAsync("""
            {|RZ0005:@using System|}
            @using System.Text

            <div></div>

            @code
            {
                public void BuildsStrings(StringBuilder b)
                {
                }
            }
            """);

    [Fact]
    public Task CSharpUnusedUsings_FallbackComponent()
       // The component can't be split (an @implements directive forces the fallback path), so its
       // declaration document is a bodiless type shell with no usings. The unused using must still be
       // reported by falling back to the implementation diagnostics rather than filtering against the
       // empty shell.
       => VerifyDiagnosticsAsync("""
            @implements System.IDisposable
            {|RZ0005:@using System.Text|}

            <div></div>

            @code
            {
                public void Dispose()
                {
                }
            }
            """);

    [Fact]
    public Task CSharpUsingUnusedInImplOnly()
       => VerifyDiagnosticsAsync("""
            @using System.Text

            <div></div>

            @code
            {
                public void BuildsStrings(StringBuilder b)
                {
                }
            }
            """);

    [Fact]
    public Task CSharpUsingUnusedInDeclOnly()
       => VerifyDiagnosticsAsync("""
            @using System.Text

            @nameof(StringBuilder)

            <div></div>

            @code
            {
                public void BuildsStrings()
                {
                }
            }
            """);

    [Fact]
    public Task CSharpUnusedUsings_NoCodeBlock()
        => VerifyDiagnosticsAsync("""
            {|RZ0005:@using System|}

            <div></div>
            """);

    [Fact]
    public Task RazorUsingAlsoPresentInImports()
       => VerifyDiagnosticsAsync("""
            @using System.Text
            {|RZ0005:@using Microsoft.AspNetCore.Components.Forms|}

            <div></div>

            <PageTitle></PageTitle>

            @code
            {
                public void BuildsStrings(StringBuilder b)
                {
                }
            }
            """);

    [Fact]
    public Task RazorUsedUsings()
        => VerifyDiagnosticsAsync(
           input: """
                @using System.Text
                @using My.Fun.Namespace

                <div></div>

                <PageTitle></PageTitle>

                <Component />

                @code
                {
                    public void BuildsStrings(StringBuilder b)
                    {
                    }
                }
                """,
           additionalFiles: [
               (FilePath("Component.razor"), """
               @namespace My.Fun.Namespace

               <div></div>
               """)]);

    [Fact]
    public Task RazorUnusedUsings()
        => VerifyDiagnosticsAsync(
            input: """
                @using System.Text
                {|RZ0005:@using My.Fun.Namespace|}
                     
                <div></div>

                <PageTitle></PageTitle>

                @code
                {
                    public void BuildsStrings(StringBuilder b)
                    {
                    }
                }
                """,
            additionalFiles: [
                 (FilePath("Component.razor"), """
                     @namespace My.Fun.Namespace

                     <div></div>
                     """)]);

    [Fact]
    public Task LegacyUnusedAddTagHelperDirective()
        => VerifyDiagnosticsAsync(
            input: """
                {|RZ0005:@addTagHelper *, SomeProject|}
                {|RZ0005:@using System.Text|}
                {|RZ0005:@using System.Text.RegularExpressions|}

                <div></div>
                """,
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
            fileKind: RazorFileKind.Legacy);

    [Fact]
    public Task LegacyUsedAddTagHelperDirective_Control()
        => VerifyDiagnosticsAsync(
            input: """
                @addTagHelper *, SomeProject

                <dw:about-box />

                @functions
                {
                    public void M()
                    {
                    }
                }
                """,
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
            fileKind: RazorFileKind.Legacy);

    [Fact]
    public Task LegacySpecificAddTagHelperDirectives_MixedUsage()
        => VerifyDiagnosticsAsync(
            input: """
                @addTagHelper AboutBoxTagHelper, SomeProject
                {|RZ0005:@addTagHelper FancyBoxTagHelper, SomeProject|}

                <dw:about-box />
                """,
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
            fileKind: RazorFileKind.Legacy);

    private async Task<LspDiagnostic[]> VerifyDiagnosticsAsync(
        TestCode input,
        FullDocumentDiagnosticReport[]? htmlResponse = null,
        RazorFileKind? fileKind = null,
        bool taskListRequest = false,
        bool miscellaneousFile = false,
        (string fileName, string contents)[]? additionalFiles = null,
        Action<RazorProjectBuilder>? projectConfigure = null)
    {
        var document = CreateProjectAndRazorDocument(input.Text, fileKind, miscellaneousFile: miscellaneousFile, additionalFiles: additionalFiles, projectConfigure: projectConfigure);
        var inputText = await document.GetTextAsync(DisposalToken);

        var htmlResult = htmlResponse is null
            ? default(SumType<FullDocumentDiagnosticReport, UnchangedDocumentDiagnosticReport>)
            : new SumType<FullDocumentDiagnosticReport, UnchangedDocumentDiagnosticReport>(Assert.Single(htmlResponse));
        var requestInvoker = new TestHtmlRequestInvoker([(Methods.TextDocumentDiagnosticName, htmlResult)]);

        if (taskListRequest)
        {
            ClientSettingsManager.Update(ClientSettingsManager.GetClientSettings().AdvancedSettings with { TaskListDescriptors = ["TODO"] });
        }

        var result = await MakeDiagnosticsRequestAsync(document, taskListRequest, requestInvoker, IncompatibleProjectService, RemoteServiceInvoker, ClientCapabilitiesService, LoggerFactory, DisposalToken);

        Assert.NotNull(result);

        var markers = result.SelectMany(d =>
            new[] {
                (index: inputText.GetTextSpan(d.Range).Start, text: $"{{|{d.Code!.Value.Second}:"),
                (index: inputText.GetTextSpan(d.Range).End, text:"|}")
            });

        var testOutput = input.Text;
        // Ordering by text last means start tags get sorted before end tags, for zero width ranges
        foreach (var (index, text) in markers.OrderByDescending(i => i.index).ThenByDescending(i => i.text))
        {
            testOutput = testOutput.Insert(index, text);
        }

        AssertEx.EqualOrDiff(input.OriginalInput, testOutput);

        if (!taskListRequest && ClientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions)
        {
            Assert.All(result,
                d =>
                {
                    var vsDiagnostic = Assert.IsType<VSDiagnostic>(d);
                    Assert.NotNull(vsDiagnostic.Identifier);
                    Assert.NotNull(vsDiagnostic.Projects);
                    var project = Assert.Single(vsDiagnostic.Projects);
                    Assert.NotNull(project.ProjectIdentifier);
                    var firstDiagnostic = Assert.IsType<VSDiagnostic>(result[0]);
                    Assert.NotNull(firstDiagnostic.Projects);
                    // We always report the same project info for all diagnostics
                    Assert.Same(project, Assert.Single(firstDiagnostic.Projects));
                });
        }

        return result;
    }

    internal static async Task<LspDiagnostic[]?> MakeDiagnosticsRequestAsync(
        TextDocument document,
        bool taskListRequest,
        TestHtmlRequestInvoker requestInvoker,
        IIncompatibleProjectService incompatibleProjectService,
        IRemoteServiceInvoker remoteServiceInvoker,
        IClientCapabilitiesService clientCapabilitiesService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var endpoint = new CohostDocumentPullDiagnosticsEndpoint(incompatibleProjectService, remoteServiceInvoker, requestInvoker, clientCapabilitiesService, NoOpTelemetryReporter.Instance, loggerFactory, VoidSessionTracker.Instance);
        var request = new DocumentDiagnosticParams
        {
            TextDocument = new TextDocumentIdentifier { DocumentUri = document.GetURI() },
            Identifier = taskListRequest
                ? PullDiagnosticCategories.Task
                : clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions ? PullDiagnosticCategories.DocumentCompilerSyntax : null,
        };

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(request, document, cancellationToken);
        return result?.Items;
    }
}
