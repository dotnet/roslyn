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
        public PerfTest([CallerFilePath] string workingFile = "") : base(workingFile) { }

        public abstract void Setup();
        public abstract void Test();
        public abstract int Iterations { get; }
        public abstract string Name { get; }
        public abstract string MeasuredProc { get; }

        public abstract bool ProvidesScenarios { get; }
        public abstract string[] GetScenarios();
    }
}
