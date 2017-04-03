// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;
using SQLitePCL;

namespace Microsoft.CodeAnalysis.SQLite.Interop
{
    /// <summary>
    /// Encapsulates a connection to a sqlite database.  On construction an attempt will be made
    /// to open the DB if it exists, or create it if it does not.
    /// 
    /// Connections are considered relatively heavyweight and are pooled until the <see cref="SQLitePersistentStorage"/>
    /// is <see cref="SQLitePersistentStorage.Close"/>d.  Connections can be used by different threads,
    /// but only as long as they are used by one thread at a time.  They are not safe for concurrent
    /// use by several threads.
    /// 
    /// <see cref="SqlStatement"/>s can be created through the user of <see cref="GetResettableStatement"/>.
    /// These statements are cached for the lifetime of the connection and are only finalized
    /// (i.e. destroyed) when the connection is closed.
    /// </summary>
    internal partial class SqlConnection
    {
        /// <summary>
        /// The raw handle to the underlying DB.
        /// </summary>
        private readonly sqlite3 _handle;

        /// <summary>
        /// Our cache of prepared statements for given sql strings.
        /// </summary>
        private readonly Dictionary<string, SqlStatement> _queryToStatement = new Dictionary<string, SqlStatement>();

        /// <summary>
        /// Whether or not we're in a transaction.  We currently don't supported nested transactions.
        /// If we want that, we can achieve it through sqlite "save points".  However, that's adds a 
        /// lot of complexity that is nice to avoid.
        /// </summary>
        private bool _inTransaction;

        public SqlConnection(string databasePath)
        {
            var flags = OpenFlags.SQLITE_OPEN_CREATE | OpenFlags.SQLITE_OPEN_READWRITE;

            var result = (Result)raw.sqlite3_open_v2(databasePath, out var handle, (int)flags, vfs: null);

            if (result != Result.OK)
            {
                throw new SqlException(result, $"Could not open database file: {databasePath} ({result})");
            }

            Contract.ThrowIfNull(handle);

            _handle = handle;
            raw.sqlite3_busy_timeout(_handle, (int)TimeSpan.FromMinutes(1).TotalMilliseconds);
        }

        ~SqlConnection()
        {
            if (!Environment.HasShutdownStarted)
            {
                FatalError.Report(new InvalidOperationException("SqlConnection was not properly closed"));
            }
        }

        internal void Close_OnlyForUseBySqlPersistentStorage()
        {
            GC.SuppressFinalize(this);

            Contract.ThrowIfNull(_handle);

            // release all the cached statements we have.
            foreach (var statement in _queryToStatement.Values)
            {
                statement.Close_OnlyForUseBySqlConnection();
            }

            _queryToStatement.Clear();

            // Finally close our handle to the actual DB.
            ThrowIfNotOk(raw.sqlite3_close(_handle));
        }

        public void ExecuteCommand(string command, bool throwOnError = true)
        {
            using (var resettableStatement = GetResettableStatement(command))
            {
                var statement = resettableStatement.Statement;
                var result = statement.Step(throwOnError);
                if (result != Result.DONE && throwOnError)
                {
                    Throw(result);
                }
            }
        }

        public ResettableSqlStatement GetResettableStatement(string query)
        {
            if (!_queryToStatement.TryGetValue(query, out var statement))
            {
                var result = (Result)raw.sqlite3_prepare_v2(_handle, query, out var rawStatement);
                ThrowIfNotOk(result);
                statement = new SqlStatement(this, rawStatement);
                _queryToStatement[query] = statement;
            }

            return new ResettableSqlStatement(statement);
        }

        public void RunInTransaction(Action action)
        {
            try
            {
                if (_inTransaction)
                {
                    throw new InvalidOperationException("Nested transactions not currently supported");
                }

                _inTransaction = true;

                ExecuteCommand("begin transaction");
                action();
                ExecuteCommand("commit transaction");
            }
            catch (SqlException ex) when (ex.Result == Result.FULL || 
                                          ex.Result == Result.IOERR ||
                                          ex.Result == Result.BUSY ||
                                          ex.Result == Result.NOMEM)
            {
                // See documentation here: https://sqlite.org/lang_transaction.html
                // If certain kinds of errors occur within a transaction, the transaction 
                // may or may not be rolled back automatically. The errors that can cause 
                // an automatic rollback include:

                // SQLITE_FULL: database or disk full
                // SQLITE_IOERR: disk I/ O error
                // SQLITE_BUSY: database in use by another process
                // SQLITE_NOMEM: out or memory

                // It is recommended that applications respond to the errors listed above by
                // explicitly issuing a ROLLBACK command. If the transaction has already been
                // rolled back automatically by the error response, then the ROLLBACK command 
                // will fail with an error, but no harm is caused by this.
                Rollback(throwOnError: false);
                throw;
            }
            catch (Exception)
            {
                Rollback(throwOnError: true);
                throw;
            }
            finally
            {
                _inTransaction = false;
            }
        }

        private void Rollback(bool throwOnError)
            => this.ExecuteCommand("rollback transaction", throwOnError);

        public int LastInsertRowId()
            => (int)raw.sqlite3_last_insert_rowid(_handle);

        public void ThrowIfNotOk(int result)
            => ThrowIfNotOk((Result)result);

        public void ThrowIfNotOk(Result result)
            => ThrowIfNotOk(_handle, result);

        public static void ThrowIfNotOk(sqlite3 handle, Result result)
        {
            if (result != Result.OK)
            {
                Throw(handle, result);
            }
        }

        public void Throw(Result result)
            => Throw(_handle, result);

        public static void Throw(sqlite3 handle, Result result)
            => throw new SqlException(result, raw.sqlite3_errmsg(handle));
    }
}