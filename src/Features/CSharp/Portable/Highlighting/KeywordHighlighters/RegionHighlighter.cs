// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Highlighting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.KeywordHighlighting.KeywordHighlighters;

[ExportHighlighter(LanguageNames.CSharp), Shared]
internal sealed class RegionHighlighter : AbstractKeywordHighlighter<DirectiveTriviaSyntax>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RegionHighlighter()
    {
    }

    protected override void AddHighlights(
        DirectiveTriviaSyntax directive, List<TextSpan> highlights, CancellationToken cancellationToken)
    {
        var matchingDirective = directive.GetMatchingDirective(cancellationToken);
        if (matchingDirective == null)
        {
            return;
        }

        highlights.Add(TextSpan.FromBounds(
            directive.HashToken.SpanStart,
            directive.DirectiveNameToken.Span.End));

        highlights.Add(TextSpan.FromBounds(
            matchingDirective.HashToken.SpanStart,
            matchingDirective.DirectiveNameToken.Span.End));
    }
}
