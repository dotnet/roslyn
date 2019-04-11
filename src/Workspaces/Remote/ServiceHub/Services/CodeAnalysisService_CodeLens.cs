// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class CodeAnalysisService : IRemoteCodeLensReferencesService
    {
        public Task<ReferenceCount> GetReferenceCountAsync(DocumentId documentId, TextSpan textSpan, int maxResultCount, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                using (Internal.Log.Logger.LogBlock(FunctionId.CodeAnalysisService_GetReferenceCountAsync, documentId.ProjectId.DebugName, token))
                {
                    var solution = await GetSolutionAsync(token).ConfigureAwait(false);

                    var document = solution.GetDocument(documentId);
                    if (document == null)
                    {
                        return null;
                    }

                    var syntaxNode = (await document.GetSyntaxRootAsync(token).ConfigureAwait(false)).FindNode(textSpan);

                    return await CodeLensReferencesServiceFactory.Instance.GetReferenceCountAsync(
                        solution,
                        documentId,
                        syntaxNode,
                        maxResultCount,
                        token).ConfigureAwait(false);
                }

            }, cancellationToken);
        }

        public Task<IEnumerable<ReferenceLocationDescriptor>> FindReferenceLocationsAsync(DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                using (Internal.Log.Logger.LogBlock(FunctionId.CodeAnalysisService_FindReferenceLocationsAsync, documentId.ProjectId.DebugName, token))
                {
                    var solution = await GetSolutionAsync(token).ConfigureAwait(false);
                    var document = solution.GetDocument(documentId);
                    if (document == null)
                    {
                        return null;
                    }

                    var syntaxNode = (await document.GetSyntaxRootAsync(token).ConfigureAwait(false)).FindNode(textSpan);

                    return await CodeLensReferencesServiceFactory.Instance.FindReferenceLocationsAsync(
                        solution,
                        documentId,
                        syntaxNode,
                        token).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public Task<IEnumerable<ReferenceMethodDescriptor>> FindReferenceMethodsAsync(DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                using (Internal.Log.Logger.LogBlock(FunctionId.CodeAnalysisService_FindReferenceMethodsAsync, documentId.ProjectId.DebugName, token))
                {
                    var solution = await GetSolutionAsync(token).ConfigureAwait(false);
                    var syntaxNode = (await solution.GetDocument(documentId).GetSyntaxRootAsync(token).ConfigureAwait(false)).FindNode(textSpan);

                    return await CodeLensReferencesServiceFactory.Instance.FindReferenceMethodsAsync(solution, documentId,
                        syntaxNode, token).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public Task<string> GetFullyQualifiedName(DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                using (Internal.Log.Logger.LogBlock(FunctionId.CodeAnalysisService_GetFullyQualifiedName, documentId.ProjectId.DebugName, token))
                {
                    var solution = await GetSolutionAsync(token).ConfigureAwait(false);
                    var syntaxNode = (await solution.GetDocument(documentId).GetSyntaxRootAsync(token).ConfigureAwait(false)).FindNode(textSpan);

                    return await CodeLensReferencesServiceFactory.Instance.GetFullyQualifiedName(solution, documentId,
                        syntaxNode, token).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public Task TrackCodeLensAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                await WorkspaceChangeTracker.TrackAsync(this, SolutionService.PrimaryWorkspace, documentId, token).ConfigureAwait(false);
            }, cancellationToken);
        }

        /// <summary>
        /// it tracks relevant changes on workspace for the given document.
        /// 
        /// better place for this is in ICodeLensContext but CodeLens OOP doesn't provide a way to call back to codelens OOP from
        /// VS so, this for now will be in Roslyn OOP
        /// </summary>
        private class WorkspaceChangeTracker
        {
            private static readonly TimeSpan s_delay = TimeSpan.FromMilliseconds(100);

            private readonly object _gate;

            private readonly CodeAnalysisService _owner;
            private readonly Workspace _workspace;
            private readonly DocumentId _documentId;
            private readonly CancellationToken _cancellationToken;

            private bool _eventSubscribed;
            private VersionStamp _lastVersion;
            private ResettableDelay _resettableDelay;

            public static async Task TrackAsync(CodeAnalysisService owner, Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
            {
                var document = workspace.CurrentSolution.GetDocument(documentId);
                if (document == null)
                {
                    return;
                }

                // if anything under the project this file belong to changes, then invalidate the code lens so that it can refresh
                var dependentVersion = await document.Project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false);
                new WorkspaceChangeTracker(owner, workspace, documentId, dependentVersion, cancellationToken);
            }

            private WorkspaceChangeTracker(
                CodeAnalysisService owner,
                Workspace workspace,
                DocumentId documentId,
                VersionStamp dependentVersion,
                CancellationToken cancellationToken)
            {
                _gate = new object();
                _eventSubscribed = false;

                _owner = owner;
                _workspace = workspace;
                _documentId = documentId;
                _cancellationToken = cancellationToken;

                _lastVersion = dependentVersion;
                _resettableDelay = ResettableDelay.CompletedDelay;

                ConnectEvents(subscription: true);
            }

            private void ConnectEvents(bool subscription)
            {
                // this is only place lock is used.
                // we have a lock here so that subscription and unsubscription of the two events
                // are happening atomic, but that doesn't mean there is no possiblity of race here
                // theoradically, there can be a race if connection got disconnected before we subscribe
                // to OnRpcDisconnected but already in the subscription code path.
                // but there is no easy way to solve the problem unless Rpc itself provide things like
                // subscribe only if connection still alive or something
                lock (_gate)
                {
                    if (subscription)
                    {
                        _owner.Disconnected += OnDisconnected;
                        _workspace.WorkspaceChanged += OnWorkspaceChanged;

                        if (_cancellationToken.IsCancellationRequested)
                        {
                            // while, we are subscribing to this service, caller side closed this connection
                            // unsubscribe from the service
                            _owner.Disconnected -= OnDisconnected;
                            _workspace.WorkspaceChanged -= OnWorkspaceChanged;
                            return;
                        }

                        _eventSubscribed = true;
                    }
                    else
                    {
                        if (_eventSubscribed)
                        {
                            _owner.Disconnected -= OnDisconnected;
                            _workspace.WorkspaceChanged -= OnWorkspaceChanged;
                        }
                    }
                }
            }

            private void OnDisconnected(object sender, EventArgs e)
            {
                ConnectEvents(subscription: false);
            }

            private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
            {
                // workspace event is serialized events. and reset delay only get updated here
                if (!_resettableDelay.Task.IsCompleted)
                {
                    _resettableDelay.Reset();
                    return;
                }

                var delay = new ResettableDelay((int)s_delay.TotalMilliseconds, expeditableDelaySource: AsynchronousOperationListenerProvider.NullListener);

                _resettableDelay = delay;
                delay.Task.ContinueWith(InvalidateAsync, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

                async Task InvalidateAsync(Task _)
                {
                    var document = _workspace.CurrentSolution.GetDocument(_documentId);
                    if (document == null)
                    {
                        return;
                    }

                    var newVersion = await document.Project.GetDependentVersionAsync(CancellationToken.None).ConfigureAwait(false);
                    if (newVersion == _lastVersion)
                    {
                        return;
                    }

                    // fire and forget.
                    // ignore any exception such as rpc already disposed (disconnected)

                    _lastVersion = newVersion;
                    await _owner.InvokeAsync(nameof(IRemoteCodeLensDataPoint.Invalidate), CancellationToken.None).ConfigureAwait(false);
                }
            }
        }
    }
}
