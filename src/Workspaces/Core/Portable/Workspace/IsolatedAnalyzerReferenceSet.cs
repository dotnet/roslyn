// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal sealed partial class IsolatedAnalyzerReferenceSet
{
    /// <summary>
    /// Given a set of analyzer references, attempts to return a new set that is in an isolated AssemblyLoadContext so
    /// that the analyzers and generators from it can be safely loaded side-by-side with prior versions of the same
    /// references that may already be loaded.
    /// </summary>
    public static partial ValueTask<ImmutableArray<AnalyzerReference>> CreateIsolatedAnalyzerReferencesAsync(
        bool useAsync,
        ImmutableArray<AnalyzerReference> references,
        SolutionServices solutionServices,
        CancellationToken cancellationToken);

    /// <summary>
    /// Given a checksum for a set of analyzer references, fetches the existing ALC-isolated set of them if already
    /// present in this process.  Otherwise, this fetches the raw serialized analyzer references from the host side,
    /// then creates and caches an isolated set on the OOP side to hold onto them, passing out that isolated set of
    /// references to be used by the caller (normally to be stored in a solution snapshot).
    /// </summary>
    public static partial ValueTask<ImmutableArray<AnalyzerReference>> CreateIsolatedAnalyzerReferencesAsync(
        bool useAsync,
        ChecksumCollection analyzerChecksums,
        SolutionServices solutionServices,
        Func<Task<ImmutableArray<AnalyzerReference>>> getReferencesAsync,
        CancellationToken cancellationToken);

    public static Guid TryGetFileReferenceMvid(string filePath)
    {
        try
        {
            return AssemblyUtilities.ReadMvid(filePath);
        }
        catch
        {
            // We have a reference but the file the reference is pointing to might not actually exist on disk. In that
            // case, rather than crashing, we will handle it gracefully.
            return Guid.Empty;
        }
    }
}
