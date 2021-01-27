// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
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

        public ProjectCacheContainerKey(bool mustSucceed, string relativePathBase, string? projectFilePath, string projectName, Checksum parseOptionsChecksum)
        {
            ContainerKey = CreateProjectContainerKey(mustSucceed, relativePathBase, projectFilePath, projectName, parseOptionsChecksum);

            _documentToContainerKeyCallback = ds => new(CreateDocumentContainerKey(
                mustSucceed, relativePathBase, projectFilePath, projectName, parseOptionsChecksum, ds.FilePath, ds.Name));
        }

        public CloudCacheContainerKey? GetValue(TextDocumentState state)
            => _documentToContainerKey.GetValue(state, _documentToContainerKeyCallback).Value;

        public static CloudCacheContainerKey? CreateProjectContainerKey(
            bool mustSucceed, string relativePathBase, string? projectFilePath, string projectName, Checksum? parseOptionsChecksum)
        {
            // Creates a container key for this project.  THe container key is a mix of the project's name, relative
            // file path (to the solution), and optional parse options.

            // If we don't have a valid solution path, we can't store anything.
            if (string.IsNullOrEmpty(relativePathBase))
                return mustSucceed ? throw new InvalidOperationException($"{nameof(relativePathBase)} is empty") : null;

            // We have to have a file path for this project
            if (string.IsNullOrEmpty(projectFilePath))
                return mustSucceed ? throw new InvalidOperationException($"{nameof(projectFilePath)} is empty") : null;

            // The file path has to be relative to the solution path.
            var relativePath = PathUtilities.GetRelativePath(relativePathBase, projectFilePath!);
            if (relativePath == projectFilePath)
                return mustSucceed ? throw new InvalidOperationException($"'{projectFilePath}' wasn't relative to '{relativePathBase}'") : null;

            var dimensions = EmptyDimensions
                .Add(nameof(projectName), projectName)
                .Add(nameof(projectFilePath), relativePath);

            if (parseOptionsChecksum != null)
                dimensions = dimensions.Add(nameof(parseOptionsChecksum), parseOptionsChecksum.ToString());

            return new CloudCacheContainerKey("Roslyn.Project", dimensions);
        }

        public static CloudCacheContainerKey? CreateDocumentContainerKey(
            bool mustSucceed,
            string relativePathBase,
            string? projectFilePath,
            string projectName,
            Checksum? parseOptionsChecksum,
            string? documentFilePath,
            string documentName)
        {
            // See if we can get a project key for this info.  If not, we def can't get a doc key.
            var projectKey = CreateProjectContainerKey(mustSucceed, relativePathBase, projectFilePath, projectName, parseOptionsChecksum);
            if (projectKey == null)
                return null;

            // We have to have a file path for this document
            if (string.IsNullOrEmpty(documentFilePath))
                return mustSucceed ? throw new InvalidOperationException($"{nameof(documentFilePath)} is empty") : null;

            // The file path has to be relative to the solution path.
            var relativePath = PathUtilities.GetRelativePath(relativePathBase, documentFilePath!);
            if (relativePath == documentFilePath)
                return mustSucceed ? throw new InvalidOperationException($"'{documentFilePath}' wasn't relative to '{relativePathBase}'") : null;

            var dimensions = EmptyDimensions
                .Add(nameof(documentFilePath), relativePath)
                .Add(nameof(documentName), documentName);

            return new CloudCacheContainerKey("Roslyn.Document", dimensions);
        }
    }
}
