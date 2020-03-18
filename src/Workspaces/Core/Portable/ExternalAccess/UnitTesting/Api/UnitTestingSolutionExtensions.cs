// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingSolutionExtensions
    {
        public static int GetWorkspaceVersion(this Solution solution)
            => solution.WorkspaceVersion;

        public static UnitTestingSolutionStateWrapper GetState(this Solution solution)
            => new UnitTestingSolutionStateWrapper(solution.State);
    }
}
