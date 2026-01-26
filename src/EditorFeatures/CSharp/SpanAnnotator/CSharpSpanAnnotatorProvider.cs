// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Logic.Find;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SpanAnnotator;

[Export(typeof(ISpanAnnotatorProvider))]
[Name(nameof(CSharpSpanAnnotatorProvider))]
[SupportsFileExtension(".cs")]
[Obsolete] // ISpanAnnotatorProvider is still experimental and subject to change
internal sealed class CSharpSpanAnnotatorProvider : ISpanAnnotatorProvider
{
    private CSharpSpanAnnotator? _annotator;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpSpanAnnotatorProvider()
    {
    }

    public ISpanAnnotator GetAnnotator(string fileExtension)
        => _annotator ??= new CSharpSpanAnnotator();

    private sealed class CSharpSpanAnnotator : ISpanAnnotator
    {
        // 40KB, the limit at which Roslyn doesn't use full strings for parsing
        private const int LargeObjectHeapLimitInChars = 40 * 1024;

        public IEnumerable<SpanAndKind> GetAnnotatedSpans(
            ITextSnapshot snapshot,
            IEnumerable<Span> spans,
            CancellationToken cancellationToken)
        {
            // TODO: Any other checks that would make us not want to try to parse?
            if (snapshot.Length >= LargeObjectHeapLimitInChars)
            {
                foreach (var span in spans)
                {
                    yield return new SpanAndKind(span);
                }
                yield break;
            }

            IEnumerator<SyntaxToken>? tokensEnumerator = null;
            try
            {
                tokensEnumerator = SyntaxFactory.ParseTokens(
                    snapshot.GetText(),
                    options: CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.None)).GetEnumerator();
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex))
            {
            }

            SyntaxToken? lastToken = null;

            foreach (var span in spans)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (tokensEnumerator is null)
                {
                    yield return new SpanAndKind(span);
                }
                else
                {
                    Assumes.NotNull(tokensEnumerator);

                    FindResultKind kind = default;

                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (lastToken is null)
                        {
                            if (!tokensEnumerator.MoveNext())
                            {
                                break;
                            }
                            lastToken = tokensEnumerator.Current;
                        }

                        var token = tokensEnumerator.Current;

                        if (token.FullSpan.Start > span.End)
                        {
                            break;
                        }

                        if (span.OverlapsWith(new Span(token.FullSpan.Start, token.FullSpan.Length)))
                        {
                            if (span.OverlapsWith(new Span(token.Span.Start, token.Span.Length)))
                            {
                                // The match is in the token itself
                                if (token.IsKind(SyntaxKind.StringLiteralToken))
                                {
                                    kind = FindResultKind.String;
                                }
                            }
                            else
                            {
                                // The match is in the trivia, and is some sort of comment
                                kind = FindResultKind.Comment;
                            }
                            break;
                        }

                        // TODO: Don't advance until no overlap, as there could be multiple input spans that match
                        //       a single token or its trivia.
                        if (!tokensEnumerator.MoveNext())
                        {
                            break;
                        }
                        lastToken = tokensEnumerator.Current;
                    }

                    yield return new SpanAndKind(span, kind);
                }
            }
        }
    }
}
