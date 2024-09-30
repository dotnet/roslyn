// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NET

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Basic no-op impl on .Net Framework.  We can't actually isolate anything in .Net Framework, so we just return the
/// assembly references as is.
/// </summary>
internal sealed partial class IsolatedAnalyzerReferenceSet
{
    public static partial ValueTask<ImmutableArray<AnalyzerReference>> CreateIsolatedAnalyzerReferencesAsync(
        bool useAsync,
        ImmutableArray<AnalyzerReference> references,
        SolutionServices solutionServices,
        CancellationToken cancellationToken)
    {
        return DefaultCreateIsolatedAnalyzerReferencesAsync(references);
    }

    public static partial ValueTask<ImmutableArray<AnalyzerReference>> CreateIsolatedAnalyzerReferencesAsync(
        bool useAsync,
        ChecksumCollection analyzerChecksums,
        SolutionServices solutionServices,
        Func<Task<ImmutableArray<AnalyzerReference>>> getReferencesAsync,
        CancellationToken cancellationToken)
    {
        return DefaultCreateIsolatedAnalyzerReferencesAsync(getReferencesAsync);
    }
}

#endif
