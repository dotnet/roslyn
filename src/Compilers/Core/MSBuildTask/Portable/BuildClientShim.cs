// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CommandLine;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    internal static class BuildClientShim
    {
        public static Task<BuildResponse> RunServerCompilation(
            RequestLanguage language,
            List<string> arguments,
            BuildPaths buildPaths,
            string keepAlive,
            string libEnvVariable,
            CancellationToken cancellationToken) => Task.FromResult<BuildResponse>(null);
    }
}
