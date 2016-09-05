// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Isam.Esent.Interop;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Esent
{
    internal partial class EsentStorage
    {
        private class SolutionTable : AbstractTable
        {
            private const string TableName = "SolutionTable";

            private const string NameColumnName = "Name";
            private const string ValueColumnName = "Value";

            private JET_COLUMNID _nameColumnId;
            private JET_COLUMNID _valueColumnId;

            public override void Create(JET_SESID sessionId, JET_DBID databaseId)
            {
                var nameColumnCreate = CreateIdColumn(NameColumnName);
                var valueColumnCreate = CreateBinaryColumn(ValueColumnName);

                var columns = new JET_COLUMNCREATE[] { nameColumnCreate, valueColumnCreate };

                var nameIndexKey = CreateIndexKey(NameColumnName);

                var indexes = new JET_INDEXCREATE[]
                {
                    CreatePrimaryIndex(nameIndexKey)
                };

                var tableCreate = CreateTable(TableName, columns, indexes);

                Api.JetCreateTableColumnIndex3(sessionId, databaseId, tableCreate);

                _nameColumnId = nameColumnCreate.columnid;
                _valueColumnId = valueColumnCreate.columnid;

                Api.JetCloseTable(sessionId, tableCreate.tableid);
            }

            public override void Initialize(JET_SESID sessionId, JET_DBID databaseId)
            {
                using (var table = new Table(sessionId, databaseId, TableName, OpenTableGrbit.ReadOnly))
                {
                    _nameColumnId = Api.GetTableColumnid(sessionId, table, NameColumnName);
                    _valueColumnId = Api.GetTableColumnid(sessionId, table, ValueColumnName);
                }
            }

            public override AbstractTableAccessor GetTableAccessor(OpenSession openSession)
            {
                return new SolutionTableAccessor(openSession, TableName, PrimaryIndexName, _nameColumnId, _valueColumnId);
            }
        }
    }
}
