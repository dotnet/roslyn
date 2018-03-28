// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters
{
    [ExportHighlighter(LanguageNames.CSharp)]
    internal class AsyncParenthesizedLambdaHighlighter : AbstractAsyncHighlighter<ParenthesizedLambdaExpressionSyntax>
    {
        protected override IEnumerable<TextSpan> GetHighlights(ParenthesizedLambdaExpressionSyntax node, CancellationToken cancellationToken)
        {
            if (node.AsyncKeyword.Kind() != SyntaxKind.AsyncKeyword)
            {
                return SpecializedCollections.EmptyEnumerable<TextSpan>();
            }

            var spans = new List<TextSpan>();

            HighlightRelatedKeywords(node, spans);

            return spans;
        }
    }
}
