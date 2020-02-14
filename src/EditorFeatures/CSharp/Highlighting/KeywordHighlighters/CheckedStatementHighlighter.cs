// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters
{
    [ExportHighlighter(LanguageNames.CSharp)]
    internal class CheckedStatementHighlighter : AbstractKeywordHighlighter<CheckedStatementSyntax>
    {
        [ImportingConstructor]
        public CheckedStatementHighlighter()
        {
        }

        protected override void AddHighlights(CheckedStatementSyntax checkedStatement, List<TextSpan> highlights, CancellationToken cancellationToken)
            => highlights.Add(checkedStatement.Keyword.Span);
    }
}
