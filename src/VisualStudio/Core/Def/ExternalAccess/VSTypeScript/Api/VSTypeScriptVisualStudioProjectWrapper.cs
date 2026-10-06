// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.VSTypeScript.Api;

internal sealed partial class VSTypeScriptVisualStudioProjectWrapper
{
    public VSTypeScriptVisualStudioProjectWrapper(ProjectSystemProject underlyingObject)
        => Project = underlyingObject;

    public ProjectId Id => Project.Id;

    public string DisplayName
    {
        get => Project.DisplayName;
        set => Project.DisplayName = value;
    }

    public void AddSourceFile(string fullPath)
        => Project.AddSourceFile(fullPath, SourceCodeKind.Regular);

    public DocumentId AddSourceTextContainer(SourceTextContainer sourceTextContainer, string fullPath, bool isLspContainedDocument = false)
    {
        var documentServiceProvider = isLspContainedDocument ? LspContainedDocumentServiceProvider.Instance : null;
        return Project.AddSourceTextContainer(sourceTextContainer, fullPath, SourceCodeKind.Regular, documentServiceProvider: documentServiceProvider);
    }

    public void RemoveSourceFile(string fullPath)
        => Project.RemoveSourceFile(fullPath);

    [Obsolete("Use RemoveVirtualDocument with the document ID instead.")]
    public void RemoveSourceTextContainer(SourceTextContainer sourceTextContainer)
    {
        Project.RemoveSourceTextContainer(sourceTextContainer);
    }

    public void RemoveVirtualDocument(DocumentId documentId)
        => Project.RemoveVirtualDocument(documentId);

    public void RemoveFromWorkspace()
        => Project.RemoveFromWorkspace();

    internal ProjectSystemProject Project { get; }
}
