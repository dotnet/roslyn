// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace Roslyn.Test.Utilities
{
    public class EnsureEnglishUICulture : IDisposable
    {
        public static CultureInfo PreferredOrNull
        {
            get
            {
                var currentUICulture = Thread.CurrentThread.CurrentUICulture;
                if (currentUICulture.Name.StartsWith("en") || currentUICulture.Name == "")
                {
                    return null;
                }

                return CultureInfo.InvariantCulture;
            }
        }

        private bool needToRestore;
        private readonly CultureInfo threadUICulture;
        private readonly int threadId;

        public EnsureEnglishUICulture()
        {
            threadId = Thread.CurrentThread.ManagedThreadId;
            var preferred = PreferredOrNull;

            if (preferred != null)
            {
                threadUICulture = Thread.CurrentThread.CurrentUICulture;
                needToRestore = true;
                Thread.CurrentThread.CurrentUICulture = preferred;
            }
        }

        public void Dispose()
        {
            Debug.Assert(threadId == Thread.CurrentThread.ManagedThreadId);

            if (needToRestore && threadId == Thread.CurrentThread.ManagedThreadId)
            {
                needToRestore = false;
                Thread.CurrentThread.CurrentUICulture = threadUICulture;
            }
        }
    }
}
