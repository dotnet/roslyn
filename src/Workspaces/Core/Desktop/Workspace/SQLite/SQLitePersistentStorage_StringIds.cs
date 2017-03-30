// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;
using SQLite;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorage
    {
        private readonly ConcurrentDictionary<string, int> _stringToIdMap = new ConcurrentDictionary<string, int>();

        private void FetchStringTable(SQLiteConnection connection)
        {
            foreach (var v in connection.Table<StringInfo>())
            {
                AddToStringTable(v);
            }
        }

        private bool AddToStringTable(StringInfo stringInfo)
        {
            // Note that TryAdd won't overwrite an existing string->id pair.  That's what
            // we want.  we don't want the strings we've allocated from the DB to be what
            // we hold onto.  We'd rather hold onto the strings we get from sources like
            // the workspaces, to prevent excessive duplication.
            return _stringToIdMap.TryAdd(stringInfo.Value, stringInfo.Id);
        }

        private int? TryGetStringId(SQLiteConnection connection, string value)
        {
            // First see if we've cached the ID for this value locally.  If so, just return
            // what we already have.
            if (_stringToIdMap.TryGetValue(value, out int existingId))
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

        private int? TryGetStringIdFromDatabase(SQLiteConnection connection, string value)
        {
            // First, check if we can find that string in the string table.
            var stringInfo = TryGetStringIdFromDatabaseWorker(connection, value, canReturnNull: true);
            if (stringInfo != null)
            {
                // Found the value already in the db.  Another process (or thread) might have added it.
                // We're done at this point.
                return stringInfo.Id;
            }

            // The string wasn't in the db string table.  Add it.  Note: this may fail if some
            // other thread/process beats us there as this table has a 'unique' constraint on the
            // values.
            try
            {
                stringInfo = new StringInfo { Value = value };
                connection.Insert(stringInfo);

                // Successfully added the string.  Return the ID it was given.
                return stringInfo.Id;
            }
            catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Constraint)
            {
                // We got a constraint violation.  This means someone else beat us to adding this
                // string to the string-table.  We should always be able to find the string now.
                stringInfo = TryGetStringIdFromDatabaseWorker(connection, value, canReturnNull: false);
                return stringInfo.Id;
            }
            catch (Exception ex)
            {
                // Some other error occurred.  Log it and return nothing.
                StorageDatabaseLogger.LogException(ex);
            }

            return null;
        }

        private StringInfo TryGetStringIdFromDatabaseWorker(
            SQLiteConnection connection, string value, bool canReturnNull)
        {
            StringInfo stringInfo = null;

            try
            {
                stringInfo = connection.Find<StringInfo>(i => i.Value == value);
            }
            catch (Exception ex)
            {
                // If we simply failed to even talk to the DB then we have to bail out.  There's
                // nothing we can accomplish at this point.
                StorageDatabaseLogger.LogException(ex);
                return null;
            }

            // If we got a real value then return it. If we got null back, then return it if our caller
            // is ok with that otherwise throw if it was not expected
            if (stringInfo != null)
            {
                return stringInfo;
            }

            if (canReturnNull)
            {
                return null;
            }

            throw new InvalidOperationException();
        }
    }
}