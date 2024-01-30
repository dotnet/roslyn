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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Text.Adornments;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal static class Extensions
    {
        public static Uri GetURI(this TextDocument document)
        {
            Contract.ThrowIfNull(document.FilePath);
            return document is SourceGeneratedDocument sourceGeneratedDocument
                ? ProtocolConversions.CreateUriForSourceGeneratedDocument(sourceGeneratedDocument)
                : ProtocolConversions.CreateAbsoluteUri(document.FilePath);
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
            return ProtocolConversions.CreateAbsoluteUri(path);
        }

        public static Uri CreateUriForDocumentWithoutFilePath(this TextDocument document)
        {
            Contract.ThrowIfNull(document.Name);
            Contract.ThrowIfNull(document.Project.FilePath);

            var projectDirectoryName = Path.GetDirectoryName(document.Project.FilePath);
            Contract.ThrowIfNull(projectDirectoryName);
            var path = Path.Combine([projectDirectoryName, .. document.Folders, document.Name]);
            return ProtocolConversions.CreateAbsoluteUri(path);
        }

        public static ImmutableArray<Document> GetDocuments(this Solution solution, Uri documentUri)
            => GetDocuments(solution, ProtocolConversions.GetDocumentFilePathFromUri(documentUri));

        public static ImmutableArray<Document> GetDocuments(this Solution solution, string documentPath)
        {
            var documentIds = solution.GetDocumentIdsWithFilePath(documentPath);

            // We don't call GetRequiredDocument here as the id could be referring to an additional document.
            var documents = documentIds.Select(solution.GetDocument).WhereNotNull().ToImmutableArray();
            return documents;
        }

        /// <summary>
        /// Get all regular and additional <see cref="TextDocument"/>s for the given <paramref name="documentUri"/>.
        /// </summary>
        public static ImmutableArray<TextDocument> GetTextDocuments(this Solution solution, Uri documentUri)
        {
            var documentIds = GetDocumentIds(solution, documentUri);

            var documents = documentIds
                .Select(solution.GetDocument)
                .Concat(documentIds.Select(solution.GetAdditionalDocument))
                .WhereNotNull()
                .ToImmutableArray();
            return documents;
        }

        public static ImmutableArray<DocumentId> GetDocumentIds(this Solution solution, Uri documentUri)
        {
            // If this is not our special scheme for generated documents, then we can just look for documents with that file path.
            if (documentUri.Scheme != ProtocolConversions.SourceGeneratedFileScheme)
                return solution.GetDocumentIdsWithFilePath(ProtocolConversions.GetDocumentFilePathFromUri(documentUri));

            // This is a generated document, so the "host" portion is just the GUID of the project ID; we'll parse that into an ID and then
            // look up the project in the Solution. This relies on the fact that technically the only part of the ID that matters for equality
            // is the GUID; looking up the project again means we can then recover the ProjectId with the debug name, so anybody looking at a crash
            // dump sees a "normal" ID. It also means if the project is gone we can trivially say there are no usable IDs anymore.
            var projectIdGuidOnly = ProjectId.CreateFromSerialized(Guid.ParseExact(documentUri.Host, ProtocolConversions.SourceGeneratedGuidFormat));
            var projectId = solution.GetProject(projectIdGuidOnly)?.Id;

            if (projectId == null)
                return ImmutableArray<DocumentId>.Empty;

            // The AbsolutePath will consist of a leading / to ignore, then the GUID that is the DocumentId, and then another slash, then the hint path
            var slashAfterId = documentUri.AbsolutePath.IndexOf('/', startIndex: 1);
            Contract.ThrowIfFalse(slashAfterId > 0, $"The URI '{documentUri}' is not formatted correctly.");

            var documentIdGuidSpan = documentUri.AbsolutePath.AsSpan()[1..slashAfterId];
            var documentIdGuid =
#if NET // netstandard2.0 doesn't have Parse methods that take Spans
                Guid.ParseExact(documentIdGuidSpan, ProtocolConversions.SourceGeneratedGuidFormat);
#else
                Guid.ParseExact(documentIdGuidSpan.ToString(), ProtocolConversions.SourceGeneratedGuidFormat);
#endif

            return ImmutableArray.Create(DocumentId.CreateFromSerialized(projectId, documentIdGuid, isSourceGenerated: true, debugName: documentUri.AbsolutePath.Substring(slashAfterId + 1)));
        }

        /// <summary>
        /// Finds a regular document for a TextDocumentIdentifier; if you need to also return source generated files, call <see cref="GetDocumentAsync"/>.
        /// </summary>
        public static Document? GetDocument(this Solution solution, TextDocumentIdentifier documentIdentifier)
        {
            var documents = solution.GetDocuments(documentIdentifier.Uri);
            return documents.Length == 0
                ? null
                : documents.FindDocumentInProjectContext(documentIdentifier, (sln, id) => sln.GetRequiredDocument(id));
        }

        /// <summary>
        /// Finds the document for a TextDocumentIdentifier, potentially returning a source-generated file.
        /// </summary>
        public static async ValueTask<Document?> GetDocumentAsync(this Solution solution, TextDocumentIdentifier documentIdentifier, CancellationToken cancellationToken)
        {
            if (documentIdentifier.Uri.Scheme == ProtocolConversions.SourceGeneratedFileScheme)
            {
                // In the case of a URI scheme for source generated files, we generate a different URI for each project, thus this URI cannot be linked into multiple projects;
                // this means we can safely call .Single() and not worry about calling FindDocumentInProjectContext.
                var documentId = solution.GetDocumentIds(documentIdentifier.Uri).Single();
                return await solution.GetDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

            }
            else
            {
                return solution.GetDocument(documentIdentifier);
            }
        }

        public static async ValueTask<TextDocument?> GetTextDocumentAsync(this Solution solution, TextDocumentIdentifier documentIdentifier, CancellationToken cancellationToken)
        {
            // If it's the URI scheme for source generated files, delegate to our other helper, otherwise we can handle anything else here.
            if (documentIdentifier.Uri.Scheme == ProtocolConversions.SourceGeneratedFileScheme)
            {
                return await solution.GetDocumentAsync(documentIdentifier, cancellationToken).ConfigureAwait(false);
            }

            var documents = solution.GetTextDocuments(documentIdentifier.Uri);
            return documents.Length == 0
                ? null
                : documents.FindDocumentInProjectContext(documentIdentifier, (sln, id) => sln.GetRequiredTextDocument(id));
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
