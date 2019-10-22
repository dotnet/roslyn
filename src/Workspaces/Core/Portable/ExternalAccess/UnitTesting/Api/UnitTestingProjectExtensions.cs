// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingProjectExtensions
    {
        public static string GetDebugName(this ProjectId projectId)
            => projectId.DebugName;

        public static Task<bool> HasSuccessfullyLoadedAsync(this Project project, CancellationToken cancellationToken)
            => project.HasSuccessfullyLoadedAsync(cancellationToken);
    }
}
