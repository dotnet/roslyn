// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CommandLine;
using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    internal sealed class TestableCompilerServerHost : ICompilerServerHost
    {
        internal Func<RunRequest, CancellationToken, BuildResponse> RunCompilation { get; }
        public ICompilerServerLogger Logger { get; }

        internal TestableCompilerServerHost(Func<RunRequest, CancellationToken, BuildResponse> runCompilation = null, ICompilerServerLogger logger = null)
        {
            RunCompilation = runCompilation;
            Logger = logger ?? EmptyCompilerServerLogger.Instance;
        }

        BuildResponse ICompilerServerHost.RunCompilation(in RunRequest request, CancellationToken cancellationToken)
        {
            return RunCompilation(request, cancellationToken);
        }
    }
}
