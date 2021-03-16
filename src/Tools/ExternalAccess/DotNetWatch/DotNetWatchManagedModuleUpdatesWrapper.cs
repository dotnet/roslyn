// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.ExternalAccess.DotNetWatch
{
    internal struct DotNetWatchManagedModuleUpdatesWrapper
    {
        private readonly ManagedModuleUpdates _instance;
        private ImmutableArray<DotNetWatchManagedModuleUpdateWrapper> _lazyUpdates;

        internal DotNetWatchManagedModuleUpdatesWrapper(in ManagedModuleUpdates instance)
        {
            _instance = instance;
            _lazyUpdates = default;
        }

        public readonly DotNetWatchManagedModuleUpdateStatus Status => (DotNetWatchManagedModuleUpdateStatus)_instance.Status;

        public ImmutableArray<DotNetWatchManagedModuleUpdateWrapper> Updates
        {
            get
            {
                if (_lazyUpdates is { IsDefault: false } updates)
                    return updates;

                updates = _instance.Updates.SelectAsArray(update => new DotNetWatchManagedModuleUpdateWrapper(in update));
                ImmutableInterlocked.InterlockedInitialize(ref _lazyUpdates, updates);
                return _lazyUpdates;
            }
        }
    }
}
