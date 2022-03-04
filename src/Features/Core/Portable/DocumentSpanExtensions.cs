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
        public static Task<bool> CanNavigateToAsync(this DocumentSpan documentSpan, CancellationToken cancellationToken)
        {
            var workspace = documentSpan.Document.Project.Solution.Workspace;
            var service = workspace.Services.GetService<IDocumentNavigationService>();
            return service.CanNavigateToSpanAsync(workspace, documentSpan.Document.Id, documentSpan.SourceSpan, cancellationToken);
        }

        private static (Workspace workspace, IDocumentNavigationService service) GetNavigationParts(DocumentSpan documentSpan)
        {
            var solution = documentSpan.Document.Project.Solution;
            var workspace = solution.Workspace;
            var service = workspace.Services.GetService<IDocumentNavigationService>();
            return (workspace, service);
        }

        public static Task<bool> TryNavigateToAsync(this DocumentSpan documentSpan, NavigationOptions options, CancellationToken cancellationToken)
        {
            var (workspace, service) = GetNavigationParts(documentSpan);
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
