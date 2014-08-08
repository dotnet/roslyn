// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

internal static class Program
{
    private static void Main(string[] args)
    {
        TestFormatterAndClassifierAsync().Wait();
    }

    private static async Task TestFormatterAndClassifierAsync()
    {
        CustomWorkspace workspace = new CustomWorkspace();
        Solution solution = workspace.CurrentSolution;
        Project project = solution.AddProject("projectName", "assemblyName", LanguageNames.CSharp);
        Document document = project.AddDocument("name.cs", 
@"class C
{
static void Main()
{
WriteLine(""Hello, World!"");
}
}");
        document = await Formatter.FormatAsync(document);
        SourceText text = await document.GetTextAsync();

        IEnumerable<ClassifiedSpan> classifiedSpans = await Classifier.GetClassifiedSpansAsync(document, TextSpan.FromBounds(0, text.Length));
        Console.BackgroundColor = ConsoleColor.Black;

        var ranges = classifiedSpans.Select(classifiedSpan => 
            new Range(classifiedSpan, text.GetSubText(classifiedSpan.TextSpan).ToString()));

        ranges = FillGaps(text, ranges);

        foreach (Range range in ranges)
        {
            switch (range.ClassificationType)
            {
                case "keyword":
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    break;
                case "class name":
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case "string":
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }

            Console.Write(range.Text);
        }

        Console.ResetColor();
        Console.WriteLine();
    }

    private static IEnumerable<Range> FillGaps(SourceText text, IEnumerable<Range> ranges)
    {
        const string WhitespaceClassification = null;
        int current = 0;
        Range previous = null;

        foreach (Range range in ranges)
        {
            int start = range.TextSpan.Start;
            if (start > current)
            {
                yield return new Range(WhitespaceClassification, TextSpan.FromBounds(current, start), text);
            }

            if (previous == null || range.TextSpan != previous.TextSpan)
            {
                yield return range;
            }

            previous = range;
            current = range.TextSpan.End;
        }

        if (current < text.Length)
        {
            yield return new Range(WhitespaceClassification, TextSpan.FromBounds(current, text.Length), text);
        }
    }
}
