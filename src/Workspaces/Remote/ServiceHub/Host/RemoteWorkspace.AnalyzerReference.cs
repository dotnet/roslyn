// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using System.Threading;
using Roslyn.Utilities;
using System.Runtime.CompilerServices;
using System.Reflection;

#if NET
using System.Runtime.Loader;
#endif

namespace Microsoft.CodeAnalysis.Remote;

internal partial class RemoteWorkspace
{
#if NET

    /// <summary>
    /// Gate around <see cref="_checksumToReferenceSet"/> to ensure it is only accessed and updated atomically.
    /// </summary>
    private readonly SemaphoreSlim _isolatedReferenceSetGate = new(initialCount: 1);

    /// <summary>
    /// Mapping from checksum for a particular set of assembly references, to the dedicated ALC and actual assembly
    /// references corresponding to it.  As long as it is alive, we will try to reuse what is in memory.  But once it is
    /// dropped from memory, we'll clean things up and produce a new one.
    /// </summary>
    private readonly Dictionary<Checksum, WeakReference<IsolatedAssemblyReferenceSet>> _checksumToReferenceSet = new();

    private sealed class IsolatedAssemblyReferenceSet
    {
        /// <summary>
        /// Final set of <see cref="AnalyzerReference"/> instances that will be passed through the workspace down to the compiler.
        /// </summary>
        public readonly ImmutableArray<AnalyzerReference> AnalyzerReferences;

        /// <summary>
        /// Dedicated ALC that all analyzer references will load their <see cref="Assembly"/>s within.
        /// </summary>
        private readonly AssemblyLoadContext _assemblyLoadContext;

        public IsolatedAssemblyReferenceSet(
            ImmutableArray<AnalyzerReference> serializedReferences,
            IAnalyzerAssemblyLoaderProvider provider)
        {
            // Make a unique ALC for this set of references.
            var isolatedRoot = Guid.NewGuid().ToString();
            _assemblyLoadContext = new AssemblyLoadContext(isolatedRoot, isCollectible: true);

            // Now make a loader that uses that ALC that will ensure these references are properly isolated.
            var shadowCopyLoader = provider.GetShadowCopyLoader(_assemblyLoadContext, isolatedRoot);

            var builder = new FixedSizeArrayBuilder<AnalyzerReference>(serializedReferences.Length);
            foreach (var analyzerReference in serializedReferences)
            {
                // Unwrap serialized analyzer references to get their file path and make a real AnalyzerFileReference
                // that now uses this isolated shadow copy loader.
                var underlyingAnalyzerReference = analyzerReference is SerializerService.SerializedAnalyzerReference
                    ? new AnalyzerFileReference(analyzerReference.FullPath!, shadowCopyLoader)
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

        /// <summary>
        /// Wrapper around a real <see cref="AnalyzerReference"/>.
        /// </summary>
        private sealed class IsolatedAnalyzerFileReference(
            IsolatedAssemblyReferenceSet isolatedAssemblyReferenceSet,
            AnalyzerReference underlyingAnalyzerReference) : AnalyzerReference
        {
            private static readonly ConditionalWeakTable<DiagnosticAnalyzer, IsolatedAssemblyReferenceSet> s_analyzerToPinnedReferenceSet = new();
            private static readonly ConditionalWeakTable<ISourceGenerator, IsolatedAssemblyReferenceSet> s_generatorToPinnedReferenceSet = new();

            /// <summary>
            /// We keep a strong reference here.  As long as the IsolatedAnalyzerFileReference is passed out and held
            /// onto (say by a Project instance), it should keep the IsolatedAssemblyReferenceSet (and its ALC) alive.
            /// </summary>
            private readonly IsolatedAssemblyReferenceSet _isolatedAssemblyReferenceSet = isolatedAssemblyReferenceSet;

            /// <summary>
            /// The actual real <see cref="AnalyzerReference"/> we defer our operations to.
            /// </summary>
            private readonly AnalyzerReference _underlyingAnalyzerReference = underlyingAnalyzerReference;

            public override string Display => _underlyingAnalyzerReference.Display;
            public override string? FullPath => _underlyingAnalyzerReference.FullPath;
            public override object Id => _underlyingAnalyzerReference.Id;

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
                => PinAnalyzers(_underlyingAnalyzerReference.GetAnalyzers(language));

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
                => PinAnalyzers(_underlyingAnalyzerReference.GetAnalyzersForAllLanguages());

            [Obsolete]
            public override ImmutableArray<ISourceGenerator> GetGenerators()
                => PinGenerators(_underlyingAnalyzerReference.GetGenerators());

            public override ImmutableArray<ISourceGenerator> GetGenerators(string language)
                => PinGenerators(_underlyingAnalyzerReference.GetGenerators(language));

            public override ImmutableArray<ISourceGenerator> GetGeneratorsForAllLanguages()
                => PinGenerators(_underlyingAnalyzerReference.GetGeneratorsForAllLanguages());

            private ImmutableArray<ISourceGenerator> PinGenerators(ImmutableArray<ISourceGenerator> generators)
            {
                // Keep a reference from each generator to the IsolatedAssemblyReferenceSet.  This will ensure it (and
                // the ALC it points at) stays alive as long as the generator instance stays alive.
                foreach (var generator in generators)
                    s_generatorToPinnedReferenceSet.TryAdd(generator, _isolatedAssemblyReferenceSet);

                return generators;
            }

            private ImmutableArray<DiagnosticAnalyzer> PinAnalyzers(ImmutableArray<DiagnosticAnalyzer> analyzers)
            {
                // Keep a reference from each analyzer to the IsolatedAssemblyReferenceSet.  This will ensure it (and
                // the ALC it points at) stays alive as long as the generator instance stays alive.
                foreach (var analyzer in analyzers)
                    s_analyzerToPinnedReferenceSet.TryAdd(analyzer, _isolatedAssemblyReferenceSet);

                return analyzers;
            }

            public override bool Equals(object? obj)
                => ReferenceEquals(this, obj);

            public override int GetHashCode()
                => RuntimeHelpers.GetHashCode(this);

            public override string ToString()
                => $"{nameof(IsolatedAnalyzerFileReference)}({_underlyingAnalyzerReference})";
        }
    }

#endif

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
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
            builder.Add(analyzerReference is SerializerService.SerializedAnalyzerReference serializedAnalyzerReference
                ? new AnalyzerFileReference(serializedAnalyzerReference.FullPath, shadowCopyLoader)
                : analyzerReference);
        }

        return builder.MoveToImmutable();

#endif

    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

}
