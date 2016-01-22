using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Completion
{
    public class CompletionLog
    {
        private static readonly Queue<string> _log = new Queue<string>();
        private static readonly int _maxLength = 10;

        public static void Log(string entry)
        {
            if (_log.Count == _maxLength)
            {
                _log.Dequeue();
            }

            _log.Enqueue(entry);
        }

        public static string GetLog()
        {
            return string.Join("\r\n", _log);
        }

    }
}
