// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CommandLine;
using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    internal sealed class TestableClientConnectionHost : IClientConnectionHost
    {
        private TaskCompletionSource<IClientConnection> _listenTask;

        public bool IsListening { get; set; }

        public TestableClientConnectionHost()
        {
            _listenTask = new TaskCompletionSource<IClientConnection>();
        }

        public void BeginListening()
        {
            IsListening = true;
        }

        public void EndListening()
        {
            IsListening = false;
        }

        public Task<IClientConnection> GetNextClientConnectionAsync() => _listenTask.Task;

        public void Add(Action<TaskCompletionSource<IClientConnection>> action)
        {
            action(_listenTask);
            _listenTask = new TaskCompletionSource<IClientConnection>();
        }
    }
}
