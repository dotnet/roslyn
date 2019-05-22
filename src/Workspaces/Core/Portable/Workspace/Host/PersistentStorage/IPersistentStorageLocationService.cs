// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    interface IPersistentStorageLocationService : IWorkspaceService
    {
        bool IsSupported(Workspace workspace);
        string TryGetStorageLocation(SolutionId solutionId);

        /// <summary>
        /// A synchronous event raised prior to the location that <see cref="TryGetStorageLocation(SolutionId)"/> would return changes.
        /// </summary>
        event EventHandler<PersistentStorageLocationChangingEventArgs> StorageLocationChanging;
    }

    internal sealed class PersistentStorageLocationChangingEventArgs : EventArgs
    {
        public PersistentStorageLocationChangingEventArgs(SolutionId solutionId, string newStorageLocation, bool mustUseNewStorageLocationImmediately)
        {
            SolutionId = solutionId;
            NewStorageLocation = newStorageLocation;
            MustUseNewStorageLocationImmediately = mustUseNewStorageLocationImmediately;
        }

        public SolutionId SolutionId { get; }

        /// <summary>
        /// The new location. May be null if there is no longer a location.
        /// </summary>
        public string NewStorageLocation { get; }

        /// <summary>
        /// Specifies if any consumers must immediately start using the new storage location.
        /// </summary>
        /// <remarks>
        /// Sometimes, the storage location is moving due to a user operation which requires components
        /// to immediately release any file locks on the old location. A good example is renaming a solution,
        /// which changes the storage location. In that case, the storage location is going to get moved
        /// synchronously after this event is fired. Other times, it's because we closed the solution
        /// and we're simply giving a hint to people that they should start shutting down.</remarks>
        public bool MustUseNewStorageLocationImmediately { get; }
    }

    [ExportWorkspaceService(typeof(IPersistentStorageLocationService)), Shared]
    internal class DefaultPersistentStorageLocationService : IPersistentStorageLocationService
    {
        [ImportingConstructor]
        public DefaultPersistentStorageLocationService()
        {
        }

        public bool IsSupported(Workspace workspace) => false;

        public string TryGetStorageLocation(SolutionId solutionId) => null;
#pragma warning disable CS0067 // the event is unused
        public event EventHandler<PersistentStorageLocationChangingEventArgs> StorageLocationChanging;
#pragma warning disable CS0067
    }
}
