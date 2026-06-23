// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test;

public class FindNodeTests(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private const string FetchDataContents = """
            @page "/fetchdata"
            @using BlazorApp.Data
            @inject WeatherForecastService ForecastService

            <PageTitle>Weather forecast</PageTitle>

            <h1>Weather forecast</h1>

            <p>This component demonstrates fetching data from a service.</p>

            @if (forecasts == null)
            {
                <p><em>Loading...</em></p>
            }
            else
            {
                <table class="table">
                    <thead>
                        <tr>
                            <th>Date</th>
                            <th>Temp. (C)</th>
                            <th>Temp. (F)</th>
                            <th>Summary</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var forecast in forecasts)
                        {
                            <tr>
                                <td>@forecast.Date.ToShortDateString()</td>
                                <td>@forecast.TemperatureC</td>
                                <td>@forecast.TemperatureF</td>
                                <td>@forecast.Summary</td>
                            </tr>
                        }
                    </tbody>
                </table>
            }

            @code {
                private WeatherForecast[]? forecasts;

                protected override async Task OnInitializedAsync()
                {
                    forecasts = await ForecastService.GetForecastAsync(DateOnly.FromDateTime(DateTime.Now));
                }
            }
            """;

    private static readonly string s_fetchDataContents = PlatformInformation.IsWindows
        ? FetchDataContents
        : FetchDataContents.Replace("\n", "\r\n");

    [Theory]
    [InlineData(0, 1, SyntaxKind.CSharpTransition, false)]
    [InlineData(0, 1, SyntaxKind.CSharpTransition, true)]
    [InlineData(1, 5, SyntaxKind.CSharpExpressionLiteral, false)]
    [InlineData(1, 5, SyntaxKind.CSharpExpressionLiteral, true)]
    [InlineData(5, 20, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(20, 21, SyntaxKind.CSharpTransition, false)]
    [InlineData(20, 21, SyntaxKind.CSharpTransition, true)]
    [InlineData(21, 41, SyntaxKind.CSharpStatementLiteral, false)]
    [InlineData(21, 41, SyntaxKind.CSharpStatementLiteral, true)]
    [InlineData(41, 43, SyntaxKind.RazorMetaCode, true)]
    [InlineData(43, 44, SyntaxKind.CSharpTransition, false)]
    [InlineData(43, 44, SyntaxKind.CSharpTransition, true)]
    [InlineData(44, 50, SyntaxKind.CSharpExpressionLiteral, false)]
    [InlineData(44, 50, SyntaxKind.CSharpExpressionLiteral, true)]
    [InlineData(50, 93, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(93, 104, SyntaxKind.MarkupStartTag, false)]
    [InlineData(93, 104, SyntaxKind.MarkupStartTag, true)]
    [InlineData(104, 120, SyntaxKind.MarkupTextLiteral, false)]
    [InlineData(104, 120, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(120, 132, SyntaxKind.MarkupEndTag, false)]
    [InlineData(120, 132, SyntaxKind.MarkupEndTag, true)]
    [InlineData(132, 136, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(136, 140, SyntaxKind.MarkupStartTag, false)]
    [InlineData(136, 140, SyntaxKind.MarkupStartTag, true)]
    [InlineData(140, 156, SyntaxKind.MarkupTextLiteral, false)]
    [InlineData(140, 156, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(156, 161, SyntaxKind.MarkupEndTag, false)]
    [InlineData(156, 161, SyntaxKind.MarkupEndTag, true)]
    [InlineData(161, 165, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(165, 168, SyntaxKind.MarkupStartTag, false)]
    [InlineData(165, 168, SyntaxKind.MarkupStartTag, true)]
    [InlineData(168, 225, SyntaxKind.MarkupTextLiteral, false)]
    [InlineData(168, 225, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(225, 229, SyntaxKind.MarkupEndTag, false)]
    [InlineData(225, 229, SyntaxKind.MarkupEndTag, true)]
    [InlineData(229, 233, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(233, 234, SyntaxKind.CSharpTransition, false)]
    [InlineData(233, 234, SyntaxKind.CSharpTransition, true)]
    [InlineData(234, 261, SyntaxKind.CSharpStatementLiteral, false)]
    [InlineData(234, 261, SyntaxKind.CSharpStatementLiteral, true)]
    [InlineData(265, 268, SyntaxKind.MarkupStartTag, false)]
    [InlineData(261, 265, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(265, 268, SyntaxKind.MarkupStartTag, true)]
    [InlineData(268, 272, SyntaxKind.MarkupStartTag, false)]
    [InlineData(268, 272, SyntaxKind.MarkupStartTag, true)]
    [InlineData(272, 282, SyntaxKind.MarkupTextLiteral, false)]
    [InlineData(272, 282, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(282, 287, SyntaxKind.MarkupEndTag, false)]
    [InlineData(282, 287, SyntaxKind.MarkupEndTag, true)]
    [InlineData(287, 291, SyntaxKind.MarkupEndTag, false)]
    [InlineData(287, 291, SyntaxKind.MarkupEndTag, true)]
    [InlineData(291, 293, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(293, 305, SyntaxKind.CSharpStatementLiteral, false)]
    [InlineData(293, 305, SyntaxKind.CSharpStatementLiteral, true)]
    [InlineData(309, 330, SyntaxKind.MarkupStartTag, false)]
    [InlineData(305, 309, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(309, 330, SyntaxKind.MarkupStartTag, true)]
    [InlineData(315, 316, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(316, 321, SyntaxKind.MarkupTextLiteral, false)]
    [InlineData(316, 321, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(315, 329, SyntaxKind.MarkupAttributeBlock, true)]
    [InlineData(322, 323, SyntaxKind.MarkupTextLiteral, false)]
    [InlineData(322, 323, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(323, 328, SyntaxKind.MarkupTextLiteral, false)]
    [InlineData(323, 328, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(328, 329, SyntaxKind.MarkupTextLiteral, false)]
    [InlineData(328, 329, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(330, 340, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(340, 347, SyntaxKind.MarkupStartTag, false)]
    [InlineData(340, 347, SyntaxKind.MarkupStartTag, true)]
    [InlineData(347, 361, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(361, 365, SyntaxKind.MarkupStartTag, false)]
    [InlineData(361, 365, SyntaxKind.MarkupStartTag, true)]
    [InlineData(365, 383, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(383, 387, SyntaxKind.MarkupStartTag, false)]
    [InlineData(383, 387, SyntaxKind.MarkupStartTag, true)]
    [InlineData(387, 391, SyntaxKind.MarkupTextLiteral, false)]
    [InlineData(387, 391, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(391, 396, SyntaxKind.MarkupEndTag, false)]
    [InlineData(391, 396, SyntaxKind.MarkupEndTag, true)]
    [InlineData(396, 414, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(414, 418, SyntaxKind.MarkupStartTag, false)]
    [InlineData(414, 418, SyntaxKind.MarkupStartTag, true)]
    [InlineData(418, 427, SyntaxKind.MarkupTextLiteral, false)]
    [InlineData(418, 427, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(427, 432, SyntaxKind.MarkupEndTag, false)]
    [InlineData(427, 432, SyntaxKind.MarkupEndTag, true)]
    [InlineData(432, 450, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(450, 454, SyntaxKind.MarkupStartTag, false)]
    [InlineData(450, 454, SyntaxKind.MarkupStartTag, true)]
    [InlineData(454, 463, SyntaxKind.MarkupTextLiteral, false)]
    [InlineData(454, 463, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(463, 468, SyntaxKind.MarkupEndTag, false)]
    [InlineData(463, 468, SyntaxKind.MarkupEndTag, true)]
    [InlineData(468, 486, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(486, 490, SyntaxKind.MarkupStartTag, false)]
    [InlineData(486, 490, SyntaxKind.MarkupStartTag, true)]
    [InlineData(490, 497, SyntaxKind.MarkupTextLiteral, false)]
    [InlineData(490, 497, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(497, 502, SyntaxKind.MarkupEndTag, false)]
    [InlineData(497, 502, SyntaxKind.MarkupEndTag, true)]
    [InlineData(502, 516, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(516, 521, SyntaxKind.MarkupEndTag, false)]
    [InlineData(516, 521, SyntaxKind.MarkupEndTag, true)]
    [InlineData(521, 531, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(531, 539, SyntaxKind.MarkupEndTag, false)]
    [InlineData(531, 539, SyntaxKind.MarkupEndTag, true)]
    [InlineData(539, 549, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(549, 556, SyntaxKind.MarkupStartTag, false)]
    [InlineData(549, 556, SyntaxKind.MarkupStartTag, true)]
    [InlineData(556, 558, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(570, 571, SyntaxKind.CSharpTransition, false)]
    [InlineData(558, 570, SyntaxKind.CSharpStatementLiteral, true)]
    [InlineData(570, 571, SyntaxKind.CSharpTransition, true)]
    [InlineData(571, 623, SyntaxKind.CSharpStatementLiteral, false)]
    [InlineData(571, 623, SyntaxKind.CSharpStatementLiteral, true)]
    [InlineData(639, 643, SyntaxKind.MarkupStartTag, false)]
    [InlineData(623, 639, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(639, 643, SyntaxKind.MarkupStartTag, true)]
    [InlineData(643, 665, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(665, 669, SyntaxKind.MarkupStartTag, false)]
    [InlineData(665, 669, SyntaxKind.MarkupStartTag, true)]
    [InlineData(669, 670, SyntaxKind.CSharpTransition, false)]
    [InlineData(669, 670, SyntaxKind.CSharpTransition, true)]
    [InlineData(670, 703, SyntaxKind.CSharpExpressionLiteral, false)]
    [InlineData(670, 703, SyntaxKind.CSharpExpressionLiteral, true)]
    [InlineData(703, 708, SyntaxKind.MarkupEndTag, false)]
    [InlineData(703, 708, SyntaxKind.MarkupEndTag, true)]
    [InlineData(708, 730, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(730, 734, SyntaxKind.MarkupStartTag, false)]
    [InlineData(730, 734, SyntaxKind.MarkupStartTag, true)]
    [InlineData(734, 735, SyntaxKind.CSharpTransition, false)]
    [InlineData(734, 735, SyntaxKind.CSharpTransition, true)]
    [InlineData(735, 756, SyntaxKind.CSharpExpressionLiteral, false)]
    [InlineData(735, 756, SyntaxKind.CSharpExpressionLiteral, true)]
    [InlineData(756, 761, SyntaxKind.MarkupEndTag, false)]
    [InlineData(756, 761, SyntaxKind.MarkupEndTag, true)]
    [InlineData(761, 783, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(783, 787, SyntaxKind.MarkupStartTag, false)]
    [InlineData(783, 787, SyntaxKind.MarkupStartTag, true)]
    [InlineData(787, 788, SyntaxKind.CSharpTransition, false)]
    [InlineData(787, 788, SyntaxKind.CSharpTransition, true)]
    [InlineData(788, 809, SyntaxKind.CSharpExpressionLiteral, false)]
    [InlineData(788, 809, SyntaxKind.CSharpExpressionLiteral, true)]
    [InlineData(809, 814, SyntaxKind.MarkupEndTag, false)]
    [InlineData(809, 814, SyntaxKind.MarkupEndTag, true)]
    [InlineData(814, 836, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(836, 840, SyntaxKind.MarkupStartTag, false)]
    [InlineData(836, 840, SyntaxKind.MarkupStartTag, true)]
    [InlineData(840, 841, SyntaxKind.CSharpTransition, false)]
    [InlineData(840, 841, SyntaxKind.CSharpTransition, true)]
    [InlineData(841, 857, SyntaxKind.CSharpExpressionLiteral, false)]
    [InlineData(841, 857, SyntaxKind.CSharpExpressionLiteral, true)]
    [InlineData(857, 862, SyntaxKind.MarkupEndTag, false)]
    [InlineData(857, 862, SyntaxKind.MarkupEndTag, true)]
    [InlineData(862, 880, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(880, 885, SyntaxKind.MarkupEndTag, false)]
    [InlineData(880, 885, SyntaxKind.MarkupEndTag, true)]
    [InlineData(885, 887, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(887, 902, SyntaxKind.CSharpStatementLiteral, false)]
    [InlineData(887, 902, SyntaxKind.CSharpStatementLiteral, true)]
    [InlineData(910, 918, SyntaxKind.MarkupEndTag, false)]
    [InlineData(902, 910, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(910, 918, SyntaxKind.MarkupEndTag, true)]
    [InlineData(918, 924, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(924, 932, SyntaxKind.MarkupEndTag, false)]
    [InlineData(924, 932, SyntaxKind.MarkupEndTag, true)]
    [InlineData(932, 934, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(934, 937, SyntaxKind.CSharpStatementLiteral, false)]
    [InlineData(934, 937, SyntaxKind.CSharpStatementLiteral, true)]
    [InlineData(939, 940, SyntaxKind.CSharpTransition, false)]
    [InlineData(937, 939, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(939, 940, SyntaxKind.CSharpTransition, true)]
    [InlineData(940, 944, SyntaxKind.CSharpExpressionLiteral, false)]
    [InlineData(940, 944, SyntaxKind.CSharpExpressionLiteral, true)]
    [InlineData(944, 1162, SyntaxKind.MarkupTextLiteral, true)]
    [InlineData(0, 1162, SyntaxKind.MarkupBlock, false)]
    internal void Test_On_FetchData(int start, int end, SyntaxKind kind, bool includeWhitespace)
        => Verify(s_fetchDataContents, start, end, kind, includeWhitespace, innermostForTie: true);

    private static void Verify(string input, int start, int end, SyntaxKind kind, bool includeWhitespace, bool innermostForTie)
    {
        var syntaxTree = RazorSyntaxTree.Parse(RazorSourceDocument.Create(input, "test.razor"));

        var node = syntaxTree.Root.FindNode(TextSpan.FromBounds(start, end), includeWhitespace, innermostForTie);

        Assert.NotNull(node);
        Assert.Equal(kind, node.Kind);
        Assert.Equal(start, node.Span.Start);
        Assert.Equal(end, node.Span.End);
    }
}
