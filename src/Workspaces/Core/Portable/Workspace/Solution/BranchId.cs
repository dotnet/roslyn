// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// solution branch Id
    /// </summary>
    internal class BranchId
    {
        private static int nextId = 0;

        private readonly int id;

        private BranchId(int id)
        {
            this.id = id;
        }

        internal static BranchId GetNextId()
        {
            return new BranchId(Interlocked.Increment(ref nextId));
        }
    }
}
