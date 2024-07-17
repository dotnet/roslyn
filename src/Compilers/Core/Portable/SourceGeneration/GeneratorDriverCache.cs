// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis
{
    internal sealed class GeneratorDriverCache
    {
        internal const int MaxCacheSize = 100;

        private readonly (string cacheKey, GeneratorDriver driver)[] _cachedDrivers = new (string, GeneratorDriver)[MaxCacheSize];

        private readonly object _cacheLock = new object();

        private int _cacheSize = 0;

        public GeneratorDriver? TryGetDriver(string cacheKey) => AddOrUpdateMostRecentlyUsed(cacheKey, driver: null);

        public void CacheGenerator(string cacheKey, GeneratorDriver driver) => AddOrUpdateMostRecentlyUsed(cacheKey, driver);

        public int CacheSize => _cacheSize;

        /// <summary>
        /// Attempts to find a driver based on <paramref name="cacheKey"/>. If a matching driver is found in the 
        /// cache, or explicitly passed via <paramref name="driver"/>, the cache is updated so that it is at the
        /// head of the list.
        /// </summary>
        /// <param name="cacheKey">The key to lookup the driver by in the cache</param>
        /// <param name="driver">An optional driver that should be cached, if not already found in the cache</param>
        /// <returns></returns>
        private GeneratorDriver? AddOrUpdateMostRecentlyUsed(string cacheKey, GeneratorDriver? driver)
        {
            lock (_cacheLock)
            {
                // try and find the driver if it's present
                int i = 0;
                for (; i < _cacheSize; i++)
                {
                    if (_cachedDrivers[i].cacheKey == cacheKey)
                    {
                        driver ??= _cachedDrivers[i].driver;
                        break;
                    }
                }

                // if we found it (or were passed a new one), update the cache so its at the head of the list
                if (driver is not null)
                {
                    for (i = Math.Min(i, MaxCacheSize - 1); i > 0; i--)
                    {
                        _cachedDrivers[i] = _cachedDrivers[i - 1];
                    }
                    _cachedDrivers[0] = (cacheKey, driver);
                    _cacheSize = Math.Min(MaxCacheSize, _cacheSize + 1);
                }

                return driver;
            }
        }
    }
}
