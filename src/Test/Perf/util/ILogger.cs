using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Test.Performance.Utilities
{
    public interface ILogger
    {
        void Log(string v);
        void Flush();
    }
}
