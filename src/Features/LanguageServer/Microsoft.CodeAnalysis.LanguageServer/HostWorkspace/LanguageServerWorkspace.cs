// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

/// <summary>
/// Mark this type as an <see cref="IMutatingLspWorkspace"/> so that LSP document changes are pushed into this instance,
/// causing our <see cref="Workspace.CurrentSolution"/> to stay in sync with all the document changes.
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
        => UpdateTextIfPresent(documentId, sourceText);
}
