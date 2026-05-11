// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostFoldingRangeEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public Task BadLooseFileUri()
       => VerifyFoldingRangesAsync("""
            <div>
              @if (true) {[|
                <div>
                  Hello World
                </div>
              }
            |]</div>

            @if (true) {[|
              <div>
                Hello World
              </div>
            }
            |]
            @if (true) {[|
            }|]
            """,
           fileKind: RazorFileKind.Legacy,
           miscellaneousFile: true,
           razorFilePath: "git:/c:/Users/dawengie/source/repos/razor01/Pages/Index.cshtml?%7B%22path%22:%22c:%5C%5CUsers%5C%5Cdawengie%5C%5Csource%5C%5Crepos%5C%5Crazor01%5C%5CPages%5C%5CIndex.cshtml%22,%22ref%22:%22~%22%7D");

    [Theory]
    [CombinatorialData]
    public Task IfStatements(bool miscellaneousFile)
        => VerifyFoldingRangesAsync("""
            <div>
              @if (true) {[|
                <div>
                  Hello World
                </div>
              }
            |]</div>

            @if (true) {[|
              <div>
                Hello World
              </div>
            }
            |]
            @if (true) {[|
            }|]
            """,
            miscellaneousFile: miscellaneousFile);

    [Fact]
    public Task LockStatement()
        => VerifyFoldingRangesAsync("""
            @lock (new object()) {[|
            }|]
            """);

    [Fact]
    public Task UsingStatement()
      => VerifyFoldingRangesAsync("""
            @using (new object()) {[|
            }|]
            """);

    [Fact]
    public Task IfElseStatements()
        => VerifyFoldingRangesAsync("""
            <div>
              @if (true) {[|
                <div>
                  Hello World
                </div>
                } else {[|
                <div>
                    Goodbye World
                </div>
                }|]
            |]  }
            </div>
            """);

    [Fact]
    public Task Usings()
        => VerifyFoldingRangesAsync("""
            @using System{|imports:
            @using System.Text|}

            <p>hello!</p>

            @using System.Buffers{|imports:
            @using System.Drawing
            @using System.CodeDom|}

            <p>hello!</p>
            """);

    [Fact]
    public Task CSharpStatement()
        => VerifyFoldingRangesAsync("""
            <p>hello!</p>

            @{[|
                var helloWorld = "";
            }|]

            @(DateTime
                .Now)

            <p>hello!</p>
            """);

    [Fact]
    public Task CSharpStatement_Nested()
        => VerifyFoldingRangesAsync("""
            <p>hello!</p>

            <div>

                @{[|
                    var helloWorld = "";
                }|]

            </div>

            @(DateTime
                .Now)

            <p>hello!</p>
            """);

    [Fact]
    public Task CSharpStatement_NotSingleLine()
        => VerifyFoldingRangesAsync("""
            <p>hello!</p>

            @{ var helloWorld = ""; }

            <p>hello!</p>
            """);

    [Fact]
    public Task CodeBlock()
        => VerifyFoldingRangesAsync("""
            <p>hello!</p>

            @code {[|
                private string helloWorld = "";
            }|]

            <p>hello!</p>
            """);

    [Fact]
    public Task CodeBlock_Mvc()
        => VerifyFoldingRangesAsync("""
            <p>hello!</p>

            @functions {[|
                private string helloWorld = "";
            }|]

            <p>hello!</p>
            """,
            fileKind: RazorFileKind.Legacy);

    [Fact]
    public Task Section()
        => VerifyFoldingRangesAsync("""
            <p>hello!</p>

            @section Hello {[|
                <p>Hello</p>
            }|]

            <p>hello!</p>
            """,
            fileKind: RazorFileKind.Legacy);

    [Fact]
    public Task Section_Invalid()
        => VerifyFoldingRangesAsync("""
            <p>hello!</p>

            @section {
                <p>Hello</p>
            }

            <p>hello!</p>
            """,
            fileKind: RazorFileKind.Legacy);

    [Fact]
    public Task CSharpCodeInCodeBlocks()
       => VerifyFoldingRangesAsync("""
            <div>
              Hello @_name
            </div>

            @code {[|
                private string _name = "Dave";

                public void M() {{|implementation:
                }|}
            }|]
            """);

    [Fact]
    public Task HtmlAndCSharp()
      => VerifyFoldingRangesAsync("""
            <div>{|html:
              Hello @_name

                <div>{|html:
                    Nests aren't just for birds!
                </div>|}
            </div>|}

            @code {[|
                private string _name = "Dave";

                public void M() {{|implementation:
                }|}
            }|]
            """);

    [Fact]
    public Task CSharp_LineFoldingOnly()
        => VerifyFoldingRangesAsync("""
            <div>{|html:
              Hello @_name
            </div>|}

            @code {[|
                class C { public void M1() {{|implementation:
                        var x = 1;
            |}        }
                }
            }|]
            """,
            lineFoldingOnly: true);

    [Fact]
    public Task CSharp_NotLineFoldingOnly()
    => VerifyFoldingRangesAsync("""
            <div>{|html:
              Hello @_name
            </div>|}

            @code {[|
                class C { public void M1() {[|
                        var x = 1;
                    }
                }|]
            }|]
            """,
        lineFoldingOnly: false);

    [Fact]
    public Task IfElseStatements_LineFoldingOnly()
      => VerifyFoldingRangesAsync("""
            <div>
              @if (true) {[|
                <div>
                  Hello World
                </div>
                } else {[|
                <div>
                    Goodbye World
                </div>
            |]    }
            |]  }
            </div>

            @code[|
            {
                void M(){|implementation:
                {
                    if (true) {[|
            |]            var x = 1;
                    } else {[|
                        var y = 2;
            |]        }
            |}    }
            }|]
            """,
            lineFoldingOnly: true);

    [Fact]
    public Task CSharpExpressionBodiedMethods()
   => VerifyFoldingRangesAsync("""
            <p>hello!</p>

            @code {[|
                private void M(){|implementation:
                {
                }|}

                private Func<object, int> M1() => __builder => {{|implementation:
                    var x = 1;
                    var y = 2;
                    var z = x + y;
                    x = y + z;
                    y = x + z;
                    return 42;
                };|}

                private Func<object, int> M2() => __builder =>{|implementation:
                {
                    var x = 1;
                    var y = 2;
                    var z = x + y;
                    x = y + z;
                    y = x + z;
                    return 42;
                };|}

                private Func<object, int> M2() =>{|implementation:
                __builder =>[|
                {
                    var x = 1;
                    var y = 2;
                    var z = x + y;
                    x = y + z;
                    y = x + z;
                    return 42;
                };|}|]


                private RenderFragment N3() => __builder =>{|implementation:
                {
                    var test = "Hello";
                };|}

                private RenderFragment N4() => __builder =>{|implementation:
                {
                    var test = "Hello";
                    <div>@test</div>
                };|}
            }|]

            <p>hello!</p>
            """);

    private async Task VerifyFoldingRangesAsync(string input, RazorFileKind? fileKind = null, bool miscellaneousFile = false, string? razorFilePath = null, bool lineFoldingOnly = false)
    {
        UpdateClientLSPInitializationOptions(c =>
        {
            c.ClientCapabilities.TextDocument!.FoldingRange = new FoldingRangeSetting()
            {
                LineFoldingOnly = lineFoldingOnly,
            };
            return c;
        });

        TestFileMarkupParser.GetSpans(input, out var source, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
        var document = CreateProjectAndRazorDocument(source, fileKind, miscellaneousFile: miscellaneousFile, documentFilePath: razorFilePath);
        var inputText = await document.GetTextAsync(DisposalToken);

        var htmlSpans = spans.GetValueOrDefault("html").NullToEmpty();
        var htmlRanges = htmlSpans
            .Select(span =>
                {
                    var (start, end) = inputText.GetLinePositionSpan(span);
                    return new FoldingRange()
                    {
                        StartLine = start.Line,
                        StartCharacter = start.Character,
                        EndLine = end.Line,
                        EndCharacter = end.Character
                    };
                })
            .ToArray();

        var requestInvoker = new TestHtmlRequestInvoker([(Methods.TextDocumentFoldingRangeName, htmlRanges)]);

        var endpoint = new CohostFoldingRangeEndpoint(IncompatibleProjectService, RemoteServiceInvoker, requestInvoker, LoggerFactory);

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(document, DisposalToken);

        if (spans.Count == 0)
        {
            Assert.Null(result);
            return;
        }

        var actual = GenerateTestInput(inputText, htmlSpans, result.AssumeNotNull());

        AssertEx.EqualOrDiff(input, actual);
    }

    private static string GenerateTestInput(SourceText inputText, ImmutableArray<TextSpan> htmlSpans, FoldingRange[] result)
    {
        var markerPositions = result
            .SelectMany(r =>
                new[] {
                    (index: inputText.GetRequiredAbsoluteIndex(r.StartLine, r.StartCharacter.AssumeNotNull()), isStart: true, r.Kind),
                    (index: inputText.GetRequiredAbsoluteIndex(r.EndLine, r.EndCharacter.AssumeNotNull()), isStart: false, r.Kind)
                });

        var actual = new StringBuilder(inputText.ToString());
        foreach (var (index, isStart, kind) in markerPositions.OrderByDescending(p => p.index))
        {
            actual.Insert(index, GetMarker(index, isStart, htmlSpans, kind));
        }

        static string GetMarker(int index, bool isStart, ImmutableArray<TextSpan> htmlSpans, FoldingRangeKind? kind)
        {
            if (isStart)
            {
                return htmlSpans.Any(r => r.Start == index)
                    ? "{|html:"
                    : kind is null
                        ? "[|"
                        : $"{{|{kind.Value.Value}:";
            }

            return htmlSpans.Any(r => r.End == index)
                ? "|}"
                : kind is null
                    ? "|]"
                    : "|}";
        }

        return actual.ToString();
    }
}
