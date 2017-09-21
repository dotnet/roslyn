// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.SQLite.Interop
{
    /// <summary>
    /// Simple wrapper struct for a <see cref="SqlStatement"/> that helps ensure that the statement
    /// is always <see cref="SqlStatement.Reset"/> after it is used.
    /// 
    /// See https://sqlite.org/c3ref/stmt.html:
    /// The life-cycle of a prepared statement object usually goes like this:
    ///    1) Create the prepared statement object using sqlite3_prepare_v2().
    ///    2) Bind values to parameters using the sqlite3_bind_* () interfaces.
    ///    3) Run the SQL by calling sqlite3_step() one or more times.
    ///    4) Reset the prepared statement using sqlite3_reset() then go back to step 2. Do this zero or more times.
    ///    5) Destroy the object using sqlite3_finalize().
    ///
    /// This type helps ensure that '4' happens properly by clients executing statement.
    /// Note that destroying/finalizing a statement is not the responsibility of a client
    /// as it will happen to all prepared statemnets when the <see cref="SqlStatement"/> is
    /// <see cref="SqlStatement.Close_OnlyForUseBySqlConnection"/>d.
    /// </summary>
    internal struct ResettableSqlStatement : IDisposable
    {
        public readonly SqlStatement Statement;

        public ResettableSqlStatement(SqlStatement statement)
        {
            Statement = statement;
        }

        public void Dispose()
            => Statement.Reset();
    }
}
