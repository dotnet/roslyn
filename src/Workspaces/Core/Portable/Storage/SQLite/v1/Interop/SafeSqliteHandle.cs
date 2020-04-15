// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using SQLitePCL;

namespace Microsoft.CodeAnalysis.SQLite.v1.Interop
{
    internal sealed class SafeSqliteHandle : SafeSqliteHandle<sqlite3>
    {
        public SafeSqliteHandle(sqlite3 wrapper)
            : base(wrapper.ptr, wrapper)
        {
        }

        protected override bool ReleaseHandle()
        {
            raw.sqlite3_close(Wrapper);
            return true;
        }
    }
}
