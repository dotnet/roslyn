// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using Microsoft.CodeAnalysis.SQLite.Interop;

namespace Microsoft.CodeAnalysis.SQLite.v2.Interop;

/// <summary>
/// Represents a prepared sqlite statement.  <see cref="SqlStatement"/>s can be 
/// <see cref="Step"/>ed (i.e. executed).  Executing a statement can result in 
/// either <see cref="Result.DONE"/> if the command completed and produced no
/// value, or <see cref="Result.ROW"/> if it evaluated out to a sql row that can
/// then be queried.
/// <para>
/// If a statement is parameterized then parameters can be provided by the 
/// BindXXX overloads.  Bind is 1-based (to match sqlite).</para>
/// <para>
/// When done executing a statement, the statement should be <see cref="Reset"/>.
/// The easiest way to ensure this is to just use a 'using' statement along with
/// a <see cref="ResettableSqlStatement"/>.  By resetting the statement, it can
/// then be used in the future with new bound parameters.</para>
/// <para>
/// Finalization/destruction of the underlying raw sqlite statement is handled
/// by <see cref="SqlConnection.Close_OnlyForUseBySQLiteConnectionPool"/>.</para>
/// </summary>
internal readonly struct SqlStatement(SqlConnection connection, SafeSqliteStatementHandle statement)
{
    internal void Close_OnlyForUseBySqlConnection()
        => statement.Dispose();

    public void ClearBindings()
        => connection.ThrowIfNotOk(NativeMethods.sqlite3_clear_bindings(statement));

    public void Reset()
        => connection.ThrowIfNotOk(NativeMethods.sqlite3_reset(statement));

    public Result Step(bool throwOnError = true)
    {
        var stepResult = NativeMethods.sqlite3_step(statement);

        // Anything other than DONE or ROW is an error when stepping.
        // throw if the caller wants that, or just return the value
        // otherwise.
        if (stepResult is not Result.DONE and not Result.ROW)
        {
            if (throwOnError)
            {
                connection.Throw(stepResult);
                throw ExceptionUtilities.Unreachable();
            }
        }

        return stepResult;
    }

    internal void BindStringParameter(int parameterIndex, string value)
    {
        const int OptimizedLengthThreshold = 2048;

        // Attempt to stackalloc UTF-8 converted small strings to avoid lots of allocs.
        // This code can be removed once we move to a build of sqlitepcl that contains:
        // https://github.com/ericsink/SQLitePCL.raw/pull/401

        // This is safe as sqlite will still copy our bytes over to its own internal memory (see
        // conversation here: https://github.com/dotnet/roslyn/pull/51111#pullrequestreview-588169715)
        // on the topic.  So it's fine that our own stackalloc'ed bytes will be gone when this function
        // returns and our caller continues to interact with this SqlStatement instance.

        // Only do this for short strings anyways.  If the string has more characters than this threshold, then it
        // will certainly have more bytes than this threshold.
        if (value.Length <= OptimizedLengthThreshold)
        {
            // Now see if the UTF-8 encoded versions is also within the threshold.  As almost all of our strings are
            // ascii, this will be true for nearly all of them.
            var utf8ByteCount = Encoding.UTF8.GetByteCount(value);
            if (utf8ByteCount <= OptimizedLengthThreshold)
            {
                Span<byte> bytes = stackalloc byte[utf8ByteCount];
#if NET
                Contract.ThrowIfFalse(Encoding.UTF8.GetBytes(value.AsSpan(), bytes) == utf8ByteCount);
#else
                unsafe
                {
                    fixed (char* charsPtr = value)
                    fixed (byte* bytesPtr = bytes)
                    {
                        Contract.ThrowIfFalse(Encoding.UTF8.GetBytes(charsPtr, value.Length, bytesPtr, utf8ByteCount) == utf8ByteCount);
                    }
                }
#endif
                connection.ThrowIfNotOk(NativeMethods.sqlite3_bind_text(statement, parameterIndex, bytes));
                return;
            }
        }

        connection.ThrowIfNotOk(NativeMethods.sqlite3_bind_text(statement, parameterIndex, value));
    }

    internal void BindInt64Parameter(int parameterIndex, long value)
        => connection.ThrowIfNotOk(NativeMethods.sqlite3_bind_int64(statement, parameterIndex, value));

    internal void BindBlobParameter(int parameterIndex, ReadOnlySpan<byte> bytes)
        => connection.ThrowIfNotOk(NativeMethods.sqlite3_bind_blob(statement, parameterIndex, bytes));

    internal int GetInt32At(int columnIndex)
        => NativeMethods.sqlite3_column_int(statement, columnIndex);

    internal long GetInt64At(int columnIndex)
        => NativeMethods.sqlite3_column_int64(statement, columnIndex);

    internal string GetStringAt(int columnIndex)
        => NativeMethods.sqlite3_column_text(statement, columnIndex);
}
