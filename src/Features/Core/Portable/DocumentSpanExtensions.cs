// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Navigation;

namespace Microsoft.CodeAnalysis
{
    internal static class DocumentSpanExtensions
    {
        public static bool CanNavigateTo(this DocumentSpan documentSpan)
        {
            var workspace = documentSpan.Document.Project.Solution.Workspace;
            var service = workspace.Services.GetService<IDocumentNavigationService>();
            return service.CanNavigateToSpan(workspace, documentSpan.Document.Id, documentSpan.SourceSpan);
        }

        public static bool TryNavigateTo(this DocumentSpan documentSpan, bool showInPreviewTab, bool activateTab)
        {
            var solution = documentSpan.Document.Project.Solution;
            var workspace = solution.Workspace;
            var service = workspace.Services.GetService<IDocumentNavigationService>();

            var options = solution.Options.WithChangedOption(NavigationOptions.PreferProvisionalTab, showInPreviewTab);
            options = options.WithChangedOption(NavigationOptions.ActivateTab, activateTab);

            return service.TryNavigateToSpan(workspace, documentSpan.Document.Id, documentSpan.SourceSpan, options);
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
