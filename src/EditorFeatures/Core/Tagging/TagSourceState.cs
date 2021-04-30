// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal partial class AbstractAsynchronousTaggerProvider<TTag>
    {
        private sealed partial class TagSource
        {
            private class TagSourceState : IDisposable
            {
                /// <summary>
                /// Token that is triggered when we are disposed.  Used to ensure that the initial work we
                /// kick off still gets stopped if the created tagger is disposed before that finishes.
                /// </summary>
                private readonly CancellationTokenSource _disposalTokenSource;

                /// <summary>
                /// Series of tokens used to cancel previous outstanding work when new work comes in.
                /// </summary>
                private readonly CancellationSeries _cancellationSeries;

                /// <summary>
                /// Work queue that collects event notifications and kicks off the work to process them.
                /// </summary>
                public Task EventWorkQueue;

                public TagSourceState()
                {
                    _disposalTokenSource = new();
                    _cancellationSeries = new CancellationSeries(_disposalTokenSource.Token);
                    EventWorkQueue = Task.CompletedTask;
                }

                public CancellationToken DisposalToken => _disposalTokenSource.Token;

                public void Dispose()
                {
                    // Stop computing any initial tags if we've been asked for them.  Use 'CancelAfter' so
                    // cancellation happens outside of the lock (that way no continuations run underneath it.
                    _disposalTokenSource.Cancel();
                    _disposalTokenSource.Dispose();
                    _cancellationSeries.Dispose();
                }

                public CancellationToken GetCancellationToken(bool initialTags)
                    => initialTags ? _disposalTokenSource.Token : _cancellationSeries.CreateNext();
            }
        }
    }
}
