// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
}
