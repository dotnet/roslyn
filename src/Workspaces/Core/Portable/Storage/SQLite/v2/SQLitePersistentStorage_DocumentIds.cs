// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.SQLite.v2.Interop;

namespace Microsoft.CodeAnalysis.SQLite.v2
{
    internal partial class SQLitePersistentStorage
    {
        /// <summary>
        /// Mapping from the workspace's ID for a document, to the ID we use in the DB for the document.
        /// Kept locally so we don't have to hit the DB for the common case of trying to determine the 
        /// DB id for a document.
        /// </summary>
        private readonly ConcurrentDictionary<DocumentId, int> _documentIdToIdMap = new();

        /// <summary>
        /// Given a document, and the name of a stream to read/write, gets the integral DB ID to 
        /// use to find the data inside the DocumentData table.
        /// </summary>
        private bool TryGetDocumentDataId(
            SqlConnection connection, DocumentKey documentKey, string name, bool allowWrite, out long dataId)
        {
            dataId = 0;

            var documentId = TryGetDocumentId(connection, documentKey, allowWrite);
            var nameId = TryGetStringId(connection, name, allowWrite);
            if (documentId == null || nameId == null)
                return false;

            // Our data ID is just a 64bit int combining the two 32bit values of our documentId and nameId.
            dataId = CombineInt32ValuesToInt64(documentId.Value, nameId.Value);
            return true;
        }

        private int? TryGetDocumentId(SqlConnection connection, DocumentKey document, bool allowWrite)
        {
            // First see if we've cached the ID for this value locally.  If so, just return
            // what we already have.
            if (_documentIdToIdMap.TryGetValue(document.Id, out var existingId))
                return existingId;

            var id = TryGetDocumentIdFromDatabase(connection, document, allowWrite);
            if (id != null)
            {
                // Cache the value locally so we don't need to go back to the DB in the future.
                _documentIdToIdMap.TryAdd(document.Id, id.Value);
            }

            return id;
        }

        private int? TryGetDocumentIdFromDatabase(SqlConnection connection, DocumentKey document, bool allowWrite)
        {
            var projectId = TryGetProjectId(connection, document.Project, allowWrite);
            if (projectId == null)
                return null;

            // Key the document off its project id, and its path and name.  That way we work properly
            // in host and test scenarios.
            var documentPathId = TryGetStringId(connection, document.FilePath, allowWrite);
            var documentNameId = TryGetStringId(connection, document.Name, allowWrite);

            if (documentPathId == null || documentNameId == null)
                return null;

            // Unique identify the document through the key:  projectId-documentPathId-documentNameId
            return TryGetStringId(
                connection, GetDocumentIdString(projectId.Value, documentPathId.Value, documentNameId.Value), allowWrite);
        }
    }
}
