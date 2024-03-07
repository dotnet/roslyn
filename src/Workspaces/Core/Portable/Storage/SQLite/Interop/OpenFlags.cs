// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.SQLite.Interop;

// From: https://sqlite.org/c3ref/c_open_autoproxy.html
// Uncomment what you need.  Leave the rest commented out to make it clear
// what we are/aren't using.
internal enum OpenFlags
{
    // SQLITE_OPEN_READONLY         = 0x00000001, /* Ok for sqlite3_open_v2() */
    SQLITE_OPEN_READWRITE = 0x00000002, /* Ok for sqlite3_open_v2() */
    SQLITE_OPEN_CREATE = 0x00000004, /* Ok for sqlite3_open_v2() */
    // SQLITE_OPEN_DELETEONCLOSE    = 0x00000008, /* VFS only */
    // SQLITE_OPEN_EXCLUSIVE        = 0x00000010, /* VFS only */
    // SQLITE_OPEN_AUTOPROXY        = 0x00000020, /* VFS only */
    SQLITE_OPEN_URI = 0x00000040, /* Ok for sqlite3_open_v2() */
    // SQLITE_OPEN_MEMORY           = 0x00000080, /* Ok for sqlite3_open_v2() */
    // SQLITE_OPEN_MAIN_DB          = 0x00000100, /* VFS only */
    // SQLITE_OPEN_TEMP_DB          = 0x00000200, /* VFS only */
    // SQLITE_OPEN_TRANSIENT_DB     = 0x00000400, /* VFS only */
    // SQLITE_OPEN_MAIN_JOURNAL     = 0x00000800, /* VFS only */
    // SQLITE_OPEN_TEMP_JOURNAL     = 0x00001000, /* VFS only */
    // SQLITE_OPEN_SUBJOURNAL       = 0x00002000, /* VFS only */
    // SQLITE_OPEN_MASTER_JOURNAL   = 0x00004000, /* VFS only */
    SQLITE_OPEN_NOMUTEX = 0x00008000, /* Ok for sqlite3_open_v2() */
    // SQLITE_OPEN_FULLMUTEX        = 0x00010000, /* Ok for sqlite3_open_v2() */
    SQLITE_OPEN_SHAREDCACHE = 0x00020000, /* Ok for sqlite3_open_v2() */
    // SQLITE_OPEN_PRIVATECACHE     = 0x00040000, /* Ok for sqlite3_open_v2() */
    // SQLITE_OPEN_WAL              = 0x00080000, /* VFS only */
}
