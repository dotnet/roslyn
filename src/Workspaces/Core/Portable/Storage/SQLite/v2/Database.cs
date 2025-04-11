// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.SQLite.v2;

internal enum Database
{
    /// <summary>
    /// The database that is stored on disk and actually persists data across VS sessions.
    /// </summary>
    Main,

    /// <summary>
    /// An in-memory database that caches values before being transferred to <see
    /// cref="Main"/>.  Does not persist across VS sessions.
    /// </summary>
    WriteCache,
}

internal static class DatabaseExtensions
{
    /// <summary>
    /// Name of the different dbs.
    /// 
    /// 1. "main" is the default that sqlite uses.  This just allows us to be explicit that we
    /// want this db.
    ///
    /// 2. "writecache" is the name for the in-memory write-cache db.  Writes will be staged
    /// there and will be periodically flushed to the real on-disk db to help with perf.
    ///
    /// Perf measurements show this as significantly better than all other design options. It's
    /// also one of the simplest in terms of the design.
    ///
    /// The design options in order of performance (slowest to fastest) are:
    ///
    /// 1. send writes directly to the main db. this is incredibly slow (since each write incurs
    /// the full IO overhead of a transaction). It is the absolute simplest in terms of
    /// implementation though.
    ///
    /// 2. send writes to a temporary on-disk db (with synchronous=off and journal_mode=memory),
    /// then flush those to the main db.  This is also quite slow due to their still needing to
    /// be disk IO with each write.  Implementation is fairly simple, with writes just going to
    /// the temp db and reads going to both.
    ///
    /// 3. Buffer writes in (.net) memory and flush them to disk.  This is much faster than '1'
    /// or '2' but requires a lot of manual book-keeping and extra complexity. For example, any
    /// reads go to the db.  So that means that reads have to ensure that any writes to the same
    /// rows have been persisted so they can observe them.
    ///
    /// 4. send writes to an sqlite in-memory cache DB.  This is extremely fast for sqlite as
    /// there is no actual IO that is performed.  It is also easy in terms of bookkeeping as
    /// both DBs have the same schema and are easy to move data between. '4' is faster than all
    /// of the above. Complexity is minimized as reading can be done just by examining both DBs
    /// in the same way. It's not as simple as '1' but it's much simpler than '3'.
    /// </summary>
    public static string GetName(this Database database)
        => database switch
        {
            Database.Main => "main",
            Database.WriteCache => "writecache",
            _ => throw ExceptionUtilities.UnexpectedValue(database),
        };
}
