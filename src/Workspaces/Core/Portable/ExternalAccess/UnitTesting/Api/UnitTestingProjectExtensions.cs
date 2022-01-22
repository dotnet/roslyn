// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingProjectExtensions
    {
        public static string? GetDebugName(this ProjectId projectId)
            => projectId.DebugName;

        public static Task<bool> HasSuccessfullyLoadedAsync(this Project project, CancellationToken cancellationToken)
            => project.HasSuccessfullyLoadedAsync(cancellationToken);
    }
}
