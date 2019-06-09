// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.SQLite.Interop
{
    internal class SqlException : Exception
    {
        public readonly Result Result;

        public SqlException(Result result, string message)
            : base(message)
        {
            this.Result = result;
        }
    }
}
