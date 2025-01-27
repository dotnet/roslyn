// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting;

internal sealed partial class FormattingContext
{
    /// <summary>
    /// data that will be used in an interval tree related to Anchor.
    /// </summary>
    private sealed class AnchorData(AnchorIndentationOperation operation, SyntaxToken anchorToken, int originalColumn)
    {
        public TextSpan TextSpan => operation.TextSpan;

        public SyntaxToken StartToken => operation.StartToken;

        public SyntaxToken EndToken => operation.EndToken;

        public SyntaxToken AnchorToken { get; } = anchorToken;

        public int OriginalColumn { get; } = originalColumn;
    }

    private readonly struct FormattingContextIntervalIntrospector :
        IIntervalIntrospector<AnchorData>,
        IIntervalIntrospector<IndentationData>,
        IIntervalIntrospector<RelativeIndentationData>
    {
        TextSpan IIntervalIntrospector<AnchorData>.GetSpan(AnchorData value)
            => value.TextSpan;

        TextSpan IIntervalIntrospector<IndentationData>.GetSpan(IndentationData value)
            => value.TextSpan;

        TextSpan IIntervalIntrospector<RelativeIndentationData>.GetSpan(RelativeIndentationData value)
            => value.InseparableRegionSpan;
    }
}
