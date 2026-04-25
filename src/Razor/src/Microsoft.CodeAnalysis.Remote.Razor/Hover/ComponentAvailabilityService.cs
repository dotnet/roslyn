// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Tooltip;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class ComponentAvailabilityService(RemoteSolutionSnapshot solutionSnapshot) : AbstractComponentAvailabilityService
{
    private readonly RemoteSolutionSnapshot _solutionSnapshot = solutionSnapshot;

    protected override ImmutableArray<IProjectSnapshot> GetProjectsContainingDocument(string documentFilePath)
        => _solutionSnapshot.GetProjectsContainingDocument(documentFilePath);
}
