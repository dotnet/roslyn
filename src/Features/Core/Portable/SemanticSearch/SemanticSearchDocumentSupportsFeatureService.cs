// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared;

namespace Microsoft.CodeAnalysis.SemanticSearch;

[ExportWorkspaceService(typeof(IDocumentSupportsFeatureService), WorkspaceKind.SemanticSearch), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SemanticSearchDocumentSupportsFeatureService(
    [Import(AllowDefault = true)] Lazy<ISemanticSearchSolutionService>? solutionService) : IDocumentSupportsFeatureService
{
    private bool IsQueryDocument(Document document)
    {
        Contract.ThrowIfNull(solutionService);
        return solutionService.Value.GetQueryDocumentId(document.Project.Solution) == document.Id;
    }

    public bool SupportsCodeFixes(Document document)
        => IsQueryDocument(document);

    public bool SupportsRefactorings(Document document)
        => IsQueryDocument(document);

    public bool SupportsRename(Document document)
        => IsQueryDocument(document);

    public bool SupportsNavigationToAnyPosition(Document document)
        => IsQueryDocument(document);

    public bool SupportsSemanticSnippets(Document document)
        => IsQueryDocument(document);
}
