// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.StringIndentation;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.StringIndentation;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.StringIndentation)]
public sealed class StringIndentationTests
{
    private static async Task TestAsync(string contents)
    {
        using var workspace = TestWorkspace.CreateWorkspace(
            TestWorkspace.CreateWorkspaceElement(LanguageNames.CSharp,
                files: [contents.Replace("|", " ")],
                isMarkup: false));
        var document = workspace.CurrentSolution.GetRequiredDocument(workspace.Documents.First().Id);
        var root = await document.GetRequiredSyntaxRootAsync(default);

        var service = document.GetRequiredLanguageService<IStringIndentationService>();
        var regions = await service.GetStringIndentationRegionsAsync(document, root.FullSpan, CancellationToken.None).ConfigureAwait(false);

        var actual = ApplyRegions(contents.Replace("|", " "), regions);
        Assert.Equal(contents, actual);
    }

    private static string ApplyRegions(string val, ImmutableArray<StringIndentationRegion> regions)
    {
        var text = SourceText.From(val);
        using var _ = ArrayBuilder<TextChange>.GetInstance(out var changes);

        foreach (var region in regions)
        {
            var firstLine = text.Lines.GetLineFromPosition(region.IndentSpan.Start);
            var lastLine = text.Lines.GetLineFromPosition(region.IndentSpan.End);
            var offset = region.IndentSpan.End - lastLine.Start;

            for (var i = firstLine.LineNumber + 1; i < lastLine.LineNumber; i++)
            {
                var lineStart = text.Lines[i].Start;
                if (region.OrderedHoleSpans.Any(s => s.Contains(lineStart)))
                    continue;

                changes.Add(new TextChange(new TextSpan(lineStart + offset - 1, 1), "|"));
            }
        }

        var changedText = text.WithChanges(changes);
        return changedText.ToString();
    }

    [Fact]
    public async Task TestEmptyFile()
        => await TestAsync(string.Empty);

