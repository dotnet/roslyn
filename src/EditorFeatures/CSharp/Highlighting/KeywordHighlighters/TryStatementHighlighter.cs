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

        protected override IEnumerable<TextSpan> GetHighlights(
            TryStatementSyntax tryStatement, CancellationToken cancellationToken)
        {
            yield return tryStatement.TryKeyword.Span;

            foreach (var catchDeclaration in tryStatement.Catches)
            {
                yield return catchDeclaration.CatchKeyword.Span;

                if (catchDeclaration.Filter != null)
                {
                    yield return catchDeclaration.Filter.WhenKeyword.Span;
                }
            }

            if (tryStatement.Finally != null)
            {
                yield return tryStatement.Finally.FinallyKeyword.Span;
            }
        }
    }
}
