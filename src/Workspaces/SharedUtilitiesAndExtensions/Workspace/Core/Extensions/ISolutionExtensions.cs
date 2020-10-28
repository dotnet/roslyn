// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ISolutionExtensions
    {
        public static IEnumerable<DocumentId> GetChangedDocuments(this Solution? newSolution, Solution oldSolution)
        {
            if (newSolution != null)
            {
                var solutionChanges = newSolution.GetChanges(oldSolution);

                foreach (var projectChanges in solutionChanges.GetProjectChanges())
                {
                    foreach (var documentId in projectChanges.GetChangedDocuments())
                    {
                        yield return documentId;
                    }
                }
            }
        }

        public static TextDocument? GetTextDocument(this Solution solution, DocumentId? documentId)
            => solution.GetDocument(documentId) ?? solution.GetAdditionalDocument(documentId) ?? solution.GetAnalyzerConfigDocument(documentId);

        public static Document GetRequiredDocument(this Solution solution, SyntaxTree syntaxTree)
            => solution.GetDocument(syntaxTree) ?? throw new InvalidOperationException();

        public static Project GetRequiredProject(this Solution solution, ProjectId projectId)
        {
            var project = solution.GetProject(projectId);
            Contract.ThrowIfNull(project);
            return project;
        }

        public static Document GetRequiredDocument(this Solution solution, DocumentId documentId)
        {
            var document = solution.GetDocument(documentId);
            Contract.ThrowIfNull(document);
            return document;
        }

        public static TextDocument GetRequiredAdditionalDocument(this Solution solution, DocumentId documentId)
        {
            var document = solution.GetAdditionalDocument(documentId);
            Contract.ThrowIfNull(document);
            return document;
        }

        public static TextDocument GetRequiredAnalyzerConfigDocument(this Solution solution, DocumentId documentId)
        {
            var document = solution.GetAnalyzerConfigDocument(documentId);
            Contract.ThrowIfNull(document);
            return document;
        }

        public static TextDocument GetRequiredTextDocument(this Solution solution, DocumentId documentId)
        {
            var document = solution.GetTextDocument(documentId);
            Contract.ThrowIfNull(document);
            return document;
        }
    }
}
