// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Isam.Esent.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Esent
{
    internal partial class EsentStorage
    {
        public abstract class ProjectDocumentTable : AbstractTable
        {
            private const string ProjectColumnName = "Project";
            private const string ProjectNameColumnName = "ProjectName";
            private const string DocumentColumnName = "Document";

            protected JET_COLUMNCREATE[] CreateProjectDocumentColumns(JET_COLUMNCREATE column1, JET_COLUMNCREATE column2)
            {
                var projectColumnCreate = CreateIdColumn(ProjectColumnName);
                var projectNameColumnCreate = CreateIdColumn(ProjectNameColumnName);
                var documentColumnCreate = CreateIdColumn(DocumentColumnName);

                return new JET_COLUMNCREATE[] {
                    projectColumnCreate,
                    projectNameColumnCreate,
                    documentColumnCreate,
                    column1, column2 };
            }

            protected string CreateProjectDocumentIndexKey()
            {
                return CreateIndexKey(ProjectColumnName, ProjectNameColumnName, DocumentColumnName);
            }

            protected string CreateProjectDocumentIndexKey(string columnName)
            {
                return CreateIndexKey(ProjectColumnName, ProjectNameColumnName, DocumentColumnName, columnName);
            }

            protected void GetColumnIds(
                JET_COLUMNCREATE[] columns, out JET_COLUMNID projectColumnId, out JET_COLUMNID projectNameColumnId, out JET_COLUMNID documentColumnId)
            {
                projectColumnId = columns[0].columnid;
                projectNameColumnId = columns[1].columnid;
                documentColumnId = columns[2].columnid;
            }

            protected void GetColumnIds(
                JET_SESID sessionId, Table table, out JET_COLUMNID projectColumnId, out JET_COLUMNID projectNameColumnId, out JET_COLUMNID documentColumnId)
            {
                projectColumnId = Api.GetTableColumnid(sessionId, table, ProjectColumnName);
                projectNameColumnId = Api.GetTableColumnid(sessionId, table, ProjectNameColumnName);
                documentColumnId = Api.GetTableColumnid(sessionId, table, DocumentColumnName);
            }

            protected JET_COLUMNCREATE[] CreateProjectColumns(JET_COLUMNCREATE column1, JET_COLUMNCREATE column2)
            {
                var projectColumnCreate = CreateIdColumn(ProjectColumnName);
                var projectNameColumnCreate = CreateIdColumn(ProjectNameColumnName);

                return new JET_COLUMNCREATE[] {
                    projectColumnCreate,
                    projectNameColumnCreate,
                    column1, column2 };
            }

            protected string CreateProjectIndexKey()
            {
                return CreateIndexKey(ProjectColumnName, ProjectNameColumnName);
            }

            protected string CreateProjectIndexKey(string columnName)
            {
                return CreateIndexKey(ProjectColumnName, ProjectNameColumnName, columnName);
            }

            protected void GetColumnIds(
                JET_COLUMNCREATE[] columns, out JET_COLUMNID projectColumnId, out JET_COLUMNID projectNameColumnId)
            {
                projectColumnId = columns[0].columnid;
                projectNameColumnId = columns[1].columnid;
            }

            protected void GetColumnIds(
                JET_SESID sessionId, Table table, out JET_COLUMNID projectColumnId, out JET_COLUMNID projectNameColumnId)
            {
                projectColumnId = Api.GetTableColumnid(sessionId, table, ProjectColumnName);
                projectNameColumnId = Api.GetTableColumnid(sessionId, table, ProjectNameColumnName);
            }
        }
    }
}
