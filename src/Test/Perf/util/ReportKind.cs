using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Test.Performance.Utilities
{
    public enum ReportKind : int
    {
        CompileTime,
        RunTime,
        FileSize,
    }
}
