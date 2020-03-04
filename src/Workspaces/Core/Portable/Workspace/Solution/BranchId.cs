﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// solution branch Id
    /// </summary>
    internal class BranchId
    {
        private static int s_nextId;

        private readonly int _id;

        private BranchId(int id)
        {
            _id = id;
        }

        internal static BranchId GetNextId()
        {
            return new BranchId(Interlocked.Increment(ref s_nextId));
        }
    }
}
