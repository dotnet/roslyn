// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using System.Threading;
using Roslyn.Utilities;



#if NET
using System.Runtime.Loader;
#endif

namespace Microsoft.CodeAnalysis.Remote;

internal partial class RemoteWorkspace
{
    private readonly SemaphoreSlim _isolatedReferenceSetGate = new(initialCount: 1);
    private readonly Dictionary<Checksum, WeakReference<IsolatedAssemblyReferenceSet>> _checksumToReferenceSet = new();

    private sealed class IsolatedAssemblyReferenceSet
    {
        public readonly ImmutableArray<AnalyzerReference> AnalyzerReferences;
    }

    private async Task<ImmutableArray<AnalyzerReference>> CreateAnalyzerReferencesInIsolatedAssemblyLoadContextAsync(
        Checksum checksum, ImmutableArray<AnalyzerReference> serializedReferences, CancellationToken cancellationToken)
    {
        var provider = this.Services.SolutionServices.GetRequiredService<IAnalyzerAssemblyLoaderProvider>();

#if NET

        using (await _isolatedReferenceSetGate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_checksumToReferenceSet.TryGetValue(checksum, out var weakIsolatedReferenceSet) &&
                weakIsolatedReferenceSet.TryGetTarget(out var isolatedAssemblyReferenceSet))
            {
                return isolatedAssemblyReferenceSet.AnalyzerReferences;
            }

            isolatedAssemblyReferenceSet = new IsolatedAssemblyReferenceSet(
                serializedReferences, provider);

            if (weakIsolatedReferenceSet is null)
            {
                weakIsolatedReferenceSet = new(null!);
                _checksumToReferenceSet[checksum] = weakIsolatedReferenceSet;
            }

            weakIsolatedReferenceSet.SetTarget(isolatedAssemblyReferenceSet);
            return isolatedAssemblyReferenceSet.AnalyzerReferences;
        }

#else

        // Assembly load contexts not supported here.
        var shadowCopyLoader = provider.GetShadowCopyLoader();
        var builder = new FixedSizeArrayBuilder<AnalyzerReference>(serializedReferences.Length);

        foreach (var analyzerReference in serializedReferences)
        {
            if (analyzerReference is SerializerService.SerializedAnalyzerReference serializedAnalyzerReference)
            {
                builder.Add(new AnalyzerFileReference(serializedAnalyzerReference.FullPath, shadowCopyLoader));
            }
            else
            {
                builder.Add(analyzerReference);
            }
        }

        return builder.MoveToImmutable();

#endif

    }
}
