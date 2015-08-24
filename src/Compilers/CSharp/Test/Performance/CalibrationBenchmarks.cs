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
        }

        [Benchmark]
        [InlineData(10)]
        [InlineData(20)]
        [InlineData(50)]
        [InlineData(100)]
        [InlineData(200)]
        [InlineData(500)]
        [InlineData(1000)]
        [InlineData(2000)]
        public void Sleep(int durationInMilliseconds)
        {
            Thread.Sleep(durationInMilliseconds);
        }
    }

    public class Facts
    {
        [Fact]
        public void EmptyFact()
        {

        }
    }
}
