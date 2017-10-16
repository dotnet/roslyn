
using System;
using System.Collections.Generic;
using System.Linq;
using static perf.PerfOutputParser;

namespace perf
{
    internal sealed class StatsPopulation
    {
        private readonly decimal[] _samples;

        public StatsPopulation(List<Measurement> samples, int entryIndex)
        {
            _samples = new decimal[samples.Count];
            for (int i = 0; i < samples.Count; i++)
            {
                _samples[i] = decimal.Parse(samples[i][entryIndex]);
            }
        }

        public decimal Mean => _samples.Sum() / _samples.Length;
    }
}