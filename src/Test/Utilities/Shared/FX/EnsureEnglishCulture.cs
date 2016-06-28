// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace Roslyn.Test.Utilities
{
    public class EnsureEnglishCulture : IDisposable
    {
        public static CultureInfo PreferredOrNull
        {
            get
            {
                var currentCultureName = CultureInfo.CurrentCulture.Name;
                if (currentCultureName.Length == 0 || currentCultureName.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return CultureInfo.InvariantCulture;
            }
        }

        private bool _needToRestore;
        private readonly CultureInfo _threadCulture;
        private readonly int _threadId;

        public EnsureEnglishCulture()
        {
            _threadId = Thread.CurrentThread.ManagedThreadId;
            var preferred = PreferredOrNull;

            if (preferred != null)
            {
                _threadCulture = CultureInfo.CurrentCulture;
                _needToRestore = true;

#if DNX
                CultureInfo.CurrentCulture = preferred;
#else
                Thread.CurrentThread.CurrentCulture = preferred;
#endif
            }
        }

        public void Dispose()
        {
            Debug.Assert(_threadId == Thread.CurrentThread.ManagedThreadId);

            if (_needToRestore && _threadId == Thread.CurrentThread.ManagedThreadId)
            {
                _needToRestore = false;
#if DNX
                CultureInfo.CurrentCulture = _threadCulture;
#else
                Thread.CurrentThread.CurrentCulture = _threadCulture;
#endif
            }
        }
    }
}
