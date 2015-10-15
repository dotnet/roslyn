// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                CancellationToken cancellationToken) :
                base(context.OptionSet, LanguageNames.CSharp)
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
            {
                return _textChanges;
            }

            public override List<SyntaxTrivia> GetTriviaList(CancellationToken cancellationToken)
            {
                return _formatter.FormatToSyntaxTrivia(cancellationToken);
            }

            public override TriviaData WithSpace(int space, FormattingContext context, ChainedFormattingRules formattingRules)
            {
                throw new NotImplementedException();
            }

            public override TriviaData WithLine(int line, int indentation, FormattingContext context, ChainedFormattingRules formattingRules, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override TriviaData WithIndentation(int indentation, FormattingContext context, ChainedFormattingRules formattingRules, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override void Format(FormattingContext context, ChainedFormattingRules formattingRules, Action<int, TriviaData> formattingResultApplier, CancellationToken cancellationToken, int tokenPairIndex = TokenPairIndexNotNeeded)
            {
                throw new NotImplementedException();
            }
        }
    }
}
