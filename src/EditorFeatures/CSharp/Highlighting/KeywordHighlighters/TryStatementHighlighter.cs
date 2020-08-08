// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters
{
    [ExportHighlighter(LanguageNames.CSharp)]
    internal class TryStatementHighlighter : AbstractKeywordHighlighter<TryStatementSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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
