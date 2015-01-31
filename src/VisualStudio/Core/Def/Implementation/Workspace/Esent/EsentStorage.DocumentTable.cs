// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Isam.Esent.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Esent
{
    internal partial class EsentStorage
    {
        public class DocumentTable : AbstractTable
        {
            private const string TableName = "DocumentTable";

            private const string ProjectColumnName = "Project";
            private const string DocumentColumnName = "Document";
            private const string NameColumnName = "Name";
            private const string ValueColumnName = "Value";

            private const string DocumentIndexName = "DocumentIndex";
            private const string ProjectAndDocumentAndNameIndexName = "ProjectAndDocumentAndNameIndex";

            private JET_COLUMNID _projectColumnId;
            private JET_COLUMNID _documentColumnId;
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

                var documentColumnCreate = new JET_COLUMNCREATE()
                {
                    szColumnName = DocumentColumnName,
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

                var columns = new JET_COLUMNCREATE[] { projectColumnCreate, documentColumnCreate, nameColumnCreate, valueColumnCreate };

                var projectAndDocumentAndNameIndexKey = "+" + ProjectColumnName + "\0+" + DocumentColumnName + "\0+" + NameColumnName + "\0\0";
                var documentIndexKey = "+" + DocumentColumnName + "\0\0";

                var indexes = new JET_INDEXCREATE[]
                {
                    new JET_INDEXCREATE
                    {
                        szIndexName = ProjectAndDocumentAndNameIndexName,
                        szKey = projectAndDocumentAndNameIndexKey,
                        cbKey = projectAndDocumentAndNameIndexKey.Length,
                        grbit = CreateIndexGrbit.IndexPrimary | CreateIndexGrbit.IndexUnique | CreateIndexGrbit.IndexDisallowNull,
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
                _nameColumnId = nameColumnCreate.columnid;
                _valueColumnId = valueColumnCreate.columnid;

                Api.JetCloseTable(sessionId, tableCreate.tableid);
            }

            public override void Initialize(JET_SESID sessionId, JET_DBID databaseId)
            {
                using (var table = new Table(sessionId, databaseId, TableName, OpenTableGrbit.ReadOnly))
                {
                    _projectColumnId = Api.GetTableColumnid(sessionId, table, ProjectColumnName);
                    _documentColumnId = Api.GetTableColumnid(sessionId, table, DocumentColumnName);
                    _nameColumnId = Api.GetTableColumnid(sessionId, table, NameColumnName);
                    _valueColumnId = Api.GetTableColumnid(sessionId, table, ValueColumnName);
                }
            }

            public override AbstractTableAccessor GetTableAccessor(OpenSession openSession)
            {
                return new DocumentTableAccessor(openSession, TableName, ProjectAndDocumentAndNameIndexName, _projectColumnId, _documentColumnId, _nameColumnId, _valueColumnId);
            }
        }
    }
}
