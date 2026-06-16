// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostSemanticTokensRangeEndpointTest(ITestOutputHelper testOutputHelper) : CohostSemanticTokensEndpointTestBase(testOutputHelper)
{
    [Theory]
    [CombinatorialData]
    public async Task RazorComponents(bool colorBackground, bool miscellaneousFile)
    {
        var input = """
            <InputText Value="someValue" />
            <InputText Value="someValue"></InputText>
            <Microsoft.AspNetCore.Components.Forms.InputText Value="someValue" />
            <Microsoft.AspNetCore.Components.Forms.InputText Value="someValue"></Microsoft.AspNetCore.Components.Forms.InputText>

            <InputText
                Value="someValue"
                CssClass="my-class"
                DisplayName="My Input" />

            @typeof(InputText).ToString()
            @typeof(Microsoft.AspNetCore.Components.Forms.InputText).ToString()
            """;

        await VerifySemanticTokensAsync(input, colorBackground, miscellaneousFile);
    }

    [Theory]
    [CombinatorialData]
    public async Task Razor(bool colorBackground, bool miscellaneousFile)
    {
        var input = """
            @page "/"
            @using System
            @using System.Diagnostics

            <div>This is some HTML</div>

            <InputText Value="someValue" />

            @* hello there *@
            <!-- how are you? -->

            @if (true)
            {
                <text>Html!</text>
            }

            @code
            {
                [DebuggerDisplay("{GetDebuggerDisplay,nq}")]
                public class MyClass
                {
                }
            
                // I am also good, thanks for asking

                /*
                    No problem.
                */

                private string someValue;

                public void M()
                {
                    RenderFragment x = @<div>This is some HTML in a render fragment</div>;
                }
            }
            """;

        await VerifySemanticTokensAsync(input, colorBackground, miscellaneousFile);
    }

    [Theory]
    [CombinatorialData]
    public async Task Legacy(bool colorBackground, bool miscellaneousFile)
    {
        var input = """
            @page "/"
            @model AppThing.Model
            @using System

            <div>This is some HTML</div>

            <component type="typeof(Component)" render-mode="ServerPrerendered" />

            @functions
            {
                public void M()
                {
                }
            }

            @section MySection {
                <div>Section content</div>
            }
            """;

        await VerifySemanticTokensAsync(input, colorBackground, miscellaneousFile, fileKind: RazorFileKind.Legacy);
    }

    [Theory]
    [CombinatorialData]
    public async Task Legacy_Compatibility(bool colorBackground, bool miscellaneousFile)
    {
        // Same test as above, but with only the things that work in FUSE and non-FUSE, to prevent regressions

        var input = """
            @page "/"
            @using System

            <div>This is some HTML</div>

            <component type="typeof(Component)" render-mode="ServerPrerendered" />

            @functions
            {
                public void M()
                {
                }
            }
            """;

        await VerifySemanticTokensAsync(input, colorBackground, miscellaneousFile, fileKind: RazorFileKind.Legacy);
    }

    [Theory]
    [CombinatorialData]
    public async Task RenderMode(bool colorBackground, bool miscellaneousFile)
    {
        var input = """
            @rendermode Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer

            <!-- above and below should be classified the same -->

            @{
                var r = Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveAuto;
            }
            """;

        await VerifySemanticTokensAsync(input, colorBackground, miscellaneousFile);
    }

    [Theory]
    [CombinatorialData]
    public async Task RenderMode_Razor9(bool colorBackground, bool miscellaneousFile)
    {
        var input = """
            @rendermode Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer

            <!-- above and below should NOT be classified the same in Razor 9.0 -->

            @{
                var r = Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveAuto;
            }
            """;

        await VerifySemanticTokensAsync(
            input,
            colorBackground,
            miscellaneousFile,
            projectConfigure: p => p.RazorLanguageVersion = RazorLanguageVersion.Version_9_0);
    }

    [Theory]
    [CombinatorialData]
    public async Task RenderMode_Expression(bool colorBackground, bool miscellaneousFile)
    {
        var input = """
            @rendermode @(Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer)

            <!-- above and below should be classified the same -->

            @{
                var r = Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveAuto;
            }
            """;

        await VerifySemanticTokensAsync(input, colorBackground, miscellaneousFile);
    }

    [Theory]
    [CombinatorialData]
    public async Task RenderFragment(bool colorBackground, bool miscellaneousFile)
    {
        var input = """
            <div>This is some HTML</div>
            @code
            {
                public void M()
                {
                    RenderFragment x = @<div>This is some HTML</div>;
                }
            }
            """;

        await VerifySemanticTokensAsync(input, colorBackground, miscellaneousFile);
    }

    [Theory]
    [CombinatorialData]
    public async Task Expressions(bool colorBackground, bool miscellaneousFile)
    {
        var input = """
            @DateTime.Now
            
            @("hello" + "\\n" + "world" + Environment.NewLine + "how are you?")
            """;

        await VerifySemanticTokensAsync(input, colorBackground, miscellaneousFile);
    }

    [Theory]
    [CombinatorialData]
    public async Task GetSemanticTokens_Razor_NestedTextDirectives(bool colorBackground, bool miscellaneousFile)
    {
        var input = """
            @using System
            @functions {
                private void BidsByShipment(string generatedId, int bids)
                {
                    if (bids > 0)
                    {
                        <a class=""Thing"">
                            @if(bids > 0)
                            {
                                <text>@DateTime.Now</text>
                            }
                        </a>
                    }
                }
            }
            """;

        await VerifySemanticTokensAsync(input, colorBackground, miscellaneousFile);
    }

    [Theory]
    [CombinatorialData]
    public async Task GetSemanticTokens_Razor_NestedTransitions(bool colorBackground, bool miscellaneousFile)
    {
        var input = """
            @using System
            @code {
                Action<object> abc = @<span></span>;
            }
            """;

        await VerifySemanticTokensAsync(input, colorBackground, miscellaneousFile);
    }

    [Theory]
    [CombinatorialData]
    public async Task GetSemanticTokens_Razor_CommentAsync(bool colorBackground, bool miscellaneousFile)
    {
        var input = """
            @* A comment *@
            """;

        await VerifySemanticTokensAsync(input, colorBackground, miscellaneousFile);
    }

    [Theory]
    [CombinatorialData]
    public async Task GetSemanticTokens_Razor_MultiLineCommentMidlineAsync(bool colorBackground, bool miscellaneousFile)
    {
        var input = """
            <a />@* kdl
            skd
            slf*@
            """;

        await VerifySemanticTokensAsync(input, colorBackground, miscellaneousFile);
    }

    [Theory]
    [CombinatorialData]
    public async Task GetSemanticTokens_Razor_MultiLineCommentWithBlankLines(bool colorBackground, bool miscellaneousFile)
    {
        var input = """
            @* kdl

            skd
                
                    sdfasdfasdf
            slf*@
            """;

        await VerifySemanticTokensAsync(input, colorBackground, miscellaneousFile);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/razor/issues/8176")]
    public async Task GetSemanticTokens_Razor_MultiLineCommentWithBlankLines_LF(bool colorBackground, bool miscellaneousFile)
    {
        var input = "@* kdl\n\nskd\n    \n        sdfasdfasdf\nslf*@";

        await VerifySemanticTokensAsync(input, colorBackground, miscellaneousFile);
    }

    [Theory]
    [CombinatorialData]
    public async Task GetSemanticTokens_Razor_MultiLineCommentAsync(bool colorBackground, bool miscellaneousFile)
    {
        var input = """
            @*stuff
            things *@
            """;

        await VerifySemanticTokensAsync(input, colorBackground, miscellaneousFile);
    }

    [Theory]
    [CombinatorialData]
    public async Task GetSemanticTokens_CSharp_Static(bool colorBackground, bool miscellaneousFile)
    {
        var input = """
            @using System
            @code
            {
                private static bool _isStatic;

                public void M()
                {
                    if (_isStatic)
                    {
                    }
                }
            }
            """;

        await VerifySemanticTokensAsync(input, colorBackground, miscellaneousFile);
    }

    [Theory]
    [CombinatorialData]
    public async Task GetSemanticTokens_Legacy_Model(bool colorBackground, bool miscellaneousFile)
    {
        var input = """
            @using System
            @model SampleApp.Pages.ErrorModel

            <div>

                @{
                    @Model.ToString();
                }

            </div>
            """;

        await VerifySemanticTokensAsync(input, colorBackground, miscellaneousFile, fileKind: RazorFileKind.Legacy);
    }

    [Theory]
    [CombinatorialData]
    public async Task Obsolete(bool colorBackground, bool miscellaneousFile)
    {
        var input = """
            @using System

            <div>
                @status
            </div>

            @code
            {
                [Obsolete]
                private string status = "All good";
            }
            """;

        await VerifySemanticTokensAsync(input, colorBackground, miscellaneousFile);
    }

    private protected override async Task<SemanticTokens?> GetSemanticTokensAsync(TextDocument document, CancellationToken cancellationToken)
    {
        var sourceText = await document.GetTextAsync(cancellationToken);
        var span = new LinePositionSpan(new(0, 0), new(sourceText.Lines.Count, 0));
        var endpoint = new CohostSemanticTokensRangeEndpoint(IncompatibleProjectService, RemoteServiceInvoker, NoOpTelemetryReporter.Instance);
        return await endpoint.GetTestAccessor().HandleRequestAsync(document, span, cancellationToken);
    }
}
