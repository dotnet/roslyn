// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Shared.Options;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Remote
{
    internal abstract class RemoteHost
    {
        private readonly Workspace _workspace;

        public RemoteHost(Workspace workspace)
        {
            _workspace = workspace;
        }

        public event EventHandler<bool> ConnectionChanged;

        public Task<Session> CreateCodeAnalysisServiceSessionAsync(Solution solution, CancellationToken cancellationToken)
        {
            return CreateCodeAnalysisServiceSessionAsync(solution, callbackTarget: null, cancellationToken: cancellationToken);
        }

        public async Task<Session> CreateCodeAnalysisServiceSessionAsync(Solution solution, object callbackTarget, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(solution.Workspace == _workspace);

            var service = _workspace.Services.GetService<ISolutionSnapshotService>();
            var snapshot = await service.CreateSnapshotAsync(solution, cancellationToken).ConfigureAwait(false);

            return await CreateCodeAnalysisServiceSessionAsync(snapshot, callbackTarget, cancellationToken).ConfigureAwait(false);
        }

        protected abstract void OnConnected();
        protected abstract void OnDisconnected();

        protected abstract Task<Session> CreateCodeAnalysisServiceSessionAsync(SolutionSnapshot snapshot, object callbackTarget, CancellationToken cancellationToken);

        internal void Shutdown()
        {
            // this should be only used by RemoteHostService to shutdown this remote host
            Disconnected();
        }

        protected void Connected()
        {
            OnConnected();

            _workspace.Options = _workspace.Options.WithChangedOption(RuntimeOptions.RemoteHostAvailable, true);

            OnConnectionChanged(true);
        }

        protected void Disconnected()
        {
            OnDisconnected();

            _workspace.Options = _workspace.Options.WithChangedOption(RuntimeOptions.RemoteHostAvailable, false);

            OnConnectionChanged(false);
        }

        private void OnConnectionChanged(bool connection)
        {
            ConnectionChanged?.Invoke(this, connection);
        }

        public abstract class Session : IDisposable
        {
            public readonly SolutionSnapshot SolutionSnapshot;

            protected Session(SolutionSnapshot snapshot)
            {
                SolutionSnapshot = snapshot;
            }

            public abstract Task InvokeAsync(string targetName, params object[] arguments);
            public abstract Task<Result> InvokeAsync<Result>(string targetName, params object[] arguments);
            public abstract Task InvokeAsync(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken);
            public abstract Task<T> InvokeAsync<T>(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken);

            public abstract void Dispose();
        }
    }
}
