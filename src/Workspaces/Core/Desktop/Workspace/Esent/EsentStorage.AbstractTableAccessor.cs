// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.Isam.Esent.Interop;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Esent
{
    internal partial class EsentStorage
    {
        public abstract class AbstractTableAccessor : IDisposable
        {
            private readonly OpenSession _session;
            private readonly string _tableName;

            private Table _table;
            private Transaction _transaction;
            private Update _updater;

            protected AbstractTableAccessor(OpenSession session, string tableName)
            {
                _session = session;
                _tableName = tableName;
            }

            protected void OpenTableForReading()
            {
                Contract.ThrowIfFalse(_table == null);

                _table = new Table(_session.SessionId, _session.DatabaseId, _tableName, OpenTableGrbit.ReadOnly);
            }

            protected void OpenTableForUpdating()
            {
                OpenTableForUpdatingWithoutTransaction();

                _transaction = new Transaction(_session.SessionId);
            }

            protected void OpenTableForUpdatingWithoutTransaction()
            {
                Contract.ThrowIfFalse(_table == null);
                _table = new Table(_session.SessionId, _session.DatabaseId, _tableName, OpenTableGrbit.Updatable);
            }

            protected void EnsureTableForUpdating()
            {
                if (_table == null)
                {
                    _table = new Table(_session.SessionId, _session.DatabaseId, _tableName, OpenTableGrbit.Updatable);
                }

                if (_transaction == null)
                {
                    _transaction = new Transaction(_session.SessionId);
                }
            }

            protected void PrepareUpdate(JET_prep mode)
            {
                Contract.ThrowIfFalse(_table != null);

                if (_updater != null)
                {
                    _updater.Dispose();
                }

                _updater = new Update(_session.SessionId, TableId, mode);
            }

            protected JET_SESID SessionId
            {
                get
                {
                    return _session.SessionId;
                }
            }

            protected JET_TABLEID TableId
            {
                get
                {
                    // one of OpenTable should have been called. otherwise, throw exception
                    return _table.JetTableid;
                }
            }

            public void Flush()
            {
                if (_transaction == null)
                {
                    return;
                }

                // commit existing transaction
                _transaction.Commit(CommitTransactionGrbit.LazyFlush);
                _transaction.Dispose();
                _transaction = null;
            }

            public bool ApplyChanges()
            {
                try
                {
                    var local = _updater;
                    _updater = null;

                    if (local != null)
                    {
                        local.Save();
                        local.Dispose();
                    }
                }
                catch (EsentKeyDuplicateException)
                {
                    return false;
                }
                catch (EsentWriteConflictException)
                {
                    return false;
                }

                Flush();

                return true;
            }

            public void Dispose()
            {
                if (_updater != null)
                {
                    _updater.Dispose();
                }

                if (_transaction != null)
                {
                    _transaction.Dispose();
                }

                if (_table != null)
                {
                    _table.Dispose();
                }

                _session.Dispose();
            }
        }
    }
}
