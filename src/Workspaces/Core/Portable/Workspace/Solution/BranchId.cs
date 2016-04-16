// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
