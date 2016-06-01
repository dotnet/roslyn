// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// a service that lets one to create <see cref="SolutionSnapshot"/> that can be used to send over to other host
    /// </summary>
    internal interface ISolutionSnapshotService : IWorkspaceService
    {
        Task<SolutionSnapshot> CreateSnapshotAsync(Solution solution, CancellationToken cancellationToken);
        Task<ChecksumObject> GetChecksumObjectAsync(Checksum checksum, CancellationToken cancellationToken);
        // Task<SnapshotStream> CreateSnapshotStreamAsync(ChecksumObject checksumObject, CancellationToken cancellationToken);
    }

    /// <summary>
    /// a solution snapshot that one can use to get checksums to send over
    /// </summary>
    internal abstract class SolutionSnapshot : IDisposable
    {
        public readonly SolutionSnapshotId Id;

        public SolutionSnapshot(SolutionSnapshotId id)
        {
            Id = id;
        }

        public abstract void Dispose();
    }
}
