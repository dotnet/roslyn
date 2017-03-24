// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.Xunit.Performance.Api;

namespace Runner
{
    public static class Program
    {
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
