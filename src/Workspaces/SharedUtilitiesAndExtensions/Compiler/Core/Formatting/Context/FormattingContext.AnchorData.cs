// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting;

internal partial class FormattingContext
{
    /// <summary>
    /// data that will be used in an interval tree related to Anchor.
    /// </summary>
    private class AnchorData(AnchorIndentationOperation operation, SyntaxToken anchorToken, int originalColumn)
    {
        public TextSpan TextSpan => operation.TextSpan;

        public SyntaxToken StartToken => operation.StartToken;

        public SyntaxToken EndToken => operation.EndToken;

        public SyntaxToken AnchorToken { get; } = anchorToken;

        public int OriginalColumn { get; } = originalColumn;
    }

    private readonly struct FormattingContextIntervalIntrospector
        : IIntervalIntrospector<AnchorData>,
        IIntervalIntrospector<IndentationData>,
        IIntervalIntrospector<RelativeIndentationData>
    {
        int IIntervalIntrospector<AnchorData>.GetStart(AnchorData value)
            => value.TextSpan.Start;

        int IIntervalIntrospector<AnchorData>.GetLength(AnchorData value)
            => value.TextSpan.Length;

        int IIntervalIntrospector<IndentationData>.GetStart(IndentationData value)
            => value.TextSpan.Start;

        int IIntervalIntrospector<IndentationData>.GetLength(IndentationData value)
            => value.TextSpan.Length;

        int IIntervalIntrospector<RelativeIndentationData>.GetStart(RelativeIndentationData value)
            => value.InseparableRegionSpan.Start;

        int IIntervalIntrospector<RelativeIndentationData>.GetLength(RelativeIndentationData value)
            => value.InseparableRegionSpan.Length;
    }
}
