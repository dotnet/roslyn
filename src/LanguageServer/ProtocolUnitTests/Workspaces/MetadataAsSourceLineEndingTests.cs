// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Workspaces;

public sealed class MetadataAsSourceLineEndingTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    [Theory]
    [InlineData(LanguageNames.CSharp, "\n")]
    [InlineData(LanguageNames.CSharp, "\r\n")]
    [InlineData(LanguageNames.VisualBasic, "\n")]
    [InlineData(LanguageNames.VisualBasic, "\r\n")]
    public async Task SimplificationPreservesConfiguredLineEndings(string language, string newLine)
    {
        var markup = $$"""
            <Workspace>
                <Project Language="{{language}}" CommonReferences="true">
                    <MetadataReferenceFromSource Language="C#" CommonReferences="true">
                        <Document>
                            namespace Referenced
                            {
                                public class C { public System.Reflection.AssemblyName Property { get; set; } }
                            }
                        </Document>
                    </MetadataReferenceFromSource>
                    <Document></Document>
                </Project>
            </Workspace>
            """;
        await using var server = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace: false);
        var document = server.GetCurrentSolution().Projects.Single().Documents.Single();
        var compilation = await document.Project.GetRequiredCompilationAsync(CancellationToken.None);
        var symbol = compilation.GetTypeByMetadataName("Referenced.C");
        Assert.NotNull(symbol);
        var options = (await document.GetSyntaxFormattingOptionsAsync(CancellationToken.None)) with
        {
            LineFormatting = LineFormattingOptions.Default with { NewLine = newLine },
        };
        var service = document.GetRequiredLanguageService<IMetadataAsSourceService>();
        var generated = await service.AddSourceToAsync(document, compilation, symbol, options, CancellationToken.None);
        var text = await generated.GetTextAsync(CancellationToken.None);
        Assert.Contains(language == LanguageNames.CSharp ? "using System.Reflection;" : "Imports System.Reflection", text.ToString());
        Assert.Contains("AssemblyName", text.ToString());

        foreach (var line in text.Lines)
        {
            if (line.EndIncludingLineBreak > line.End)
                Assert.Equal(newLine, text.ToString(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak)));
        }
    }
}
