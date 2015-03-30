// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Isam.Esent.Interop;
using Microsoft.Isam.Esent.Interop.Vista;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Esent
{
    internal partial class EsentStorage
    {
        private class NameTable : AbstractTable
        {
            private const string TableName = "NameTable";

            private const string IdColumnName = "Id";
            private const string NameColumnName = "Name";

            private const string IdIndexName = "IdIndex";
            private const string NameIndexName = "NameIndex";

            private JET_COLUMNID _idColumnId;
            private JET_COLUMNID _nameColumnId;

            public override void Create(JET_SESID sessionId, JET_DBID databaseId)
            {
                var idColumnCreate = new JET_COLUMNCREATE()
                {
                    szColumnName = IdColumnName,
                    coltyp = JET_coltyp.Long,
                    grbit = ColumndefGrbit.ColumnAutoincrement | ColumndefGrbit.ColumnNotNULL
                };

                var nameColumnCreate = new JET_COLUMNCREATE()
                {
                    szColumnName = NameColumnName,
                    coltyp = JET_coltyp.LongText,
                    cp = JET_CP.Unicode,
                    grbit = ColumndefGrbit.ColumnNotNULL
                };

                var columns = new JET_COLUMNCREATE[] { idColumnCreate, nameColumnCreate };

                var idIndexKey = "+" + IdColumnName + "\0\0";
                var nameIndexKey = "+" + NameColumnName + "\0\0";

                var indexes = new JET_INDEXCREATE[]
                {
                    new JET_INDEXCREATE
                    {
                        szIndexName = IdIndexName,
                        szKey = idIndexKey,
                        cbKey = idIndexKey.Length,
                        grbit = CreateIndexGrbit.IndexPrimary | CreateIndexGrbit.IndexUnique | CreateIndexGrbit.IndexDisallowNull,
                        ulDensity = 80
                    },
                    new JET_INDEXCREATE
                    {
                        szIndexName = NameIndexName,
                        szKey = nameIndexKey,
                        cbKey = nameIndexKey.Length,
                        grbit = CreateIndexGrbit.IndexUnique | CreateIndexGrbit.IndexDisallowNull | VistaGrbits.IndexDisallowTruncation,
                        ulDensity = 80,
                        // this should be 2000 bytes Max after vista
                        cbKeyMost = SystemParameters.KeyMost
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
