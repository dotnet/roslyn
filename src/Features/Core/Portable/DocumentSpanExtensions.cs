// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis
{
    internal static class DocumentSpanExtensions
    {
        [Obsolete("Use CanNavigateToAsync instead", error: false)]
        public static bool CanNavigateTo(this DocumentSpan documentSpan, CancellationToken cancellationToken)
        {
            var workspace = documentSpan.Document.Project.Solution.Workspace;
            var service = workspace.Services.GetService<IDocumentNavigationService>();
            return service.CanNavigateToSpan(workspace, documentSpan.Document.Id, documentSpan.SourceSpan, cancellationToken);
        }

        public static Task<bool> CanNavigateToAsync(this DocumentSpan documentSpan, CancellationToken cancellationToken)
        {
            var workspace = documentSpan.Document.Project.Solution.Workspace;
            var service = workspace.Services.GetService<IDocumentNavigationService>();
            return service.CanNavigateToSpanAsync(workspace, documentSpan.Document.Id, documentSpan.SourceSpan, cancellationToken);
        }

        private static (Workspace workspace, IDocumentNavigationService service, OptionSet options) GetNavigationParts(
            DocumentSpan documentSpan, bool showInPreviewTab, bool activateTab)
        {
            var solution = documentSpan.Document.Project.Solution;
            var workspace = solution.Workspace;
            var service = workspace.Services.GetService<IDocumentNavigationService>();

            var options = solution.Options.WithChangedOption(NavigationOptions.PreferProvisionalTab, showInPreviewTab);
            options = options.WithChangedOption(NavigationOptions.ActivateTab, activateTab);

            return (workspace, service, options);
        }

        [Obsolete("Use TryNavigateToAsync instead", error: false)]
        public static bool TryNavigateTo(this DocumentSpan documentSpan, bool showInPreviewTab, bool activateTab, CancellationToken cancellationToken)
        {
            var (workspace, service, options) = GetNavigationParts(documentSpan, showInPreviewTab, activateTab);

            // We're starting with one doc snapshot, but we're navigating to the current version of the doc.  As such,
            // the span we're trying to navigate to may no longer be there.  Allow for that and don't crash in that case.
            return service.TryNavigateToSpan(
                workspace, documentSpan.Document.Id, documentSpan.SourceSpan, options, allowInvalidSpan: true, cancellationToken);
        }

        public static Task<bool> TryNavigateToAsync(this DocumentSpan documentSpan, bool showInPreviewTab, bool activateTab, CancellationToken cancellationToken)
        {
            var (workspace, service, options) = GetNavigationParts(documentSpan, showInPreviewTab, activateTab);
            return service.TryNavigateToSpanAsync(workspace, documentSpan.Document.Id, documentSpan.SourceSpan, options, cancellationToken);
        }

        public static async Task<bool> IsHiddenAsync(
            this DocumentSpan documentSpan, CancellationToken cancellationToken)
        {
            var document = documentSpan.Document;
            if (document.SupportsSyntaxTree)
            {
                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                return tree.IsHiddenPosition(documentSpan.SourceSpan.Start, cancellationToken);
            }

            return false;
        }
    }
}
