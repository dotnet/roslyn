// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal interface IRazorSpanMappingService
    {
        Task<ImmutableArray<RazorMappedSpanResult>> MapSpansAsync(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken);
    }

    internal struct RazorMappedSpanResult
    {
        public readonly string FilePath;

        public readonly LinePositionSpan LinePositionSpan;

        public readonly TextSpan Span;

        public RazorMappedSpanResult(string filePath, LinePositionSpan linePositionSpan, TextSpan span)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new System.ArgumentException(nameof(filePath));
            }

            FilePath = filePath;
            LinePositionSpan = linePositionSpan;
            Span = span;
        }

        public bool IsDefault => FilePath == null;
    }
}
