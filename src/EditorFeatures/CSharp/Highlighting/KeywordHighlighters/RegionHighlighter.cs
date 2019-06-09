// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters
{
    [ExportHighlighter(LanguageNames.CSharp)]
    internal class RegionHighlighter : AbstractKeywordHighlighter<DirectiveTriviaSyntax>
    {
        [ImportingConstructor]
        public RegionHighlighter()
        {
        }

        protected override IEnumerable<TextSpan> GetHighlights(
            DirectiveTriviaSyntax directive, CancellationToken cancellationToken)
        {
            var matchingDirective = directive.GetMatchingDirective(cancellationToken);
            if (matchingDirective == null)
            {
                yield break;
            }

            yield return TextSpan.FromBounds(
                directive.HashToken.SpanStart,
                directive.DirectiveNameToken.Span.End);

            yield return TextSpan.FromBounds(
                matchingDirective.HashToken.SpanStart,
                matchingDirective.DirectiveNameToken.Span.End);
        }
    }
}
