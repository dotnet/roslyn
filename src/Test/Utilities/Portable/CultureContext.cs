// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
