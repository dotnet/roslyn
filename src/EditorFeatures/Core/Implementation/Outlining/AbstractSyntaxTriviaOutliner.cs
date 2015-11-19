// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Outlining
{
    internal abstract class AbstractSyntaxTriviaOutliner : AbstractSyntaxOutliner
    {
        public sealed override void CollectOutliningSpans(
            Document document,
            SyntaxNode node,
            List<OutliningSpan> spans,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        // For testing purposes.
        internal IEnumerable<OutliningSpan> GetOutliningSpans(Document document, SyntaxTrivia trivia, CancellationToken cancellationToken)
        {
            var spans = new List<OutliningSpan>();
            this.CollectOutliningSpans(document, trivia, spans, cancellationToken);
            return spans;
        }
    }
}
