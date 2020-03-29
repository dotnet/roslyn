// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal partial class TriviaDataFactory
    {
        private class FormattedComplexTrivia : TriviaDataWithList
        {
            private readonly CSharpTriviaFormatter _formatter;
            private readonly IList<TextChange> _textChanges;

            public FormattedComplexTrivia(
                FormattingContext context,
                ChainedFormattingRules formattingRules,
                SyntaxToken token1,
                SyntaxToken token2,
                int lineBreaks,
                int spaces,
                string originalString,
                CancellationToken cancellationToken)
                : base(context.Options, LanguageNames.CSharp)
            {
                Contract.ThrowIfNull(context);
                Contract.ThrowIfNull(formattingRules);
                Contract.ThrowIfNull(originalString);

                this.LineBreaks = Math.Max(0, lineBreaks);
                this.Spaces = Math.Max(0, spaces);

                _formatter = new CSharpTriviaFormatter(context, formattingRules, token1, token2, originalString, this.LineBreaks, this.Spaces);
                _textChanges = _formatter.FormatToTextChanges(cancellationToken);
            }

            public override bool TreatAsElastic
            {
                get { return false; }
            }

            public override bool IsWhitespaceOnlyTrivia
            {
                get { return false; }
            }

            public override bool ContainsChanges
            {
                get { return _textChanges.Count > 0; }
            }

            public override IEnumerable<TextChange> GetTextChanges(TextSpan span)
                => _textChanges;

            public override SyntaxTriviaList GetTriviaList(CancellationToken cancellationToken)
                => _formatter.FormatToSyntaxTrivia(cancellationToken);

            public override TriviaData WithSpace(int space, FormattingContext context, ChainedFormattingRules formattingRules)
                => throw new NotImplementedException();

            public override TriviaData WithLine(int line, int indentation, FormattingContext context, ChainedFormattingRules formattingRules, CancellationToken cancellationToken)
                => throw new NotImplementedException();

            public override TriviaData WithIndentation(int indentation, FormattingContext context, ChainedFormattingRules formattingRules, CancellationToken cancellationToken)
                => throw new NotImplementedException();

            public override void Format(FormattingContext context, ChainedFormattingRules formattingRules, Action<int, TokenStream, TriviaData> formattingResultApplier, CancellationToken cancellationToken, int tokenPairIndex = TokenPairIndexNotNeeded)
                => throw new NotImplementedException();
        }
    }
}
