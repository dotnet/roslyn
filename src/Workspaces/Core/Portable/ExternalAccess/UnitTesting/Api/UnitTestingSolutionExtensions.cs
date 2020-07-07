// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingSolutionExtensions
    {
        public static int GetWorkspaceVersion(this Solution solution)
            => solution.WorkspaceVersion;

        public static async Task<UnitTestingChecksumWrapper> GetChecksumAsync(this Solution solution, CancellationToken cancellationToken)
            => new UnitTestingChecksumWrapper(await solution.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false));
    }
}
