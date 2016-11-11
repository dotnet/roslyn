// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// this let us have isolated workspace services between solutions such as option services
    /// </summary>
    internal class RemoteWorkspace : Workspace
    {
        public RemoteWorkspace()
            : base(RoslynServices.HostServices, workspaceKind: SolutionService.WorkspaceKind_RemoteWorkspace)
        {
        }

        public override bool CanApplyChange(ApplyChangesKind feature)
        {
            // all kinds supported.
            return true;
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

            // TODO: under serialization lock ?
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
