// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Microsoft.Isam.Esent.Interop;

namespace Microsoft.CodeAnalysis.Esent
{
    internal partial class EsentStorage
    {
        public class IdentifierLocationTableAccessor : ProjectDocumentTableAccessor
        {
            private readonly JET_COLUMNID _identifierColumnId;
            private readonly JET_COLUMNID _valueColumnId;

            public IdentifierLocationTableAccessor(
                OpenSession session, string tableName, string primaryIndexName,
                JET_COLUMNID projectColumnId, JET_COLUMNID projectNameColumnId, JET_COLUMNID documentColumnId,
                JET_COLUMNID identifierColumnId, JET_COLUMNID valueColumnId) :
                base(session, tableName, primaryIndexName, projectColumnId, projectNameColumnId, documentColumnId)
            {
                _identifierColumnId = identifierColumnId;
                _valueColumnId = valueColumnId;
            }

            private bool TrySeek(Key key, int identifierId)
            {
                return TrySeek(IndexName, key, identifierId);
            }

            public bool Contains(Key key, int identifierId)
            {
                OpenTableForReading();

                return TrySeek(key, identifierId);
            }

            public void Delete(Key key, CancellationToken cancellationToken)
            {
                OpenTableForUpdating();

                // set upper bound using index
                Api.JetSetCurrentIndex(SessionId, TableId, IndexName);
                MakeKey(key);

                // put the cursor at the first record and set range for exact match
                if (!Api.TrySeek(SessionId, TableId, SeekGrbit.SeekEQ))
                {
                    return;
                }

                MakeKey(key);
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

            public override Stream GetReadStream(Key key, int nameId)
            {
                OpenTableForReading();

                if (TrySeek(key, nameId))
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

            public override Stream GetWriteStream(Key key, int nameId)
            {
                var args = GetColumnValues(key, _identifierColumnId, nameId);
                Api.SetColumns(SessionId, TableId, args);
                Free(args);

                return new ColumnStream(SessionId, TableId, _valueColumnId);
            }
        }
    }
}
