// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Classification.Classifiers;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers;

/// <summary>
/// Classifier that provides syntax highlighting for C# code within &lt;code&gt; blocks in documentation comments.
/// This classifier works on the semantic level to properly classify C# code within doc comments.
/// </summary>
internal sealed class DocCommentCodeBlockClassifier : AbstractSyntaxClassifier
{
    public override ImmutableArray<Type> SyntaxNodeTypes { get; } = [typeof(XmlElementSyntax)];

    public override void AddClassifications(
        SyntaxNode syntax,
        TextSpan textSpan,
        SemanticModel semanticModel,
        ClassificationOptions options,
        SegmentedList<ClassifiedSpan> result,
        CancellationToken cancellationToken)
    {
        if (syntax is not XmlElementSyntax xmlElement)
            return;

        // Try to classify as C# code. If it fails for any reason, fall back to regular syntactic classification
        if (TryClassifyCodeBlock(xmlElement, textSpan, semanticModel, options, result, cancellationToken))
            return;

        // Fall back to syntactic classification of the element content
        Worker.CollectClassifiedSpans(xmlElement, textSpan, result, cancellationToken);
    }

    private static bool TryClassifyCodeBlock(
        XmlElementSyntax xmlElement,
        TextSpan textSpan,
        SemanticModel semanticModel,
        ClassificationOptions options,
        SegmentedList<ClassifiedSpan> result,
        CancellationToken cancellationToken)
    {
        // Check if this is a C# code block
        if (!ClassificationHelpers.IsCodeBlockWithCSharpLang(xmlElement))
            return false;

        // Extract the code content from the XML element
        if (!TryExtractCodeContent(xmlElement, semanticModel.SyntaxTree, out var virtualChars, out var contentSpan))
            return false;

        // Create a source text from the virtual chars
        var sourceText = new VirtualCharSequenceSourceText(virtualChars, semanticModel.SyntaxTree.Encoding);

        CSharpTestEmbeddedLanguageUtilities

        // Parse the C# code
        var testFileTree = SyntaxFactory.ParseSyntaxTree(sourceText, semanticModel.SyntaxTree.Options, cancellationToken: cancellationToken);
        var compilationWithTestFile = semanticModel.Compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(testFileTree);
        var semanticModelWithTestFile = compilationWithTestFile.GetSemanticModel(testFileTree);

        // Classify using syntactic classification only for now
        using var _ = Classifier.GetPooledList(out var classifiedSpans);

        // Add syntactic classifications
        Worker.CollectClassifiedSpans(
            testFileTree.GetRoot(cancellationToken),
            new TextSpan(0, virtualChars.Count),
            classifiedSpans,
            cancellationToken);

        // Map the classifications back to the original document positions
        foreach (var classifiedSpan in classifiedSpans)
        {
            if (classifiedSpan.TextSpan.Start >= virtualChars.Count)
                continue;

            // Map the classified span back to the original document
            AddMappedClassifications(virtualChars, classifiedSpan, textSpan, result);
        }

        return true;
    }

    private static bool TryExtractCodeContent(
        XmlElementSyntax xmlElement,
        SyntaxTree syntaxTree,
        out ImmutableSegmentedList<VirtualChar> virtualChars,
        out TextSpan contentSpan)
    {
        virtualChars = default;
        contentSpan = default;

        var builder = ImmutableSegmentedList.CreateBuilder<VirtualChar>();

        // Get the text content between the start and end tags
        foreach (var content in xmlElement.Content)
        {
            if (content is not XmlTextSyntax xmlText)
                continue;

            foreach (var token in xmlText.TextTokens)
            {
                // Get the token text
                var tokenText = token.Text;
                var tokenStart = token.Span.Start;

                // For each line in a doc comment, we need to skip the leading trivia (///)
                var leadingTrivia = token.LeadingTrivia;
                var offset = 0;
                foreach (var trivia in leadingTrivia)
                {
                    if (trivia.Kind() == SyntaxKind.DocumentationCommentExteriorTrivia)
                    {
                        // Skip the /// part in our offset calculation
                        offset += trivia.Span.Length;
                    }
                }

                // Add each character as a virtual char with its original span
                for (int i = 0; i < tokenText.Length; i++)
                {
                    var ch = tokenText[i];
                    builder.Add(new VirtualChar(new VirtualCharGreen(ch, offset + i, 1), tokenStart));
                }
            }
        }

        if (builder.Count == 0)
            return false;

        virtualChars = builder.ToImmutable();
        contentSpan = TextSpan.FromBounds(virtualChars[0].Span.Start, virtualChars[^1].Span.End);
        return true;
    }

    private static void AddMappedClassifications(
        ImmutableSegmentedList<VirtualChar> virtualChars,
        ClassifiedSpan classifiedSpan,
        TextSpan requestedSpan,
        SegmentedList<ClassifiedSpan> result)
    {
        if (classifiedSpan.TextSpan.IsEmpty)
            return;

        var classificationType = classifiedSpan.ClassificationType;
        var startIndexInclusive = classifiedSpan.TextSpan.Start;
        var endIndexExclusive = classifiedSpan.TextSpan.End;

        // The classified span may map to discontinuous regions in the original doc comment
        // (e.g., if there are line breaks with /// prefixes)
        var currentStartIndexInclusive = startIndexInclusive;
        while (currentStartIndexInclusive < endIndexExclusive)
        {
            var currentEndIndexExclusive = currentStartIndexInclusive + 1;

            // Find contiguous span
            while (currentEndIndexExclusive < endIndexExclusive &&
                   virtualChars[currentEndIndexExclusive - 1].Span.End == virtualChars[currentEndIndexExclusive].Span.Start)
            {
                currentEndIndexExclusive++;
            }

            var mappedSpan = TextSpan.FromBounds(
                virtualChars[currentStartIndexInclusive].Span.Start,
                virtualChars[currentEndIndexExclusive - 1].Span.End);

            // Only add if it intersects with the requested span
            if (mappedSpan.IntersectsWith(requestedSpan))
            {
                result.Add(new ClassifiedSpan(classificationType, mappedSpan));
            }

            currentStartIndexInclusive = currentEndIndexExclusive;
        }
    }
}
