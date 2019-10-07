// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting
{
    public class FormattingEngineMultiSpanTests : CSharpFormattingTestBase
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task EndOfLine()
        {
            var content = @"namespace A{/*1*/}/*2*/";
            var expected = @"namespace A{}";

            await AssertFormatAsync(content, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Simple1()
        {
            await AssertFormatAsync("namespace A/*1*/{}/*2*/ class A {}", "namespace A{ } class A {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task DontFormatTriviaOutsideOfSpan_IncludingTrailingTriviaOnNewLine()
        {
            var content = @"namespace A
/*1*/{
        }/*2*/      

class A /*1*/{}/*2*/";

            var expected = @"namespace A
{
}      

class A { }";

            await AssertFormatAsync(content, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatIncludingTrivia()
        {
            var content = @"namespace A
/*1*/{
        }   /*2*/   

class A /*1*/{}/*2*/";

            var expected = @"namespace A
{
}

class A { }";

            await AssertFormatAsync(content, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task MergeSpanAndFormat()
        {
            var content = @"namespace A
/*1*/{
        }   /*2*/   /*1*/

class A{}/*2*/";

            var expected = @"namespace A
{
}

class A { }";

            await AssertFormatAsync(content, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task OverlappedSpan()
        {
            var content = @"namespace A
/*1*/{
     /*1*/   }   /*2*/   

class A{}/*2*/";

            var expected = @"namespace A
{
}

class A { }";

            await AssertFormatAsync(content, expected);
        }

        [Fact]
        [WorkItem(554160, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554160")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatSpanNullReference01()
        {
            var code = @"/*1*/class C
{
    void F()
    {
        System.Console.WriteLine();
    }
}/*2*/";

            var expected = @"class C
{
    void F()
    {
    System.Console.WriteLine();
    }
}";
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.IndentBlock, false }
            };
            await AssertFormatAsync(code, expected, changedOptionSet: changingOptions);
        }

        [Fact]
        [WorkItem(554160, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554160")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatSpanNullReference02()
        {
            var code = @"class C/*1*/
{
    void F()
    {
        System.Console.WriteLine();
    }
}/*2*/";

            var expected = @"class C
{
    void F()
    {
        System.Console.WriteLine();
    }
}";
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.WrappingPreserveSingleLine, false }
            };
            await AssertFormatAsync(code, expected, changedOptionSet: changingOptions);
        }

        [WorkItem(539231, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539231")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task EmptySpan()
        {
            using var workspace = new AdhocWorkspace();

            var project = workspace.CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.CSharp);
            var document = project.AddDocument("Document", SourceText.From(""));

            var syntaxTree = await document.GetSyntaxTreeAsync();
            var result = Formatter.Format(await syntaxTree.GetRootAsync(), TextSpan.FromBounds(0, 0), workspace, cancellationToken: CancellationToken.None);
        }

        private Task AssertFormatAsync(string content, string expected, Dictionary<OptionKey, object> changedOptionSet = null)
        {
            var tuple = PreprocessMarkers(content);

            return AssertFormatAsync(expected, tuple.Item1, tuple.Item2, changedOptionSet: changedOptionSet);
        }

        private Tuple<string, List<TextSpan>> PreprocessMarkers(string codeWithMarker)
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

                codeWithMarker = codeWithMarker.Substring(0, startPosition) + codeWithMarker.Substring(startPosition + 5);

                var endPosition = codeWithMarker.IndexOf("/*2*/", startPosition, StringComparison.Ordinal);

                codeWithMarker = codeWithMarker.Substring(0, endPosition) + codeWithMarker.Substring(endPosition + 5);

                spans.Add(TextSpan.FromBounds(startPosition, endPosition));

                currentIndex = startPosition;
            }

            return Tuple.Create(codeWithMarker, spans);
        }
    }
}
