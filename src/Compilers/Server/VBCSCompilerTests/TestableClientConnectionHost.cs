// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CommandLine;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    internal sealed class TestableClientConnectionHost : IClientConnectionHost
    {
        private readonly object _guard = new object();
        private Queue<Func<Task<IClientConnection>>> _waitingTasks = new Queue<Func<Task<IClientConnection>>>();

        public bool IsListening { get; set; }

        public TestableClientConnectionHost()
        {

        }

        public void BeginListening()
        {
            IsListening = true;
        }

        public void EndListening()
        {
            IsListening = false;

            lock (_guard)
            {
                _waitingTasks.Clear();
            }
        }

        public Task<IClientConnection> GetNextClientConnectionAsync()
        {
            Func<Task<IClientConnection>>? func = null;
            lock (_guard)
            {
                if (_waitingTasks.Count == 0)
                {
                    throw new InvalidOperationException();
                }

                func = _waitingTasks.Dequeue();
            }

            return func();
        }

        public void Add(Func<Task<IClientConnection>> func)
        {
            lock (_guard)
            {
                _waitingTasks.Enqueue(func);
            }
        }
    }
}
