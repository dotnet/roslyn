// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters
{
    [ExportHighlighter(LanguageNames.CSharp)]
    internal class ConditionalPreprocessorHighlighter : AbstractKeywordHighlighter<DirectiveTriviaSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ConditionalPreprocessorHighlighter()
        {
        }

        protected override void AddHighlights(
            DirectiveTriviaSyntax directive, List<TextSpan> highlights, CancellationToken cancellationToken)
        {
            var conditionals = directive.GetMatchingConditionalDirectives(cancellationToken);
            if (conditionals == null)
            {
                return;
            }

            foreach (var conditional in conditionals)
            {
                highlights.Add(TextSpan.FromBounds(
                    conditional.HashToken.SpanStart,
                    conditional.DirectiveNameToken.Span.End));
            }
        }
    }
}
