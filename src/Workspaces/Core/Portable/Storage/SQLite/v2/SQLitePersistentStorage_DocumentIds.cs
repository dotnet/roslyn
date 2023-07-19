// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.IO;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SQLite.v2.Interop;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite.v2
{
    internal partial class SQLitePersistentStorage
    {
        /// <summary>
        /// Mapping from the workspace's ID for a document, to the ID we use in the DB for the document.
        /// Kept locally so we don't have to hit the DB for the common case of trying to determine the 
        /// DB id for a document.
        /// </summary>
        private readonly ConcurrentDictionary<DocumentId, DocumentPrimaryKey> _documentIdToPrimaryKeyMap = new();

        /// <summary>
        /// Given a document, and the name of a stream to read/write, gets the integral DB ID to 
        /// use to find the data inside the DocumentData table.
        /// </summary>
        private DocumentPrimaryKey? TryGetDocumentPrimaryKey(SqlConnection connection, DocumentKey documentKey, bool allowWrite)
        {
            // First see if we've cached the ID for this value locally.  If so, just return
            // what we already have.
            if (!_documentIdToPrimaryKeyMap.TryGetValue(documentKey.Id, out var existingId))
            {
                // Store the document as its folder and file name.  The folder is relative to the solution path so that
                // we're not dependent on file-system location.
                var documentFolder =
                    documentKey.FilePath != null && PathUtilities.GetDirectoryName(PathUtilities.GetRelativePath(_solutionDirectory, documentKey.FilePath)) is { Length: > 0 } directoryName
                        ? directoryName
                        : documentKey.FilePath;

                if (TryGetProjectPrimaryKey(connection, documentKey.Project, allowWrite) is not ProjectPrimaryKey projectPrimaryKey ||
                    TryGetStringId(connection, documentFolder, allowWrite) is not int documentFolderId ||
                    TryGetStringId(connection, documentKey.Name, allowWrite) is not int documentNameId)
                {
                    return null;
                }

                // Cache the value locally so we don't need to go back to the DB in the future.
                existingId = new DocumentPrimaryKey(projectPrimaryKey, documentFolderId, documentNameId);
                _documentIdToPrimaryKeyMap.TryAdd(documentKey.Id, existingId);
            }

            return existingId;
        }
    }
}
