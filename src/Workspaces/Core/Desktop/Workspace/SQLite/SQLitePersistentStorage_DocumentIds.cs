// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.SQLite.Interop;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorage
    {
        /// <summary>
        /// Mapping from the workspace's ID for a document, to the ID we use in the DB for the document.
        /// Kept locally so we don't have to hit the DB for the common case of trying to determine the 
        /// DB id for a document.
        /// </summary>
        private readonly ConcurrentDictionary<DocumentId, int> _documentIdToIdMap = new ConcurrentDictionary<DocumentId, int>();

        /// <summary>
        /// Given a document, and the name of a stream to read/write, gets the integral DB ID to 
        /// use to find the data inside the DocumentData table.
        /// </summary>
        private bool TryGetDocumentDataId(SqlConnection connection, Document document, string name, out long dataId)
        {
            dataId = 0;

            // First, try to get all the IDs for our project in sync with the DB.
            // This will only be expensive the first time we do this.  But will save
            // us from tons of back-and-forth as any BG analyzer processes all the
            // documents in a solution.
            BulkPopulateProjectIds(connection, document.Project, fetchStringTable: true);

            var documentId = TryGetDocumentId(connection, document);
            var nameId = TryGetStringId(connection, name);
            if (documentId == null || nameId == null)
            {
                return false;
            }

            // Our data ID is just a 64bit int combining the two 32bit values of our documentId and nameId.
            dataId = CombineInt32ValuesToInt64(documentId.Value, nameId.Value);
            return true;
        }

        private int? TryGetDocumentId(SqlConnection connection, Document document)
        {
            // First see if we've cached the ID for this value locally.  If so, just return
            // what we already have.
            if (_documentIdToIdMap.TryGetValue(document.Id, out var existingId))
            {
                return existingId;
            }

            var id = TryGetDocumentIdFromDatabase(connection, document);
            if (id != null)
            {
                // Cache the value locally so we don't need to go back to the DB in the future.
                _documentIdToIdMap.TryAdd(document.Id, id.Value);
            }

            return id;
        }

        private int? TryGetDocumentIdFromDatabase(SqlConnection connection, Document document)
        {
            var projectId = TryGetProjectId(connection, document.Project);
            if (projectId == null)
            {
                return null;
            }

            // Key the document off its project id, and its path and name.  That way we work properly
            // in host and test scenarios.
            var documentPathId = TryGetStringId(connection, document.FilePath);
            var documentNameId = TryGetStringId(connection, document.Name);

            if (documentPathId == null || documentNameId == null)
            {
                return null;
            }

            // Unique identify the document through the key:  projectId-documentPathId-documentNameId
            return TryGetStringId(
                connection,
                GetDocumentIdString(projectId.Value, documentPathId.Value, documentNameId.Value));
        }
    }
}
