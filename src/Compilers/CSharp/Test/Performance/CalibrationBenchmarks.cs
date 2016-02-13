// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.Xunit.Performance;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.PerformanceTests
{
    public class CalibrationBenchmarks
    {
        [Benchmark]
        public void DoNothing()
        {
            Benchmark.Iterate(() => { });
        }

        [Benchmark]
        [InlineData(100)]
        public void Sleep(int durationInMilliseconds)
        {
            Benchmark.Iterate(() => Thread.Sleep(durationInMilliseconds));
        }
    }
}
