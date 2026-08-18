// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostDocumentSpellCheckEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task Handle()
    {
        var input = """
            @page [|"this is csharp"|]

            <div>[|

                Eat more chickin.

            |]</div>

            <script>
                // no spell checking of script tags
                @([|"unless they contain csharp"|])
            </script>

            <style>
                // no spell checking of style tags
                @([|"unless they contain csharp"|])
            </style>

            @{ var [|x|] = [|"csharp"|];

            @*[| Eat more chickin. |]*@

            <div class="[|fush|]" />

            @code
            {
                void [|M|]()
                {
                    [|// Eat more chickin|]
                }
            }
            """;

        await VerifySpellCheckableRangesAsync(input);
    }

    [Fact]
    public async Task ComponentAttributes()
    {
        await VerifySpellCheckableRangesAsync(
            input: """
                <SurveyPrompt Title="[|Hello|][| there|]" />
                <SurveyPrompt @bind-Title="InputValue" />
            
                <form @onsubmit="DoSubmit" required></form>
            
                <input type="[|checkbox|]" checked></input>
            
                @code
                {
                    private string? [|InputValue|] { get; set; }
                }
            """,
            additionalFiles: [
                (FilePath("SurveyPrompt.razor"), """
                    @namespace SomeProject
                    
                    <div></div>
                    
                    @code
                    {
                        [Parameter]
                        public string Title { get; set; }
                    }
                    """)]);
    }

    private async Task VerifySpellCheckableRangesAsync(TestCode input, (string file, string contents)[]? additionalFiles = null)
    {
        var document = CreateProjectAndRazorDocument(input.Text, additionalFiles: additionalFiles);
        var sourceText = await document.GetTextAsync(DisposalToken);

        var endpoint = new CohostDocumentSpellCheckEndpoint(IncompatibleProjectService, RemoteServiceInvoker);

        var span = new LinePositionSpan(new(0, 0), new(sourceText.Lines.Count, 0));

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(document, DisposalToken);

        Assert.NotNull(result);
        var ranges = result.First().Ranges.AssumeNotNull();

        // To make for easier test failure analysis, we convert the ranges back to the test input, so we can show a diff
        // rather than "Expected 23, got 53" and leave the developer to deal with what that means.
        // As a bonus, this also ensures the ranges array has the right number of elements (ie, multiple of 3)
        var absoluteRanges = new List<(int Start, int End)>();
        var absoluteStart = 0;
        for (var i = 0; i < ranges.Length; i += 3)
        {
            var kind = ranges[i];
            var start = ranges[i + 1];
            var length = ranges[i + 2];

            absoluteStart += start;
            absoluteRanges.Add((absoluteStart, absoluteStart + length));
            absoluteStart += length;
        }

        // Make sure the response is sorted correctly, or the IDE will complain
        Assert.True(absoluteRanges.SequenceEqual(absoluteRanges.OrderBy(r => r.Start)), "Results are not in order!");

        absoluteRanges.Reverse();

        var actual = input.Text;
        foreach (var (start, end) in absoluteRanges)
        {
            actual = actual.Insert(end, "|]").Insert(start, "[|");
        }

        AssertEx.EqualOrDiff(input.OriginalInput, actual);
    }
}
