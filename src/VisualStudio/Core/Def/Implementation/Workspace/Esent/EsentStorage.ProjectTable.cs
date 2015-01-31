// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Isam.Esent.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Esent
{
    internal partial class EsentStorage
    {
        private class ProjectTable : AbstractTable
        {
            private const string TableName = "ProjectTable";

            private const string ProjectColumnName = "Project";
            private const string NameColumnName = "Name";
            private const string ValueColumnName = "Value";

            private const string ProjectAndNameIndexName = "ProjectAndNameIndex";

            private JET_COLUMNID _projectColumnId;
            private JET_COLUMNID _nameColumnId;
            private JET_COLUMNID _valueColumnId;

            public override void Create(JET_SESID sessionId, JET_DBID databaseId)
            {
                var projectColumnCreate = new JET_COLUMNCREATE()
                {
                    szColumnName = ProjectColumnName,
                    coltyp = JET_coltyp.Long,
                    grbit = ColumndefGrbit.ColumnNotNULL
                };

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

                var columns = new JET_COLUMNCREATE[] { projectColumnCreate, nameColumnCreate, valueColumnCreate };

                var projectAndNameIndexKey = "+" + ProjectColumnName + "\0+" + NameColumnName + "\0\0";

                var indexes = new JET_INDEXCREATE[]
                {
                    new JET_INDEXCREATE
                    {
                        szIndexName = ProjectAndNameIndexName,
                        szKey = projectAndNameIndexKey,
                        cbKey = projectAndNameIndexKey.Length,
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

                _projectColumnId = projectColumnCreate.columnid;
                _nameColumnId = nameColumnCreate.columnid;
                _valueColumnId = valueColumnCreate.columnid;

                Api.JetCloseTable(sessionId, tableCreate.tableid);
            }

            public override void Initialize(JET_SESID sessionId, JET_DBID databaseId)
            {
                using (var table = new Table(sessionId, databaseId, TableName, OpenTableGrbit.ReadOnly))
                {
                    _projectColumnId = Api.GetTableColumnid(sessionId, table, ProjectColumnName);
                    _nameColumnId = Api.GetTableColumnid(sessionId, table, NameColumnName);
                    _valueColumnId = Api.GetTableColumnid(sessionId, table, ValueColumnName);
                }
            }

            public override AbstractTableAccessor GetTableAccessor(OpenSession openSession)
            {
                return new ProjectTableAccessor(openSession, TableName, ProjectAndNameIndexName, _projectColumnId, _nameColumnId, _valueColumnId);
            }
        }
    }
}
