// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal partial class FormattingContext :
        IIntervalIntrospector<FormattingContext.IndentationData>,
        IIntervalIntrospector<FormattingContext.RelativeIndentationData>
    {
        /// <summary>
        /// data that will be used in an interval tree related to indentation.
        /// </summary>
        private abstract class IndentationData
        {
            public IndentationData(TextSpan textSpan)
            {
                this.TextSpan = textSpan;
            }

            public TextSpan TextSpan { get; }
            public abstract int Indentation { get; }
        }

        private class RootIndentationData : SimpleIndentationData
        {
            // first and last token of root indentation are not valid
            public RootIndentationData(SyntaxNode rootNode)
                : base(rootNode.FullSpan, indentation: 0)
            {
            }
        }

        private class SimpleIndentationData : IndentationData
        {
            private readonly int _indentation;

            public SimpleIndentationData(TextSpan textSpan, int indentation)
                : base(textSpan)
            {
                _indentation = indentation;
            }

            public override int Indentation => _indentation;
        }

        private class LazyIndentationData : IndentationData
        {
            private readonly Lazy<int> _indentationGetter;
            public LazyIndentationData(TextSpan textSpan, Lazy<int> indentationGetter)
                : base(textSpan)
            {
                _indentationGetter = indentationGetter;
            }

            public override int Indentation => _indentationGetter.Value;
        }

        private class RelativeIndentationData : LazyIndentationData
        {
            public RelativeIndentationData(int inseparableRegionSpanStart, TextSpan textSpan, IndentBlockOperation operation, Lazy<int> indentationGetter)
                : base(textSpan, indentationGetter)
            {
                this.Operation = operation;
                this.InseparableRegionSpan = TextSpan.FromBounds(inseparableRegionSpanStart, textSpan.End);
            }

            public TextSpan InseparableRegionSpan { get; }
            public IndentBlockOperation Operation { get; }

            public SyntaxToken EndToken
            {
                get { return this.Operation.EndToken; }
            }
        }

        int IIntervalIntrospector<IndentationData>.GetStart(IndentationData value)
        {
            return value.TextSpan.Start;
        }

        int IIntervalIntrospector<IndentationData>.GetLength(IndentationData value)
        {
            return value.TextSpan.Length;
        }

        int IIntervalIntrospector<RelativeIndentationData>.GetStart(RelativeIndentationData value)
        {
            return value.InseparableRegionSpan.Start;
        }

        int IIntervalIntrospector<RelativeIndentationData>.GetLength(RelativeIndentationData value)
        {
            return value.InseparableRegionSpan.Length;
        }
    }
}
