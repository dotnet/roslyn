// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

/// <summary>
/// this holds onto changes made by formatting engine.
/// </summary>
internal sealed class FormattingResult : AbstractFormattingResult
{
    internal FormattingResult(TreeData treeInfo, TokenStream tokenStream, TextSpan spanToFormat)
        : base(treeInfo, tokenStream, spanToFormat)
    {
    }

    protected override SyntaxNode Rewriter(Dictionary<ValueTuple<SyntaxToken, SyntaxToken>, TriviaData> changeMap, CancellationToken cancellationToken)
    {
        var rewriter = new TriviaRewriter(this.TreeInfo.Root, new TextSpanMutableIntervalTree(this.FormattedSpan), changeMap, cancellationToken);
        return rewriter.Transform();
    }
}
