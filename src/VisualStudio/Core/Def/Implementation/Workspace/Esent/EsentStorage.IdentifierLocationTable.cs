// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Isam.Esent.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Esent
{
    internal partial class EsentStorage
    {
        public class IdentifierLocationTable : ProjectDocumentTable
        {
            private const string TableName = "IdentifierLocationTable";

            private const string IdentifierColumnName = "Identifier";
            private const string LocationsColumnName = "Locations";

            private JET_COLUMNID _projectColumnId;
            private JET_COLUMNID _projectNameColumnId;
            private JET_COLUMNID _documentColumnId;
            private JET_COLUMNID _identifierColumnId;
            private JET_COLUMNID _locationsColumnId;

            public override void Create(JET_SESID sessionId, JET_DBID databaseId)
            {
                var identifierColumnCreate = CreateIdColumn(IdentifierColumnName);
                var locationsColumnCreate = CreateBinaryColumn(LocationsColumnName);

                var columns = CreateProjectDocumentColumns(identifierColumnCreate, locationsColumnCreate);

                var primaryIndexKey = CreateProjectDocumentIndexKey(IdentifierColumnName);

                var indexes = new JET_INDEXCREATE[]
                {
                    CreatePrimaryIndex(primaryIndexKey)
                };

                var tableCreate = CreateTable(TableName, columns, indexes);

                Api.JetCreateTableColumnIndex3(sessionId, databaseId, tableCreate);

                GetColumnIds(columns, out _projectColumnId, out _projectNameColumnId, out _documentColumnId);

                _identifierColumnId = identifierColumnCreate.columnid;
                _locationsColumnId = locationsColumnCreate.columnid;

                Api.JetCloseTable(sessionId, tableCreate.tableid);
            }

            public override void Initialize(JET_SESID sessionId, JET_DBID databaseId)
            {
                using (var table = new Table(sessionId, databaseId, TableName, OpenTableGrbit.ReadOnly))
                {
                    GetColumnIds(sessionId, table, out _projectColumnId, out _projectNameColumnId, out _documentColumnId);

                    _identifierColumnId = Api.GetTableColumnid(sessionId, table, IdentifierColumnName);
                    _locationsColumnId = Api.GetTableColumnid(sessionId, table, LocationsColumnName);
                }
            }

            public override AbstractTableAccessor GetTableAccessor(OpenSession openSession)
            {
                return new IdentifierLocationTableAccessor(
                    openSession, TableName, PrimaryIndexName,
                    _projectColumnId, _projectNameColumnId, _documentColumnId, _identifierColumnId, _locationsColumnId);
            }
        }
    }
}
