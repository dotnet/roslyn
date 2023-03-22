// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

/// <summary>
/// Mark this type as an <see cref="IMutatingLspWorkspace"/> so that LSP document changes are pushed into this instance,
/// causing our <see cref="Workspace.CurrentSolution"/> to stay in sync with all the document changes.
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
/// 
/// Put another way, the lsp workspace manager will use the data in the workspace if it sees it is in alignment with
/// what it believes is the state of the world with respect to <see cref="LSP.Methods.TextDocumentDidOpen"/>/<see
/// cref="LSP.Methods.TextDocumentDidChange"/>/<see cref="LSP.Methods.TextDocumentDidClose"/>.  However, if it is not,
/// it will use the local information it has outside of the workspace to ensure it is always matched with the lsp
/// client.
/// </summary>
internal class LanguageServerWorkspace : Workspace, IMutatingLspWorkspace
{
    public LanguageServerWorkspace(
        HostServices host)
        : base(host, WorkspaceKind.Host)
    {
    }

    protected internal override bool PartialSemanticsEnabled => true;

    void IMutatingLspWorkspace.CloseIfPresent(DocumentId documentId, string filePath)
        => OnDocumentClosed(
            documentId,
            new WorkspaceFileTextLoader(this.Services.SolutionServices, filePath, defaultEncoding: null),
            _: false,
            requireDocumentPresent: false);

    void IMutatingLspWorkspace.OpenIfPresent(DocumentId documentId, SourceTextContainer container)
        => OnDocumentOpened(documentId, container, isCurrentContext: false, requireDocumentPresent: false);

    void IMutatingLspWorkspace.UpdateTextIfPresent(DocumentId documentId, SourceText sourceText)
        => OnDocumentTextChanged(documentId, sourceText, PreservationMode.PreserveIdentity, requireDocumentPresent: false);
}
