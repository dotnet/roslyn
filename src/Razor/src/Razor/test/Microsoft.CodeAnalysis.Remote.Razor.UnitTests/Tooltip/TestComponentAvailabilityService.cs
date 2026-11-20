// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor.Tooltip;

internal sealed class TestComponentAvailabilityService : IComponentAvailabilityService
{
    public Task<ImmutableArray<(RemoteProjectSnapshot Project, bool IsAvailable)>> GetComponentAvailabilityAsync(string documentFilePath, string typeName, CancellationToken cancellationToken)
    {
        return SpecializedTasks.EmptyImmutableArray<(RemoteProjectSnapshot Project, bool IsAvailable)>();
    }
}
