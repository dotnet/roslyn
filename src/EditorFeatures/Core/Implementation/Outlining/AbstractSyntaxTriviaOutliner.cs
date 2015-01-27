// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Outlining
{
    internal abstract class AbstractSyntaxTriviaOutliner
    {
        public abstract void CollectOutliningSpans(Document document, SyntaxTrivia trivia, List<OutliningSpan> spans, CancellationToken cancellationToken);

        // For testing purposes.
        internal IEnumerable<OutliningSpan> GetOutliningSpans(Document document, SyntaxTrivia trivia, CancellationToken cancellationToken)
        {
            var spans = new List<OutliningSpan>();
            this.CollectOutliningSpans(document, trivia, spans, cancellationToken);
            return spans;
        }
    }
}
