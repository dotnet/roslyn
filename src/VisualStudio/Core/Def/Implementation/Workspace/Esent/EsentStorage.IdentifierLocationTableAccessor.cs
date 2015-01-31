// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using Microsoft.Isam.Esent.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Esent
{
    internal partial class EsentStorage
    {
        public class IdentifierLocationTableAccessor : AbstractTableAccessor
        {
            private readonly JET_COLUMNID _projectColumnId;
            private readonly JET_COLUMNID _documentColumnId;
            private readonly JET_COLUMNID _identifierColumnId;
            private readonly JET_COLUMNID _valueColumnId;

            private readonly string _identifierIndexName;
            private readonly string _documentIndexName;

            public IdentifierLocationTableAccessor(
                OpenSession session, string tableName, string identifierIndexName, string documentIndexName,
                JET_COLUMNID projectColumnId, JET_COLUMNID documentColumnId, JET_COLUMNID identifierColumnId, JET_COLUMNID valueColumnId) : base(session, tableName)
            {
                _identifierIndexName = identifierIndexName;
                _documentIndexName = documentIndexName;

                _projectColumnId = projectColumnId;
                _documentColumnId = documentColumnId;
                _identifierColumnId = identifierColumnId;
                _valueColumnId = valueColumnId;
            }

            private bool TrySeek(int projectId, int documentId, int identifierId)
            {
                Api.JetSetCurrentIndex(SessionId, TableId, _identifierIndexName);
                Api.MakeKey(SessionId, TableId, projectId, MakeKeyGrbit.NewKey);
                Api.MakeKey(SessionId, TableId, documentId, MakeKeyGrbit.None);
                Api.MakeKey(SessionId, TableId, identifierId, MakeKeyGrbit.None);

                return Api.TrySeek(SessionId, TableId, SeekGrbit.SeekEQ);
            }

            public bool Contains(int projectId, int documentId, int identifierId)
            {
                OpenTableForReading();

                return TrySeek(projectId, documentId, identifierId);
            }

            public void Delete(int projectId, int documentId, CancellationToken cancellationToken)
            {
                OpenTableForUpdating();

                // set upper bound using index
                Api.JetSetCurrentIndex(SessionId, TableId, _documentIndexName);
                Api.MakeKey(SessionId, TableId, projectId, MakeKeyGrbit.NewKey);
                Api.MakeKey(SessionId, TableId, documentId, MakeKeyGrbit.None);

                // put the cursor at the first record and set range for exact match
                if (!Api.TrySeek(SessionId, TableId, SeekGrbit.SeekEQ))
                {
                    return;
                }

                Api.MakeKey(SessionId, TableId, projectId, MakeKeyGrbit.NewKey);
                Api.MakeKey(SessionId, TableId, documentId, MakeKeyGrbit.None);
                if (!Api.TrySetIndexRange(SessionId, TableId, SetIndexRangeGrbit.RangeInclusive | SetIndexRangeGrbit.RangeUpperLimit))
                {
                    return;
                }

                // delete all matching record
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Api.JetDelete(SessionId, TableId);
                }
                while (Api.TryMoveNext(SessionId, TableId));
            }

            public Stream GetReadStream(int projectId, int documentId, int nameId)
            {
                OpenTableForReading();

                if (TrySeek(projectId, documentId, nameId))
                {
                    return new ColumnStream(SessionId, TableId, _valueColumnId);
                }

                return null;
            }

            public void PrepareBatchOneInsert()
            {
                EnsureTableForUpdating();

                Api.JetPrepareUpdate(SessionId, TableId, JET_prep.Insert);
            }

            public void FinishBatchOneInsert()
            {
                Api.JetUpdate(SessionId, TableId);
            }

            public Stream GetBatchInsertStream(int projectId, int documentId, int nameId)
            {
                var args = Pool.GetInt32Columns(_projectColumnId, projectId, _documentColumnId, documentId, _identifierColumnId, nameId);
                Api.SetColumns(SessionId, TableId, args);
                Pool.Free(args);

                return new ColumnStream(SessionId, TableId, _valueColumnId);
            }
        }
    }
}
