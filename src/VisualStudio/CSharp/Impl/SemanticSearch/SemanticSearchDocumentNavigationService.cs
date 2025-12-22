// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.SemanticSearch;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp;

[ExportWorkspaceService(typeof(IDocumentNavigationService), WorkspaceKind.SemanticSearch), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SemanticSearchDocumentNavigationService(SemanticSearchToolWindowImpl window)
    : AbstractDocumentNavigationService
{
    public override async Task<bool> CanNavigateToSpanAsync(Workspace workspace, DocumentId documentId, TextSpan textSpan, bool allowInvalidSpan, CancellationToken cancellationToken)
        => true;

    public override async Task<INavigableLocation?> GetLocationForSpanAsync(Workspace workspace, DocumentId documentId, TextSpan textSpan, bool allowInvalidSpan, CancellationToken cancellationToken)
    {
        Debug.Assert(workspace is SemanticSearchWorkspace);
        Debug.Assert(documentId == window.SemanticSearchService.GetQueryDocumentId(workspace.CurrentSolution));

        return window.GetNavigableLocation(textSpan);
    }
}
