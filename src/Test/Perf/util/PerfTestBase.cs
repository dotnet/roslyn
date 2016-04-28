using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Test.Performance.Utilities
{
    public abstract class PerfTest : RelativeDirectory
    {
        private List<Tuple<int, string, object>> _metrics = new List<Tuple<int, string, object>>();
        protected ILogger _logger;

        public PerfTest(ILogger logger, [CallerFilePath] string workingFile = "") : base(workingFile) { _logger = logger; }

        /// Reports a metric to be recorded in the performance monitor.
        protected void Report(ReportKind reportKind, string description, object value)
        {
            _metrics.Add(Tuple.Create((int)reportKind, description, value));
            _logger.Log(description + ": " + value.ToString());
        }

        public abstract void Setup();
        public abstract void Test();
        public abstract int Iterations { get; }
        public abstract string Name { get; }
        public abstract string MeasuredProc { get; }

        public abstract bool ProvidesScenarios { get; }
        public abstract string[] GetScenarios();
    }
}
