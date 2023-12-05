// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
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

        /// <summary>
        /// Generate the Uri of a document by replace the name in file path using the document's name.
        /// Used to generate the correct Uri when rename a document, because calling <seealso cref="Document.WithName(string)"/> doesn't update the file path.
        /// </summary>
        public static Uri GetUriForRenamedDocument(this TextDocument document)
        {
            Contract.ThrowIfNull(document.FilePath);
            Contract.ThrowIfNull(document.Name);
            Contract.ThrowIfTrue(document is SourceGeneratedDocument);
            var directoryName = Path.GetDirectoryName(document.FilePath);

            Contract.ThrowIfNull(directoryName);
            var path = Path.Combine(directoryName, document.Name);
            return ProtocolConversions.GetUriFromFilePath(path);
        }

        /// <summary>
        /// Generate the Uri of a document based on the name and the project path of the document.
        /// </summary>
        public static Uri GetUriFromProjectPath(this TextDocument document)
        {
            Contract.ThrowIfNull(document.Name);
            Contract.ThrowIfNull(document.Project.FilePath);
            var directoryName = Path.GetDirectoryName(document.Project.FilePath);
            Contract.ThrowIfNull(directoryName);

            var path = Path.Combine(directoryName, document.Name);
            return ProtocolConversions.GetUriFromFilePath(path);
        }

        public static Uri? TryGetURI(this TextDocument document, RequestContext? context = null)
            => ProtocolConversions.TryGetUriFromFilePath(document.FilePath, context);

        public static ImmutableArray<Document> GetDocuments(this Solution solution, Uri documentUri)
        {
            var documentIds = GetDocumentIds(solution, documentUri);

            // We don't call GetRequiredDocument here as the id could be referring to an additional document.
            var documents = documentIds.Select(solution.GetDocument).WhereNotNull().ToImmutableArray();
            return documents;
        }

        public static ImmutableArray<DocumentId> GetDocumentIds(this Solution solution, Uri documentUri)
        {
            // This logic needs to be cleaned up when we support URIs as a first class concept.
            // For now we do our best to handle as many cases as we can.
            // Tracking issue - https://github.com/dotnet/roslyn/issues/68083

            // If the uri is a file then use the simplified absolute or local path.
            // In other cases (e.g. git files) use the full uri string. This ensures documents
            // with the same local path (e.g. git://someFilePath and file://someFilePath) are differentiated.
            if (documentUri.IsFile)
            {
                var fileDocumentIds = solution.GetDocumentIdsWithFilePath(documentUri.AbsolutePath);
                if (fileDocumentIds.Any())
                    return fileDocumentIds;

                fileDocumentIds = solution.GetDocumentIdsWithFilePath(documentUri.LocalPath);
                return fileDocumentIds;
            }
            else
            {
                var documentIds = solution.GetDocumentIdsWithFilePath(documentUri.OriginalString);
                return documentIds;
            }
        }

        public static Document? GetDocument(this Solution solution, TextDocumentIdentifier documentIdentifier)
        {
            var documents = solution.GetDocuments(documentIdentifier.Uri);
            return documents.Length == 0
                ? null
                : documents.FindDocumentInProjectContext(documentIdentifier, (sln, id) => sln.GetRequiredDocument(id));
        }

        private static T FindItemInProjectContext<T>(
            ImmutableArray<T> items,
            TextDocumentIdentifier itemIdentifier,
            Func<T, ProjectId> projectIdGetter,
            Func<T> defaultGetter)
        {
            if (items.Length > 1)
            {
                // We have more than one document; try to find the one that matches the right context
                if (itemIdentifier is VSTextDocumentIdentifier vsDocumentIdentifier && vsDocumentIdentifier.ProjectContext != null)
                {
                    var projectId = ProtocolConversions.ProjectContextToProjectId(vsDocumentIdentifier.ProjectContext);
                    var matchingItem = items.FirstOrDefault(d => projectIdGetter(d) == projectId);

                    if (matchingItem != null)
                    {
                        return matchingItem;
                    }
                }
                else
                {
                    return defaultGetter();
                }
            }

            // We either have only one item or have multiple, but none of them  matched our context. In the
            // latter case, we'll just return the first one arbitrarily since this might just be some temporary mis-sync
            // of client and server state.
            return items[0];
        }

        public static T FindDocumentInProjectContext<T>(this ImmutableArray<T> documents, TextDocumentIdentifier documentIdentifier, Func<Solution, DocumentId, T> documentGetter) where T : TextDocument
        {
            return FindItemInProjectContext(documents, documentIdentifier, projectIdGetter: (item) => item.Project.Id, defaultGetter: () =>
            {
                // We were not passed a project context.  This can happen when the LSP powered NavBar is not enabled.
                // This branch should be removed when we're using the LSP based navbar in all scenarios.

                var solution = documents.First().Project.Solution;
                // Lookup which of the linked documents is currently active in the workspace.
                var documentIdInCurrentContext = solution.Workspace.GetDocumentIdInCurrentContext(documents.First().Id);
                return documentGetter(solution, documentIdInCurrentContext);
            });
        }

        public static Project? GetProject(this Solution solution, TextDocumentIdentifier projectIdentifier)
        {
            var projects = solution.Projects.Where(project => project.FilePath == projectIdentifier.Uri.LocalPath).ToImmutableArray();
            return !projects.Any()
                ? null
                : FindItemInProjectContext(projects, projectIdentifier, projectIdGetter: (item) => item.Id, defaultGetter: () => projects[0]);
        }

        public static TextDocument? GetAdditionalDocument(this Solution solution, TextDocumentIdentifier documentIdentifier)
        {
            var documentIds = GetDocumentIds(solution, documentIdentifier.Uri);

            // We don't call GetRequiredAdditionalDocument as the id could be referring to a regular document.
            var additionalDocuments = documentIds.Select(solution.GetAdditionalDocument).WhereNotNull().ToImmutableArray();
            return !additionalDocuments.Any()
                ? null
                : additionalDocuments.FindDocumentInProjectContext(documentIdentifier, (sln, id) => sln.GetRequiredAdditionalDocument(id));
        }

        public static async Task<int> GetPositionFromLinePositionAsync(this TextDocument document, LinePosition linePosition, CancellationToken cancellationToken)
        {
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
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
