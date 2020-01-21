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
    internal class UsingStatementHighlighter : AbstractKeywordHighlighter<UsingStatementSyntax>
    {
        [ImportingConstructor]
        public UsingStatementHighlighter()
        {
        }

        protected override void AddHighlights(UsingStatementSyntax usingStatement, List<TextSpan> highlights, CancellationToken cancellationToken)
            => highlights.Add(usingStatement.UsingKeyword.Span);
    }
}
