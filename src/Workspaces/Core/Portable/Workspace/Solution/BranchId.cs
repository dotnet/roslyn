// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// solution branch Id
    /// </summary>
    [DebuggerDisplay("{_id}")]
    internal class BranchId
    {
        private static int s_nextId;

#pragma warning disable IDE0052 // Remove unread private members
        private readonly int _id;
#pragma warning restore IDE0052 // Remove unread private members

        private BranchId(int id)
            => _id = id;

        internal static BranchId GetNextId()
            => new(Interlocked.Increment(ref s_nextId));
    }
}
