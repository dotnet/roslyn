// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal partial class FormattingContext
    {
        /// <summary>
        /// data that will be used in an interval tree related to indentation.
        /// </summary>
        private abstract class IndentationData
        {
            public IndentationData(TextSpan textSpan)
                => this.TextSpan = textSpan;

            public TextSpan TextSpan { get; }
            public abstract int Indentation { get; }

            public IndentationData WithTextSpan(TextSpan span)
                => span == TextSpan ? this : WithTextSpanCore(span);

            protected abstract IndentationData WithTextSpanCore(TextSpan span);
        }

        private sealed class SimpleIndentationData : IndentationData
        {
            private readonly int _indentation;

            public SimpleIndentationData(TextSpan textSpan, int indentation)
                : base(textSpan)
            {
                _indentation = indentation;
            }

            public override int Indentation => _indentation;

            protected override IndentationData WithTextSpanCore(TextSpan span)
            {
                return new SimpleIndentationData(span, _indentation);
            }
        }

        private sealed class RelativeIndentationData : IndentationData
        {
            private const int UninitializedIndentationDelta = int.MinValue;

            private readonly FormattingContext _formattingContext;
            private readonly Func<FormattingContext, IndentBlockOperation, SyntaxToken> _effectiveBaseTokenGetter;
            private readonly Func<FormattingContext, IndentBlockOperation, SyntaxToken, int> _indentationDeltaGetter;
            private readonly Func<FormattingContext, SyntaxToken, int> _baseIndentationGetter;

            /// <summary>
            /// Caches the value produced by <see cref="GetOrComputeIndentationDelta"/>.
            /// </summary>
            /// <value>
            /// <see cref="UninitializedIndentationDelta"/> if the field is not yet initialized; otherwise, the value
            /// returned from <see cref="_indentationDeltaGetter"/>.
            /// </value>
            private int _lazyIndentationDelta;

            public RelativeIndentationData(FormattingContext formattingContext, int inseparableRegionSpanStart, TextSpan textSpan, IndentBlockOperation operation, Func<FormattingContext, IndentBlockOperation, SyntaxToken> effectiveBaseTokenGetter, Func<FormattingContext, IndentBlockOperation, SyntaxToken, int> indentationDeltaGetter, Func<FormattingContext, SyntaxToken, int> baseIndentationGetter)
                : base(textSpan)
            {
                _formattingContext = formattingContext;
                _effectiveBaseTokenGetter = effectiveBaseTokenGetter;
                _indentationDeltaGetter = indentationDeltaGetter;
                _baseIndentationGetter = baseIndentationGetter;

                _lazyIndentationDelta = UninitializedIndentationDelta;

                this.Operation = operation;
                this.InseparableRegionSpan = TextSpan.FromBounds(inseparableRegionSpanStart, textSpan.End);
            }

            private RelativeIndentationData(FormattingContext formattingContext, int inseparableRegionSpanStart, TextSpan textSpan, IndentBlockOperation operation, Func<FormattingContext, IndentBlockOperation, SyntaxToken> effectiveBaseTokenGetter, Func<FormattingContext, IndentBlockOperation, SyntaxToken, int> indentationDeltaGetter, Func<FormattingContext, SyntaxToken, int> baseIndentationGetter, int lazyIndentationDelta)
                : base(textSpan)
            {
                _formattingContext = formattingContext;
                _effectiveBaseTokenGetter = effectiveBaseTokenGetter;
                _indentationDeltaGetter = indentationDeltaGetter;
                _baseIndentationGetter = baseIndentationGetter;

                _lazyIndentationDelta = lazyIndentationDelta;

                this.Operation = operation;
                this.InseparableRegionSpan = TextSpan.FromBounds(inseparableRegionSpanStart, textSpan.End);
            }

            public TextSpan InseparableRegionSpan { get; }
            public IndentBlockOperation Operation { get; }

            public SyntaxToken EndToken
            {
                get { return this.Operation.EndToken; }
            }

            private int GetOrComputeIndentationDelta()
            {
                var indentationDelta = Volatile.Read(ref _lazyIndentationDelta);
                if (indentationDelta != UninitializedIndentationDelta)
                    return indentationDelta;

                indentationDelta = _indentationDeltaGetter(_formattingContext, Operation, _effectiveBaseTokenGetter(_formattingContext, Operation));
                var existingIndentationDelta = Interlocked.CompareExchange(ref _lazyIndentationDelta, indentationDelta, UninitializedIndentationDelta);
                if (existingIndentationDelta != UninitializedIndentationDelta)
                    return existingIndentationDelta;

                return indentationDelta;
            }

            public override int Indentation => GetOrComputeIndentationDelta() + _baseIndentationGetter(_formattingContext, _effectiveBaseTokenGetter(_formattingContext, Operation));

            protected override IndentationData WithTextSpanCore(TextSpan span)
            {
                return new RelativeIndentationData(_formattingContext, InseparableRegionSpan.Start, span, Operation, _effectiveBaseTokenGetter, _indentationDeltaGetter, _baseIndentationGetter, _lazyIndentationDelta);
            }
        }

        private sealed class AdjustedIndentationData : IndentationData
        {
            public AdjustedIndentationData(TextSpan textSpan, IndentationData baseIndentationData, int adjustment)
                : base(textSpan)
            {
                BaseIndentationData = baseIndentationData;
                Adjustment = adjustment;
            }

            public IndentationData BaseIndentationData { get; }
            public int Adjustment { get; }

            public override int Indentation => BaseIndentationData.Indentation + Adjustment;

            protected override IndentationData WithTextSpanCore(TextSpan span)
            {
                return new AdjustedIndentationData(span, BaseIndentationData, Adjustment);
            }
        }
    }
}
