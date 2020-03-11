﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal partial class FormattingContext
    {
        /// <summary>
        /// data that will be used in an interval tree related to Anchor.
        /// </summary>
        private class AnchorData
        {
            private readonly AnchorIndentationOperation _operation;

            public AnchorData(AnchorIndentationOperation operation, int originalColumn)
            {
                _operation = operation;
                this.OriginalColumn = originalColumn;
            }

            public TextSpan TextSpan => _operation.TextSpan;

            public SyntaxToken AnchorToken => _operation.AnchorToken;

            public SyntaxToken StartToken => _operation.StartToken;

            public SyntaxToken EndToken => _operation.EndToken;

            public int OriginalColumn { get; }
        }

        private readonly struct FormattingContextIntervalIntrospector
            : IIntervalIntrospector<AnchorData>,
            IIntervalIntrospector<IndentationData>,
            IIntervalIntrospector<RelativeIndentationData>
        {
            int IIntervalIntrospector<AnchorData>.GetStart(AnchorData value)
            {
                return value.TextSpan.Start;
            }

            int IIntervalIntrospector<AnchorData>.GetLength(AnchorData value)
            {
                return value.TextSpan.Length;
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
}
