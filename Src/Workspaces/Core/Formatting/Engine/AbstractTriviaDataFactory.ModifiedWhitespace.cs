// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal abstract partial class AbstractTriviaDataFactory
    {
        protected class ModifiedWhitespace : Whitespace
        {
            private readonly Whitespace original;

            public ModifiedWhitespace(OptionSet optionSet, int lineBreaks, int indentation, bool elastic, string language) :
                base(optionSet, lineBreaks, indentation, elastic, language)
            {
                this.original = null;
            }

            public ModifiedWhitespace(OptionSet optionSet, Whitespace original, int lineBreaks, int indentation, bool elastic, string language) :
                base(optionSet, lineBreaks, indentation, elastic, language)
            {
                Contract.ThrowIfNull(original);
                this.original = original;
            }

            public override bool ContainsChanges
            {
                get
                {
                    return false;
                }
            }

            public override TriviaData WithSpace(int space, FormattingContext context, ChainedFormattingRules formattingRules)
            {
                if (this.original == null)
                {
                    return base.WithSpace(space, context, formattingRules);
                }

                if (this.LineBreaks == this.original.LineBreaks && this.original.Spaces == space)
                {
                    return this.original;
                }

                return base.WithSpace(space, context, formattingRules);
            }

            public override TriviaData WithLine(int line, int indentation, FormattingContext context, ChainedFormattingRules formattingRules, CancellationToken cancellationToken)
            {
                if (this.original == null)
                {
                    return base.WithLine(line, indentation, context, formattingRules, cancellationToken);
                }

                if (this.original.LineBreaks == line && this.original.Spaces == indentation)
                {
                    return this.original;
                }

                return base.WithLine(line, indentation, context, formattingRules, cancellationToken);
            }

            public override TriviaData WithIndentation(
                int indentation, FormattingContext context, ChainedFormattingRules formattingRules, CancellationToken cancellationToken)
            {
                if (this.original == null)
                {
                    return base.WithIndentation(indentation, context, formattingRules, cancellationToken);
                }

                if (this.LineBreaks == this.original.LineBreaks && this.original.Spaces == indentation)
                {
                    return this.original;
                }

                return base.WithIndentation(indentation, context, formattingRules, cancellationToken);
            }

            public override void Format(
                FormattingContext context,
                ChainedFormattingRules formattingRules,
                Action<int, TriviaData> formattingResultApplier,
                CancellationToken cancellationToken,
                int tokenPairIndex)
            {
                formattingResultApplier(tokenPairIndex, new FormattedWhitespace(this.OptionSet, this.LineBreaks, this.Spaces, this.Language));
            }
        }
    }
}
