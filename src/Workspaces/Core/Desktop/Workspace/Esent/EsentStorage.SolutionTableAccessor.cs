// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.Isam.Esent.Interop;

namespace Microsoft.CodeAnalysis.Esent
{
    internal partial class EsentStorage
    {
        public class SolutionTableAccessor : AbstractTableAccessor
        {
            private readonly JET_COLUMNID _nameColumnId;
            private readonly JET_COLUMNID _valueColumnId;

            private readonly string _indexName;

            public SolutionTableAccessor(
                OpenSession session, string tableName, string indexName, JET_COLUMNID nameColumnId, JET_COLUMNID valueColumnId) : base(session, tableName)
            {
                _indexName = indexName;

                _nameColumnId = nameColumnId;
                _valueColumnId = valueColumnId;
            }

            protected bool TrySeek(int nameId)
            {
                Api.JetSetCurrentIndex(SessionId, TableId, _indexName);
                Api.MakeKey(SessionId, TableId, nameId, MakeKeyGrbit.NewKey);

                return Api.TrySeek(SessionId, TableId, SeekGrbit.SeekEQ);
            }

            public Stream GetReadStream(int nameId)
            {
                OpenTableForReading();

                if (TrySeek(nameId))
                {
                    return new ColumnStream(SessionId, TableId, _valueColumnId);
                }

                return null;
            }

            public Stream GetWriteStream(int nameId)
            {
                OpenTableForUpdating();

                if (TrySeek(nameId))
                {
                    PrepareUpdate(JET_prep.ReplaceNoLock);
                }
                else
                {
                    PrepareUpdate(JET_prep.Insert);
                    Api.SetColumn(SessionId, TableId, _nameColumnId, nameId);
                }

                return new ColumnStream(SessionId, TableId, _valueColumnId);
            }
        }
    }
}
