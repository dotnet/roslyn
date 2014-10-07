// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host
{
    internal static class CacheId
    {
        private static int nextCacheId = 0;
        private static long nextItemId = 0;

        public static int NextCacheId()
        {
            return Interlocked.Increment(ref nextCacheId);
        }

        public static long NextItemId()
        {
            return Interlocked.Increment(ref nextItemId);
        }
    }
}
