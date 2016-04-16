// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Isam.Esent.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Esent
{
    internal partial class EsentStorage
    {
        private class IdentifierNameTable : AbstractTable
        {
            private const string TableName = "IdentifierTable";

            private const string IdColumnName = "Id";

            private const string IdentifierIndexName = "IdentifierIndex";
            private const string IdentifierColumnName = "Identifier";

            private JET_COLUMNID _idColumnId;
            private JET_COLUMNID _identifierColumnId;

            public override void Create(JET_SESID sessionId, JET_DBID databaseId)
            {
                var idColumnCreate = CreateAutoIncrementIdColumn(IdColumnName);
                var identifierColumnCreate = CreateTextColumn(IdentifierColumnName);

                var columns = new JET_COLUMNCREATE[] { idColumnCreate, identifierColumnCreate };

                var primaryIndexKey = CreateIndexKey(IdColumnName);
                var identifierIndexKey = CreateIndexKey(IdentifierColumnName);

                var indexes = new JET_INDEXCREATE[]
                {
                    CreatePrimaryIndex(primaryIndexKey),
                    CreateUniqueTextIndex(IdentifierIndexName, identifierIndexKey)
                };

                var tableCreate = CreateTable(TableName, columns, indexes);

                Api.JetCreateTableColumnIndex3(sessionId, databaseId, tableCreate);

                _idColumnId = idColumnCreate.columnid;
                _identifierColumnId = identifierColumnCreate.columnid;

                Api.JetCloseTable(sessionId, tableCreate.tableid);
            }

            public override void Initialize(JET_SESID sessionId, JET_DBID databaseId)
            {
                using (var table = new Table(sessionId, databaseId, TableName, OpenTableGrbit.ReadOnly))
                {
                    _idColumnId = Api.GetTableColumnid(sessionId, table, IdColumnName);
                    _identifierColumnId = Api.GetTableColumnid(sessionId, table, IdentifierColumnName);
                }
            }

            public override AbstractTableAccessor GetTableAccessor(OpenSession openSession)
            {
                return new StringNameTableAccessor(openSession, TableName, IdentifierIndexName, _idColumnId, _identifierColumnId);
            }
        }
    }
}
