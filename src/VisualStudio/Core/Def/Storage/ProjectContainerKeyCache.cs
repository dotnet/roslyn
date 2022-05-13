// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.VisualStudio.RpcContracts.Caching;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Storage
{
    /// <summary>
    /// Cache of our own internal roslyn storage keys to the equivalent platform cloud cache keys.  Cloud cache keys can
    /// store a lot of date in them (like their 'dimensions' dictionary.  We don't want to continually recreate these as
    /// we read/write date to the db.
    /// </summary>
    internal class ProjectContainerKeyCache
    {
        private static readonly ImmutableSortedDictionary<string, string?> EmptyDimensions = ImmutableSortedDictionary.Create<string, string?>(StringComparer.Ordinal);

        /// <summary>
        /// Container key explicitly for the project itself.
        /// </summary>
        public readonly CacheContainerKey? ProjectContainerKey;

        /// <summary>
        /// Cache from document green nodes to the container keys we've computed for it. We can avoid computing these
        /// container keys when called repeatedly for the same documents.
        /// </summary>
        /// <remarks>
        /// We can use a normal Dictionary here instead of a <see cref="ConditionalWeakTable{TKey, TValue}"/> as
        /// instances of <see cref="ProjectContainerKeyCache"/> are always owned in a context where the <see
        /// cref="ProjectState"/> is alive.  As that instance is alive, all <see cref="TextDocumentState"/>s the project
        /// points at will be held alive strongly too.
        /// </remarks>
        private readonly Dictionary<TextDocumentState, CacheContainerKey?> _documentToContainerKey = new();
        private readonly Func<TextDocumentState, CacheContainerKey?> _documentToContainerKeyCallback;

        public ProjectContainerKeyCache(string relativePathBase, ProjectKey projectKey)
        {
            ProjectContainerKey = CreateProjectContainerKey(relativePathBase, projectKey);

            _documentToContainerKeyCallback = ds => CreateDocumentContainerKey(relativePathBase, DocumentKey.ToDocumentKey(projectKey, ds));
        }

        public CacheContainerKey? GetDocumentContainerKey(TextDocumentState state)
        {
            lock (_documentToContainerKey)
                return _documentToContainerKey.GetOrAdd(state, _documentToContainerKeyCallback);
        }

        public static CacheContainerKey? CreateProjectContainerKey(
            string relativePathBase, ProjectKey projectKey)
        {
            // Creates a container key for this project.  The container key is a mix of the project's name, relative
            // file path (to the solution), and optional parse options.

            // If we don't have a valid solution path, we can't store anything.
            if (string.IsNullOrEmpty(relativePathBase))
                return null;

            // We have to have a file path for this project
            if (RoslynString.IsNullOrEmpty(projectKey.FilePath))
                return null;

            // The file path has to be relative to the base path the DB is associated with (either the solution-path or
            // repo-path).
            var relativePath = PathUtilities.GetRelativePath(relativePathBase, projectKey.FilePath!);
            if (relativePath == projectKey.FilePath)
                return null;

            var dimensions = EmptyDimensions
                .Add($"{nameof(ProjectKey)}.{nameof(ProjectKey.Name)}", projectKey.Name)
                .Add($"{nameof(ProjectKey)}.{nameof(ProjectKey.FilePath)}", relativePath)
                .Add($"{nameof(ProjectKey)}.{nameof(ProjectKey.ParseOptionsChecksum)}", projectKey.ParseOptionsChecksum.ToString());

            return new CacheContainerKey("Roslyn.Project", dimensions);
        }

        public static CacheContainerKey? CreateDocumentContainerKey(
            string relativePathBase,
            DocumentKey documentKey)
        {
            // See if we can get a project key for this info.  If not, we def can't get a doc key.
            var projectContainerKey = CreateProjectContainerKey(relativePathBase, documentKey.Project);
            if (projectContainerKey == null)
                return null;

            // We have to have a file path for this document
            if (string.IsNullOrEmpty(documentKey.FilePath))
                return null;

            // The file path has to be relative to the base path the DB is associated with (either the solution-path or
            // repo-path).
            var relativePath = PathUtilities.GetRelativePath(relativePathBase, documentKey.FilePath!);
            if (relativePath == documentKey.FilePath)
                return null;

            var dimensions = projectContainerKey.Value.Dimensions
                .Add($"{nameof(DocumentKey)}.{nameof(DocumentKey.Name)}", documentKey.Name)
                .Add($"{nameof(DocumentKey)}.{nameof(DocumentKey.FilePath)}", relativePath);

            return new CacheContainerKey("Roslyn.Document", dimensions);
        }
    }
}
