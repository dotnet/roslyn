// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Isam.Esent.Interop;

namespace Microsoft.CodeAnalysis.Esent
{
    internal partial class EsentStorage
    {
        private class NameTable : AbstractTable
        {
            private const string TableName = "NameTable";

            private const string IdColumnName = "Id";

            private const string NameIndexName = "NameIndex";
            private const string NameColumnName = "Name";

            private JET_COLUMNID _idColumnId;
            private JET_COLUMNID _nameColumnId;

            public override void Create(JET_SESID sessionId, JET_DBID databaseId)
            {
                var idColumnCreate = CreateAutoIncrementIdColumn(IdColumnName);
                var nameColumnCreate = CreateTextColumn(NameColumnName);

                var columns = new JET_COLUMNCREATE[] { idColumnCreate, nameColumnCreate };

                var primaryIndexKey = CreateIndexKey(IdColumnName);
                var nameIndexKey = CreateIndexKey(NameColumnName);

                var indexes = new JET_INDEXCREATE[]
                {
                    CreatePrimaryIndex(primaryIndexKey),
                    CreateUniqueTextIndex(NameIndexName, nameIndexKey)
                };

                var tableCreate = CreateTable(TableName, columns, indexes);

                Api.JetCreateTableColumnIndex3(sessionId, databaseId, tableCreate);

                _idColumnId = idColumnCreate.columnid;
                _nameColumnId = nameColumnCreate.columnid;

                Api.JetCloseTable(sessionId, tableCreate.tableid);
            }

            public override void Initialize(JET_SESID sessionId, JET_DBID databaseId)
            {
                using (var table = new Table(sessionId, databaseId, TableName, OpenTableGrbit.ReadOnly))
                {
                    _idColumnId = Api.GetTableColumnid(sessionId, table, IdColumnName);
                    _nameColumnId = Api.GetTableColumnid(sessionId, table, NameColumnName);
                }
            }

            public override AbstractTableAccessor GetTableAccessor(OpenSession openSession)
            {
                return new StringNameTableAccessor(openSession, TableName, NameIndexName, _idColumnId, _nameColumnId);
            }
        }
    }
}
