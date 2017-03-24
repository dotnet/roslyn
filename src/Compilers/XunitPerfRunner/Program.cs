// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.Xunit.Performance.Api;
using Microsoft.Xunit.Performance;
using System.Threading;

namespace Runner
{
    public static class Program
    {
        [Benchmark]
        public static void BenchFoo() {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Thread.Sleep(1);
                }
            }
        }

        public static void Main(string[] args)
        {
            using (XunitPerformanceHarness p = new XunitPerformanceHarness(args))
            {
                string entryAssemblyPath = Assembly.GetEntryAssembly().Location;
                p.RunBenchmarks(entryAssemblyPath);
            }
        }
    }
}
