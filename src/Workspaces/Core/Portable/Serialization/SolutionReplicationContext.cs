// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Serialization
{
    internal readonly struct SolutionReplicationContext : IDisposable
    {
        private readonly ArrayBuilder<IDisposable> _resources;

        private SolutionReplicationContext(ArrayBuilder<IDisposable> resources)
            => _resources = resources;

        public static SolutionReplicationContext Create()
            => new(ArrayBuilder<IDisposable>.GetInstance());

        public void AddResource(IDisposable resource)
            => _resources.Add(resource);

        public void Dispose()
        {
            // TODO: https://github.com/dotnet/roslyn/issues/49973
            // Currently we don't dispose resources, only keep them alive.
            // Shouldn't we dispose them? 
            // _resources.All(resource => resource.Dispose());
            _resources.Free();
        }
    }
}
