// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis
{
    internal static class DocumentSpanExtensions
    {
        private static (Workspace workspace, IDocumentNavigationService service) GetNavigationParts(DocumentSpan documentSpan)
        {
            var solution = documentSpan.Document.Project.Solution;
            var workspace = solution.Workspace;
            var service = workspace.Services.GetRequiredService<IDocumentNavigationService>();
            return (workspace, service);
        }

        public static Task<INavigableLocation?> GetNavigableLocationAsync(this DocumentSpan documentSpan, CancellationToken cancellationToken)
        {
            var (workspace, service) = GetNavigationParts(documentSpan);
            return service.GetLocationForSpanAsync(workspace, documentSpan.Document.Id, documentSpan.SourceSpan, allowInvalidSpan: false, cancellationToken);
        }

        public static async Task<bool> IsHiddenAsync(
            this DocumentSpan documentSpan, CancellationToken cancellationToken)
        {
            var document = documentSpan.Document;
            if (document.SupportsSyntaxTree)
            {
                var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                return tree.IsHiddenPosition(documentSpan.SourceSpan.Start, cancellationToken);
            }

            return false;
        }
    }
}
