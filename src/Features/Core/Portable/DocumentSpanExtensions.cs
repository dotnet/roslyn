// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public static bool TryNavigateTo(this DocumentSpan documentSpan, bool isPreview)
        {
            var solution = documentSpan.Document.Project.Solution;
            var workspace = solution.Workspace;
            var service = workspace.Services.GetService<IDocumentNavigationService>();
            return service.TryNavigateToSpan(workspace, documentSpan.Document.Id, documentSpan.SourceSpan,
                options: solution.Options.WithChangedOption(NavigationOptions.PreferProvisionalTab, isPreview));
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
