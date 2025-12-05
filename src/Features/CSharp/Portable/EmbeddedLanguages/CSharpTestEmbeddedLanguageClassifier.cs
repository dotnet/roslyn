// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Features.EmbeddedLanguages;

using static VirtualCharUtilities;

[ExportEmbeddedLanguageClassifier(
    PredefinedEmbeddedLanguageNames.CSharpTest, [LanguageNames.CSharp], supportsUnannotatedAPIs: false,
    PredefinedEmbeddedLanguageNames.CSharpTest, LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpTestEmbeddedLanguageClassifier() : IEmbeddedLanguageClassifier
{
    public void RegisterClassifications(EmbeddedLanguageClassificationContext context)
    {
        var cancellationToken = context.CancellationToken;

        var token = context.SyntaxToken;
        var semanticModel = context.SemanticModel;

        if (token.Kind() is not (SyntaxKind.StringLiteralToken or SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken))
            return;

        var virtualCharsWithMarkup = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
        if (virtualCharsWithMarkup.IsDefaultOrEmpty())
            return;

        // Note: if we get here, then we know that the token is well formed (TryConvertToVirtualChars will fail if
        // the token has diagnostics).

        cancellationToken.ThrowIfCancellationRequested();
        using var _ = ArrayBuilder<VirtualChar>.GetInstance(virtualCharsWithMarkup.Length, out var virtualCharsBuilder);
        foreach (var vc in virtualCharsWithMarkup)
            virtualCharsBuilder.Add(vc);

        // Break the full sequence of virtual chars into the actual C# code and the markup
        var (virtualCharsWithoutMarkup, markdownSpans) = StripMarkupCharacters(virtualCharsBuilder, cancellationToken);

        // First, add all the markdown components (`$$`, `[|`, etc.) into the result.
        foreach (var span in markdownSpans)
            context.AddClassification(ClassificationTypeNames.TestCodeMarkdown, span);

        // Next, fill in everything with the "TestCode" classification.  This will ensure it gets the right
        // background highlighting.  For raw strings we don't want to classify the full lines with this background
        // color.  Only the parts to the right of the "start column" designation for that raw string.

        if (token.Kind() is SyntaxKind.MultiLineRawStringLiteralToken)
        {
            var text = semanticModel.SyntaxTree.GetText(cancellationToken);
            var lines = text.Lines;
            var firstLine = lines.GetLineFromPosition(token.Span.Start);
            var lastLine = lines.GetLineFromPosition(token.Span.End);
            var whitespaceCount = 0;
            for (var i = lastLine.Start; i < lastLine.End && SyntaxFacts.IsWhitespace(text[i]); i++)
                whitespaceCount++;

            for (var i = firstLine.LineNumber + 1; i < lastLine.LineNumber; i++)
            {
                var currentLine = lines[i];
                if (currentLine.Start + whitespaceCount < currentLine.End)
                {
                    context.AddClassification(
                        ClassificationTypeNames.TestCode,
                        TextSpan.FromBounds(
                            currentLine.Start + whitespaceCount,
                            currentLine.End));
                }
            }
        }
        else if (virtualCharsWithoutMarkup.Count > 0)
        {
            context.AddClassification(
                ClassificationTypeNames.TestCode,
                TextSpan.FromBounds(
                    virtualCharsWithoutMarkup[0].Span.Start,
                    virtualCharsWithoutMarkup[^1].Span.End));
        }

        // Next, get all the embedded language classifications for the test file.  Combine these with the markdown
        // components. Note: markdown components may be in between individual language components.  For example
        // `ret$$urn`.  This will break the `return` classification into two individual classifications around the
        // `$$` classification.
        var testFileClassifiedSpans = CSharpTestEmbeddedLanguageUtilities.GetTestFileClassifiedSpans(
            context.SolutionServices, semanticModel, virtualCharsWithoutMarkup, cancellationToken);

        CSharpTestEmbeddedLanguageUtilities.AddClassifications(
            virtualCharsWithoutMarkup, testFileClassifiedSpans,
            static (context, classificationType, span) => context.AddClassification(classificationType, span),
            context);
    }
}
