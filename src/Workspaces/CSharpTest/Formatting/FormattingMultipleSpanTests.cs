// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        public void EndOfLine()
        {
            var content = @"namespace A{/*1*/}/*2*/";
            var expected = @"namespace A{}";

            AssertFormat(content, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Simple1()
        {
            AssertFormat("namespace A/*1*/{}/*2*/ class A {}", "namespace A{ } class A {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DontFormatTriviaOutsideOfSpan_IncludingTrailingTriviaOnNewLine()
        {
            var content = @"namespace A
/*1*/{
        }/*2*/      

class A /*1*/{}/*2*/";

            var expected = @"namespace A
{
}      

class A { }";

            AssertFormat(content, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatIncludingTrivia()
        {
            var content = @"namespace A
/*1*/{
        }   /*2*/   

class A /*1*/{}/*2*/";

            var expected = @"namespace A
{
}

class A { }";

            AssertFormat(content, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void MergeSpanAndFormat()
        {
            var content = @"namespace A
/*1*/{
        }   /*2*/   /*1*/

class A{}/*2*/";

            var expected = @"namespace A
{
}

class A { }";

            AssertFormat(content, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void OverlappedSpan()
        {
            var content = @"namespace A
/*1*/{
     /*1*/   }   /*2*/   

class A{}/*2*/";

            var expected = @"namespace A
{
}

class A { }";

            AssertFormat(content, expected);
        }

        [Fact]
        [WorkItem(554160, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatSpanNullReference01()
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
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.IndentBlock, false);
            AssertFormat(code, expected, changedOptionSet: changingOptions);
        }

        [Fact]
        [WorkItem(554160, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatSpanNullReference02()
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
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.WrappingPreserveSingleLine, false);
            AssertFormat(code, expected, changedOptionSet: changingOptions);
        }

        [WorkItem(539231, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void EmptySpan()
        {
            using (var workspace = new AdhocWorkspace())
            {
                var project = workspace.CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.CSharp);
                var document = project.AddDocument("Document", SourceText.From(""));

                var syntaxTree = document.GetSyntaxTreeAsync().Result;
                var result = Formatter.Format(syntaxTree.GetRoot(CancellationToken.None), TextSpan.FromBounds(0, 0), workspace, cancellationToken: CancellationToken.None);
            }
        }

        private void AssertFormat(string content, string expected, Dictionary<OptionKey, object> changedOptionSet = null)
        {
            var tuple = PreprocessMarkers(content);

            AssertFormat(expected, tuple.Item1, tuple.Item2, changedOptionSet: changedOptionSet);
        }

        private Tuple<string, List<TextSpan>> PreprocessMarkers(string codeWithMarker)
        {
            int currentIndex = 0;
            var spans = new List<TextSpan>();

            while (currentIndex < codeWithMarker.Length)
            {
                int startPosition = codeWithMarker.IndexOf("/*1*/", currentIndex, StringComparison.Ordinal);
                if (startPosition < 0)
                {
                    // no more markers
                    break;
                }

                codeWithMarker = codeWithMarker.Substring(0, startPosition) + codeWithMarker.Substring(startPosition + 5);

                int endPosition = codeWithMarker.IndexOf("/*2*/", startPosition, StringComparison.Ordinal);

                codeWithMarker = codeWithMarker.Substring(0, endPosition) + codeWithMarker.Substring(endPosition + 5);

                spans.Add(TextSpan.FromBounds(startPosition, endPosition));

                currentIndex = startPosition;
            }

            return Tuple.Create(codeWithMarker, spans);
        }
    }
}
