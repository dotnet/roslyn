// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting;

[Trait(Traits.Feature, Traits.Features.Formatting)]
public sealed class FormattingEngineMultiSpanTests : CSharpFormattingTestBase
{
    [Fact]
    public Task EndOfLine()
        => AssertFormatAsync(@"namespace A{/*1*/}/*2*/", @"namespace A{}");

    [Fact]
    public async Task Simple1()
        => await AssertFormatAsync("namespace A/*1*/{}/*2*/ class A {}", "namespace A{ } class A {}");

    [Fact]
    public Task DoNotFormatTriviaOutsideOfSpan_IncludingTrailingTriviaOnNewLine()
        => AssertFormatAsync("""
            namespace A
            /*1*/{
                    }/*2*/      

            class A /*1*/{}/*2*/
            """, """
            namespace A
            {
            }      

            class A { }
            """);

    [Fact]
    public Task FormatIncludingTrivia()
        => AssertFormatAsync("""
            namespace A
            /*1*/{
                    }   /*2*/   

            class A /*1*/{}/*2*/
            """, """
            namespace A
            {
            }

            class A { }
            """);

    [Fact]
    public Task MergeSpanAndFormat()
        => AssertFormatAsync("""
            namespace A
            /*1*/{
                    }   /*2*/   /*1*/

            class A{}/*2*/
            """, """
            namespace A
            {
            }

            class A { }
            """);

    [Fact]
    public Task OverlappedSpan()
        => AssertFormatAsync("""
            namespace A
            /*1*/{
                 /*1*/   }   /*2*/   

            class A{}/*2*/
            """, """
            namespace A
            {
            }

            class A { }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554160")]
    public async Task FormatSpanNullReference01()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.IndentBlock, false }
        };
        await AssertFormatAsync("""
            /*1*/class C
            {
                void F()
                {
                    System.Console.WriteLine();
                }
            }/*2*/
            """, """
            class C
            {
                void F()
                {
                System.Console.WriteLine();
                }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554160")]
    public async Task FormatSpanNullReference02()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.WrappingPreserveSingleLine, false }
        };
        await AssertFormatAsync("""
            class C/*1*/
            {
                void F()
                {
                    System.Console.WriteLine();
                }
            }/*2*/
            """, """
            class C
            {
                void F()
                {
                    System.Console.WriteLine();
                }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539231")]
    public async Task EmptySpan()
    {
        using var workspace = new AdhocWorkspace();

        var project = workspace.CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.CSharp);
        var document = project.AddDocument("Document", SourceText.From(""));

        var syntaxTree = await document.GetSyntaxTreeAsync();
        var root = await syntaxTree.GetRootAsync();
        var result = Formatter.Format(root, TextSpan.FromBounds(0, 0), workspace.Services.SolutionServices, CSharpSyntaxFormattingOptions.Default, CancellationToken.None);
    }

    private Task AssertFormatAsync(string content, string expected, OptionsCollection changedOptionSet = null)
    {
        var tuple = PreprocessMarkers(content);

        return AssertFormatAsync(expected, tuple.Item1, tuple.Item2, changedOptionSet: changedOptionSet);
    }

    private static Tuple<string, List<TextSpan>> PreprocessMarkers(string codeWithMarker)
    {
        var currentIndex = 0;
        var spans = new List<TextSpan>();

        while (currentIndex < codeWithMarker.Length)
        {
            var startPosition = codeWithMarker.IndexOf("/*1*/", currentIndex, StringComparison.Ordinal);
            if (startPosition < 0)
            {
                // no more markers
                break;
            }

            codeWithMarker = codeWithMarker[..startPosition] + codeWithMarker[(startPosition + 5)..];

            var endPosition = codeWithMarker.IndexOf("/*2*/", startPosition, StringComparison.Ordinal);

            codeWithMarker = codeWithMarker[..endPosition] + codeWithMarker[(endPosition + 5)..];

            spans.Add(TextSpan.FromBounds(startPosition, endPosition));

            currentIndex = startPosition;
        }

        return Tuple.Create(codeWithMarker, spans);
    }
}
