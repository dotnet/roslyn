// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal abstract partial class AbstractTriviaDataFactory
    {
        protected class ModifiedWhitespace : Whitespace
        {
            private readonly Whitespace _original;

            public ModifiedWhitespace(OptionSet optionSet, int lineBreaks, int indentation, bool elastic, string language)
                : base(optionSet, lineBreaks, indentation, elastic, language)
            {
                _original = null;
            }

            public ModifiedWhitespace(OptionSet optionSet, Whitespace original, int lineBreaks, int indentation, bool elastic, string language)
                : base(optionSet, lineBreaks, indentation, elastic, language)
            {
                Contract.ThrowIfNull(original);
                _original = original;
            }

            public override bool ContainsChanges => false;

            public override TriviaData WithSpace(int space, FormattingContext context, ChainedFormattingRules formattingRules)
            {
                if (_original == null)
                {
                    return base.WithSpace(space, context, formattingRules);
                }

                if (this.LineBreaks == _original.LineBreaks && _original.Spaces == space)
                {
                    return _original;
                }

                return base.WithSpace(space, context, formattingRules);
            }

            public override TriviaData WithLine(int line, int indentation, FormattingContext context, ChainedFormattingRules formattingRules, CancellationToken cancellationToken)
            {
                if (_original == null)
                {
                    return base.WithLine(line, indentation, context, formattingRules, cancellationToken);
                }

                if (_original.LineBreaks == line && _original.Spaces == indentation)
                {
                    return _original;
                }

                return base.WithLine(line, indentation, context, formattingRules, cancellationToken);
            }

            public override TriviaData WithIndentation(
                int indentation, FormattingContext context, ChainedFormattingRules formattingRules, CancellationToken cancellationToken)
            {
                if (_original == null)
                {
                    return base.WithIndentation(indentation, context, formattingRules, cancellationToken);
                }

                if (this.LineBreaks == _original.LineBreaks && _original.Spaces == indentation)
                {
                    return _original;
                }

                return base.WithIndentation(indentation, context, formattingRules, cancellationToken);
            }

            public override void Format(
                FormattingContext context,
                ChainedFormattingRules formattingRules,
                Action<int, TokenStream, TriviaData> formattingResultApplier,
                CancellationToken cancellationToken,
                int tokenPairIndex = TokenPairIndexNotNeeded)
            {
                formattingResultApplier(tokenPairIndex, context.TokenStream, new FormattedWhitespace(this.OptionSet, this.LineBreaks, this.Spaces, this.Language));
            }
        }
    }
}
