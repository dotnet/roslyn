// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    internal sealed class TestableClientConnection : IClientConnection
    {
        public string LoggingIdentifier { get; set; } = "TestableClient";
        public Task DisconnectTask { get; set; } = new TaskCompletionSource<object>().Task;
        public Action DisposeFunc { get; set; } = delegate { };
        public Func<CancellationToken, Task<BuildRequest>> ReadBuildRequestFunc = delegate { throw new Exception(); };
        public Func<BuildResponse, CancellationToken, Task> WriteBuildResponseFunc = delegate { throw new Exception(); };

        public void Dispose() => DisposeFunc();
        public Task<BuildRequest> ReadBuildRequestAsync(CancellationToken cancellationToken) => ReadBuildRequestFunc(cancellationToken);
        public Task WriteBuildResponseAsync(BuildResponse response, CancellationToken cancellationToken) => WriteBuildResponseFunc(response, cancellationToken);
    }
}
