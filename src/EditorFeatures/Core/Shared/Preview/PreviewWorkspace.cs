// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Preview
{
    internal class PreviewWorkspace : Workspace
    {
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
            var (oldSolution, newSolution) = this.SetCurrentSolutionEx(solution);

            this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionChanged, oldSolution, newSolution);
        }

        public void EnableSolutionCrawler()
        {
            Services.GetRequiredService<ISolutionCrawlerRegistrationService>().Register(this);
        }

        public override bool CanApplyChange(ApplyChangesKind feature)
        {
            // one can manipulate preview workspace solution as mush as they want.
            return true;
        }

        // This method signature is the base method signature which should be used for a client of a workspace to
        // tell the host to open it; in our case we want to open documents directly by passing the known buffer we created
        // for it.
        [Obsolete("Do not call the base OpenDocument method; instead call the overload that takes a container.", error: true)]
        public new void OpenDocument(DocumentId documentId, bool activate = true)
        {
        }

        public void OpenDocument(DocumentId documentId, SourceTextContainer textContainer)
        {
            var document = this.CurrentSolution.GetTextDocument(documentId);

            // This could be null if we're previewing a source generated document; we can't wire those up yet
            // TODO: implement this
            if (document == null)
            {
                return;
            }

            if (document is AnalyzerConfigDocument)
            {
                this.OnAnalyzerConfigDocumentOpened(documentId, textContainer);
            }
            else if (document is Document)
            {
                this.OnDocumentOpened(documentId, textContainer);
            }
            else
            {
                this.OnAdditionalDocumentOpened(documentId, textContainer);
            }
        }

        public override void CloseDocument(DocumentId documentId)
        {
            var document = this.CurrentSolution.GetRequiredDocument(documentId);
            var text = document.GetTextSynchronously(CancellationToken.None);
            var version = document.GetTextVersionSynchronously(CancellationToken.None);

            this.OnDocumentClosed(documentId, TextLoader.From(TextAndVersion.Create(text, version)));
        }

        public override void CloseAdditionalDocument(DocumentId documentId)
        {
            var document = this.CurrentSolution.GetRequiredAdditionalDocument(documentId);
            var text = document.GetTextSynchronously(CancellationToken.None);
            var version = document.GetTextVersionSynchronously(CancellationToken.None);

            this.OnAdditionalDocumentClosed(documentId, TextLoader.From(TextAndVersion.Create(text, version)));
        }

        public override void CloseAnalyzerConfigDocument(DocumentId documentId)
        {
            var document = this.CurrentSolution.GetRequiredAnalyzerConfigDocument(documentId);
            var text = document.GetTextSynchronously(CancellationToken.None);
            var version = document.GetTextVersionSynchronously(CancellationToken.None);

            this.OnAnalyzerConfigDocumentClosed(documentId, TextLoader.From(TextAndVersion.Create(text, version)));
        }

        protected override void Dispose(bool finalize)
        {
            base.Dispose(finalize);

            Services.GetRequiredService<ISolutionCrawlerRegistrationService>().Unregister(this);
            ClearSolution();
        }
    }
}
