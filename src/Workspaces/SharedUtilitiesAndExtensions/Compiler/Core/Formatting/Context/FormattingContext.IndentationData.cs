// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting;

internal sealed partial class FormattingContext
{
    /// <summary>
    /// data that will be used in an interval tree related to indentation.
    /// </summary>
    private abstract class IndentationData(TextSpan textSpan)
    {
        public TextSpan TextSpan { get; } = textSpan;
        public abstract int Indentation { get; }

        public IndentationData WithTextSpan(TextSpan span)
            => span == TextSpan ? this : WithTextSpanCore(span);

        protected abstract IndentationData WithTextSpanCore(TextSpan span);
    }

    private sealed class SimpleIndentationData(TextSpan textSpan, int indentation) : IndentationData(textSpan)
    {
        public override int Indentation => indentation;

        protected override IndentationData WithTextSpanCore(TextSpan span)
        {
            return new SimpleIndentationData(span, indentation);
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
            return InterlockedOperations.Initialize(
                ref _lazyIndentationDelta,
                UninitializedIndentationDelta,
                static self => self._indentationDeltaGetter(
                    self._formattingContext,
                    self.Operation,
                    self._effectiveBaseTokenGetter(self._formattingContext, self.Operation)),
                this);
        }

        public override int Indentation => GetOrComputeIndentationDelta() + _baseIndentationGetter(_formattingContext, _effectiveBaseTokenGetter(_formattingContext, Operation));

        protected override IndentationData WithTextSpanCore(TextSpan span)
        {
            return new RelativeIndentationData(_formattingContext, InseparableRegionSpan.Start, span, Operation, _effectiveBaseTokenGetter, _indentationDeltaGetter, _baseIndentationGetter, _lazyIndentationDelta);
        }
    }

    /// <summary>
    /// Represents an indentation in which a fixed offset (<see cref="Adjustment"/>) is applied to a reference
    /// indentation amount (<see cref="BaseIndentationData"/>).
    /// </summary>
    private sealed class AdjustedIndentationData : IndentationData
    {
        public AdjustedIndentationData(TextSpan textSpan, IndentationData baseIndentationData, int adjustment)
            : base(textSpan)
        {
            RoslynDebug.Assert(adjustment != 0, $"Indentation with no adjustment should be represented by {nameof(BaseIndentationData)} directly.");
            RoslynDebug.Assert(baseIndentationData is not AdjustedIndentationData, $"Indentation data should only involve one layer of adjustment (multiples can be combined by adding the {nameof(Adjustment)} fields.");

            BaseIndentationData = baseIndentationData;
            Adjustment = adjustment;
        }

        /// <summary>
        /// The reference indentation data which needs to be adjusted.
        /// </summary>
        public IndentationData BaseIndentationData { get; }

        /// <summary>
        /// The adjustment to apply to the <see cref="IndentationData.Indentation"/> value providede by
        /// <see cref="BaseIndentationData"/>.
        /// </summary>
        public int Adjustment { get; }

        public override int Indentation => BaseIndentationData.Indentation + Adjustment;

        protected override IndentationData WithTextSpanCore(TextSpan span)
        {
            return new AdjustedIndentationData(span, BaseIndentationData, Adjustment);
        }
    }
}
