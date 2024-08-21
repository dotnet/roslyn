// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization;

/// <summary>
/// A set of <see cref="IsolatedAnalyzerReference"/>s and their associated <see cref="AssemblyLoadContext"/> (ALC).  As
/// long as something is keeping this set alive, the ALC will be kept alive.  Once this set is dropped, the ALC will be
/// explicitly <see cref="AssemblyLoadContext.Unload"/>'ed in its finalizer.
/// </summary>
internal sealed class IsolatedAssemblyReferenceSet
{
    /// <summary>
    /// Final set of <see cref="AnalyzerReference"/> instances that will be passed through the workspace down to the compiler.
    /// </summary>
    private readonly ImmutableArray<IsolatedAnalyzerReference> _analyzerReferences;

    /// <summary>
    /// Dedicated ALC that all analyzer references will load their <see cref="System.Reflection.Assembly"/>s within.
    /// </summary>
    private readonly AnalyzerAssemblyLoader shadowCopyLoader;

    public ImmutableArray<AnalyzerReference> AnalyzerReferences => _analyzerReferences.CastArray<AnalyzerReference>();

    public IsolatedAssemblyReferenceSet(
        ImmutableArray<AnalyzerReference> serializedReferences,
        IAnalyzerAssemblyLoaderProvider provider)
    {
        // We should really only be handed SerializedAnalyzerReference here (as that's the mainline case for a host
        // communicating references to the OOP side).  However, we may also get special test-specific analyzer
        // references.  So we can't assert we *only* have that type.
        //
        // We can *firmly* state though that we should never get an AnalyzerFileReference or IsolatedAnalyzerReference
        // as that would mean we were not properly getting the real analyzer references produced by the serialized
        // system.
        Contract.ThrowIfTrue(serializedReferences.Any(r => r is AnalyzerFileReference), $"Should not have gotten an {nameof(AnalyzerFileReference)}");
        Contract.ThrowIfTrue(serializedReferences.Any(r => r is IsolatedAnalyzerReference), $"Should not have gotten an {nameof(IsolatedAnalyzerReference)}");

        // Now make a fresh loader that uses that ALC that will ensure these references are properly isolated.
        var shadowCopyLoader = provider.GetShadowCopyLoader(getSharedLoader: false);

        var builder = new FixedSizeArrayBuilder<IsolatedAnalyzerReference>(serializedReferences.Length);
        foreach (var analyzerReference in serializedReferences)
        {
            // Unwrap serialized analyzer references to get their file path and make a real AnalyzerFileReference
            // that now uses this isolated shadow copy loader.
            var underlyingAnalyzerReference = analyzerReference is SerializerService.SerializedAnalyzerReference
                ? new AnalyzerFileReference(analyzerReference.FullPath!, shadowCopyLoader)
                : analyzerReference;

            // Create a special wrapped analyzer reference here.  It will ensure that any DiagnosticAnalyzers and
            // ISourceGenerators handed out will keep this IsolatedAssemblyReferenceSet alive.
            var isolatedReference = new IsolatedAnalyzerReference(this, underlyingAnalyzerReference);
            builder.Add(isolatedReference);
        }

        _analyzerReferences = builder.MoveToImmutable();
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
