// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages;

internal static class CSharpTestEmbeddedLanguageUtilities
{
    public static IEnumerable<ClassifiedSpan> GetTestFileClassifiedSpans(
        Host.SolutionServices solutionServices, SemanticModel semanticModel,
        ImmutableSegmentedList<VirtualChar> virtualCharsWithoutMarkup, CancellationToken cancellationToken)
    {
        var compilation = semanticModel.Compilation;
        var encoding = semanticModel.SyntaxTree.Encoding;
        var testFileSourceText = new VirtualCharSequenceSourceText(virtualCharsWithoutMarkup, encoding);

        var testFileTree = SyntaxFactory.ParseSyntaxTree(testFileSourceText, semanticModel.SyntaxTree.Options, cancellationToken: cancellationToken);
        var compilationWithTestFile = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(testFileTree);
        var semanticModeWithTestFile = compilationWithTestFile.GetSemanticModel(testFileTree);

        var testFileClassifiedSpans = Classifier.GetClassifiedSpans(
            solutionServices,
            project: null,
            semanticModeWithTestFile,
            new TextSpan(0, virtualCharsWithoutMarkup.Count),
            ClassificationOptions.Default,
            cancellationToken);
        return testFileClassifiedSpans;
    }

    public static void AddClassifications<TArgs>(
        ImmutableSegmentedList<VirtualChar> virtualChars,
        IEnumerable<ClassifiedSpan> classifiedSpans,
        Action<TArgs, string, TextSpan> addClassification,
        TArgs args)
    {
        foreach (var classifiedSpan in classifiedSpans)
            AddClassifications(virtualChars, classifiedSpan, addClassification, args);
    }

    private static void AddClassifications<TArgs>(
        ImmutableSegmentedList<VirtualChar> virtualChars,
        ClassifiedSpan classifiedSpan,
        Action<TArgs, string, TextSpan> addClassification,
        TArgs args)
    {
        if (classifiedSpan.TextSpan.IsEmpty)
            return;

        // The classified span in C# may actually spread over discontinuous chunks when mapped back to the original
        // virtual chars in the C#-Test content.  For example: `yield ret$$urn;`  There will be a classified span
        // for `return` that has span [6, 12) (exactly the 6 characters corresponding to the contiguous 'return'
        // seen). However, those positions will map to the two virtual char spans [6, 9) and [11, 14).

        var classificationType = classifiedSpan.ClassificationType;
        var startIndexInclusive = classifiedSpan.TextSpan.Start;
        var endIndexExclusive = classifiedSpan.TextSpan.End;

        var currentStartIndexInclusive = startIndexInclusive;
        while (currentStartIndexInclusive < endIndexExclusive)
        {
            var currentEndIndexExclusive = currentStartIndexInclusive + 1;

            while (currentEndIndexExclusive < endIndexExclusive &&
                   virtualChars[currentEndIndexExclusive - 1].Span.End == virtualChars[currentEndIndexExclusive].Span.Start)
            {
                currentEndIndexExclusive++;
            }

            addClassification(args, classificationType,
                VirtualCharUtilities.FromBounds(virtualChars[currentStartIndexInclusive], virtualChars[currentEndIndexExclusive - 1]));
            currentStartIndexInclusive = currentEndIndexExclusive;
        }
    }
}
