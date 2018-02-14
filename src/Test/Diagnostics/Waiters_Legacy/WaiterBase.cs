// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Roslyn.Hosting.Diagnostics.Waiters
{
    internal class WaiterBase : IAsynchronousOperationListener, IAsynchronousOperationWaiter
    {
        private readonly AsynchronousOperationListener _delegatee;

        protected WaiterBase(string name, IAsynchronousOperationListenerProvider provider)
        {
            _delegatee = (AsynchronousOperationListener)provider.GetListener(name);
        }

        public bool TrackActiveTokens
        {
            get
            {
                return _delegatee.TrackActiveTokens;
            }
            set
            {
                _delegatee.TrackActiveTokens = value;
            }
        }

        public bool HasPendingWork => _delegatee.HasPendingWork;

        public ImmutableArray<AsynchronousOperationListener.DiagnosticAsyncToken> ActiveDiagnosticTokens =>
            _delegatee.ActiveDiagnosticTokens;

        public IAsyncToken BeginAsyncOperation(string name, object tag = null, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) =>
            _delegatee.BeginAsyncOperation(name, tag, filePath, lineNumber);

        public Task CreateWaitTask() => _delegatee.CreateWaitTask();
    }
}
