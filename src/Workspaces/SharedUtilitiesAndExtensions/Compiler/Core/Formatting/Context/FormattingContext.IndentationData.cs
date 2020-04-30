// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
    }
}
