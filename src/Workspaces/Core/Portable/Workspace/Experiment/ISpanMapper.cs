// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Experiment
{
    internal interface ISpanMapper
    {
        Task<ImmutableArray<SpanMapResult>> MapSpansAsync(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken);
    }

    internal struct SpanMapResult
    {
        public readonly Document Document;
        public readonly LinePositionSpan LinePositionSpan;

        public SpanMapResult(Document document, LinePositionSpan linePositionSpan)
        {
            Document = document;
            LinePositionSpan = linePositionSpan;
        }
    }
}
