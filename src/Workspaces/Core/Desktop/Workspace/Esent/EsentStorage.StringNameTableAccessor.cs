// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
using Microsoft.Isam.Esent.Interop;

namespace Microsoft.CodeAnalysis.Esent
{
    internal partial class EsentStorage
    {
        public class StringNameTableAccessor : AbstractTableAccessor
        {
            private readonly JET_COLUMNID _idColumnId;
            private readonly JET_COLUMNID _nameColumnId;

            private readonly string _indexName;

            public StringNameTableAccessor(
                OpenSession session, string tableName, string indexName, JET_COLUMNID idColumnId, JET_COLUMNID nameColumnId) : base(session, tableName)
            {
                _indexName = indexName;
                _idColumnId = idColumnId;
                _nameColumnId = nameColumnId;
            }

            private bool TrySeek(string value)
            {
                Api.JetSetCurrentIndex(SessionId, TableId, _indexName);
                Api.MakeKey(SessionId, TableId, value, Encoding.Unicode, MakeKeyGrbit.NewKey);

                return Api.TrySeek(SessionId, TableId, SeekGrbit.SeekEQ);
            }

            public int GetUniqueId(string value)
            {
                OpenTableForUpdatingWithoutTransaction();

                while (true)
                {
                    if (TrySeek(value))
                    {
                        return Api.RetrieveColumnAsInt32(SessionId, TableId, _idColumnId).Value;
                    }

                    using (var transaction = new Transaction(SessionId))
                    {
                        PrepareUpdate(JET_prep.Insert);

                        var id = Api.RetrieveColumnAsInt32(SessionId, TableId, _idColumnId, RetrieveColumnGrbit.RetrieveCopy).Value;

                        // set name
                        Api.SetColumn(SessionId, TableId, _nameColumnId, value, Encoding.Unicode);

                        if (ApplyChanges())
                        {
                            transaction.Commit(CommitTransactionGrbit.LazyFlush);
                            return id;
                        }
                    }
                }
            }
        }
    }
}
