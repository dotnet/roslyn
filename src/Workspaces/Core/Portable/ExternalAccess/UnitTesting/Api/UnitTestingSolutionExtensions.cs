// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingSolutionExtensions
    {
        public static int UnitTesting_GetWorkspaceVersion(this Solution solution)
            => solution.WorkspaceVersion;

        public static UnitTestingSolutionStateWrapper GetUnitTestingSolutionStateWrapper(this Solution solution)
            => new UnitTestingSolutionStateWrapper(solution.State);
    }
}
