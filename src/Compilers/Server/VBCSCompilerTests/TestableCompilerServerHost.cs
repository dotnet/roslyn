// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CommandLine;
using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    internal sealed class TestableCompilerServerHost : ICompilerServerHost
    {
        internal Func<RunRequest, CancellationToken, BuildResponse> RunCompilation;

        internal TestableCompilerServerHost(Func<RunRequest, CancellationToken, BuildResponse> runCompilation = null)
        {
            RunCompilation = runCompilation;
        }

        BuildResponse ICompilerServerHost.RunCompilation(RunRequest request, CancellationToken cancellationToken)
        {
            return RunCompilation(request, cancellationToken);
        }
    }
}
