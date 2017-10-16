
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace perf
{
    internal static class PerfOutputParser
    {
        // Sample measurements
        // 1.727527,,task-clock,1727527,100.00,0.816,CPUs utilized
        // 1,,context-switches,1727527,100.00,0.579,K/sec
        // 0,,cpu-migrations,1727527,100.00,0.000,K/sec
        // 360,,page-faults,1727527,100.00,0.208,M/sec
        // 2707404,,cycles,752879,43.23,1.567,GHz
        // 1026832,,stalled-cycles-frontend,1059529,100.00,37.93,frontend cycles idle
        // 1082093,,stalled-cycles-backend,1741695,100.00,39.97,backend cycles idle
        // 2967037,,instructions,1741695,100.00,1.10,insn per cycle
        // ,,,,,0.36,stalled cycles per insn
        // 554513,,branches,1741695,100.00,320.987,M/sec
        // 16004,,branch-misses,1670982,95.94,2.89,of all branches
        public struct Measurement
        {
            private readonly string[] _entries;
            public Measurement(string[] entries)
            {
                Debug.Assert(entries.Length == 7);
                _entries = entries;
            }

            public string CounterName => _entries[2];
            public string CounterValue => _entries[0];

            public string this[int i] => _entries[i];

            public override string ToString()
            {
                return string.Join(' ', _entries);
            }
        }

        public static List<Measurement> Parse(string perfOutput)
        {
            perfOutput = perfOutput.Trim();
            var measurements = new List<Measurement>();
            using (var reader = new StringReader(perfOutput))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    measurements.Add(new Measurement(line.Split(',')));
                }
            }
            return measurements;
        }
    }
}