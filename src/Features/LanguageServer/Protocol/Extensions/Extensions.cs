﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal static class Extensions
    {
        public static Uri GetURI(this Document document)
        {
            return ProtocolConversions.GetUriFromFilePath(document.FilePath);
        }

        public static ImmutableArray<Document> GetDocuments(this Solution solution, Uri documentUri)
        {
            // TODO: we need to normalize this. but for now, we check both absolute and local path
            //       right now, based on who calls this, solution might has "/" or "\\" as directory
            //       separator
            var documentIds = solution.GetDocumentIdsWithFilePath(documentUri.AbsolutePath);

            if (!documentIds.Any())
            {
                documentIds = solution.GetDocumentIdsWithFilePath(documentUri.LocalPath);
            }

            return documentIds.SelectAsArray(id => solution.GetRequiredDocument(id));
        }

        public static ImmutableArray<Document> GetDocuments(this ILspSolutionProvider solutionProvider, Uri uri, string? clientName)
        {
            var documents = solutionProvider.GetDocuments(uri);

            // If we don't have a client name, then we're done filtering
            if (clientName == null)
            {
                return documents;
            }

            // We have a client name, so we need to filter to only documents that match that name
            return documents.WhereAsArray(document =>
            {
                var documentPropertiesService = document.Services.GetService<DocumentPropertiesService>();

                // When a client name is specified, only return documents that have a matching client name.
                // Allows the razor lsp server to return results only for razor documents.
                // This workaround should be removed when https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1106064/
                // is fixed (so that the razor language server is only asked about razor buffers).
                return Equals(documentPropertiesService?.DiagnosticsLspClientName, clientName);
            });
        }

        public static Document? GetDocument(this ILspSolutionProvider solutionProvider, TextDocumentIdentifier documentIdentifier, string? clientName = null)
        {
            var documents = solutionProvider.GetDocuments(documentIdentifier.Uri, clientName);

            if (documents.Length == 0)
            {
                return null;
            }

            if (documents.Length > 1)
            {
                // We have more than one document; try to find the one that matches the right context
                if (documentIdentifier is VSTextDocumentIdentifier vsDocumentIdentifier)
                {
                    if (vsDocumentIdentifier.ProjectContext != null)
                    {
                        var projectId = ProtocolConversions.ProjectContextToProjectId(vsDocumentIdentifier.ProjectContext);
                        var matchingDocument = documents.FirstOrDefault(d => d.Project.Id == projectId);

                        if (matchingDocument != null)
                        {
                            return matchingDocument;
                        }
                    }
                }
            }

            // We either have only one document or have multiple, but none of them  matched our context. In the
            // latter case, we'll just return the first one arbitrarily since this might just be some temporary mis-sync
            // of client and server state.
            return documents[0];
        }

        public static async Task<int> GetPositionFromLinePositionAsync(this Document document, LinePosition linePosition, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return text.Lines.GetPosition(linePosition);
        }

        public static bool HasVisualStudioLspCapability(this ClientCapabilities clientCapabilities)
        {
            if (clientCapabilities is VSClientCapabilities vsClientCapabilities)
            {
                return vsClientCapabilities.SupportsVisualStudioExtensions;
            }

            return false;
        }

        public static string GetMarkdownLanguageName(this Document document)
        {
            switch (document.Project.Language)
            {
                case LanguageNames.CSharp:
                    return "csharp";
                case LanguageNames.VisualBasic:
                    return "vb";
                case LanguageNames.FSharp:
                    return "fsharp";
                case "TypeScript":
                    return "typescript";
                default:
                    throw new ArgumentException(string.Format("Document project language {0} is not valid", document.Project.Language));
            }
        }

        public static ClassifiedTextElement GetClassifiedText(this DefinitionItem definition)
            => new ClassifiedTextElement(definition.DisplayParts.Select(part => new ClassifiedTextRun(part.Tag.ToClassificationTypeName(), part.Text)));
    }
}
