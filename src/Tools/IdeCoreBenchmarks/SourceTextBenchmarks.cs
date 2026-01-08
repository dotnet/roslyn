// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Text;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class SourceTextBenchmarks
    {
        [Params(10, 100, 1_000, 10_000, 100_000, 1_000_000)]
        public int Length { get; set; }

        private SourceText? _sourceText;

        [IterationSetup]
        public void Setup()
        {
            _sourceText = SourceText.From(new string('a', Length));
        }

        [Benchmark]
        public void SourceText_ToString()
        {
            for (var i = 0; i < 100; i++)
                _ = _sourceText!.ToString();
        }

        [Benchmark]
        public void SourceText_ToStringOld()
        {
            for (var i = 0; i < 100; i++)
                _ = _sourceText!.ToStringOld();
        }
    }
}
