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
    internal class ProjectCacheContainerKey
    {
        private static readonly ImmutableSortedDictionary<string, string?> EmptyDimensions = ImmutableSortedDictionary.Create<string, string?>(StringComparer.Ordinal);

        /// <summary>
        /// Container key explicitly for the project itself.
        /// </summary>
        public readonly CloudCacheContainerKey? ContainerKey;

        private readonly ConditionalWeakTable<TextDocumentState, StrongBox<CloudCacheContainerKey?>> _documentToContainerKey = new();
        private readonly ConditionalWeakTable<TextDocumentState, StrongBox<CloudCacheContainerKey?>>.CreateValueCallback _documentToContainerKeyCallback;

        public ProjectCacheContainerKey(bool mustSucceed, string relativePathBase, ProjectKey projectKey)
        {
            ContainerKey = CreateProjectContainerKey(mustSucceed, relativePathBase, projectKey);

            _documentToContainerKeyCallback = ds => new(CreateDocumentContainerKey(
                mustSucceed, relativePathBase, DocumentKey.ToDocumentKey(projectKey, ds)));
        }

        public CloudCacheContainerKey? GetValue(TextDocumentState state)
            => _documentToContainerKey.GetValue(state, _documentToContainerKeyCallback).Value;

        public static CloudCacheContainerKey? CreateProjectContainerKey(
            bool mustSucceed, string relativePathBase, ProjectKey projectKey)
        {
            // Creates a container key for this project.  THe container key is a mix of the project's name, relative
            // file path (to the solution), and optional parse options.

            // If we don't have a valid solution path, we can't store anything.
            if (string.IsNullOrEmpty(relativePathBase))
                return mustSucceed ? throw new InvalidOperationException($"{nameof(relativePathBase)} is empty") : null;

            // We have to have a file path for this project
            if (string.IsNullOrEmpty(projectKey.FilePath))
                return mustSucceed ? throw new InvalidOperationException($"{nameof(projectKey.FilePath)} is empty") : null;

            // The file path has to be relative to the solution path.
            var relativePath = PathUtilities.GetRelativePath(relativePathBase, projectKey.FilePath!);
            if (relativePath == projectKey.FilePath)
                return mustSucceed ? throw new InvalidOperationException($"'{projectKey.FilePath}' wasn't relative to '{relativePathBase}'") : null;

            var dimensions = EmptyDimensions
                .Add($"{nameof(ProjectKey)}.{nameof(ProjectKey.Name)}", projectKey.Name)
                .Add($"{nameof(ProjectKey)}.{nameof(ProjectKey.FilePath)}", relativePath)
                .Add($"{nameof(ProjectKey)}.{nameof(ProjectKey.ParseOptionsChecksum)}", projectKey.ParseOptionsChecksum.ToString());

            return new CloudCacheContainerKey("Roslyn.Project", dimensions);
        }

        public static CloudCacheContainerKey? CreateDocumentContainerKey(
            bool mustSucceed,
            string relativePathBase,
            DocumentKey documentKey)
        {
            // See if we can get a project key for this info.  If not, we def can't get a doc key.
            var projectContinerKey = CreateProjectContainerKey(mustSucceed, relativePathBase, documentKey.Project);
            if (projectContinerKey == null)
                return null;

            // We have to have a file path for this document
            if (string.IsNullOrEmpty(documentKey.FilePath))
                return mustSucceed ? throw new InvalidOperationException($"{nameof(documentKey.FilePath)} is empty") : null;

            // The file path has to be relative to the solution path.
            var relativePath = PathUtilities.GetRelativePath(relativePathBase, documentKey.FilePath!);
            if (relativePath == documentKey.FilePath)
                return mustSucceed ? throw new InvalidOperationException($"'{documentKey.FilePath}' wasn't relative to '{relativePathBase}'") : null;

            var dimensions = projectContinerKey.Value.Dimensions
                .Add($"{nameof(DocumentKey)}.{nameof(DocumentKey.Name)}", documentKey.Name)
                .Add($"{nameof(DocumentKey)}.{nameof(DocumentKey.FilePath)}", relativePath);

            return new CloudCacheContainerKey("Roslyn.Document", dimensions);
        }
    }
}
