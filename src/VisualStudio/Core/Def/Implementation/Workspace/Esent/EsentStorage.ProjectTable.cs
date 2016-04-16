// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Isam.Esent.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Esent
{
    internal partial class EsentStorage
    {
        private class ProjectTable : ProjectDocumentTable
        {
            private const string TableName = "ProjectTable";

            private const string NameColumnName = "Name";
            private const string ValueColumnName = "Value";

            private JET_COLUMNID _projectColumnId;
            private JET_COLUMNID _projectNameColumnId;
            private JET_COLUMNID _nameColumnId;
            private JET_COLUMNID _valueColumnId;

            public override void Create(JET_SESID sessionId, JET_DBID databaseId)
            {
                var nameColumnCreate = CreateIdColumn(NameColumnName);
                var valueColumnCreate = CreateBinaryColumn(ValueColumnName);

                var columns = CreateProjectColumns(nameColumnCreate, valueColumnCreate);

                var projectAndNameIndexKey = CreateProjectIndexKey(NameColumnName);

                var indexes = new JET_INDEXCREATE[]
                {
                    CreatePrimaryIndex(projectAndNameIndexKey)
                };

                var tableCreate = CreateTable(TableName, columns, indexes);

                Api.JetCreateTableColumnIndex3(sessionId, databaseId, tableCreate);

                GetColumnIds(columns, out _projectColumnId, out _projectNameColumnId);
                _nameColumnId = nameColumnCreate.columnid;
                _valueColumnId = valueColumnCreate.columnid;

                Api.JetCloseTable(sessionId, tableCreate.tableid);
            }

            public override void Initialize(JET_SESID sessionId, JET_DBID databaseId)
            {
                using (var table = new Table(sessionId, databaseId, TableName, OpenTableGrbit.ReadOnly))
                {
                    GetColumnIds(sessionId, table, out _projectColumnId, out _projectNameColumnId);
                    _nameColumnId = Api.GetTableColumnid(sessionId, table, NameColumnName);
                    _valueColumnId = Api.GetTableColumnid(sessionId, table, ValueColumnName);
                }
            }

            public override AbstractTableAccessor GetTableAccessor(OpenSession openSession)
            {
                return new ProjectTableAccessor(openSession, TableName, PrimaryIndexName, _projectColumnId, _projectNameColumnId, _nameColumnId, _valueColumnId);
            }
        }
    }
}
