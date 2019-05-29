// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Preview
{
    internal class PreviewWorkspace : Workspace
    {
        private ISolutionCrawlerRegistrationService _registrationService;

        public PreviewWorkspace()
        : base(MefHostServices.DefaultHost, WorkspaceKind.Preview)
        {
        }

        public PreviewWorkspace(HostServices hostServices)
            : base(hostServices, WorkspaceKind.Preview)
        {
        }

        public PreviewWorkspace(Solution solution)
            : base(solution.Workspace.Services.HostServices, WorkspaceKind.Preview)
        {
            var oldSolution = this.CurrentSolution;
            var newSolution = this.SetCurrentSolution(solution);

            this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionChanged, oldSolution, newSolution);
        }

        public void EnableDiagnostic()
        {
            _registrationService = this.Services.GetService<ISolutionCrawlerRegistrationService>();
            if (_registrationService != null)
            {
                _registrationService.Register(this);
            }
        }

        public override bool CanApplyChange(ApplyChangesKind feature)
        {
            // one can manipulate preview workspace solution as mush as they want.
            return true;
        }

        public override void OpenDocument(DocumentId documentId, bool activate = true)
        {
            if (this.CurrentSolution.ContainsAdditionalDocument(documentId))
            {
                OpenAdditionalDocument(documentId, activate);
                return;
            }

            var document = this.CurrentSolution.GetDocument(documentId);
            var text = document.GetTextSynchronously(CancellationToken.None);

            this.OnDocumentOpened(documentId, text.Container);
        }

        public override void OpenAdditionalDocument(DocumentId documentId, bool activate = true)
        {
            var document = this.CurrentSolution.GetAdditionalDocument(documentId);
            var text = document.GetTextSynchronously(CancellationToken.None);

            this.OnAdditionalDocumentOpened(documentId, text.Container);
        }

        public override void OpenAnalyzerConfigDocument(DocumentId documentId, bool activate = true)
        {
            var document = this.CurrentSolution.GetAnalyzerConfigDocument(documentId);
            var text = document.GetTextSynchronously(CancellationToken.None);

            this.OnAnalyzerConfigDocumentOpened(documentId, text.Container);
        }

        public override void CloseDocument(DocumentId documentId)
        {
            var document = this.CurrentSolution.GetDocument(documentId);
            var text = document.GetTextSynchronously(CancellationToken.None);
            var version = document.GetTextVersionSynchronously(CancellationToken.None);

            this.OnDocumentClosed(documentId, TextLoader.From(TextAndVersion.Create(text, version)));
        }

        public override void CloseAdditionalDocument(DocumentId documentId)
        {
            var document = this.CurrentSolution.GetAdditionalDocument(documentId);
            var text = document.GetTextSynchronously(CancellationToken.None);
            var version = document.GetTextVersionSynchronously(CancellationToken.None);

            this.OnAdditionalDocumentClosed(documentId, TextLoader.From(TextAndVersion.Create(text, version)));
        }

        public override void CloseAnalyzerConfigDocument(DocumentId documentId)
        {
            var document = this.CurrentSolution.GetAnalyzerConfigDocument(documentId);
            var text = document.GetTextSynchronously(CancellationToken.None);
            var version = document.GetTextVersionSynchronously(CancellationToken.None);

            this.OnAnalyzerConfigDocumentClosed(documentId, TextLoader.From(TextAndVersion.Create(text, version)));
        }

        protected override void Dispose(bool finalize)
        {
            base.Dispose(finalize);

            if (_registrationService != null)
            {
                _registrationService.Unregister(this);
                _registrationService = null;
            }

            this.ClearSolution();
        }
    }
}
