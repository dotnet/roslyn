using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Test.Performance.Utilities
{
    public class TrivialLogger : ILogger
    {
        public static ILogger Instance = new TrivialLogger();

        public void Flush()
        {
            throw new NotImplementedException();
        }

        public void Log(string v)
        {
        }
    }
}
