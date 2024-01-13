// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal interface IAddSolutionItemService : IWorkspaceService
    {
        /// <summary>
        /// Tracks the given file path of a non-existent file and whenever a new file with this file path is created,
        /// it adds it as a solution item.
        /// NOTE: <paramref name="filePath"/> is expected to be an absolute path of a file that does not yet exist.
        /// </summary>
        void TrackFilePathAndAddSolutionItemWhenFileCreated(string filePath);

        /// <summary>
        /// Adds a file at the given path as a solution item.
        /// NOTE: <paramref name="filePath"/> is expected to be an absolute path of an existing file.
        /// </summary>
        Task AddSolutionItemAsync(string filePath, CancellationToken cancellationToken);
    }
}
