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
        const int OperationsPerInvoke = 1000;

        [Params(10, 100, 1_000, 10_000, 100_000, 1_000_000)]
        public int Length { get; set; }

        private SourceText? _sourceText;

        [IterationSetup]
        public void Setup()
        {
            var change = new TextChange(new TextSpan(0, 0), "a");
            _sourceText = SourceText.From(new string('a', Length - 1)).WithChanges([change]);
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void SourceText_ToString()
        {
            var span = new TextSpan(1, Length - 2);
            for (var i = 0; i < OperationsPerInvoke; i++)
                _ = _sourceText!.ToString(span);
        }
    }
}
