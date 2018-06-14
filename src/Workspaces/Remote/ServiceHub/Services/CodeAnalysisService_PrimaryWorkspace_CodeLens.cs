// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote.CodeLensOOP;
using Microsoft.CodeAnalysis.Text;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class CodeAnalysisService : IRemoteCodeLensReferencesFromPrimaryWorkspaceService
    {
        public Task<ReferenceCount> GetReferenceCountAsync(Guid projectIdGuid, string filePath, TextSpan textSpan, int maxResultCount, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                using (Internal.Log.Logger.LogBlock(FunctionId.CodeAnalysisService_GetReferenceCountAsync, filePath, token))
                {
                    var solution = SolutionService.PrimaryWorkspace.CurrentSolution;

                    var documentId = GetDocumentId(solution, projectIdGuid, filePath);
                    if (documentId == null)
                    {
                        return new ReferenceCount(0, isCapped: false);
                    }

                    var syntaxNode = (await solution.GetDocument(documentId).GetSyntaxRootAsync().ConfigureAwait(false)).FindNode(textSpan);
                    return await CodeLensReferencesServiceFactory.Instance.GetReferenceCountAsync(solution, documentId,
                        syntaxNode, maxResultCount, token).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public Task<IEnumerable<ReferenceLocationDescriptor>> FindReferenceLocationsAsync(Guid projectIdGuid, string filePath, TextSpan textSpan, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                using (Internal.Log.Logger.LogBlock(FunctionId.CodeAnalysisService_FindReferenceLocationsAsync, filePath, token))
                {
                    var solution = SolutionService.PrimaryWorkspace.CurrentSolution;

                    var documentId = GetDocumentId(solution, projectIdGuid, filePath);
                    if (documentId == null)
                    {
                        return Array.Empty<ReferenceLocationDescriptor>();
                    }

                    var syntaxNode = (await solution.GetDocument(documentId).GetSyntaxRootAsync().ConfigureAwait(false)).FindNode(textSpan);
                    return await CodeLensReferencesServiceFactory.Instance.FindReferenceLocationsAsync(solution, documentId,
                        syntaxNode, token).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public void SetCodeLensReferenceCallback(Guid projectIdGuid, string filePath, CancellationToken cancellationToken)
        {
            RunService(token =>
            {
                var solution = SolutionService.PrimaryWorkspace.CurrentSolution;
                var documentId = GetDocumentId(solution, projectIdGuid, filePath);
                if (documentId == null)
                {
                    return;
                }

                SemanticChangeTracker.Track(solution.Workspace, Rpc, documentId);
            }, cancellationToken);
        }

        private static DocumentId GetDocumentId(Solution solution, Guid projectIdGuid, string filePath)
        {
            var documentIds = solution.GetDocumentIdsWithFilePath(filePath);

            if (projectIdGuid == Guid.Empty)
            {
                // this is misc project case. in this case, just return first match if there is one
                return documentIds.FirstOrDefault();
            }

            var projectId = ProjectId.CreateFromSerialized(projectIdGuid);
            return documentIds.FirstOrDefault(id => id.ProjectId == projectId);
        }

        private class SemanticChangeTracker
        {
            private const int DelayInMS = 100;

            private readonly Workspace _workspace;
            private readonly JsonRpc _rpc;
            private readonly DocumentId _documentId;

            private readonly object _gate;

            private ResettableDelay _resettableDelay;

            public static void Track(Workspace workspace, JsonRpc rpc, DocumentId documentId)
            {
                var _ = new SemanticChangeTracker(workspace, rpc, documentId);
            }

            public SemanticChangeTracker(Workspace workspace, JsonRpc rpc, DocumentId documentId)
            {
                _gate = new object();

                _workspace = workspace;
                _rpc = rpc;

                _documentId = documentId;

                _resettableDelay = ResettableDelay.CompletedDelay;

                ConnectEvents(subscription: true);
            }

            private void ConnectEvents(bool subscription)
            {
                lock (_gate)
                {
                    if (subscription)
                    {
                        _rpc.Disconnected += OnRpcDisconnected;
                        _workspace.WorkspaceChanged += OnWorkspaceChanged;
                    }
                    else
                    {
                        _rpc.Disconnected -= OnRpcDisconnected;
                        _workspace.WorkspaceChanged -= OnWorkspaceChanged;
                    }
                }
            }

            private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
            {
                // workspace event is serialized events. and reset delay only get updated here
                if (!_resettableDelay.Task.IsCompleted)
                {
                    _resettableDelay.Reset();
                    return;
                }

                var delay = new ResettableDelay(DelayInMS);
                _resettableDelay = delay;

                delay.Task.ContinueWith(_ =>
                {
                    try
                    {
                        // fire and forget.
                        // ignore any exception such as rps already disposed (disconnected)
                        _rpc.InvokeAsync("Invalidate");
                    }
                    catch { }
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }

            private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
            {
                ConnectEvents(subscription: false);
            }
        }
    }
}
