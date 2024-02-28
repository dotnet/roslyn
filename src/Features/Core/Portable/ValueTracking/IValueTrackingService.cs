// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ValueTracking;

internal interface IValueTrackingService : IWorkspaceService
{
    Task<ImmutableArray<ValueTrackedItem>> TrackValueSourceAsync(TextSpan selection, Document document, CancellationToken cancellationToken);
    Task<ImmutableArray<ValueTrackedItem>> TrackValueSourceAsync(Solution solution, ValueTrackedItem previousTrackedItem, CancellationToken cancellationToken);
}

internal interface IRemoteValueTrackingService
{
    ValueTask<ImmutableArray<SerializableValueTrackedItem>> TrackValueSourceAsync(Checksum solutionChecksum, TextSpan selection, DocumentId document, CancellationToken cancellationToken);
    ValueTask<ImmutableArray<SerializableValueTrackedItem>> TrackValueSourceAsync(Checksum solutionChecksum, SerializableValueTrackedItem previousTrackedItem, CancellationToken cancellationToken);
}
