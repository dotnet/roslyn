// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.Isam.Esent.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Esent
{
    internal partial class EsentStorage
    {
        public abstract class ProjectDocumentTableAccessor : AbstractTableAccessor
        {
            protected readonly JET_COLUMNID ProjectColumnId;
            protected readonly JET_COLUMNID ProjectNameColumnId;
            protected readonly JET_COLUMNID DocumentColumnId;

            protected readonly string IndexName;

            public ProjectDocumentTableAccessor(
                OpenSession session, string tableName, string indexName,
                JET_COLUMNID projectColumnId, JET_COLUMNID projectNameColumnId, JET_COLUMNID documentColumnId) : base(session, tableName)
            {
                IndexName = indexName;
                ProjectColumnId = projectColumnId;
                ProjectNameColumnId = projectNameColumnId;
                DocumentColumnId = documentColumnId;
            }

            public abstract Stream GetReadStream(Key key, int nameId);
            public abstract Stream GetWriteStream(Key key, int nameId);

            protected bool TrySeek(string indexName, Key key, int id1)
            {
                SetIndexAndMakeKey(indexName, key, id1);

                return Api.TrySeek(SessionId, TableId, SeekGrbit.SeekEQ);
            }

            protected void SetIndexAndMakeKey(string indexName, Key key, int id1)
            {
                Api.JetSetCurrentIndex(SessionId, TableId, indexName);
                MakeKey(key, id1);
            }

            protected void MakeKey(Key key)
            {
                Api.MakeKey(SessionId, TableId, key.ProjectId, MakeKeyGrbit.NewKey);
                Api.MakeKey(SessionId, TableId, key.ProjectNameId, MakeKeyGrbit.None);

                if (key.DocumentIdOpt.HasValue)
                {
                    Api.MakeKey(SessionId, TableId, key.DocumentIdOpt.Value, MakeKeyGrbit.None);
                }
            }

            protected void MakeKey(Key key, int id1)
            {
                MakeKey(key);

                Api.MakeKey(SessionId, TableId, id1, MakeKeyGrbit.None);
            }

            protected ColumnValue[] GetColumnValues(Key key, JET_COLUMNID columnId1, int id1)
            {
                if (key.DocumentIdOpt.HasValue)
                {
                    return Pool.GetInt32Columns(ProjectColumnId, key.ProjectId, ProjectNameColumnId, key.ProjectNameId, DocumentColumnId, key.DocumentIdOpt.Value, columnId1, id1);
                }

                return Pool.GetInt32Columns(ProjectColumnId, key.ProjectId, ProjectNameColumnId, key.ProjectNameId, columnId1, id1);
            }

            protected void Free(ColumnValue[] values)
            {
                Pool.Free(values);
            }
        }

        public struct Key
        {
            public int ProjectId;
            public int ProjectNameId;
            public int? DocumentIdOpt;

            public Key(int projectId, int projectNameId, int documentId)
            {
                ProjectId = projectId;
                ProjectNameId = projectNameId;
                DocumentIdOpt = documentId;
            }

            public Key(int projectId, int projectNameId)
            {
                ProjectId = projectId;
                ProjectNameId = projectNameId;
                DocumentIdOpt = null;
            }
        }
    }
}
