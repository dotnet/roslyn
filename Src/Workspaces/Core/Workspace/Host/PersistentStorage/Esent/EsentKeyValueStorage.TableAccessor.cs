using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host.Esent
{
    internal partial class EsentKeyValueStorage
    {
        /// <summary>
        /// Provides read\write access to the specified table. Automatically starts transaction in construction time.
        /// Transaction is committed in Dispose implementation if Commit was called before - otherwise transaction is rolled back.
        /// </summary>
        internal class TableAccessor : IDisposable
        {
            private readonly EsentKeyValueStorage storage;
            private readonly EsentSession session;
            private readonly TableInfo table;
            private readonly Transaction transaction;

            private bool editHasStarted;

            internal TableAccessor(EsentKeyValueStorage storage, EsentSession session, TableInfo table)
            {
                this.session = session;
                this.storage = storage;
                this.table = table;

                transaction = new Transaction(session.Session);
            }

            /// <summary>
            /// For the specified <paramref name="originalTableName"/> returns string that can be used as a valid table name.
            /// Association: <paramref name="originalTableName"/> to returned value is stored in the table defined by <paramref name="table"/>.
            /// </summary>
            /// <param name="session"></param>
            /// <param name="table"></param>
            /// <param name="originalTableName"></param>
            internal static string GetSanitizedTableName(EsentSession session, TableInfo table, string originalTableName)
            {
                var found = false;
                int id;
                do
                {
                    found = true;
                    using (var tx = new Transaction(session.Session))
                    {
                        var result = FindKey(session, table, originalTableName);
                        if (result.ReturnCode == EsentReturnCode.RecordNotFound)
                        {
                            PrepareInsert(session, table);
                            InsertKey(session, table, originalTableName);
                            id = RetrieveValue(session, table);

                            result = Update(session, table);

                            // uniqueness of indexes is held across transactions - someone has already inserted record with the given key
                            if (result.ReturnCode == EsentReturnCode.KeyDuplicate)
                            {
                                // do not commit the transaction - it will be rolled back and we'll retry one more time
                                found = false;
                            }
                            else
                            {
                                result.EnsureSucceeded();
                                tx.Commit();
                            }
                        }
                        else
                        {
                            result.EnsureSucceeded();
                            id = RetrieveValue(session, table);
                            tx.Commit();
                        }
                    }
                }
                while (!found);

                return id.ToString();
            }

            public Stream GetReadStream(string key)
            {
                var result = FindKey(session, table, key);
                if (result.ReturnCode == EsentReturnCode.RecordNotFound)
                {
                    return null;
                }
                else
                {
                    result.EnsureSucceeded();
                    return new EsentStream(session, table);
                }
            }

            public Stream GetWriteStream(string key)
            {
                var err = FindKey(session, table, key);
                if (err.ReturnCode == EsentReturnCode.RecordNotFound)
                {
                    PrepareInsert(session, table);
                    InsertKey(session, table, key);
                }
                else
                {
                    err.EnsureSucceeded();
                    PrepareUpdate(session, table);
                }

                editHasStarted = true;
                return new EsentStream(session, table);
            }

            public bool ApplyChanges()
            {
                if (!editHasStarted)
                {
                    return true;
                }

                var retCode = Update(session, table);
                if (retCode.ReturnCode == EsentReturnCode.KeyDuplicate || retCode.ReturnCode == EsentReturnCode.WriteConflict)
                {
                    return false;
                }
                else
                {
                    retCode.EnsureSucceeded();
                    Commit();

                    return true;
                }
            }

            protected void Commit()
            {
                transaction.Commit(CommitTransactionGrbit.LazyFlush);
            }

            public void Dispose()
            {
                try
                {
                    transaction.Dispose();

                    // By default transactions are committed using LazyFlush flag to decrease delays - Esent initiates commit but doesn't wait for its completion.
                    // Add some robustness: session tracks the number of lazily flushed transactions and after it reaches some threshold - waits until all of them are finished.
                    if (transaction.IsCommitted && session.LogLazyFlushTransaction())
                    {
                        // it is safe to call JetCommitTransaction with WaitLastLevel0Commit without calling JetBeginTransaction first.
                        NativeMethods.JetCommitTransaction(
                            sesid: session.Session,
                            grbit: (uint)CommitTransactionGrbit.WaitLastLevel0Commit).EnsureSucceeded();
                    }
                }
                finally
                {
                    storage.FreeSession(session);
                }
            }

            private static void PrepareInsert(EsentSession session, TableInfo table)
            {
                NativeMethods.JetPrepareUpdate(
                    sesid: session.Session,
                    tableid: table.TableId,
                    prep: (uint)JetPrepareUpdateFlags.Insert).EnsureSucceeded();
            }

            private static void PrepareUpdate(EsentSession session, TableInfo table)
            {
                NativeMethods.JetPrepareUpdate(
                    sesid: session.Session,
                    tableid: table.TableId,
                    prep: (uint)JetPrepareUpdateFlags.ReplaceNoLock).EnsureSucceeded();
            }

            private static void InsertKey(EsentSession session, TableInfo table, string key)
            {
                unsafe
                {
                    fixed (char* buffer = key)
                    {
                        NativeMethods.JetSetColumn(
                            sesid: session.Session,
                            tableid: table.TableId,
                            columnid: table.KeyColumnId,
                            pvData: new IntPtr(buffer),
                            cbData: (uint)(key.Length * sizeof(char)),
                            grbit: (uint)SetColumnGrbit.IntrinsicLV, // try to store long value in a record to avoid extra seeks
                            psetinfo: IntPtr.Zero).EnsureSucceeded();
                    }
                }
            }

            private static int RetrieveValue(EsentSession session, TableInfo table)
            {
                int value;
                uint actual;
                unsafe
                {
                    NativeMethods.JetRetrieveColumn(
                        sesid: session.Session,
                        tableid: table.TableId,
                        columnid: table.ValueColumnId,
                        pvData: new IntPtr(&value),
                        cbData: sizeof(int),
                        cbActual: out actual,
                        grbit: (uint)RetrieveColumnGrbit.RetrieveCopy,
                        pretinfo: IntPtr.Zero).EnsureSucceeded();
                }

                return value;
            }

            private static EsentResult Update(EsentSession session, TableInfo table)
            {
                uint actual;
                return NativeMethods.JetUpdate(
                    sesid: session.Session,
                    tableid: table.TableId,
                    pvBookmark: null,
                    cbBookmark: 0,
                    cbActual: out actual);
            }

            private static EsentResult FindKey(EsentSession session, TableInfo table, string key)
            {
                // szIndexName == null -> use clustered index (for our tables this is primary index)
                NativeMethods.JetSetCurrentIndex(
                    sesid: session.Session,
                    tableid: table.TableId,
                    szIndexName: null).EnsureSucceeded();

                unsafe
                {
                    fixed (char* buffer = key)
                    {
                        NativeMethods.JetMakeKey(
                            sesid: session.Session,
                            tableid: table.TableId,
                            pvData: new IntPtr(buffer),
                            cbData: (uint)(key.Length * sizeof(char)),
                            grbit: (uint)MakeKeyGrbit.NewKey).EnsureSucceeded();
                    }
                }

                return NativeMethods.JetSeek(
                    sesid: session.Session,
                    tableid: table.TableId,
                    grbit: (uint)SeekGrbit.SeekEQ);
            }
        }
    }
}
