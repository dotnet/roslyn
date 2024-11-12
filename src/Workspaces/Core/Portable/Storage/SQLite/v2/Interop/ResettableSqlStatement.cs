// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.SQLite.v2.Interop;

/// <summary>
/// Simple wrapper struct for a <see cref="SqlStatement"/> that helps ensure that the statement is always has it's
/// bindings cleared (<see cref="SqlStatement.ClearBindings"/>) and is <see cref="SqlStatement.Reset"/> after it is
/// used.
/// <para/>
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
internal readonly struct ResettableSqlStatement(SqlStatement statement) : IDisposable
{
    public readonly SqlStatement Statement = statement;

    public void Dispose()
    {
        // Clear out any bindings we've made so the statement doesn't hold onto data longer than necessary.
        Statement.ClearBindings();

        // Reset the statement so it can be run again.
        Statement.Reset();
    }
}
