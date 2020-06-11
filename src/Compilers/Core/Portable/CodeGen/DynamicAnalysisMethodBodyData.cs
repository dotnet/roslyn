// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal sealed class DynamicAnalysisMethodBodyData
    {
        public readonly ImmutableArray<SourceSpan> Spans;

        public DynamicAnalysisMethodBodyData(ImmutableArray<SourceSpan> spans)
        {
            Debug.Assert(!spans.IsDefault);
            Spans = spans;
        }
    }
}
