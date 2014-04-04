using System;
using System.Globalization;
using System.Threading;

namespace Roslyn.Test.Utilities
{
    public class CultureContext : IDisposable
    {
        private readonly CultureInfo threadCulture = CultureInfo.InvariantCulture;
        public CultureContext(string testCulture)
        {
            threadCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(testCulture);
        }

        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = threadCulture;
        }
    }
}
