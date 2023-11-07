// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Features.EmbeddedLanguages;

[ExportEmbeddedLanguageClassifier(
    PredefinedEmbeddedLanguageNames.CSharpTest, new[] { LanguageNames.CSharp }, supportsUnannotatedAPIs: false,
    PredefinedEmbeddedLanguageNames.CSharpTest), Shared]
internal sealed class CSharpTestEmbeddedLanguageClassifier : IEmbeddedLanguageClassifier
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpTestEmbeddedLanguageClassifier()
    {
    }

    private static TextSpan FromBounds(VirtualChar vc1, VirtualChar vc2)
        => TextSpan.FromBounds(vc1.Span.Start, vc2.Span.End);

    public void RegisterClassifications(EmbeddedLanguageClassificationContext context)
    {
        var cancellationToken = context.CancellationToken;

        var token = context.SyntaxToken;
        var semanticModel = context.SemanticModel;
        var compilation = semanticModel.Compilation;

        if (token.Kind() is not (SyntaxKind.StringLiteralToken or SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken))
            return;

        var virtualCharsWithMarkup = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
        if (virtualCharsWithMarkup.IsDefaultOrEmpty)
            return;

        // Note: if we get here, then we know that the token is well formed (TryConvertToVirtualChars will fail if
        // the token has diagnostics).

        cancellationToken.ThrowIfCancellationRequested();

        // Simpler to only support literals where all characters/escapes map to a single utf16 character.  That way
        // we can build a source-text as a trivial O(1) view over the virtual char sequence.
        if (virtualCharsWithMarkup.Any(static vc => vc.Utf16SequenceLength != 1))
            return;

        using var _ = ArrayBuilder<TextSpan>.GetInstance(out var markdownSpans);

        // First, add all the markdown components (`$$`, `[|`, etc.) into the result.
        var virtualCharsWithoutMarkup = StripMarkupCharacters(virtualCharsWithMarkup, markdownSpans, cancellationToken);
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
        else
        {
            context.AddClassification(
                ClassificationTypeNames.TestCode,
                TextSpan.FromBounds(
                    virtualCharsWithoutMarkup.First().Span.Start,
                    virtualCharsWithoutMarkup.Last().Span.End));
        }

        // Next, get all the embedded language classifications for the test file.  Combine these with the markdown
        // components. Note: markdown components may be in between individual language components.  For example
        // `ret$$urn`.  This will break the `return` classification into two individual classifications around the
        // `$$` classification.
        var testFileClassifiedSpans = GetTestFileClassifiedSpans(context.SolutionServices, semanticModel, virtualCharsWithoutMarkup, cancellationToken);
        foreach (var testClassifiedSpan in testFileClassifiedSpans)
            AddClassifications(context, virtualCharsWithoutMarkup, testClassifiedSpan);
    }

    private static IEnumerable<ClassifiedSpan> GetTestFileClassifiedSpans(
        Host.SolutionServices solutionServices, SemanticModel semanticModel, VirtualCharSequence virtualCharsWithoutMarkup, CancellationToken cancellationToken)
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
            new TextSpan(0, virtualCharsWithoutMarkup.Length),
            ClassificationOptions.Default,
            cancellationToken);
        return testFileClassifiedSpans;
    }

    /// <summary>
    /// Takes a <see cref="VirtualCharSequence"/> and returns the same characters from it, without any characters
    /// corresponding to test markup (e.g. <c>$$</c> and the like).  Because the virtual chars contain their
    /// original text span, these final virtual chars can be used both as the underlying source of a <see
    /// cref="SourceText"/> (which only cares about their <see cref="char"/> value), as well as the way to then map
    /// positions/spans within that <see cref="SourceText"/> to actual full virtual char spans in the original
    /// document for classification.
    /// </summary>
    private static VirtualCharSequence StripMarkupCharacters(
        VirtualCharSequence virtualChars, ArrayBuilder<TextSpan> markdownSpans, CancellationToken cancellationToken)
    {
        var builder = ImmutableSegmentedList.CreateBuilder<VirtualChar>();

        var nestedAnonymousSpanCount = 0;
        var nestedNamedSpanCount = 0;

        for (int i = 0, n = virtualChars.Length; i < n;)
        {
            var vc1 = virtualChars[i];
            var vc2 = i + 1 < n ? virtualChars[i + 1] : default;

            // These casts are safe because we disallowed virtual chars whose Value doesn't fit in a char in
            // RegisterClassifications.
            //
            // TODO: this algorithm is not actually the one used in roslyn or the roslyn-sdk for parsing a
            // markup file.  for example it will get `[|]` wrong (as that depends on knowing if we're starting
            // or ending an existing span).  Fix this up to follow the actual algorithm we use.
            switch (((char)vc1.Value, (char)vc2.Value))
            {
                case ('$', '$'):
                    markdownSpans.Add(FromBounds(vc1, vc2));
                    i += 2;
                    continue;
                case ('|', ']'):
                    nestedAnonymousSpanCount = Math.Max(0, nestedAnonymousSpanCount - 1);
                    markdownSpans.Add(FromBounds(vc1, vc2));
                    i += 2;
                    continue;
                case ('|', '}'):
                    markdownSpans.Add(FromBounds(vc1, vc2));
                    nestedNamedSpanCount = Math.Max(0, nestedNamedSpanCount - 1);
                    i += 2;
                    continue;

                // We have a slight ambiguity with cases like these:
                //
                // [|]    [|}
                //
                // Is it starting a new match, or ending an existing match.  As a workaround, we special case
                // these and consider it ending a match if we have something on the stack already.

                case ('[', '|'):
                    var vc3 = i + 2 < n ? virtualChars[i + 2] : default;
                    if ((vc3.Value == ']' && nestedAnonymousSpanCount > 0) ||
                        (vc3.Value == '}' && nestedNamedSpanCount > 0))
                    {
                        // not the start of a span, don't classify this '[' specially.
                        break;
                    }

                    nestedAnonymousSpanCount++;
                    markdownSpans.Add(FromBounds(vc1, vc2));
                    i += 2;
                    continue;

                case ('{', '|'):
                    if (TryConsumeNamedSpanStart(ref i, n))
                        continue;

                    // didn't find the colon.  don't classify these specially.
                    break;
            }

            // Nothing special, add character as is.
            builder.Add(vc1);
            i++;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return VirtualCharSequence.Create(builder.ToImmutable());

        bool TryConsumeNamedSpanStart(ref int i, int n)
        {
            var start = i;
            var seekPoint = i;
            while (seekPoint < n)
            {
                var colonChar = virtualChars[seekPoint];
                if (colonChar.Value == ':')
                {
                    markdownSpans.Add(FromBounds(virtualChars[start], colonChar));
                    nestedNamedSpanCount++;
                    i = seekPoint + 1;
                    return true;
                }

                seekPoint++;
            }

            return false;
        }
    }

    private static void AddClassifications(
        EmbeddedLanguageClassificationContext context,
        VirtualCharSequence virtualChars,
        ClassifiedSpan classifiedSpan)
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

            context.AddClassification(
                classificationType,
                FromBounds(virtualChars[currentStartIndexInclusive], virtualChars[currentEndIndexExclusive - 1]));
            currentStartIndexInclusive = currentEndIndexExclusive;
        }
    }

    /// <summary>
    /// Trivial implementation of a <see cref="SourceText"/> that directly maps over a <see
    /// cref="VirtualCharSequence"/>.
    /// </summary>
    private sealed class VirtualCharSequenceSourceText : SourceText
    {
        private readonly VirtualCharSequence _virtualChars;

        public override Encoding? Encoding { get; }

        public VirtualCharSequenceSourceText(VirtualCharSequence virtualChars, Encoding? encoding)
        {
            _virtualChars = virtualChars;
            Encoding = encoding;
        }

        public override int Length => _virtualChars.Length;

        public override char this[int position]
        {
            // This cast is safe because we disallowed virtual chars whose Value doesn't fit in a char in
            // RegisterClassifications.
            get => (char)_virtualChars[position].Value;
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            for (int i = sourceIndex, n = sourceIndex + count; i < n; i++)
                destination[destinationIndex + i] = this[i];
        }
    }
}
