// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Globalization;

namespace Roslyn.Test.Utilities
{
    public class EnsureEnglishUICulture : IDisposable
    {
        public static CultureInfo PreferredOrNull
        {
            get
            {
                var currentUICultureName = CultureInfo.CurrentUICulture.Name;
                if (currentUICultureName.Length == 0 || currentUICultureName.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return CultureInfo.InvariantCulture;
            }
        }

        private bool _needToRestore;
        private readonly CultureInfo _threadUICulture;
        private readonly int _threadId;

        public EnsureEnglishUICulture()
        {
            _threadId = Environment.CurrentManagedThreadId;
            var preferred = PreferredOrNull;

            if (preferred != null)
            {
                _threadUICulture = CultureInfo.CurrentUICulture;
                _needToRestore = true;

                CultureInfo.CurrentUICulture = preferred;
            }
        }

        public void Dispose()
        {
            Debug.Assert(_threadId == Environment.CurrentManagedThreadId);

            if (_needToRestore && _threadId == Environment.CurrentManagedThreadId)
            {
                _needToRestore = false;
                CultureInfo.CurrentUICulture = _threadUICulture;
            }
        }
    }
}
