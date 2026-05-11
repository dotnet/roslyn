// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.Tooltip;

internal sealed class TestComponentAvailabilityService : IComponentAvailabilityService
{
    public Task<ImmutableArray<(IProjectSnapshot Project, bool IsAvailable)>> GetComponentAvailabilityAsync(string documentFilePath, string typeName, CancellationToken cancellationToken)
    {
        return SpecializedTasks.EmptyImmutableArray<(IProjectSnapshot Project, bool IsAvailable)>();
    }
}
