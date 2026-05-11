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
        const int SourceTextPooledBufferSize = 32 * 1024;

        // 1024: PooledStringBuilder recycles (capacity <= 1024)
        // 1025: PooledStringBuilder discarded (capacity > 1024), optimization avoids this allocation
        // 32K-1: largest size that fits the fast path (single CopyTo, no StringBuilder)
        // 32K: falls back to StringBuilder loop
        [Params(512, 1024, 1025, 2 * 1024, 16 * 1024, SourceTextPooledBufferSize, SourceTextPooledBufferSize + 1, 64 * 1024)]
        public int Length { get; set; }

        private SourceText? _sourceText;

        [IterationSetup]
        public void Setup()
        {
            // WithChanges produces a ChangedText wrapping a CompositeText. CompositeText doesn't
            // override ToString, so it exercises the base SourceText.ToString path.
            var change = new TextChange(new TextSpan(0, 0), "a");
            _sourceText = SourceText.From(new string('a', Length - 1)).WithChanges(new[] { change });
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void SourceText_ToString()
        {
            for (var i = 0; i < OperationsPerInvoke; i++)
                _ = _sourceText!.ToString();
        }
    }
}
