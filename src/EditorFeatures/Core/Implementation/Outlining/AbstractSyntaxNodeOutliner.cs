// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Outlining
{
    internal abstract class AbstractSyntaxNodeOutliner
    {
        public abstract void CollectOutliningSpans(Document document, SyntaxNode node, List<OutliningSpan> spans, CancellationToken cancellationToken);

        // For testing purposes.
        internal IEnumerable<OutliningSpan> GetOutliningSpans(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var spans = new List<OutliningSpan>();
            this.CollectOutliningSpans(document, node, spans, cancellationToken);
            return spans;
        }
    }
}
