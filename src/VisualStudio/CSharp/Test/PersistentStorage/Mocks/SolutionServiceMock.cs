// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Copy of https://devdiv.visualstudio.com/DevDiv/_git/VS.CloudCache?path=%2Ftest%2FMicrosoft.VisualStudio.Cache.Tests%2FMocks&_a=contents&version=GBmain
// Try to keep in sync and avoid unnecessary changes here.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.RpcContracts.Solution;
using Microsoft.VisualStudio.Threading;

#pragma warning disable CS0067 // events that are never used

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices.Mocks
{
    internal class SolutionServiceMock : ISolutionService
    {
        private readonly BroadcastObservable<OpenCodeContainersState> openContainerObservable = new BroadcastObservable<OpenCodeContainersState>(new OpenCodeContainersState());

        public event EventHandler<ProjectsLoadedEventArgs>? ProjectsLoaded;

        public event EventHandler<ProjectsUnloadedEventArgs>? ProjectsUnloaded;

        public event EventHandler<ProjectsLoadProgressEventArgs>? ProjectLoadProgressChanged;

        internal Uri? SolutionFilePath
        {
            get => this.openContainerObservable.Value.SolutionFilePath;
            set => this.openContainerObservable.Value = this.openContainerObservable.Value with { SolutionFilePath = value };
        }

        public ValueTask<bool[]> AreProjectsLoadedAsync(Guid[] projectIds, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<IDisposable> SubscribeToOpenCodeContainersStateAsync(IObserver<OpenCodeContainersState> observer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(this.openContainerObservable.Subscribe(observer));
        }

        public Task<OpenCodeContainersState> GetOpenCodeContainersStateAsync(CancellationToken cancellationToken) => Task.FromResult(this.openContainerObservable.Value);

        public Task CloseSolutionAndFolderAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        public ValueTask<IReadOnlyList<string?>> GetPropertyValuesAsync(IReadOnlyList<int> propertyIds, CancellationToken cancellationToken) => throw new NotImplementedException();

        public ValueTask<IReadOnlyList<string?>> GetSolutionTelemetryContextPropertyValuesAsync(IReadOnlyList<string> propertyNames, CancellationToken cancellationToken) => throw new NotImplementedException();

        public ValueTask<bool> LoadProjectsAsync(Guid[] projectIds, CancellationToken cancellationToken) => throw new NotImplementedException();

        public ValueTask<ProjectLoadResult> LoadProjectsWithResultAsync(Guid[] projectIds, CancellationToken cancellationToken) => throw new NotImplementedException();

        public ValueTask<bool> RemoveProjectsAsync(IReadOnlyList<Guid> projectIds, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task RequestProjectEventsAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task SaveSolutionFilterFileAsync(string filterFileDirectory, string filterFileName, CancellationToken cancellationToken) => throw new NotImplementedException();

        public ValueTask<bool> UnloadProjectsAsync(Guid[] projectIds, ProjectUnloadReason unloadReason, CancellationToken cancellationToken) => throw new NotImplementedException();

        internal void SimulateFolderChange(IReadOnlyList<Uri> folderPaths) => this.openContainerObservable.Value = this.openContainerObservable.Value with { OpenFolderPaths = folderPaths };

        private class BroadcastObservable<T> : IObservable<T>
        {
            private readonly BroadcastBlock<T> sourceBlock = new(v => v);
            private T value;

            internal BroadcastObservable(T initialValue)
            {
                this.sourceBlock.Post(this.value = initialValue);
            }

            internal T Value
            {
                get => this.value;
                set => this.sourceBlock.Post(this.value = value);
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                var actionBlock = new ActionBlock<T>(observer.OnNext);
                actionBlock.Completion.ContinueWith(
                    static (t, s) =>
                    {
                        var observer = (IObserver<T>)s!;
                        if (t.Exception is object)
                        {
                            observer.OnError(t.Exception);
                        }
                        else
                        {
                            observer.OnCompleted();
                        }
                    },
                    observer,
                    TaskScheduler.Default).Forget();
                return this.sourceBlock.LinkTo(actionBlock, new DataflowLinkOptions { PropagateCompletion = true });
            }
        }
    }
}
