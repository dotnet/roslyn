// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis
{
    internal class GeneratorDriverCache
    {
        internal const int MaxCacheSize = 10;

        private readonly (string cacheKey, GeneratorDriver driver)[] _cachedDrivers = new (string, GeneratorDriver)[MaxCacheSize];

        private readonly object _cacheLock = new object();

        private int _cacheSize = 0;

        public GeneratorDriver? TryGetDriver(string cacheKey)
        {
            lock (_cacheLock)
            {
                int i = 0;
                GeneratorDriver? driver = null;

                for (; i < _cacheSize; i++)
                {
                    if (_cachedDrivers[i].cacheKey == cacheKey)
                    {
                        driver = _cachedDrivers[i].driver;
                        break;
                    }
                }

                if (driver is not null)
                {
                    // update the cache so that the found driver is at the head
                    for (; i > 0; i--)
                    {
                        _cachedDrivers[i] = _cachedDrivers[i - 1];
                    }
                    _cachedDrivers[0] = (cacheKey, driver);
                }

                return driver;
            }
        }

        public void CacheGenerator(string cacheKey, GeneratorDriver driver)
        {
            lock (_cacheLock)
            {
                // try and find the driver if it's present
                int i = 0;
                for (; i < _cacheSize; i++)
                {
                    if (_cachedDrivers[i].cacheKey == cacheKey)
                    {
                        break;
                    }
                }

                // update the cache so that the driver is at the head
                for (i = Math.Min(i, MaxCacheSize - 1); i > 0; i--)
                {
                    _cachedDrivers[i] = _cachedDrivers[i - 1];
                }
                _cachedDrivers[0] = (cacheKey, driver);
                _cacheSize = Math.Min(MaxCacheSize, _cacheSize + 1);
            }
        }
    }
}
