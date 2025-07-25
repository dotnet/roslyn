// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;

internal static class UnitTestingProjectExtensions
{
    extension(ProjectId projectId)
    {
        public string? GetDebugName()
        => projectId.DebugName;
    }

    extension(Project project)
    {
        public Task<bool> HasSuccessfullyLoadedAsync(CancellationToken cancellationToken)
        => project.HasSuccessfullyLoadedAsync(cancellationToken);
    }
}
