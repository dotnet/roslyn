// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Roslyn.Utilities;
using SQLitePCL;

namespace Microsoft.CodeAnalysis.SQLite.Interop
{
    /// <summary>
    /// Represents a prepared sqlite statement.  <see cref="SqlStatement"/>s can be 
    /// <see cref="Step"/>ed (i.e. executed).  Executing a statement can result in 
    /// either <see cref="Result.DONE"/> if the command completed and produced no
    /// value, or <see cref="Result.ROW"/> if it evaluated out to a sql row that can
    /// then be queried.
    /// 
    /// If a statement is parameterized then parameters can be provided by the 
    /// BindXXX overloads.  Bind is 1-based (to match sqlite).  
    /// 
    /// When done executing a statement, the statement should be <see cref="Reset"/>.
    /// The easiest way to ensure this is to just use a 'using' statement along with
    /// a <see cref="ResettableSqlStatement"/>.  By resetting the statement, it can
    /// then be used in the future with new bound parameters.
    /// 
    /// Finalization/destruction of the underlying raw sqlite statement is handled
    /// by <see cref="SqlConnection.Close_OnlyForUseBySqlPersistentStorage"/>.
    /// </summary>
    internal struct SqlStatement
    {
        private readonly SqlConnection _connection;
        private readonly sqlite3_stmt _rawStatement;

        public SqlStatement(SqlConnection connection, sqlite3_stmt statement)
        {
            _connection = connection;
            _rawStatement = statement;
        }

        internal void Close_OnlyForUseBySqlConnection()
            => _connection.ThrowIfNotOk(raw.sqlite3_finalize(_rawStatement));

        public void Reset()
            => _connection.ThrowIfNotOk(raw.sqlite3_reset(_rawStatement));

        public Result Step(bool throwOnError = true)
        {
            var stepResult = (Result)raw.sqlite3_step(_rawStatement);

            // Anything other than DONE or ROW is an error when stepping.
            // throw if the caller wants that, or just return the value
            // otherwise.
            if (stepResult != Result.DONE && stepResult != Result.ROW)
            {
                if (throwOnError)
                {
                    _connection.Throw(stepResult);
                    throw ExceptionUtilities.Unreachable;
                }
            }

            return stepResult;
        }

        internal void BindStringParameter(int parameterIndex, string value)
            => _connection.ThrowIfNotOk(raw.sqlite3_bind_text(_rawStatement, parameterIndex, value));

        internal void BindInt64Parameter(int parameterIndex, long value)
            => _connection.ThrowIfNotOk(raw.sqlite3_bind_int64(_rawStatement, parameterIndex, value));

        // SQLite PCL does not expose sqlite3_bind_blob function that takes a length.  So we explicitly
        // DLL import it here.  See https://github.com/ericsink/SQLitePCL.raw/issues/135

        internal void BindBlobParameter(int parameterIndex, byte[] value, int length)
            => _connection.ThrowIfNotOk(sqlite3_bind_blob(_rawStatement.ptr, parameterIndex, value, length, new IntPtr(-1)));

        [DllImport("e_sqlite3.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_bind_blob(IntPtr stmt, int index, byte[] val, int nSize, IntPtr nTransient);

        internal byte[] GetBlobAt(int columnIndex)
            => raw.sqlite3_column_blob(_rawStatement, columnIndex);

        internal int GetInt32At(int columnIndex)
            => raw.sqlite3_column_int(_rawStatement, columnIndex);

        internal long GetInt64At(int columnIndex)
            => raw.sqlite3_column_int64(_rawStatement, columnIndex);

        internal string GetStringAt(int columnIndex)
            => raw.sqlite3_column_text(_rawStatement, columnIndex);
    }
}
