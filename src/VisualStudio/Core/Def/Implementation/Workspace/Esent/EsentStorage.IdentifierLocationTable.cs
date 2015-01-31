// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Isam.Esent.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Esent
{
    internal partial class EsentStorage
    {
        public class IdentifierLocationTable : AbstractTable
        {
            private const string TableName = "IdentifierLocationTable";

            private const string ProjectColumnName = "Project";
            private const string DocumentColumnName = "Document";
            private const string IdentifierColumnName = "Identifier";
            private const string LocationsColumnName = "Locations";

            private const string DocumentIndexName = "DocumentIndex";
            private const string ProjectAndDocumentIndexName = "ProjectAndDocumentIndex";
            private const string ProjectAndDocumentAndIdentifierIndexName = "ProjectAndDocumentAndIdentifierIndex";

            private JET_COLUMNID _projectColumnId;
            private JET_COLUMNID _documentColumnId;
            private JET_COLUMNID _identifierColumnId;
            private JET_COLUMNID _locationsColumnId;

            public override void Create(JET_SESID sessionId, JET_DBID databaseId)
            {
                var projectColumnCreate = new JET_COLUMNCREATE()
                {
                    szColumnName = ProjectColumnName,
                    coltyp = JET_coltyp.Long,
                    grbit = ColumndefGrbit.ColumnNotNULL
                };

                var documentColumnCreate = new JET_COLUMNCREATE()
                {
                    szColumnName = DocumentColumnName,
                    coltyp = JET_coltyp.Long,
                    grbit = ColumndefGrbit.ColumnNotNULL
                };

                var identifierColumnCreate = new JET_COLUMNCREATE()
                {
                    szColumnName = IdentifierColumnName,
                    coltyp = JET_coltyp.Long,
                    grbit = ColumndefGrbit.ColumnNotNULL
                };

                var locationsColumnCreate = new JET_COLUMNCREATE()
                {
                    szColumnName = LocationsColumnName,
                    coltyp = JET_coltyp.LongBinary,
                    grbit = ColumndefGrbit.None
                };

                var columns = new JET_COLUMNCREATE[] { projectColumnCreate, documentColumnCreate, identifierColumnCreate, locationsColumnCreate };

                var projectAndDocumentAndIdentifierIndexKey = "+" + ProjectColumnName + "\0+" + DocumentColumnName + "\0+" + IdentifierColumnName + "\0\0";
                var projectAndDocumentIndexKey = "+" + ProjectColumnName + "\0+" + DocumentColumnName + "\0\0";
                var documentIndexKey = "+" + DocumentColumnName + "\0\0";

                var indexes = new JET_INDEXCREATE[]
                {
                    new JET_INDEXCREATE
                    {
                        szIndexName = ProjectAndDocumentAndIdentifierIndexName,
                        szKey = projectAndDocumentAndIdentifierIndexKey,
                        cbKey = projectAndDocumentAndIdentifierIndexKey.Length,
                        grbit = CreateIndexGrbit.IndexPrimary | CreateIndexGrbit.IndexUnique | CreateIndexGrbit.IndexDisallowNull,
                        ulDensity = 80
                    },
                    new JET_INDEXCREATE
                    {
                        szIndexName = ProjectAndDocumentIndexName,
                        szKey = projectAndDocumentIndexKey,
                        cbKey = projectAndDocumentIndexKey.Length,
                        grbit = CreateIndexGrbit.IndexDisallowNull,
                        ulDensity = 80
                    },
                    new JET_INDEXCREATE
                    {
                        szIndexName = DocumentIndexName,
                        szKey = documentIndexKey,
                        cbKey = documentIndexKey.Length,
                        grbit = CreateIndexGrbit.IndexDisallowNull,
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
                _documentColumnId = documentColumnCreate.columnid;
                _identifierColumnId = identifierColumnCreate.columnid;
                _locationsColumnId = locationsColumnCreate.columnid;

                Api.JetCloseTable(sessionId, tableCreate.tableid);
            }

            public override void Initialize(JET_SESID sessionId, JET_DBID databaseId)
            {
                using (var table = new Table(sessionId, databaseId, TableName, OpenTableGrbit.ReadOnly))
                {
                    _projectColumnId = Api.GetTableColumnid(sessionId, table, ProjectColumnName);
                    _documentColumnId = Api.GetTableColumnid(sessionId, table, DocumentColumnName);
                    _identifierColumnId = Api.GetTableColumnid(sessionId, table, IdentifierColumnName);
                    _locationsColumnId = Api.GetTableColumnid(sessionId, table, LocationsColumnName);
                }
            }

            public override AbstractTableAccessor GetTableAccessor(OpenSession openSession)
            {
                return new IdentifierLocationTableAccessor(
                    openSession, TableName, ProjectAndDocumentAndIdentifierIndexName, ProjectAndDocumentIndexName,
                    _projectColumnId, _documentColumnId, _identifierColumnId, _locationsColumnId);
            }
        }
    }
}
