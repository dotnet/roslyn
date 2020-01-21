// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters
{
    [ExportHighlighter(LanguageNames.CSharp)]
    internal class TryStatementHighlighter : AbstractKeywordHighlighter<TryStatementSyntax>
    {
        [ImportingConstructor]
        public TryStatementHighlighter()
        {
        }

        protected override void AddHighlights(
            TryStatementSyntax tryStatement, List<TextSpan> highlights, CancellationToken cancellationToken)
        {
            highlights.Add(tryStatement.TryKeyword.Span);

            foreach (var catchDeclaration in tryStatement.Catches)
            {
                highlights.Add(catchDeclaration.CatchKeyword.Span);

                if (catchDeclaration.Filter != null)
                {
                    highlights.Add(catchDeclaration.Filter.WhenKeyword.Span);
                }
            }

            if (tryStatement.Finally != null)
            {
                highlights.Add(tryStatement.Finally.FinallyKeyword.Span);
            }
        }
    }
}
