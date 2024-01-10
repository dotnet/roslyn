// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

/// <summary>
/// Mark this type as an <see cref="ILspWorkspace"/> so that LSP document changes are pushed into this instance, causing
/// our <see cref="Workspace.CurrentSolution"/> to stay in sync with all the document changes.
/// <para/>
/// There is a fundamental race with how solution snapshot data is stored in this type.  Specifically, two entities push
/// changes into this workspace.  First, the <see cref="LspWorkspaceManager"/> pushes changes into this relating to the
/// open/closed state of documents, and the current text contents that it knows about from <see
/// cref="LSP.Methods.TextDocumentDidChange"/> events.  Second, the project system may push changes about what files
/// actually exist in the workspace or not. As neither of these entities synchronizes on anything, we may hear about
/// things like changes to files that the project system has or has not told us about, or which it has added/removed
/// documents for already.
/// <para/>
/// Because of this, this type takes the stance that the actual presence/absence of files is dictated by the project
/// system.  However, if the files are present, the open/closed state and the contents are dictated by the <see
/// cref="LspWorkspaceManager"/>.  This incongruity is not a problem due to how the <see cref="LspWorkspaceManager"/>
/// ends up working.  For example, say the manager believes a file exists with some content, but the project system has
/// removed that file from this workspace.  In that case, the manager will simply not see this type as containing the
/// document, and it will then add the changed doc to the misc workspace.  Similarly, if the project system and
/// workspace manager ever disagree on document contents, that is never itself an issue as the workspace manager always
/// prefers the in-memory source text it is holding onto if the checksums of files change.
/// <para/>
/// Put another way, the lsp workspace manager will use the data in the workspace if it sees it is in alignment with
/// what it believes is the state of the world with respect to <see cref="LSP.Methods.TextDocumentDidOpen"/>/<see
/// cref="LSP.Methods.TextDocumentDidChange"/>/<see cref="LSP.Methods.TextDocumentDidClose"/>.  However, if it is not,
/// it will use the local information it has outside of the workspace to ensure it is always matched with the lsp
/// client.
/// </summary>
internal class LanguageServerWorkspace : Workspace, ILspWorkspace
{
    /// <summary>
    /// Will be set by LanguageServerProjectSystem immediately after creating this instance.  Can't be passed into the
    /// constructor as the factory needs a reference to this type.
    /// </summary>
    public ProjectSystemProjectFactory ProjectSystemProjectFactory { private get; set; } = null!;

    public LanguageServerWorkspace(HostServices host)
        : base(host, WorkspaceKind.Host)
    {
    }

    protected internal override bool PartialSemanticsEnabled => true;

    bool ILspWorkspace.SupportsMutation => true;

    ValueTask ILspWorkspace.UpdateTextIfPresentAsync(DocumentId documentId, SourceText sourceText, CancellationToken cancellationToken)
    {
        // We need to ensure that our changes, and the changes made by the ProjectSystemProjectFactory don't interleave.
        // Specifically, ProjectSystemProjectFactory often makes several changes in a row that it thinks cannot be
        // interrupted.  To ensure this, we call into ProjectSystemProjectFactory to synchronize on the same lock that
        // it has when making workspace changes.
        //
        // https://github.com/dotnet/roslyn/issues/67510 tracks cleaning up ProjectSystemProjectFactory so that it
        // shares the same sync/lock/application code with the core workspace code.  Once that happens, we won't need
        // to do special coordination here.
        return this.ProjectSystemProjectFactory.ApplyChangeToWorkspaceAsync(
            _ =>
            {
                this.OnDocumentTextChanged(documentId, sourceText, PreservationMode.PreserveIdentity, requireDocumentPresent: false);
                return ValueTask.CompletedTask;
            },
            cancellationToken);
    }

    internal override ValueTask TryOnDocumentOpenedAsync(DocumentId documentId, SourceTextContainer textContainer, bool isCurrentContext, CancellationToken cancellationToken)
    {
        return this.ProjectSystemProjectFactory.ApplyChangeToWorkspaceAsync(
            _ =>
            {
                this.OnDocumentOpened(documentId, textContainer, isCurrentContext, requireDocumentPresentAndClosed: false);
                return ValueTask.CompletedTask;
            },
            cancellationToken);
    }

    internal override ValueTask TryOnDocumentClosedAsync(DocumentId documentId, CancellationToken cancellationToken)
    {
        return this.ProjectSystemProjectFactory.ApplyChangeToWorkspaceAsync(
            async w =>
            {
                // TODO(cyrusn): This only works for normal documents currently.  We'll have to rethink how things work
                // in the world if we ever support additionalfiles/editorconfig in our language server.
                var document = w.CurrentSolution.GetDocument(documentId);

                if (document is { FilePath: { } filePath })
                {
                    TextLoader loader;
                    if (document.DocumentState.Attributes.DesignTimeOnly)
                    {
                        // Dynamic files don't exist on disk so if we were to use the FileTextLoader we'd effectively be emptying out the document.
                        // We also assume they're not user editable, and hence can't have "unsaved" changes that are expected to go away on close.
                        // Instead we just maintain their current state as per the LSP view of the world.
                        var documentText = await document.GetTextAsync(cancellationToken);
                        loader = new SourceTextLoader(documentText, filePath);
                    }
                    else
                    {
                        loader = this.ProjectSystemProjectFactory.CreateFileTextLoader(filePath);
                    }

                    this.OnDocumentClosedEx(documentId, loader, requireDocumentPresentAndOpen: false);
                }
            },
            cancellationToken);
    }
}
