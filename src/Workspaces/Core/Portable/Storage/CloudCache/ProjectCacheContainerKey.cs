// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.PersistentStorage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Storage
{
    /// <summary>
    /// Cache of our own internal roslyn storage keys to the equivalent platform cloud cache keys.  Cloud cache keys can
    /// store a lot of date in them (like their 'dimensions' dictionary.  We don't want to continually recreate these as
    /// we read/write date to the db.
    /// </summary>
    internal class ProjectCacheContainerKey
    {
        private static readonly ImmutableSortedDictionary<string, string?> EmptyDimensions = ImmutableSortedDictionary.Create<string, string?>(StringComparer.Ordinal);

        /// <summary>
        /// Container key explicitly for the project itself.
        /// </summary>
        public readonly CloudCacheContainerKey? ContainerKey;

        /// <summary>
        /// Cache from document green nodes to the container keys we've computed for it. We can avoid computing these
        /// container keys when called repeatedly for the same documents.
        /// </summary>
        private readonly ConditionalWeakTable<TextDocumentState, StrongBox<CloudCacheContainerKey?>> _documentToContainerKey = new();
        private readonly ConditionalWeakTable<TextDocumentState, StrongBox<CloudCacheContainerKey?>>.CreateValueCallback _documentToContainerKeyCallback;

        public ProjectCacheContainerKey(string relativePathBase, ProjectKey projectKey)
        {
            ContainerKey = CreateProjectContainerKey(relativePathBase, projectKey);

            _documentToContainerKeyCallback = ds => new(CreateDocumentContainerKey(
                relativePathBase, DocumentKey.ToDocumentKey(projectKey, ds)));
        }

        public CloudCacheContainerKey? GetValue(TextDocumentState state)
            => _documentToContainerKey.GetValue(state, _documentToContainerKeyCallback).Value;

        public static CloudCacheContainerKey? CreateProjectContainerKey(
            string relativePathBase, ProjectKey projectKey)
        {
            // Creates a container key for this project.  THe container key is a mix of the project's name, relative
            // file path (to the solution), and optional parse options.

            // If we don't have a valid solution path, we can't store anything.
            if (string.IsNullOrEmpty(relativePathBase))
                return null;

            // We have to have a file path for this project
            if (string.IsNullOrEmpty(projectKey.FilePath))
                return null;

            // The file path has to be relative to the solution path.
            var relativePath = PathUtilities.GetRelativePath(relativePathBase, projectKey.FilePath!);
            if (relativePath == projectKey.FilePath)
                return null;

            var dimensions = EmptyDimensions
                .Add($"{nameof(ProjectKey)}.{nameof(ProjectKey.Name)}", projectKey.Name)
                .Add($"{nameof(ProjectKey)}.{nameof(ProjectKey.FilePath)}", relativePath)
                .Add($"{nameof(ProjectKey)}.{nameof(ProjectKey.ParseOptionsChecksum)}", projectKey.ParseOptionsChecksum.ToString());

            return new CloudCacheContainerKey("Roslyn.Project", dimensions);
        }

        public static CloudCacheContainerKey? CreateDocumentContainerKey(
            string relativePathBase,
            DocumentKey documentKey)
        {
            // See if we can get a project key for this info.  If not, we def can't get a doc key.
            var projectContinerKey = CreateProjectContainerKey(relativePathBase, documentKey.Project);
            if (projectContinerKey == null)
                return null;

            // We have to have a file path for this document
            if (string.IsNullOrEmpty(documentKey.FilePath))
                return null;

            // The file path has to be relative to the solution path.
            var relativePath = PathUtilities.GetRelativePath(relativePathBase, documentKey.FilePath!);
            if (relativePath == documentKey.FilePath)
                return null;

            var dimensions = projectContinerKey.Value.Dimensions
                .Add($"{nameof(DocumentKey)}.{nameof(DocumentKey.Name)}", documentKey.Name)
                .Add($"{nameof(DocumentKey)}.{nameof(DocumentKey.FilePath)}", relativePath);

            return new CloudCacheContainerKey("Roslyn.Document", dimensions);
        }
    }
}
