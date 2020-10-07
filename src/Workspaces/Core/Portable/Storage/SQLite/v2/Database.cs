// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite.v2
{
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
}
