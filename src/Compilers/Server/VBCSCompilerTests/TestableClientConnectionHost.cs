// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CommandLine;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    internal sealed class TestableClientConnectionHost : IClientConnectionHost
    {
        private readonly object _guard = new object();
        private TaskCompletionSource<IClientConnection>? _finalTaskCompletionSource;
        private readonly Queue<Func<Task<IClientConnection>>> _waitingTasks = new Queue<Func<Task<IClientConnection>>>();

        public bool IsListening { get; set; }

        public TestableClientConnectionHost()
        {

        }

        public void BeginListening()
        {
            IsListening = true;
            _finalTaskCompletionSource = new TaskCompletionSource<IClientConnection>();
        }

        public void EndListening()
        {
            IsListening = false;

            lock (_guard)
            {
                _waitingTasks.Clear();
                _finalTaskCompletionSource?.SetCanceled();
                _finalTaskCompletionSource = null;
            }
        }

        public Task<IClientConnection> GetNextClientConnectionAsync()
        {
            Func<Task<IClientConnection>>? func = null;
            lock (_guard)
            {
                if (_waitingTasks.Count == 0)
                {
                    if (_finalTaskCompletionSource is null)
                    {
                        _finalTaskCompletionSource = new TaskCompletionSource<IClientConnection>();
                    }

                    return _finalTaskCompletionSource.Task;
                }

                func = _waitingTasks.Dequeue();
            }

            return func();
        }

        public void Add(Func<Task<IClientConnection>> func)
        {
            lock (_guard)
            {
                if (_finalTaskCompletionSource is object)
                {
                    throw new InvalidOperationException("All Adds must be called before they are exhausted");
                }

                _waitingTasks.Enqueue(func);
            }
        }
    }
}
