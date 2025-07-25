// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class ISolutionExtensions
{
    extension(Solution solution)
    {
        public async Task<ImmutableArray<INamespaceSymbol>> GetGlobalNamespacesAsync(
        CancellationToken cancellationToken)
        {
            var results = ArrayBuilder<INamespaceSymbol>.GetInstance();

            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId)!;
                if (project.SupportsCompilation)
                {
                    var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
#nullable disable // Can 'compilation' be null here?
                    results.Add(compilation.Assembly.GlobalNamespace);
#nullable enable
                }
            }

            return results.ToImmutableAndFree();
        }

        public TextDocumentKind? GetDocumentKind(DocumentId documentId)
            => solution.GetTextDocument(documentId)?.Kind;

        internal TextDocument? GetTextDocumentForLocation(Location location)
        {
            switch (location.Kind)
            {
                case LocationKind.SourceFile:
                    return solution.GetDocument(location.SourceTree);
                case LocationKind.ExternalFile:
                    var documentId = solution.GetDocumentIdsWithFilePath(location.GetLineSpan().Path).FirstOrDefault();
                    return solution.GetTextDocument(documentId);
                default:
                    return null;
            }
        }

        public Solution WithTextDocumentText(DocumentId documentId, SourceText text, PreservationMode mode = PreservationMode.PreserveIdentity)
        {
            var documentKind = solution.GetDocumentKind(documentId);
            switch (documentKind)
            {
                case TextDocumentKind.Document:
                    return solution.WithDocumentText(documentId, text, mode);

                case TextDocumentKind.AnalyzerConfigDocument:
                    return solution.WithAnalyzerConfigDocumentText(documentId, text, mode);

                case TextDocumentKind.AdditionalDocument:
                    return solution.WithAdditionalDocumentText(documentId, text, mode);

                case null:
                    throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);

                default:
                    throw ExceptionUtilities.UnexpectedValue(documentKind);
            }
        }

        public Workspace? TryGetWorkspace()
            => solution.WorkspaceKind == WorkspaceKind.RemoteWorkspace
                ? null
                : solution.Workspace;
    }
}
