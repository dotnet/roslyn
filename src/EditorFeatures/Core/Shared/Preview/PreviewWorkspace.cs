// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Preview;

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

    public static ReferenceCountedDisposable<PreviewWorkspace> CreateWithDocumentContents(
        TextDocument document, SourceTextContainer textContainer)
    {
        // Ensure the solution's view of this file is consistent across all linked files within it.

        // Performance: Replace the SourceText of all related documents in this 
        // workspace. This prevents cascading forks as taggers call to
        // GetOpenTextDocumentInCurrentContextWithChanges would eventually wind up
        // calling Solution.WithDocumentText using the related ids.
        var newSolution = document.Project.Solution.WithDocumentText(
            document.Project.Solution.GetRelatedDocumentIds(document.Id),
            textContainer.CurrentText,
            PreservationMode.PreserveIdentity);

        // Ensure we don't leak the preview workspace in the event that an exception happens below.
        using var previewWorkspace = new ReferenceCountedDisposable<PreviewWorkspace>(new PreviewWorkspace(newSolution));

        // TODO: Determine if this is necesarry.  Existing code comments mention that this is needed so that things
        // like the LightBulb work.  But it is unclear if that's actually the case.  It is possible some features
        // may do things slightly differently if a doc is open or not.  But those cases should be rare.
        previewWorkspace.Target.OpenDocument(document.Id, textContainer);

        return previewWorkspace.TryAddReference() ?? throw ExceptionUtilities.Unreachable();
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

        ClearSolution();
    }
}
