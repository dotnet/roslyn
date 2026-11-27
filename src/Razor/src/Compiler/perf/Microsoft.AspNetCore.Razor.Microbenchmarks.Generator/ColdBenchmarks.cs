// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks.Generator;
public class ColdBenchmarks : AbstractBenchmark
{
    public ColdBenchmarks()
    {
        this.Cold = true;
    }

    [Benchmark]
    public void Cold_Compilation() => RunBenchmark((p) => p.GeneratorDriver);
}
