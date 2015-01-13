// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading;

namespace Roslyn.Test.Utilities
{
    public class CultureContext : IDisposable
    {
        private readonly CultureInfo threadCulture = CultureInfo.InvariantCulture;
        public CultureContext(string testCulture)
        {
            threadCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(testCulture, useUserOverride:false);
        }

        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = threadCulture;
        }
    }
}
