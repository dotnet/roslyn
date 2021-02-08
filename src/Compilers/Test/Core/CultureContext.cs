// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Globalization;
using System.Threading;

namespace Roslyn.Test.Utilities
{
    public class CultureContext : IDisposable
    {
        private readonly CultureInfo _threadCulture;

        public CultureContext(CultureInfo cultureInfo)
        {
            _threadCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = cultureInfo;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _threadCulture;
        }
    }
}
