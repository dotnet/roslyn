// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.Isam.Esent.Interop;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Esent
{
    internal partial class EsentStorage
    {
        public class DocumentTable : ProjectDocumentTable
        {
            private const string TableName = "DocumentTable";
            private const string NameColumnName = "Name";
            private const string ValueColumnName = "Value";

            private JET_COLUMNID _projectColumnId;
            private JET_COLUMNID _projectNameColumnId;
            private JET_COLUMNID _documentColumnId;
            private JET_COLUMNID _nameColumnId;
            private JET_COLUMNID _valueColumnId;

            public override void Create(JET_SESID sessionId, JET_DBID databaseId)
            {
                var nameColumnCreate = CreateIdColumn(NameColumnName);
                var valueColumnCreate = CreateBinaryColumn(ValueColumnName);

                var columns = CreateProjectDocumentColumns(nameColumnCreate, valueColumnCreate);

                var primaryIndexKey = CreateProjectDocumentIndexKey(NameColumnName);

                var indexes = new JET_INDEXCREATE[]
                {
                    CreatePrimaryIndex(primaryIndexKey)
                };

                var tableCreate = CreateTable(TableName, columns, indexes);

                Api.JetCreateTableColumnIndex3(sessionId, databaseId, tableCreate);

                GetColumnIds(columns, out _projectColumnId, out _projectNameColumnId, out _documentColumnId);

                _nameColumnId = nameColumnCreate.columnid;
                _valueColumnId = valueColumnCreate.columnid;

                Api.JetCloseTable(sessionId, tableCreate.tableid);
            }

            public override void Initialize(JET_SESID sessionId, JET_DBID databaseId)
            {
                using (var table = new Table(sessionId, databaseId, TableName, OpenTableGrbit.ReadOnly))
                {
                    GetColumnIds(sessionId, table, out _projectColumnId, out _projectNameColumnId, out _documentColumnId);

                    _nameColumnId = Api.GetTableColumnid(sessionId, table, NameColumnName);
                    _valueColumnId = Api.GetTableColumnid(sessionId, table, ValueColumnName);
                }
            }

            public override AbstractTableAccessor GetTableAccessor(OpenSession openSession)
            {
                return new DocumentTableAccessor(
                    openSession, TableName, PrimaryIndexName,
                    _projectColumnId, _projectNameColumnId, _documentColumnId, _nameColumnId, _valueColumnId);
            }
        }
    }
}
