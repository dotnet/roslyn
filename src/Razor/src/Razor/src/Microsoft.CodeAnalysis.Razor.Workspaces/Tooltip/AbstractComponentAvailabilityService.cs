// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.Tooltip;

internal abstract class AbstractComponentAvailabilityService : IComponentAvailabilityService
{
    public async Task<ImmutableArray<(IProjectSnapshot Project, bool IsAvailable)>> GetComponentAvailabilityAsync(
        string documentFilePath,
        string typeName,
        CancellationToken cancellationToken)
    {
        var projects = GetProjectsContainingDocument(documentFilePath);
        if (projects.IsEmpty)
        {
            return [];
        }

        using var result = new PooledArrayBuilder<(IProjectSnapshot, bool IsAvailable)>(capacity: projects.Length);

        foreach (var project in projects)
        {
            var containsTagHelper = await ContainsTagHelperAsync(project, typeName, cancellationToken).ConfigureAwait(false);

            result.Add((project, IsAvailable: containsTagHelper));
        }

        return result.ToImmutableAndClear();
    }

    protected abstract ImmutableArray<IProjectSnapshot> GetProjectsContainingDocument(string documentFilePath);

    private static async Task<bool> ContainsTagHelperAsync(
        IProjectSnapshot projectSnapshot,
        string typeName,
        CancellationToken cancellationToken)
    {
        var tagHelpers = await projectSnapshot.GetTagHelpersAsync(cancellationToken).ConfigureAwait(false);

        foreach (var tagHelper in tagHelpers)
        {
            if (tagHelper.TypeName == typeName)
            {
                return true;
            }
        }

        return false;
    }
}
