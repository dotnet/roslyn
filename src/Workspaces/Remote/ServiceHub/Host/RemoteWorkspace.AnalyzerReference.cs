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

#if NET
using System.Runtime.Loader;
#endif

namespace Microsoft.CodeAnalysis.Remote;

internal partial class RemoteWorkspace
{
    private static ImmutableArray<AnalyzerReference> CreateAnalyzerReferencesInIsolatedAssemblyLoadContext(
        IAnalyzerAssemblyLoaderProvider provider, ImmutableArray<AnalyzerReference> serializedReferences)
    {
#if NET

        var isolatedRoot = Guid.NewGuid().ToString();
        var shadowCopyLoader = provider.GetShadowCopyLoader(isolatedRoot);

        
    
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
