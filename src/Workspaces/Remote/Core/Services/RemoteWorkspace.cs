// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// primary workspace for remote host. no one except solution service can update this workspace
    /// </summary>
    internal class RemoteWorkspace : Workspace
    {
        public const string WorkspaceKind_RemoteWorkspace = "RemoteWorkspace";

        // REVIEW: I am using semaphoreSlim since workspace is using it, but not sure why it uses
        //         semaphore rather than just object since workspace is not using anything specific
        //         to semaphore
        private readonly SemaphoreSlim _serializationLock = new SemaphoreSlim(initialCount: 1);

        public RemoteWorkspace()
            : base(RoslynServices.HostServices, workspaceKind: RemoteWorkspace.WorkspaceKind_RemoteWorkspace)
        {
            PrimaryWorkspace.Register(this);

            Options = Options.WithChangedOption(CacheOptions.RecoverableTreeLengthThreshold, 0);
        }

        // this workspace doesn't allow modification by calling TryApplyChanges.
        // consumer of solution is still free to fork solution as they want, they just can't apply those changes
        // back to primary workspace. only solution service can update primary workspace
        public override bool CanApplyChange(ApplyChangesKind feature) => false;

        // enables simulation of having documents open.
        public override bool CanOpenDocuments => true;

        /// <summary>
        /// Clears all projects and documents from the workspace.
        /// </summary>
        public new void ClearSolution()
        {
            using (_serializationLock.DisposableWait())
            {
                base.ClearSolution();
            }
        }

        /// <summary>
        /// Adds an entire solution to the workspace, replacing any existing solution.
        /// </summary>
        public Solution AddSolution(SolutionInfo solutionInfo)
        {
            if (solutionInfo == null)
            {
                throw new ArgumentNullException(nameof(solutionInfo));
            }

            using (_serializationLock.DisposableWait())
            {
                this.OnSolutionAdded(solutionInfo);
                this.UpdateReferencesAfterAdd();

                return this.CurrentSolution;
            }
        }

        /// <summary>
        /// update primary solution
        /// </summary>
        public Solution UpdateSolution(Solution solution)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            using (_serializationLock.DisposableWait())
            {
                var oldSolution = this.CurrentSolution;
                Contract.ThrowIfFalse(oldSolution.Id == solution.Id && oldSolution.FilePath == solution.FilePath);

                var newSolution = this.SetCurrentSolution(solution);
                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionChanged, oldSolution, newSolution);

                this.UpdateReferencesAfterAdd();

                return this.CurrentSolution;
            }
        }

        /// <summary>
        /// Puts the specified document into the open state.
        /// </summary>
        public override void OpenDocument(DocumentId documentId, bool activate = true)
        {
            using (_serializationLock.DisposableWait())
            {
                var doc = this.CurrentSolution.GetDocument(documentId);
                if (doc != null)
                {
                    var text = doc.GetTextAsync(CancellationToken.None).WaitAndGetResult_CanCallOnBackground(CancellationToken.None);
                    this.OnDocumentOpened(documentId, text.Container, activate);
                }
            }
        }

        /// <summary>
        /// Puts the specified document into the closed state.
        /// </summary>
        public override void CloseDocument(DocumentId documentId)
        {
            using (_serializationLock.DisposableWait())
            {
                var doc = this.CurrentSolution.GetDocument(documentId);
                if (doc != null)
                {
                    var text = doc.GetTextAsync(CancellationToken.None).WaitAndGetResult_CanCallOnBackground(CancellationToken.None);
                    var version = doc.GetTextVersionAsync(CancellationToken.None).WaitAndGetResult_CanCallOnBackground(CancellationToken.None);
                    var loader = TextLoader.From(TextAndVersion.Create(text, version, doc.FilePath));
                    this.OnDocumentClosed(documentId, loader);
                }
            }
        }

        /// <summary>
        /// Puts the specified additional document into the open state.
        /// </summary>
        public override void OpenAdditionalDocument(DocumentId documentId, bool activate = true)
        {
            using (_serializationLock.DisposableWait())
            {
                var doc = this.CurrentSolution.GetAdditionalDocument(documentId);
                if (doc != null)
                {
                    var text = doc.GetTextAsync(CancellationToken.None).WaitAndGetResult_CanCallOnBackground(CancellationToken.None);
                    this.OnAdditionalDocumentOpened(documentId, text.Container, activate);
                }
            }
        }

        /// <summary>
        /// Puts the specified additional document into the closed state
        /// </summary>
        public override void CloseAdditionalDocument(DocumentId documentId)
        {
            using (_serializationLock.DisposableWait())
            {
                var doc = this.CurrentSolution.GetAdditionalDocument(documentId);
                if (doc != null)
                {
                    var text = doc.GetTextAsync(CancellationToken.None).WaitAndGetResult_CanCallOnBackground(CancellationToken.None);
                    var version = doc.GetTextVersionAsync(CancellationToken.None).WaitAndGetResult_CanCallOnBackground(CancellationToken.None);
                    var loader = TextLoader.From(TextAndVersion.Create(text, version, doc.FilePath));
                    this.OnAdditionalDocumentClosed(documentId, loader);
                }
            }
        }
    }
}
