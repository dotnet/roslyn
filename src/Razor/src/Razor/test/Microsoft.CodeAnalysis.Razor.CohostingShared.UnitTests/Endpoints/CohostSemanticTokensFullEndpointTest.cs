// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostSemanticTokensFullEndpointTest(ITestOutputHelper testOutputHelper) : CohostSemanticTokensEndpointTestBase(testOutputHelper)
{
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

    private protected override Task<SemanticTokens?> GetSemanticTokensAsync(TextDocument document, CancellationToken cancellationToken)
    {
        var endpoint = new CohostSemanticTokensFullEndpoint(IncompatibleProjectService, RemoteServiceInvoker, NoOpTelemetryReporter.Instance);

        return endpoint.GetTestAccessor().HandleRequestAsync(document, cancellationToken);
    }
}
