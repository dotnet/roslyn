// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting;

/// <summary>
/// it holds onto trivia information between two tokens
/// </summary>
internal abstract class TriviaData
{
    protected const int TokenPairIndexNotNeeded = int.MinValue;

    protected TriviaData(LineFormattingOptions options)
    {
        Options = options;
    }

    protected LineFormattingOptions Options { get; }

    public int LineBreaks { get; protected set; }
    public int Spaces { get; protected set; }

    public bool SecondTokenIsFirstTokenOnLine { get { return this.LineBreaks > 0; } }

    public abstract bool TreatAsElastic { get; }
    public abstract bool IsWhitespaceOnlyTrivia { get; }
    public abstract bool ContainsChanges { get; }

    public abstract IEnumerable<TextChange> GetTextChanges(TextSpan span);

    public abstract TriviaData WithSpace(int space, FormattingContext context, ChainedFormattingRules formattingRules);

    public abstract TriviaData WithLine(int line, int indentation, FormattingContext context, ChainedFormattingRules formattingRules, CancellationToken cancellationToken);

    public abstract TriviaData WithIndentation(int indentation, FormattingContext context, ChainedFormattingRules formattingRules, CancellationToken cancellationToken);

    public abstract void Format(
        FormattingContext context,
        ChainedFormattingRules formattingRules,
        Action<int, TokenStream, TriviaData> formattingResultApplier,
        CancellationToken cancellationToken,
        int tokenPairIndex = TokenPairIndexNotNeeded);
}
