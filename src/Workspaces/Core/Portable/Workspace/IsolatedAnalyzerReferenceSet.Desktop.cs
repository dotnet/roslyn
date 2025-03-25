// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NET

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

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
        var assemblyLoaderProvider = solutionServices.GetRequiredService<IAnalyzerAssemblyLoaderProvider>();
        using var _ = ArrayBuilder<AnalyzerReference>.GetInstance(out var builder);
        foreach (var ar in references)
        {
            if (ar is AnalyzerFileReference afr)
            {
                var fullPath = ((AnalyzerFileReference)ar).FullPath;
                var newAr = new AnalyzerFileReference(fullPath, assemblyLoaderProvider.SharedShadowCopyLoader);
                builder.Add(newAr);
            }
            else
            {
                Debug.Assert(ar is AnalyzerImageReference);
                builder.Add(ar);
            }
        }

        return ValueTaskFactory.FromResult(builder.ToImmutableAndClear());
    }

    public static async partial ValueTask<ImmutableArray<AnalyzerReference>> CreateIsolatedAnalyzerReferencesAsync(
        bool useAsync,
        ChecksumCollection analyzerChecksums,
        SolutionServices solutionServices,
        Func<Task<ImmutableArray<AnalyzerReference>>> getReferencesAsync,
        CancellationToken cancellationToken)
    {
        var references = await getReferencesAsync().ConfigureAwait(false);
        return await CreateIsolatedAnalyzerReferencesAsync(useAsync, references, solutionServices, cancellationToken).ConfigureAwait(false);
    }
}

#endif
