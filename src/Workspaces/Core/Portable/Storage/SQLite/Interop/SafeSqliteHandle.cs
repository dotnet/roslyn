// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using SQLitePCL;

namespace Microsoft.CodeAnalysis.SQLite.Interop
{
    internal sealed class SafeSqliteHandle : SafeSqliteHandle<sqlite3>
    {
        public SafeSqliteHandle(sqlite3? wrapper)
            : base(wrapper?.ptr ?? IntPtr.Zero, wrapper)
        {
        }

        protected override bool ReleaseHandle()
        {
            var result = (Result)raw.sqlite3_close(Wrapper);
            return result == Result.OK;
        }
    }
}
