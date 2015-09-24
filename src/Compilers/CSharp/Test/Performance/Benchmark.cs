// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.CSharp.PerformanceTests
{
    /// <summary>
    /// Shadows <see cref="Xunit.Performance.Benchmark"/> to provider the <see cref="Iterate(Action)"/> method.
    /// This is a stop-gap until we can upgrade to a version of Microsoft.DotNet.xunit.performance that has this method.
    /// </summary>
    internal static class Benchmark
    {
        public static void Iterate(Action action)
        {
            foreach(var iteration in Xunit.Performance.Benchmark.Iterations)
            {
                using (var measurement = iteration.StartMeasurement())
                {
                    action();
                }
            }
        }
    }
}
