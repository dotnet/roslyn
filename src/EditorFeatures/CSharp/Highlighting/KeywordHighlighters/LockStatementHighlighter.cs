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
    internal class LockStatementHighlighter : AbstractKeywordHighlighter<LockStatementSyntax>
    {
#pragma warning disable RS0033 // Importing constructor should be [Obsolete]
        [ImportingConstructor]
#pragma warning restore RS0033 // Importing constructor should be [Obsolete]
        public LockStatementHighlighter()
        {
        }

        protected override IEnumerable<TextSpan> GetHighlights(
            LockStatementSyntax lockStatement, CancellationToken cancellationToken)
        {
            yield return lockStatement.LockKeyword.Span;
        }
    }
}
