// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.SQLite.Interop;

// From https://sqlite.org/c3ref/c_abort.html
// Uncomment what you need.  Leave the rest commented out to make it clear
// what we are/aren't using.
internal enum Result
{
    OK = 0,                /* Successful result */
    ERROR = 1,             /* SQL error or missing database */
    // INTERNAL = 2,       /* Internal logic error in SQLite */
    // PERM = 3,           /* Access permission denied */
    // ABORT = 4,          /* Callback routine requested an abort */
    // BUSY = 5,           /* The database file is locked */
    // LOCKED = 6,         /* A table in the database is locked */
    // NOMEM = 7,          /* A malloc() failed */
    // READONLY = 8,       /* Attempt to write a readonly database */
    // INTERRUPT = 9,      /* Operation terminated by sqlite3_interrupt()*/
    // IOERR = 10,         /* Some kind of disk I/O error occurred */
    // CORRUPT = 11,       /* The database disk image is malformed */
    // NOTFOUND = 12,      /* Unknown opcode in sqlite3_file_control() */
    // FULL = 13,          /* Insertion failed because database is full */
    // CANTOPEN = 14,      /* Unable to open the database file */
    // PROTOCOL = 15,      /* Database lock protocol error */
    // EMPTY = 16,         /* Database is empty */
    // SCHEMA = 17,        /* The database schema changed */
    // TOOBIG = 18,        /* String or BLOB exceeds size limit */
    CONSTRAINT = 19,       /* Abort due to constraint violation */
    // MISMATCH = 20,      /* Data type mismatch */
    // MISUSE = 21,        /* Library used incorrectly */
    // NOLFS = 22,         /* Uses OS features not supported on host */
    // AUTH = 23,          /* Authorization denied */
    // FORMAT = 24,        /* Auxiliary database format error */
    // RANGE = 25,         /* 2nd parameter to sqlite3_bind out of range */
    // NOTADB = 26,        /* File opened that is not a database file */
    // NOTICE = 27,        /* Notifications from sqlite3_log() */
    // WARNING = 28,       /* Warnings from sqlite3_log() */
    ROW = 100,             /* sqlite3_step() has another row ready */
    DONE = 101             /* sqlite3_step() has finished executing */
}
