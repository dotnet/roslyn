// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.ExternalAccess.DotNetWatch
{
    internal readonly struct DotNetWatchManagedModuleUpdateWrapper
    {
        private readonly ManagedModuleUpdate _instance;

        internal DotNetWatchManagedModuleUpdateWrapper(in ManagedModuleUpdate instance)
        {
            _instance = instance;
        }

        public Guid Module => _instance.Module;
        public ImmutableArray<byte> ILDelta => _instance.ILDelta;
        public ImmutableArray<byte> MetadataDelta => _instance.MetadataDelta;
        public ImmutableArray<byte> PdbDelta => _instance.PdbDelta;
        public ImmutableArray<int> UpdatedMethods => _instance.UpdatedMethods;
    }
}
