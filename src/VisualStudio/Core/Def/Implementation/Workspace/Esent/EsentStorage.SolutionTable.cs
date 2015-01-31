// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Isam.Esent.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Esent
{
    internal partial class EsentStorage
    {
        private class SolutionTable : AbstractTable
        {
            private const string TableName = "SolutionTable";

            private const string NameColumnName = "Name";
            private const string ValueColumnName = "Value";

            private const string NameIndexName = "NameIndex";

            private JET_COLUMNID _nameColumnId;
            private JET_COLUMNID _valueColumnId;

            public override void Create(JET_SESID sessionId, JET_DBID databaseId)
            {
                var nameColumnCreate = new JET_COLUMNCREATE()
                {
                    szColumnName = NameColumnName,
                    coltyp = JET_coltyp.Long,
                    grbit = ColumndefGrbit.ColumnNotNULL
                };

                var valueColumnCreate = new JET_COLUMNCREATE()
                {
                    szColumnName = ValueColumnName,
                    coltyp = JET_coltyp.LongBinary,
                    grbit = ColumndefGrbit.None
                };

                var columns = new JET_COLUMNCREATE[] { nameColumnCreate, valueColumnCreate };

                var nameIndexKey = "+" + NameColumnName + "\0\0";

                var indexes = new JET_INDEXCREATE[]
                {
                    new JET_INDEXCREATE
                    {
                        szIndexName = NameIndexName,
                        szKey = nameIndexKey,
                        cbKey = nameIndexKey.Length,
                        grbit = CreateIndexGrbit.IndexPrimary | CreateIndexGrbit.IndexUnique | CreateIndexGrbit.IndexDisallowNull,
                        ulDensity = 80
                    }
                };

                var tableCreate = new JET_TABLECREATE()
                {
                    szTableName = TableName,
                    ulPages = 16,
                    ulDensity = 80,
                    rgcolumncreate = columns,
                    cColumns = columns.Length,
                    rgindexcreate = indexes,
                    cIndexes = indexes.Length
                };

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
                return new SolutionTableAccessor(openSession, TableName, NameIndexName, _nameColumnId, _valueColumnId);
            }
        }
    }
}
