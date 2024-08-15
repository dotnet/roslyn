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
#if NET

    private readonly SemaphoreSlim _isolatedReferenceSetGate = new(initialCount: 1);
    private readonly Dictionary<Checksum, WeakReference<IsolatedAssemblyReferenceSet>> _checksumToReferenceSet = new();

    private sealed class IsolatedAssemblyReferenceSet
    {
        public readonly ImmutableArray<AnalyzerReference> AnalyzerReferences;
        private readonly AssemblyLoadContext _assemblyLoadContext;

        public IsolatedAssemblyReferenceSet(
            ImmutableArray<AnalyzerReference> serializedReferences,
            IAnalyzerAssemblyLoaderProvider provider)
        {
            // Make a unique ALC for this set of references.
            var isolatedRoot = Guid.NewGuid().ToString();
            _assemblyLoadContext = new AssemblyLoadContext(isolatedRoot, isCollectible: true);

            // Now make a loader that will ensure these references are properly isolated.
            var shadowCopyLoader = provider.GetShadowCopyLoader(_assemblyLoadContext, isolatedRoot);

            var builder = new FixedSizeArrayBuilder<AnalyzerReference>(serializedReferences.Length);
            foreach (var analyzerReference in serializedReferences)
            {
                var underlyingAnalyzerReference = analyzerReference is SerializerService.SerializedAnalyzerReference serializedAnalyzerReference
                    ? new AnalyzerFileReference(serializedAnalyzerReference.FullPath, shadowCopyLoader)
                    : analyzerReference;

                // Create a special wrapped analyzer reference here.  It will ensure that any DiagnosticAnalyzers and
                // ISourceGenerators handed out will keep this IsolatedAssemblyReferenceSet alive.
                var isolatedReference = new IsolatedAnalyzerFileReference(this, underlyingAnalyzerReference);
                builder.Add(isolatedReference);
            }

            this.AnalyzerReferences = builder.MoveToImmutable();
        }

        /// <summary>
        /// When the last reference this to this reference set finally goes away, it is safe to unload our ALC.
        /// </summary>
        ~IsolatedAssemblyReferenceSet()
        {
            _assemblyLoadContext.Unload();
        }
    }

#endif

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

            isolatedAssemblyReferenceSet = new IsolatedAssemblyReferenceSet(serializedReferences, provider);

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
