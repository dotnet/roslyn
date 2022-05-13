// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
            private readonly Lazy<int> _lazyIndentationDelta;
            private readonly Func<int> _baseIndentationGetter;

            public RelativeIndentationData(int inseparableRegionSpanStart, TextSpan textSpan, IndentBlockOperation operation, Func<int> indentationDeltaGetter, Func<int> baseIndentationGetter)
                : base(textSpan)
            {
                _lazyIndentationDelta = new Lazy<int>(indentationDeltaGetter, isThreadSafe: true);
                _baseIndentationGetter = baseIndentationGetter;

                this.Operation = operation;
                this.InseparableRegionSpan = TextSpan.FromBounds(inseparableRegionSpanStart, textSpan.End);
            }

            private RelativeIndentationData(int inseparableRegionSpanStart, TextSpan textSpan, IndentBlockOperation operation, Lazy<int> lazyIndentationDelta, Func<int> baseIndentationGetter)
                : base(textSpan)
            {
                _lazyIndentationDelta = lazyIndentationDelta;
                _baseIndentationGetter = baseIndentationGetter;

                this.Operation = operation;
                this.InseparableRegionSpan = TextSpan.FromBounds(inseparableRegionSpanStart, textSpan.End);
            }

            public TextSpan InseparableRegionSpan { get; }
            public IndentBlockOperation Operation { get; }

            public SyntaxToken EndToken
            {
                get { return this.Operation.EndToken; }
            }

            public override int Indentation => _lazyIndentationDelta.Value + _baseIndentationGetter();

            protected override IndentationData WithTextSpanCore(TextSpan span)
            {
                return new RelativeIndentationData(InseparableRegionSpan.Start, span, Operation, _lazyIndentationDelta, _baseIndentationGetter);
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
