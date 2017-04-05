// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.Isam.Esent.Interop;

namespace Microsoft.CodeAnalysis.Esent
{
    internal partial class EsentStorage
    {
        public class ProjectTableAccessor : ProjectDocumentTableAccessor
        {
            private readonly JET_COLUMNID _nameColumnId;
            private readonly JET_COLUMNID _valueColumnId;

            public ProjectTableAccessor(
                OpenSession session, string tableName, string indexName,
                JET_COLUMNID projectColumnId, JET_COLUMNID projectNameColumnId, JET_COLUMNID nameColumnId, JET_COLUMNID valueColumnId) :
                base(session, tableName, indexName, projectColumnId, projectNameColumnId, default(JET_COLUMNID))
            {
                _nameColumnId = nameColumnId;
                _valueColumnId = valueColumnId;
            }

            private bool TrySeek(Key key, int nameId)
            {
                return TrySeek(IndexName, key, nameId);
            }

            public override Stream GetReadStream(Key key, int nameId)
            {
                OpenTableForReading();

                if (TrySeek(key, nameId))
                {
                    return new ColumnStream(SessionId, TableId, _valueColumnId);
                }

                return null;
            }

            public override Stream GetWriteStream(Key key, int nameId)
            {
                OpenTableForUpdating();

                if (TrySeek(key, nameId))
                {
                    PrepareUpdate(JET_prep.ReplaceNoLock);
                }
                else
                {
                    PrepareUpdate(JET_prep.Insert);

                    var args = GetColumnValues(key, _nameColumnId, nameId);

                    Api.SetColumns(SessionId, TableId, args);
                    Free(args);
                }

                return new ColumnStream(SessionId, TableId, _valueColumnId);
            }
        }
    }
}
