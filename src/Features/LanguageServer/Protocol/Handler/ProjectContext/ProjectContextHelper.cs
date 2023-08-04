// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal static class ProjectContextHelper
    {
        public static VSProjectContextList? GetContextList(Workspace workspace, Solution solution, Uri documentUri)
        {
            // We specifically don't use context.Document here because we want multiple
            var documents = solution.GetDocuments(documentUri);

            if (!documents.Any())
            {
                return null;
            }

            var contexts = new List<VSProjectContext>();

            foreach (var document in documents)
            {
                var project = document.Project;
                var projectContext = ProtocolConversions.ProjectToProjectContext(project);
                contexts.Add(projectContext);
            }

            // If the document is open, it doesn't matter which DocumentId we pass to GetDocumentIdInCurrentContext since
            // all the documents are linked at that point, so we can just pass the first arbitrarily. If the document is closed
            // GetDocumentIdInCurrentContext will just return the same ID back, which means we're going to pick the first
            // ID in GetDocumentIdsWithFilePath, but there's really nothing we can do since we don't have contexts for
            // close documents anyways.
            var openDocument = documents.First();
            var currentContextDocumentId = workspace.GetDocumentIdInCurrentContext(openDocument.Id);

            return new VSProjectContextList
            {
                ProjectContexts = contexts.ToArray(),
                DefaultIndex = documents.IndexOf(d => d.Id == currentContextDocumentId)
            };
        }
    }
}
