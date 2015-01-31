// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Isam.Esent.Interop;
using Microsoft.Isam.Esent.Interop.Vista;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Esent
{
    internal partial class EsentStorage
    {
        private class IdentifierNameTable : AbstractTable
        {
            private const string TableName = "IdentifierTable";

            private const string IdColumnName = "Id";
            private const string IdentifierColumnName = "Identifier";

            private const string IdIndexName = "IdIndex";
            private const string IdentifierIndexName = "IdentifierIndex";

            private JET_COLUMNID _idColumnId;
            private JET_COLUMNID _identifierColumnId;

            public override void Create(JET_SESID sessionId, JET_DBID databaseId)
            {
                var idColumnCreate = new JET_COLUMNCREATE()
                {
                    szColumnName = IdColumnName,
                    coltyp = JET_coltyp.Long,
                    grbit = ColumndefGrbit.ColumnAutoincrement | ColumndefGrbit.ColumnNotNULL
                };

                var identifierColumnCreate = new JET_COLUMNCREATE()
                {
                    szColumnName = IdentifierColumnName,
                    coltyp = JET_coltyp.LongText,
                    cp = JET_CP.Unicode,
                    grbit = ColumndefGrbit.ColumnNotNULL
                };

                var columns = new JET_COLUMNCREATE[] { idColumnCreate, identifierColumnCreate };

                var idIndexKey = "+" + IdColumnName + "\0\0";
                var identifierIndexKey = "+" + IdentifierColumnName + "\0\0";

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
                        szIndexName = IdentifierIndexName,
                        szKey = identifierIndexKey,
                        cbKey = identifierIndexKey.Length,
                        grbit = CreateIndexGrbit.IndexUnique | CreateIndexGrbit.IndexDisallowNull | VistaGrbits.IndexDisallowTruncation,
                        ulDensity = 80,
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
