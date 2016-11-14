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

        public RemoteWorkspace()
            : base(RoslynServices.HostServices, workspaceKind: RemoteWorkspace.WorkspaceKind_RemoteWorkspace)
        {
            PrimaryWorkspace.Register(this);

            Options = Options.WithChangedOption(CacheOptions.RecoverableTreeLengthThreshold, 0);
        }

        public override bool CanApplyChange(ApplyChangesKind feature)
        {
            // apply change is not allowed
            return false;
        }

        public override bool CanOpenDocuments
        {
            get
            {
                // enables simulation of having documents open.
                return true;
            }
        }

        /// <summary>
        /// Clears all projects and documents from the workspace.
        /// </summary>
        public new void ClearSolution()
        {
            base.ClearSolution();
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

            this.OnSolutionAdded(solutionInfo);
            this.UpdateReferencesAfterAdd();

            return this.CurrentSolution;
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

            var oldSolution = this.CurrentSolution;
            Contract.ThrowIfFalse(oldSolution.Id == solution.Id && oldSolution.FilePath == solution.FilePath);

            // this is not under serialization lock for events. but apply changes are not allowed. so no conflict.
            // no one except this method can update primary workspace. they can still freely fork solution. just
            // can't update current solution of workspace.
            var newSolution = this.SetCurrentSolution(solution);
            this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionChanged, oldSolution, newSolution);

            this.UpdateReferencesAfterAdd();

            return this.CurrentSolution;
        }

        /// <summary>
        /// Puts the specified document into the open state.
        /// </summary>
        public override void OpenDocument(DocumentId documentId, bool activate = true)
        {
            var doc = this.CurrentSolution.GetDocument(documentId);
            if (doc != null)
            {
                var text = doc.GetTextAsync(CancellationToken.None).WaitAndGetResult_CanCallOnBackground(CancellationToken.None);
                this.OnDocumentOpened(documentId, text.Container, activate);
            }
        }

        /// <summary>
        /// Puts the specified document into the closed state.
        /// </summary>
        public override void CloseDocument(DocumentId documentId)
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

        /// <summary>
        /// Puts the specified additional document into the open state.
        /// </summary>
        public override void OpenAdditionalDocument(DocumentId documentId, bool activate = true)
        {
            var doc = this.CurrentSolution.GetAdditionalDocument(documentId);
            if (doc != null)
            {
                var text = doc.GetTextAsync(CancellationToken.None).WaitAndGetResult_CanCallOnBackground(CancellationToken.None);
                this.OnAdditionalDocumentOpened(documentId, text.Container, activate);
            }
        }

        /// <summary>
        /// Puts the specified additional document into the closed state
        /// </summary>
        public override void CloseAdditionalDocument(DocumentId documentId)
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
