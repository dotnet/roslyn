// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters
{
    [ExportHighlighter(LanguageNames.CSharp)]
    internal class AwaitHighlighter : AbstractAsyncHighlighter<AwaitExpressionSyntax>
    {
        protected override IEnumerable<TextSpan> GetHighlights(AwaitExpressionSyntax awaitExpression, CancellationToken cancellationToken)
        {
            var parent = awaitExpression
                             .AncestorsAndSelf()
                             .FirstOrDefault(n => n.IsReturnableConstruct());

            if (parent == null)
            {
                return SpecializedCollections.EmptyEnumerable<TextSpan>();
            }

            var spans = new List<TextSpan>();

            HighlightRelatedKeywords(parent, spans);

            return spans;
        }
    }
}
