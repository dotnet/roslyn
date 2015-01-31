// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.Isam.Esent.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Esent
{
    internal partial class EsentStorage
    {
        public class DocumentTableAccessor : AbstractTableAccessor
        {
            private readonly JET_COLUMNID _projectColumnId;
            private readonly JET_COLUMNID _documentColumnId;
            private readonly JET_COLUMNID _nameColumnId;
            private readonly JET_COLUMNID _valueColumnId;

            private readonly string _indexName;

            public DocumentTableAccessor(
                OpenSession session, string tableName, string indexName,
                JET_COLUMNID projectColumnId, JET_COLUMNID documentColumnId, JET_COLUMNID nameColumnId, JET_COLUMNID valueColumnId) : base(session, tableName)
            {
                _indexName = indexName;
                _projectColumnId = projectColumnId;
                _documentColumnId = documentColumnId;
                _nameColumnId = nameColumnId;
                _valueColumnId = valueColumnId;
            }

            private bool TrySeek(int projectId, int documentId, int nameId)
            {
                Api.JetSetCurrentIndex(SessionId, TableId, _indexName);
                Api.MakeKey(SessionId, TableId, projectId, MakeKeyGrbit.NewKey);
                Api.MakeKey(SessionId, TableId, documentId, MakeKeyGrbit.None);
                Api.MakeKey(SessionId, TableId, nameId, MakeKeyGrbit.None);

                return Api.TrySeek(SessionId, TableId, SeekGrbit.SeekEQ);
            }

            public Stream GetReadStream(int projectId, int documentId, int nameId)
            {
                OpenTableForReading();

                if (TrySeek(projectId, documentId, nameId))
                {
                    return new ColumnStream(SessionId, TableId, _valueColumnId);
                }

                return null;
            }

            public Stream GetWriteStream(int projectId, int documentId, int nameId)
            {
                OpenTableForUpdating();

                if (TrySeek(projectId, documentId, nameId))
                {
                    PrepareUpdate(JET_prep.ReplaceNoLock);
                }
                else
                {
                    PrepareUpdate(JET_prep.Insert);

                    var args = Pool.GetInt32Columns(_projectColumnId, projectId, _documentColumnId, documentId, _nameColumnId, nameId);

                    Api.SetColumns(SessionId, TableId, args);
                    Pool.Free(args);
                }

                return new ColumnStream(SessionId, TableId, _valueColumnId);
            }
        }
    }
}
