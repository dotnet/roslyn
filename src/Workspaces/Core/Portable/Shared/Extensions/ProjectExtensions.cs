// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ProjectExtensions
    {
        internal static Project WithSolutionOptions(this Project project, OptionSet options)
            => project.Solution.WithOptions(options).GetProject(project.Id)!;

        public static TextDocument? GetTextDocument(this Project project, DocumentId? documentId)
            => project.Solution.GetTextDocument(documentId);

        internal static DocumentId? GetDocumentForExternalLocation(this Project project, Location location)
        {
            Debug.Assert(location.Kind == LocationKind.ExternalFile);
            return project.GetDocumentIdWithFilePath(location.GetLineSpan().Path);
        }

        internal static DocumentId? GetDocumentForFile(this Project project, AdditionalText additionalText)
            => project.GetDocumentIdWithFilePath(additionalText.Path);

        private static DocumentId? GetDocumentIdWithFilePath(this Project project, string filePath)
            => project.Solution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault(id => id.ProjectId == project.Id);

        public static Document GetRequiredDocument(this Project project, DocumentId documentId)
            => project.GetDocument(documentId) ?? throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);

        public static Document GetRequiredDocument(this Project project, SyntaxTree tree)
            => project.GetDocument(tree) ?? throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);

        public static TextDocument GetRequiredAdditionalDocument(this Project project, DocumentId documentId)
            => project.GetAdditionalDocument(documentId) ?? throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);

        public static TextDocument GetRequiredAnalyzerConfigDocument(this Project project, DocumentId documentId)
            => project.GetAnalyzerConfigDocument(documentId) ?? throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);

        public static TextDocument GetRequiredTextDocument(this Project project, DocumentId documentId)
            => project.GetTextDocument(documentId) ?? throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);

        public static async ValueTask<Document> GetRequiredSourceGeneratedDocumentAsync(this Project project, DocumentId documentId, CancellationToken cancellationToken)
        {
            var document = await project.GetSourceGeneratedDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
            return document ?? throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);
        }
    }
}