    [Theory]
    [InlineData("")]
    [InlineData("u8")]
    public Task TestLiteralError1(string suffix)
        => TestAsync($$""""
            class C
            {
                void M()
                {
                    // not enough lines in literal
                    var v = """
                            """{{suffix}};
                }
            }
            """");

    [Theory]
    [InlineData("")]
    [InlineData("u8")]
    public Task TestLiteralError2(string suffix)
        => TestAsync($$""""
            class C
            {
                void M()
                {
                    // invalid literal
                    var v = """
                        text too early
                            """{{suffix}};
                }
            }
            """");

    [Theory]
    [InlineData("")]
    [InlineData("u8")]
    public Task TestZeroColumn1(string suffix)
        => TestAsync($$""""
            class C
            {
                void M()
                {
                    var v = """
            goo
            """{{suffix}};
                }
            }
            """");

    [Theory]
    [InlineData("")]
    [InlineData("u8")]
    public Task TestZeroColumn2(string suffix)
        => TestAsync($$""""
            class C
            {
                void M()
                {
                    var v = """
                goo
            """{{suffix}};
                }
            }
            """");

    [Theory]
    [InlineData("")]
    [InlineData("u8")]
    public Task TestOneColumn1(string suffix)
        => TestAsync($$""""
            class C
            {
                void M()
                {
                    var v = """
            |goo
             """{{suffix}};
                }
            }
            """");

    [Theory]
    [InlineData("")]
    [InlineData("u8")]
    public Task TestOneColumn2(string suffix)
        => TestAsync($$""""
            class C
            {
                void M()
                {
                    var v = """
            |   goo
             """{{suffix}};
                }
            }
            """");

    [Theory]
    [InlineData("")]
    [InlineData("u8")]
    public Task TestCase1(string suffix)
        => TestAsync($$""""
            class C
            {
                void M()
                {
                    var v = """
                           |goo
                            """{{suffix}};
                }
            }
            """");

    [Theory]
    [InlineData("")]
    [InlineData("u8")]
    public Task TestCase2(string suffix)
        => TestAsync($$""""
            class C
            {
                void M()
                {
                    var v = """
                           |goo
                           |bar
                            """{{suffix}};
                }
            }
            """");

    [Theory]
    [InlineData("")]
    [InlineData("u8")]
    public Task TestCase3(string suffix)
        => TestAsync($$""""
            class C
            {
                void M()
                {
                    var v = """
                           |goo
                           |bar
                           |baz
                            """{{suffix}};
                }
            }
            """");

    [Theory]
    [InlineData("")]
    [InlineData("u8")]
    public Task TestCase4(string suffix)
        => TestAsync($$""""
            class C
            {
                void M()
                {
                    var v = """
                           |goo
                           |
                           |baz
                            """{{suffix}};
                }
            }
            """");

    [Theory]
    [InlineData("")]
    [InlineData("u8")]
    public Task TestCase5(string suffix)
        => TestAsync($$""""
            class C
            {
                void M()
                {
                    var v = """
                       |    goo
                       |
                       |    baz
                        """{{suffix}};
                }
            }
            """");

    [Theory]
    [InlineData("")]
    [InlineData("u8")]
    public Task TestCase6(string suffix)
        => TestAsync($$""""
            class C
            {
                void M()
                {
                    var v =
                        $"""
                       |goo
                        """{{suffix}};
                }
            }
            """");

    [Theory]
    [InlineData("")]
    [InlineData("u8")]
    public Task TestCase7(string suffix)
        => TestAsync($$""""
            class C
            {
                void M()
                {
                    var v =
                        $"""
                        |goo
                         """{{suffix}};
                }
            }
            """");

    [Theory]
    [InlineData("")]
    [InlineData("u8")]
    public Task TestCase8(string suffix)
        => TestAsync($$"""""
            class C
            {
                void M()
                {
                    var v =
                        $""""
                        |goo
                         """"{{suffix}};
                }
            }
            """"");

    [Theory]
    [InlineData("")]
    [InlineData("u8")]
    public Task TestCase9(string suffix)
        => TestAsync($$"""""
            class C
            {
                void M()
                {
                    var v =
                         """"
                        |goo
                         """"{{suffix}};
                }
            }
            """"");

    [Fact]
    public Task TestCase10()
        => TestAsync("""""
            class C
            {
                void M()
                {
                    var v =
                         $$""""
                        |goo
                         """";
                }
            }
            """"");

    [Fact]
    public Task TestCase11()
        => TestAsync("""""
            class C
            {
                void M()
                {
                    var v =
                        $$""""
                        |goo
                         """";
                }
            }
            """"");

    [Fact]
    public Task TestCase12()
        => TestAsync("""""
            class C
            {
                void M()
                {
                    var v =
                       $$""""
                        |goo
                         """";
                }
            }
            """"");

    [Fact]
    public Task TestWithHoles1()
        => TestAsync(""""
            class C
            {
                void M()
                {
                    var v = $"""
                       |    goo
                       |    { 1 + 1 }
                       |    baz
                        """;
                }
            }
            """");

    [Fact]
    public Task TestWithHoles2()
        => TestAsync(""""
            class C
            {
                void M()
                {
                    var v = $"""
                       |    goo{
                       |    1 + 1
                       |    }baz
                        """;
                }
            }
            """");

    [Fact]
    public Task TestWithHoles3()
        => TestAsync(""""
            class C
            {
                void M()
                {
                    var v = $"""
                       |    goo{
                       |1 + 1
                       |    }baz
                        """;
                }
            }
            """");

    [Fact]
    public Task TestWithHoles4()
        => TestAsync(""""
            class C
            {
                void M()
                {
                    var v = $"""
                       |    goo{
                       1 + 1
                            }baz
                        """;
                }
            }
            """");

    [Fact]
    public Task TestWithHoles5()
        => TestAsync(""""
            class C
            {
                void M()
                {
                    var v = $"""
                       |    goo{
                       |1 + 1
                       |    }baz
                       |    quux
                        """;
                }
            }
            """");

    [Fact]
    public Task TestWithHoles6()
        => TestAsync(""""
            class C
            {
                void M()
                {
                    var v = $"""
                       |    goo{
                     1 + 1
                            }baz
                       |    quux
                        """;
                }
            }
            """");

    [Fact]
    public Task TestWithHoles7()
        => TestAsync(""""
            class C
            {
                void M()
                {
                    var v = $"""
                       |goo{
                     1 + 1
                     }baz
                       |quux
                        """;
                }
            }
            """");

    [Fact]
    public Task TestWithHoles8()
        => TestAsync(""""
            class C
            {
                void M()
                {
                    var v = $"""
                       |    { 1 + 1 }
                       |    baz
                        """;
                }
            }
            """");

    [Fact]
    public Task TestWithHoles9()
        => TestAsync(""""
            class C
            {
                void M()
                {
                    var v = $"""
                       |    {
                       |        1 + 1
                       |    }
                       |    baz
                        """;
                }
            }
            """");

    [Fact]
    public Task TestWithHoles10()
        => TestAsync(""""
            class C
            {
                void M()
                {
                    var v = $"""
                       |    {
                    1 + 1
                            }
                       |    baz
                        """;
                }
            }
            """");

    [Fact]
    public Task TestWithNestedHoles1()
        => TestAsync(""""
            class C
            {
                void M()
                {
                    var x =
                        $"""
                        |goo
                        |{
                        |   $"""
                        |   |bar
                        |    """
                        |}
                        |baz
                         """;
                }
            }
            """");

    [Fact]
    public Task TestWithNestedHoles2()
        => TestAsync(""""
            class C
            {
                void M()
                {
                    var x =
                        $"""
                        |goo
                        |{
                        |   $"""
                        |   |bar
                        |   |{
                        |   |   1 + 1
                        |   |}
                        |    """
                        |}
                        |baz
                         """;
                }
            }
            """");

    [Fact]
    public Task TestWithNestedHoles3()
        => TestAsync(""""
            class C
            {
                void M()
                {
                    var x =
                        $"""
                        |goo
                        |{
                        |   $"""
                        |   |bar
                        |   |{
                        |   1 + 1
                        |    }
                        |    """
                        |}
                        |baz
                         """;
                }
            }
            """");

    [Fact]
    public Task TestWithNestedHoles4()
        => TestAsync(""""
            class C
            {
                void M()
                {
                    var x =
                        $"""
                        |goo
                        |{
                            $"""
                            |bar
                            |{
                        1 + 1
                             }
                             """
                         }
                        |baz
                         """;
                }
            }
            """");

    [Fact]
    public Task TestWithNestedHoles5()
        => TestAsync(""""
            class C
            {
                void M()
                {
                    var x =
                        $"""
                        |goo
                        |{
                    $"""
                    |bar
                     """
                         }
                        |baz
                         """;
                }
            }
            """");

    [Fact]
    public Task TestWithNestedHoles6()
        => TestAsync(""""
            class C
            {
                void M()
                {
                    var x =
                        $"""
                        |goo
                        |{
                    $"""
                    |bar
                    |{
                    |   1 + 1
                    |}
                     """
                         }
                        |baz
                         """;
                }
            }
            """");

    [Fact]
    public Task TestWithNestedHoles7()
        => TestAsync(""""
            class C
            {
                void M()
                {
                    var x =
                        $"""
                        |goo
                        |{
                    $"""
                    |bar
                    |{
                    1 + 1
                     }
                     """
                         }
                        |baz
                         """;
                }
            }
            """");

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1542623")]
    public async Task TestWithManyConcatenatedStrings()
    {
        var input = new StringBuilder(
            """
            class C
            {
                void M()
                {
                    _ =
            """);

        for (var i = 0; i < 2000; i++)
        {
            input.AppendLine(
                """
                        @"" + "" + @"" + "" + @"" + "" + @"" + "" + @"" + "" + @"" + "" + @"" +
                """);
        }

        input.AppendLine(
            """
                    @"" + "" + @"" + "" + @"" + "" + @"" + "" + @"" + "" + @"" + "" + @"";
                }
            }
            """);

        await TestAsync(input.ToString());
    }
}
