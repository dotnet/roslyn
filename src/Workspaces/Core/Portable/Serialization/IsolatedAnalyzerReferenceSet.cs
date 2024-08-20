// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;

#if NET
using System.Runtime.Loader;
#endif

namespace Microsoft.CodeAnalysis.Serialization;

#if NET

/// <summary>
/// A set of <see cref="IsolatedAnalyzerReference"/>s and their associated <see cref="AssemblyLoadContext"/>.  
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
    private readonly AssemblyLoadContext _assemblyLoadContext;

    public ImmutableArray<AnalyzerReference> AnalyzerReferences => _analyzerReferences.CastArray<AnalyzerReference>();

    public IsolatedAssemblyReferenceSet(
        ImmutableArray<AnalyzerReference> serializedReferences,
        IAnalyzerAssemblyLoaderProvider provider)
    {
        // Make a unique ALC for this set of references.
        var isolatedRoot = Guid.NewGuid().ToString();
        _assemblyLoadContext = new AssemblyLoadContext(isolatedRoot, isCollectible: true);

        // Now make a loader that uses that ALC that will ensure these references are properly isolated.
        var shadowCopyLoader = provider.GetShadowCopyLoader(_assemblyLoadContext, isolatedRoot);

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
