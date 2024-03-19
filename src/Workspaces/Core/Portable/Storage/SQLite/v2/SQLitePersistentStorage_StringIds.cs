// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SQLite.Interop;
using Microsoft.CodeAnalysis.SQLite.v2.Interop;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite.v2;

internal partial class SQLitePersistentStorage
{
    private readonly ConcurrentDictionary<string, int> _stringToIdMap = [];

    private int? TryGetStringId(SqlConnection connection, string? value, bool allowWrite)
    {
        // Null strings are not supported at all.  Just ignore these. Any read/writes 
        // to null values will fail and will return 'false/null' to indicate failure
        // (which is part of the documented contract of the persistence layer API).
        if (value == null)
        {
            return null;
        }

        // First see if we've cached the ID for this value locally.  If so, just return
        // what we already have.
        if (_stringToIdMap.TryGetValue(value, out var existingId))
        {
            return existingId;
        }

        // Otherwise, try to get or add the string to the string table in the database.
        var id = TryGetStringIdFromDatabase(connection, value, allowWrite);
        if (id != null)
        {
            _stringToIdMap[value] = id.Value;
        }

        return id;
    }

    private int? TryGetStringIdFromDatabase(SqlConnection connection, string value, bool allowWrite)
    {
        // We're reading or writing.  This can be under either of our schedulers.
        Contract.ThrowIfFalse(
            TaskScheduler.Current == _connectionPoolService.Scheduler.ExclusiveScheduler ||
            TaskScheduler.Current == _connectionPoolService.Scheduler.ConcurrentScheduler);

        // First, check if we can find that string in the string table.
        var stringId = TryGetStringIdFromDatabaseWorker(connection, value, canReturnNull: true);
        if (stringId != null)
        {
            // Found the value already in the db.  Another process (or thread) might have added it.
            // We're done at this point.
            return stringId;
        }

        // If we're in a context where our caller doesn't have the write lock, give up now.  They will
        // call back in with the write lock to allow safe adding of this db ID after this.
        if (!allowWrite)
            return null;

        // We're writing.  This better always be under the exclusive scheduler.
        Contract.ThrowIfFalse(TaskScheduler.Current == _connectionPoolService.Scheduler.ExclusiveScheduler);

        // The string wasn't in the db string table.  Add it.  Note: this may fail if some
        // other thread/process beats us there as this table has a 'unique' constraint on the
        // values.
        try
        {
            // Pass in `throwOnSqlException: false` so we get the exception bubbled back to us as a result value.
            var (result, exception) = connection.RunInTransaction(
                static t => t.self.InsertStringIntoDatabase_MustRunInTransaction(t.connection, t.value),
                (self: this, connection, value),
                throwOnSqlException: false);

            if (exception != null)
            {
                // we can get two types of exceptions.  A 'CONSTRAINT' violation is an expected result and means
                // someone else beat us to adding this string to the string-table.  As such, we should always be
                // able to find the string now.
                if (exception.Result == Result.CONSTRAINT)
                    return TryGetStringIdFromDatabaseWorker(connection, value, canReturnNull: false);

                // Some other sql exception occurred (like SQLITE_FULL). These are not exceptions we can suitably
                // recover from.  In this case, transition the storage instance into being unusable. Future
                // reads/writes will get empty results.
                this.DisableStorage(exception);
                return null;
            }

            // If we didn't get an exception, we must have gotten a string back.
            return result;
        }
        catch (Exception ex)
        {
            // Some other error occurred.  Log it and return nothing.
            StorageDatabaseLogger.LogException(ex);
        }

        return null;
    }

    private int InsertStringIntoDatabase_MustRunInTransaction(SqlConnection connection, string value)
    {
        if (!connection.IsInTransaction)
        {
            throw new InvalidOperationException("Must call this while connection has transaction open");
        }

        var id = -1;

        using (var resettableStatement = connection.GetResettableStatement(_insert_into_string_table_values_0))
        {
            var statement = resettableStatement.Statement;

            // SQLite bindings are 1-based.
            statement.BindStringParameter(parameterIndex: 1, value: value);

            // Try to insert the value.  This may throw a constraint exception if some
            // other process beat us to this string.
            statement.Step();

            // Successfully added the string.  The ID for it can be retrieved as the LastInsertRowId
            // for the db.  This is also safe to call because we must be in a transaction when this
            // is invoked.
            id = connection.LastInsertRowId();
        }

        Contract.ThrowIfTrue(id == -1);
        return id;
    }

    private int? TryGetStringIdFromDatabaseWorker(
        SqlConnection connection, string value, bool canReturnNull)
    {
        try
        {
            using var resettableStatement = connection.GetResettableStatement(_select_star_from_string_table_where_0_limit_one);
            var statement = resettableStatement.Statement;

            // SQLite's binding indices are 1-based. 
            statement.BindStringParameter(parameterIndex: 1, value: value);

            var stepResult = statement.Step();
            if (stepResult == Result.ROW)
            {
                return statement.GetInt32At(columnIndex: 0);
            }
        }
        catch (Exception ex)
        {
            // If we simply failed to even talk to the DB then we have to bail out.  There's
            // nothing we can accomplish at this point.
            StorageDatabaseLogger.LogException(ex);
            return null;
        }

        // No item with this value in the table.
        if (canReturnNull)
        {
            return null;
        }

        // This should not be possible.  We only called here if we got a constraint violation.
        // So how could we then not find the string in the table?
        throw new InvalidOperationException();
    }

    private void LoadExistingStringIds(SqlConnection connection)
    {
        try
        {
            using var resettableStatement = connection.GetResettableStatement(_select_star_from_string_table);
            var statement = resettableStatement.Statement;

            Result stepResult;
            while ((stepResult = statement.Step()) == Result.ROW)
            {
                var id = statement.GetInt32At(columnIndex: 0);
                var value = statement.GetStringAt(columnIndex: 1);
                _stringToIdMap.TryAdd(value, id);
            }
        }
        catch (Exception ex)
        {
            // If we simply failed to even talk to the DB then we have to bail out.  There's
            // nothing we can accomplish at this point.
            StorageDatabaseLogger.LogException(ex);
            return;
        }
    }
}
