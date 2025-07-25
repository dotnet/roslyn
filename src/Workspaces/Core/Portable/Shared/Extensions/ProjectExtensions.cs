// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class ProjectExtensions
{
    extension(Project project)
    {
        internal Project WithSolutionOptions(OptionSet options)
        => project.Solution.WithOptions(options).GetProject(project.Id)!;

        public TextDocument? GetTextDocument(DocumentId? documentId)
            => project.Solution.GetTextDocument(documentId);

        internal DocumentId? GetDocumentForExternalLocation(Location location)
        {
            Debug.Assert(location.Kind == LocationKind.ExternalFile);
            return project.GetDocumentIdWithFilePath(location.GetLineSpan().Path);
        }

        internal DocumentId? GetDocumentForFile(AdditionalText additionalText)
            => project.GetDocumentIdWithFilePath(additionalText.Path);

        private DocumentId? GetDocumentIdWithFilePath(string filePath)
            => project.Solution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault(id => id.ProjectId == project.Id);

        public Document GetRequiredDocument(DocumentId documentId)
            => project.GetDocument(documentId) ?? throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);

        public Document GetRequiredDocument(SyntaxTree tree)
            => project.GetDocument(tree) ?? throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);

        public TextDocument GetRequiredAdditionalDocument(DocumentId documentId)
            => project.GetAdditionalDocument(documentId) ?? throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);

        public TextDocument GetRequiredAnalyzerConfigDocument(DocumentId documentId)
            => project.GetAnalyzerConfigDocument(documentId) ?? throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);

        public TextDocument GetRequiredTextDocument(DocumentId documentId)
            => project.GetTextDocument(documentId) ?? throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);

        public async ValueTask<Document> GetRequiredSourceGeneratedDocumentAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            var document = await project.GetSourceGeneratedDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
            return document ?? throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);
        }
    }
}
