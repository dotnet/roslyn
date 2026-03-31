// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Text;

namespace IdeCoreBenchmarks
{
    /// <summary>
    /// Benchmark to measure the performance difference of accessing End vs EndIncludingLineBreak
    /// properties after refactoring TextLine to store _endAndLineBreakLength instead of _endIncludingBreaks.
    /// </summary>
    [MemoryDiagnoser]
    public class TextLinePropertyAccessBenchmark
    {
        private SourceText _smallText;
        private SourceText _largeText;
        private TextLine[] _smallTextLines;
        private TextLine[] _largeTextLines;

        [GlobalSetup]
        public void GlobalSetup()
        {
            // Small text: 100 lines
            var smallLines = new string[100];
            for (int i = 0; i < 100; i++)
            {
                smallLines[i] = $"Line {i} with some content to make it realistic";
            }
            _smallText = SourceText.From(string.Join("\n", smallLines));
            _smallTextLines = new TextLine[_smallText.Lines.Count];
            for (int i = 0; i < _smallText.Lines.Count; i++)
            {
                _smallTextLines[i] = _smallText.Lines[i];
            }

            // Large text: Load a real file or create synthetic large text
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            if (!string.IsNullOrEmpty(roslynRoot))
            {
                var csFilePath = Path.Combine(roslynRoot, @"src\Compilers\CSharp\Portable\Symbols\Symbol.cs");
                if (File.Exists(csFilePath))
                {
                    _largeText = SourceText.From(File.ReadAllText(csFilePath));
                }
            }

            if (_largeText == null)
            {
                // Fallback: Create synthetic large text (10000 lines)
                var largeLines = new string[10000];
                for (int i = 0; i < 10000; i++)
                {
                    largeLines[i] = $"Line {i} with some content to make it realistic and longer than usual";
                }
                _largeText = SourceText.From(string.Join("\n", largeLines));
            }

            _largeTextLines = new TextLine[_largeText.Lines.Count];
            for (int i = 0; i < _largeText.Lines.Count; i++)
            {
                _largeTextLines[i] = _largeText.Lines[i];
            }
        }

        // Benchmarks for End property

        [Benchmark]
        public int AccessEnd_SmallText()
        {
            int sum = 0;
            foreach (var line in _smallTextLines)
            {
                sum += line.End;
            }
            return sum;
        }

        [Benchmark]
        public int AccessEnd_LargeText()
        {
            int sum = 0;
            foreach (var line in _largeTextLines)
            {
                sum += line.End;
            }
            return sum;
        }

        // Benchmarks for EndIncludingLineBreak property

        [Benchmark]
        public int AccessEndIncludingLineBreak_SmallText()
        {
            int sum = 0;
            foreach (var line in _smallTextLines)
            {
                sum += line.EndIncludingLineBreak;
            }
            return sum;
        }

        [Benchmark]
        public int AccessEndIncludingLineBreak_LargeText()
        {
            int sum = 0;
            foreach (var line in _largeTextLines)
            {
                sum += line.EndIncludingLineBreak;
            }
            return sum;
        }

        // Benchmarks for mixed access patterns (realistic usage)

        [Benchmark]
        public int MixedAccess_MostlyEnd_SmallText()
        {
            int sum = 0;
            for (int i = 0; i < _smallTextLines.Length; i++)
            {
                var line = _smallTextLines[i];
                // 90% End access, 10% EndIncludingLineBreak
                if (i % 10 == 0)
                    sum += line.EndIncludingLineBreak;
                else
                    sum += line.End;
            }
            return sum;
        }

        [Benchmark]
        public int MixedAccess_MostlyEnd_LargeText()
        {
            int sum = 0;
            for (int i = 0; i < _largeTextLines.Length; i++)
            {
                var line = _largeTextLines[i];
                // 90% End access, 10% EndIncludingLineBreak
                if (i % 10 == 0)
                    sum += line.EndIncludingLineBreak;
                else
                    sum += line.End;
            }
            return sum;
        }

        // Benchmarks for Span property (uses End internally)

        [Benchmark]
        public int AccessSpan_SmallText()
        {
            int sum = 0;
            foreach (var line in _smallTextLines)
            {
                sum += line.Span.Length;
            }
            return sum;
        }

        [Benchmark]
        public int AccessSpan_LargeText()
        {
            int sum = 0;
            foreach (var line in _largeTextLines)
            {
                sum += line.Span.Length;
            }
            return sum;
        }

        // Benchmarks for SpanIncludingLineBreak property (uses EndIncludingLineBreak internally)

        [Benchmark]
        public int AccessSpanIncludingLineBreak_SmallText()
        {
            int sum = 0;
            foreach (var line in _smallTextLines)
            {
                sum += line.SpanIncludingLineBreak.Length;
            }
            return sum;
        }

        [Benchmark]
        public int AccessSpanIncludingLineBreak_LargeText()
        {
            int sum = 0;
            foreach (var line in _largeTextLines)
            {
                sum += line.SpanIncludingLineBreak.Length;
            }
            return sum;
        }

        // Benchmark for the realistic 2.15:1 usage ratio we found in the codebase

        [Benchmark]
        public int RealisticUsagePattern_SmallText()
        {
            int sum = 0;
            // Access End 144 times and EndIncludingLineBreak 67 times (approximately 2.15:1 ratio)
            for (int iteration = 0; iteration < 211; iteration++)
            {
                foreach (var line in _smallTextLines)
                {
                    if (iteration < 144)
                        sum += line.End;
                    else
                        sum += line.EndIncludingLineBreak;
                }
            }
            return sum;
        }

        [Benchmark]
        public int RealisticUsagePattern_LargeText()
        {
            int sum = 0;
            // Simulate realistic access pattern with proper ratio
            for (int i = 0; i < _largeTextLines.Length; i++)
            {
                var line = _largeTextLines[i];
                // Out of every 211 accesses: 144 End, 67 EndIncludingLineBreak
                int accessType = i % 211;
                if (accessType < 144)
                    sum += line.End;
                else
                    sum += line.EndIncludingLineBreak;
            }
            return sum;
        }
    }
}
