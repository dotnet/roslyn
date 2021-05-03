// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExternalAccess.DotNetWatch
{
    internal sealed class DotNetWatchManagedModuleUpdate
    {
        public readonly Guid Module;
        public readonly ImmutableArray<byte> ILDelta;
        public readonly ImmutableArray<byte> MetadataDelta;
        public readonly ImmutableArray<byte> PdbDelta;
        public readonly ImmutableArray<int> UpdatedMethods;

        public DotNetWatchManagedModuleUpdate(Guid module, ImmutableArray<byte> ilDelta, ImmutableArray<byte> metadataDelta, ImmutableArray<byte> pdbDelta, ImmutableArray<int> updatedMethods)
        {
            Module = module;
            ILDelta = ilDelta;
            MetadataDelta = metadataDelta;
            PdbDelta = pdbDelta;
            UpdatedMethods = updatedMethods;
        }
    }
}
