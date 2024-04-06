// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.StringIndentation;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.StringIndentation
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.StringIndentation)]
    public class StringIndentationTests
    {
        private static async Task TestAsync(string contents)
        {
            using var workspace = TestWorkspace.CreateWorkspace(
                TestWorkspace.CreateWorkspaceElement(LanguageNames.CSharp,
                    files: new[] { contents.Replace("|", " ") },
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
        public async Task TestLiteralError1(string suffix)
        {
            await TestAsync($@"class C
{{
    void M()
    {{
        // not enough lines in literal
        var v = """"""
                """"""{suffix};
    }}
}}");
        }

        [Theory]
        [InlineData("")]
        [InlineData("u8")]
        public async Task TestLiteralError2(string suffix)
        {
            await TestAsync($@"class C
{{
    void M()
    {{
        // invalid literal
        var v = """"""
            text too early
                """"""{suffix};
    }}
}}");
        }

        [Theory]
        [InlineData("")]
        [InlineData("u8")]
        public async Task TestZeroColumn1(string suffix)
        {
            await TestAsync($@"class C
{{
    void M()
    {{
        var v = """"""
goo
""""""{suffix};
    }}
}}");
        }

        [Theory]
        [InlineData("")]
        [InlineData("u8")]
        public async Task TestZeroColumn2(string suffix)
        {
            await TestAsync($@"class C
{{
    void M()
    {{
        var v = """"""
    goo
""""""{suffix};
    }}
}}");
        }

        [Theory]
        [InlineData("")]
        [InlineData("u8")]
        public async Task TestOneColumn1(string suffix)
        {
            await TestAsync($@"class C
{{
    void M()
    {{
        var v = """"""
|goo
 """"""{suffix};
    }}
}}");
        }

        [Theory]
        [InlineData("")]
        [InlineData("u8")]
        public async Task TestOneColumn2(string suffix)
        {
            await TestAsync($@"class C
{{
    void M()
    {{
        var v = """"""
|   goo
 """"""{suffix};
    }}
}}");
        }

        [Theory]
        [InlineData("")]
        [InlineData("u8")]
        public async Task TestCase1(string suffix)
        {
            await TestAsync($@"class C
{{
    void M()
    {{
        var v = """"""
               |goo
                """"""{suffix};
    }}
}}");
        }

        [Theory]
        [InlineData("")]
        [InlineData("u8")]
        public async Task TestCase2(string suffix)
        {
            await TestAsync($@"class C
{{
    void M()
    {{
        var v = """"""
               |goo
               |bar
                """"""{suffix};
    }}
}}");
        }

        [Theory]
        [InlineData("")]
        [InlineData("u8")]
        public async Task TestCase3(string suffix)
        {
            await TestAsync($@"class C
{{
    void M()
    {{
        var v = """"""
               |goo
               |bar
               |baz
                """"""{suffix};
    }}
}}");
        }

        [Theory]
        [InlineData("")]
        [InlineData("u8")]
        public async Task TestCase4(string suffix)
        {
            await TestAsync($@"class C
{{
    void M()
    {{
        var v = """"""
               |goo
               |
               |baz
                """"""{suffix};
    }}
}}");
        }

        [Theory]
        [InlineData("")]
        [InlineData("u8")]
        public async Task TestCase5(string suffix)
        {
            await TestAsync($@"class C
{{
    void M()
    {{
        var v = """"""
           |    goo
           |
           |    baz
            """"""{suffix};
    }}
}}");
        }

        [Theory]
        [InlineData("")]
        [InlineData("u8")]
        public async Task TestCase6(string suffix)
        {
            await TestAsync($@"class C
{{
    void M()
    {{
        var v =
            $""""""
           |goo
            """"""{suffix};
    }}
}}");
        }

        [Theory]
        [InlineData("")]
        [InlineData("u8")]
        public async Task TestCase7(string suffix)
        {
            await TestAsync($@"class C
{{
    void M()
    {{
        var v =
            $""""""
            |goo
             """"""{suffix};
    }}
}}");
        }

        [Theory]
        [InlineData("")]
        [InlineData("u8")]
        public async Task TestCase8(string suffix)
        {
            await TestAsync($@"class C
{{
    void M()
    {{
        var v =
            $""""""""
            |goo
             """"""""{suffix};
    }}
}}");
        }

        [Theory]
        [InlineData("")]
        [InlineData("u8")]
        public async Task TestCase9(string suffix)
        {
            await TestAsync($@"class C
{{
    void M()
    {{
        var v =
             """"""""
            |goo
             """"""""{suffix};
    }}
}}");
        }

        [Fact]
        public async Task TestCase10()
        {
            await TestAsync("""""
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
        }

        [Fact]
        public async Task TestCase11()
        {
            await TestAsync("""""
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
        }

        [Fact]
        public async Task TestCase12()
        {
            await TestAsync("""""
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
        }

        [Fact]
        public async Task TestWithHoles1()
        {
            await TestAsync(""""
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
        }

        [Fact]
        public async Task TestWithHoles2()
        {
            await TestAsync(""""
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
        }

        [Fact]
        public async Task TestWithHoles3()
        {
            await TestAsync(""""
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
        }

        [Fact]
        public async Task TestWithHoles4()
        {
            await TestAsync(""""
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
        }

        [Fact]
        public async Task TestWithHoles5()
        {
            await TestAsync(""""
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
        }

        [Fact]
        public async Task TestWithHoles6()
        {
            await TestAsync(""""
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
        }

        [Fact]
        public async Task TestWithHoles7()
        {
            await TestAsync(""""
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
        }

        [Fact]
        public async Task TestWithHoles8()
        {
            await TestAsync(""""
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
        }

        [Fact]
        public async Task TestWithHoles9()
        {
            await TestAsync(""""
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
        }

        [Fact]
        public async Task TestWithHoles10()
        {
            await TestAsync(""""
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
        }

        [Fact]
        public async Task TestWithNestedHoles1()
        {
            await TestAsync(""""
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
        }

        [Fact]
        public async Task TestWithNestedHoles2()
        {
            await TestAsync(""""
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
        }

        [Fact]
        public async Task TestWithNestedHoles3()
        {
            await TestAsync(""""
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
        }

        [Fact]
        public async Task TestWithNestedHoles4()
        {
            await TestAsync(""""
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
        }

        [Fact]
        public async Task TestWithNestedHoles5()
        {
            await TestAsync(""""
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
        }

        [Fact]
        public async Task TestWithNestedHoles6()
        {
            await TestAsync(""""
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
        }

        [Fact]
        public async Task TestWithNestedHoles7()
        {
            await TestAsync(""""
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
        }

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
}
