// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers;

using static VirtualCharUtilities;

/// <summary>
/// Classifier that provides syntax highlighting for C# code within &lt;code&gt; blocks in documentation comments.
/// This classifier works on the semantic level to properly classify C# code within doc comments.
/// </summary>
internal sealed class DocCommentCodeBlockClassifier(SolutionServices solutionServices) : AbstractSyntaxClassifier
{
    private readonly SolutionServices _solutionServices = solutionServices;

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

        var (isCSharp, isCSharpTest) = ClassificationHelpers.IsCodeBlockWithCSharpLang(xmlElement);
        if (!isCSharp && !isCSharpTest)
            return;

        // Try to classify as C# code. If it fails for any reason, fall back to regular syntactic classification
        if (TryClassifyCodeBlock(xmlElement, textSpan, semanticModel, result, isTest: isCSharpTest, cancellationToken))
            return;

        // Normal syntactic classifier will have classified everything but the xml text tokens.  Recurse ourselves to
        // take case of that.
        ProcessTextTokens(
            xmlElement,
            textSpan,
            static (result, token) =>
            {
                result.Add(new(token.Span, ClassificationTypeNames.XmlDocCommentText));
                return true;
            },
            result,
            cancellationToken);
    }

    private static bool ProcessTextTokens<TArgs>(
        XmlElementSyntax xmlElement,
        TextSpan textSpan,
        Func<TArgs, SyntaxToken, bool> processToken,
        TArgs arg,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var stack);
        for (var i = xmlElement.Content.Count - 1; i >= 0; i--)
            stack.Push(xmlElement.Content[i]);

        while (stack.TryPop(out var current))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (current.AsNode(out var currentNode))
            {
                foreach (var child in currentNode.ChildNodesAndTokens().Reverse())
                {
                    if (child.FullSpan.OverlapsWith(textSpan))
                        stack.Push(child);
                }
            }
            else if (current.Kind() is SyntaxKind.XmlEntityLiteralToken or SyntaxKind.XmlTextLiteralToken or SyntaxKind.XmlTextLiteralNewLineToken)
            {
                if (!processToken(arg, current.AsToken()))
                    return false;
            }
        }

        return true;
    }

    private bool TryClassifyCodeBlock(
        XmlElementSyntax xmlElement,
        TextSpan textSpan,
        SemanticModel semanticModel,
        SegmentedList<ClassifiedSpan> result,
        bool isTest,
        CancellationToken cancellationToken)
    {
        // Extract the code content from the XML element
        using var _ = ArrayBuilder<VirtualChar>.GetInstance(out var virtualCharsBuilder);

        ExtractTextTokenVirtualChars(xmlElement, textSpan, virtualCharsBuilder, cancellationToken);
        if (virtualCharsBuilder.Count == 0)
            return false;

        // If this is C#-test break the full sequence of virtual chars into the actual C# code and the markup.
        // Otherwise, process the code as-is.
        var (virtualCharsWithoutMarkup, markdownSpans) = isTest
            ? StripMarkupCharacters(virtualCharsBuilder, cancellationToken)
            : (ImmutableSegmentedList.CreateRange(virtualCharsBuilder), []);

        // First, add all the markdown components (`$$`, `[|`, etc.) into the result.
        foreach (var span in markdownSpans)
            result.Add(new(ClassificationTypeNames.TestCodeMarkdown, span));

        // Next, fill in everything with the "TestCode" classification.  This will ensure it gets the right background
        // highlighting, making it easier to distinguish for normal C# code. 
        AddTestCodeBackgroundClassification(result, virtualCharsBuilder);

        var classifiedSpans = CSharpTestEmbeddedLanguageUtilities.GetTestFileClassifiedSpans(
            _solutionServices, semanticModel, virtualCharsWithoutMarkup, cancellationToken);

        CSharpTestEmbeddedLanguageUtilities.AddClassifications(
            virtualCharsWithoutMarkup,
            classifiedSpans,
            static (result, classificationType, span) => result.Add(new(classificationType, span)),
            result);

        return true;
    }

    private static void AddTestCodeBackgroundClassification(SegmentedList<ClassifiedSpan> result, ArrayBuilder<VirtualChar> virtualCharsBuilder)
    {
        var skipFirstSpace = true;
        for (var i = 0; i < virtualCharsBuilder.Count;)
        {
            var ch = virtualCharsBuilder[i++];

            // Skip the immediately following space after the `///` to make the highlighted code look cleaner and stand
            // out better from teh rest of the doc comment.
            if (ch == ' ' && skipFirstSpace)
            {
                skipFirstSpace = false;
                continue;
            }

            var start = ch.Span.Start;
            var end = ch.Span.End;

            while (i < virtualCharsBuilder.Count && virtualCharsBuilder[i] is var nextCh && nextCh.Span.Start == end)
            {
                i++;
                end = nextCh.Span.End;
                skipFirstSpace = true;
            }

            result.Add(new(
                ClassificationTypeNames.TestCode,
                TextSpan.FromBounds(start, end)));
        }
    }

    private static bool ExtractTextTokenVirtualChars(
        XmlElementSyntax xmlElement, TextSpan textSpan, ArrayBuilder<VirtualChar> virtualCharsBuilder, CancellationToken cancellationToken)
    {
        return ProcessTextTokens(
            xmlElement,
            textSpan,
            static (virtualCharsBuilder, token) =>
            {
                if (token.Kind() == SyntaxKind.XmlEntityLiteralToken)
                {
                    // We only know how to deal with single character entities like:
                    //
                    //      &lt;  &gt;  &amp;  &apos;  &quot;       corresponding to:
                    //
                    //       <    >     &      '       "

                    if (token.ValueText.Length != 1)
                        return false;

                    // Make a virtual char from that single char (but spanning the whole entity).
                    virtualCharsBuilder.Add(new(new(token.ValueText[0], offset: 0, token.Text.Length), token.SpanStart));
                    return true;
                }
                else
                {
                    // All other xml text token characters are treated like a normal C# character.
                    for (var i = 0; i < token.Text.Length; i++)
                        virtualCharsBuilder.Add(new(new(token.Text[i], offset: i, width: 1), token.SpanStart));

                    return true;
                }
            },
            virtualCharsBuilder,
            cancellationToken);
    }
}
