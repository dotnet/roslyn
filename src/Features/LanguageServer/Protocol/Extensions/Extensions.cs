// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal static class Extensions
    {
        public static Uri GetURI(this TextDocument document)
        {
            Contract.ThrowIfNull(document.FilePath);
            return document is SourceGeneratedDocument
                ? ProtocolConversions.GetUriFromPartialFilePath(document.FilePath)
                : ProtocolConversions.GetUriFromFilePath(document.FilePath);
        }

        public static Uri? TryGetURI(this TextDocument document, RequestContext? context = null)
            => ProtocolConversions.TryGetUriFromFilePath(document.FilePath, context);

        public static ImmutableArray<Document> GetDocuments(this Solution solution, Uri documentUri)
            => GetDocuments(solution, documentUri, clientName: null, logger: null);

        public static ImmutableArray<Document> GetDocuments(this Solution solution, Uri documentUri, string? clientName)
            => GetDocuments(solution, documentUri, clientName, logger: null);

        public static ImmutableArray<Document> GetDocuments(this Solution solution, Uri documentUri, string? clientName, ILspLogger? logger)
        {
            var documentIds = GetDocumentIds(solution, documentUri);

            var documents = documentIds.SelectAsArray(id => solution.GetRequiredDocument(id));

            return FilterDocumentsByClientName(documents, clientName, logger);
        }

        public static ImmutableArray<DocumentId> GetDocumentIds(this Solution solution, Uri documentUri)
        {
            // TODO: we need to normalize this. but for now, we check both absolute and local path
            //       right now, based on who calls this, solution might has "/" or "\\" as directory
            //       separator
            var documentIds = solution.GetDocumentIdsWithFilePath(documentUri.AbsolutePath);

            if (!documentIds.Any())
            {
                documentIds = solution.GetDocumentIdsWithFilePath(documentUri.LocalPath);
            }

            return documentIds;
        }

        private static ImmutableArray<Document> FilterDocumentsByClientName(ImmutableArray<Document> documents, string? clientName, ILspLogger? logger)
        {
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
                var documentClientName = documentPropertiesService?.DiagnosticsLspClientName;
                var clientNameMatch = Equals(documentClientName, clientName);
                if (!clientNameMatch && logger is not null)
                {
                    logger.TraceInformation($"Found matching document but it's client name '{documentClientName}' is not a match.");
                }

                return clientNameMatch;
            });
        }

        public static Document? GetDocument(this Solution solution, TextDocumentIdentifier documentIdentifier)
            => solution.GetDocument(documentIdentifier, clientName: null);

        public static Document? GetDocument(this Solution solution, TextDocumentIdentifier documentIdentifier, string? clientName)
        {
            var documents = solution.GetDocuments(documentIdentifier.Uri, clientName, logger: null);
            if (documents.Length == 0)
            {
                return null;
            }

            return documents.FindDocumentInProjectContext(documentIdentifier);
        }

        public static Document FindDocumentInProjectContext(this ImmutableArray<Document> documents, TextDocumentIdentifier documentIdentifier)
        {
            if (documents.Length > 1)
            {
                // We have more than one document; try to find the one that matches the right context
                if (documentIdentifier is VSTextDocumentIdentifier vsDocumentIdentifier && vsDocumentIdentifier.ProjectContext != null)
                {
                    var projectId = ProtocolConversions.ProjectContextToProjectId(vsDocumentIdentifier.ProjectContext);
                    var matchingDocument = documents.FirstOrDefault(d => d.Project.Id == projectId);

                    if (matchingDocument != null)
                    {
                        return matchingDocument;
                    }
                }
                else
                {
                    // We were not passed a project context.  This can happen when the LSP powered NavBar is not enabled.
                    // This branch should be removed when we're using the LSP based navbar in all scenarios.

                    var solution = documents.First().Project.Solution;
                    // Lookup which of the linked documents is currently active in the workspace.
                    var documentIdInCurrentContext = solution.Workspace.GetDocumentIdInCurrentContext(documents.First().Id);
                    return solution.GetRequiredDocument(documentIdInCurrentContext);
                }
            }

            // We either have only one document or have multiple, but none of them  matched our context. In the
            // latter case, we'll just return the first one arbitrarily since this might just be some temporary mis-sync
            // of client and server state.
            return documents[0];
        }

        public static async Task<int> GetPositionFromLinePositionAsync(this TextDocument document, LinePosition linePosition, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return text.Lines.GetPosition(linePosition);
        }

        public static bool HasVisualStudioLspCapability(this ClientCapabilities? clientCapabilities)
        {
            if (clientCapabilities is VSInternalClientCapabilities vsClientCapabilities)
            {
                return vsClientCapabilities.SupportsVisualStudioExtensions;
            }

            return false;
        }

        public static bool HasCompletionListDataCapability(this ClientCapabilities clientCapabilities)
        {
            if (!TryGetVSCompletionListSetting(clientCapabilities, out var completionListSetting))
            {
                return false;
            }

            return completionListSetting.Data;
        }

        public static bool HasCompletionListCommitCharactersCapability(this ClientCapabilities clientCapabilities)
        {
            if (!TryGetVSCompletionListSetting(clientCapabilities, out var completionListSetting))
            {
                return false;
            }

            return completionListSetting.CommitCharacters;
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
                case InternalLanguageNames.TypeScript:
                    return "typescript";
                default:
                    throw new ArgumentException(string.Format("Document project language {0} is not valid", document.Project.Language));
            }
        }

        public static ClassifiedTextElement GetClassifiedText(this DefinitionItem definition)
            => new ClassifiedTextElement(definition.DisplayParts.Select(part => new ClassifiedTextRun(part.Tag.ToClassificationTypeName(), part.Text)));

        public static bool IsRazorDocument(this Document document)
        {
            // Only razor docs have an ISpanMappingService, so we can use the presence of that to determine if this doc
            // belongs to them.
            var spanMapper = document.Services.GetService<ISpanMappingService>();
            return spanMapper != null;
        }

        private static bool TryGetVSCompletionListSetting(ClientCapabilities clientCapabilities, [NotNullWhen(returnValue: true)] out VSInternalCompletionListSetting? completionListSetting)
        {
            if (clientCapabilities is not VSInternalClientCapabilities vsClientCapabilities)
            {
                completionListSetting = null;
                return false;
            }

            var textDocumentCapability = vsClientCapabilities.TextDocument;
            if (textDocumentCapability == null)
            {
                completionListSetting = null;
                return false;
            }

            if (textDocumentCapability.Completion is not VSInternalCompletionSetting vsCompletionSetting)
            {
                completionListSetting = null;
                return false;
            }

            completionListSetting = vsCompletionSetting.CompletionList;
            if (completionListSetting == null)
            {
                return false;
            }

            return true;
        }
    }
}
