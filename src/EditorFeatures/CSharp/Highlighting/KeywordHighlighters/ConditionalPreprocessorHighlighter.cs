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
    internal class ConditionalPreprocessorHighlighter : AbstractKeywordHighlighter<DirectiveTriviaSyntax>
    {
        [ImportingConstructor]
        public ConditionalPreprocessorHighlighter()
        {
        }

        protected override IEnumerable<TextSpan> GetHighlights(
            DirectiveTriviaSyntax directive, CancellationToken cancellationToken)
        {
            var conditionals = directive.GetMatchingConditionalDirectives(cancellationToken);
            if (conditionals == null)
            {
                yield break;
            }

            foreach (var conditional in conditionals)
            {
                yield return TextSpan.FromBounds(
                    conditional.HashToken.SpanStart,
                    conditional.DirectiveNameToken.Span.End);
            }
        }
    }
}
