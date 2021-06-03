// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.SQLite.Interop;
using Microsoft.CodeAnalysis.SQLite.v2.Interop;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite.v2
{
    internal partial class SQLitePersistentStorage
    {
        private readonly ConcurrentDictionary<string, int> _stringToIdMap = new();

        private int? TryGetStringId(SqlConnection connection, string? value)
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
            var id = TryGetStringIdFromDatabase(connection, value);
            if (id != null)
            {
                _stringToIdMap[value] = id.Value;
            }

            return id;
        }

        private int? TryGetStringIdFromDatabase(SqlConnection connection, string value)
        {
            // First, check if we can find that string in the string table.
            var stringId = TryGetStringIdFromDatabaseWorker(connection, value, canReturnNull: true);
            if (stringId != null)
            {
                // Found the value already in the db.  Another process (or thread) might have added it.
                // We're done at this point.
                return stringId;
            }

            // The string wasn't in the db string table.  Add it.  Note: this may no-op if some
            // other thread/process beats us there as this table has a 'unique' constraint on the
            // values.
            try
            {
                stringId = connection.RunInTransaction(
                    static t => t.self.TryInsertStringIntoDatabase_MustRunInTransaction(t.connection, t.value),
                    (self: this, connection, value));

                if (stringId == null)
                {
                    // Another thread beat us to adding this string.  In this case we should just be able
                    // to read the string out from the table.  Note: this cannot fail as the string must
                    // be in the table.
                    stringId = TryGetStringIdFromDatabaseWorker(connection, value, canReturnNull: false);
                }

                Contract.ThrowIfTrue(stringId == null);
                return stringId;
            }
            catch (Exception ex)
            {
                // Some other error occurred.  Log it and return nothing.
                StorageDatabaseLogger.LogException(ex);
            }

            return null;
        }

        private int? TryInsertStringIntoDatabase_MustRunInTransaction(SqlConnection connection, string value)
        {
            if (!connection.IsInTransaction)
                throw new InvalidOperationException("Must call this while connection has transaction open");

            using var resettableStatement = connection.GetResettableStatement(_insert_into_string_table_values_0, throwOnResetError: false);

            var statement = resettableStatement.Statement;

            // SQLite bindings are 1-based.
            statement.BindStringParameter(parameterIndex: 1, value: value);

            // Try to insert the value.  Because we of the UNIQUE constraint on the table this may fail with a
            // constraint violation if another thread beats us to this.
            var stepResult = statement.Step(throwOnError: false);

            // If we got a constraint violation, notify our caller so they can retry reading the value that
            // someone else added.
            if (stepResult == Result.CONSTRAINT)
                return null;

            // Otherwise, we should have successfully inserted the value.  Return its row for the caller as that
            // is the effective ID we have for it.
            if (stepResult == Result.DONE || stepResult == Result.ROW)
                return connection.LastInsertRowId();

            // Anything else is a true failure and we want to have our exception path handle this.
            connection.Throw(stepResult);
            throw ExceptionUtilities.Unreachable;
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
    }
}
