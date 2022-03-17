// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests
{
    public class GeneratorDriverCacheTests : CommandLineTestBase
    {

        [Fact]
        public void DriverCache_Returns_Null_For_No_Match()
        {
            var driverCache = new GeneratorDriverCache();
            var driver = driverCache.TryGetDriver("0");

            Assert.Null(driver);
        }

        [Fact]
        public void DriverCache_Returns_Cached_Driver()
        {
            var drivers = GetDrivers(1);
            var driverCache = new GeneratorDriverCache();
            driverCache.CacheGenerator("0", drivers[0]);

            var driver = driverCache.TryGetDriver("0");
            Assert.Same(driver, drivers[0]);
        }

        [Fact]
        public void DriverCache_Can_Cache_Multiple_Drivers()
        {
            var drivers = GetDrivers(3);

            var driverCache = new GeneratorDriverCache();
            driverCache.CacheGenerator("0", drivers[0]);
            driverCache.CacheGenerator("1", drivers[1]);
            driverCache.CacheGenerator("2", drivers[2]);

            var driver = driverCache.TryGetDriver("0");
            Assert.Same(driver, drivers[0]);

            driver = driverCache.TryGetDriver("1");
            Assert.Same(driver, drivers[1]);

            driver = driverCache.TryGetDriver("2");
            Assert.Same(driver, drivers[2]);
        }

        [Fact]
        public void DriverCache_Evicts_Least_Recently_Used()
        {
            var drivers = GetDrivers(GeneratorDriverCache.MaxCacheSize + 2);
            var driverCache = new GeneratorDriverCache();

            // put n+1 drivers into the cache
            for (int i = 0; i < GeneratorDriverCache.MaxCacheSize + 1; i++)
            {
                driverCache.CacheGenerator(i.ToString(), drivers[i]);
            }
            // current cache state is 
            // (10, 9, 8, 7, 6, 5, 4, 3, 2, 1)

            // now try and retrieve the first driver which should no longer be in the cache
            var driver = driverCache.TryGetDriver("0");
            Assert.Null(driver);

            // add it back
            driverCache.CacheGenerator("0", drivers[0]);

            // current cache state is 
            // (0, 10, 9, 8, 7, 6, 5, 4, 3, 2)

            // access some drivers in the middle
            driver = driverCache.TryGetDriver("7");
            driver = driverCache.TryGetDriver("4");
            driver = driverCache.TryGetDriver("2");

            // current cache state is 
            // (2, 4, 7, 0, 10, 9, 8, 6, 5, 3)

            // try and get a new driver that was never in the cache
            driver = driverCache.TryGetDriver("11");
            Assert.Null(driver);
            driverCache.CacheGenerator("11", drivers[11]);

            // current cache state is 
            // (11, 2, 4, 7, 0, 10, 9, 8, 6, 5)

            // get a driver that has been evicted
            driver = driverCache.TryGetDriver("3");
            Assert.Null(driver);
        }

        private static GeneratorDriver[] GetDrivers(int count) => Enumerable.Range(0, count).Select(i => CSharpGeneratorDriver.Create(Array.Empty<ISourceGenerator>())).ToArray();
    }
}
