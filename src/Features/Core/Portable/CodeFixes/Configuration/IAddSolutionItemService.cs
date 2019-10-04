// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal interface IAddSolutionItemService : IWorkspaceService
    {
        /// <summary>
        /// Tracks the given file path and whenever a new file with this file path is created,
        /// it adds it as a solution item.
        /// NOTE: <paramref name="filePath"/> is expected to be an absolute path of a file.
        /// </summary>
        Task TrackFilePathAndAddSolutionItemWhenFileCreatedAsync(string filePath, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a file at the given path as a solution item.
        /// NOTE: <paramref name="filePath"/> is expected to be an absolute path of an existing file.
        /// </summary>
        Task AddSolutionItemAsync(string filePath, CancellationToken cancellationToken);
    }
}
