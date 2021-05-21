// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Globalization;

namespace Roslyn.Test.Utilities
{
    public class EnsureInvariantCulture : IDisposable
    {
        private readonly CultureInfo _threadCulture;
        private readonly int _threadId;

        public EnsureInvariantCulture()
        {
            _threadId = Environment.CurrentManagedThreadId;
            _threadCulture = CultureInfo.CurrentCulture;

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        public void Dispose()
        {
            Debug.Assert(_threadId == Environment.CurrentManagedThreadId);

            if (_threadId == Environment.CurrentManagedThreadId)
            {
                CultureInfo.CurrentCulture = _threadCulture;
            }
        }
    }
}
