// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.Tooltip;

internal interface IComponentAvailabilityService
{
    /// <summary>
    ///  Returns an array of projects that contain the specified document and whether the
    ///  given component or tag helper type name is available within it.
    /// </summary>
    Task<ImmutableArray<(IProjectSnapshot Project, bool IsAvailable)>> GetComponentAvailabilityAsync(
        string documentFilePath,
        string typeName,
        CancellationToken cancellationToken);
}
