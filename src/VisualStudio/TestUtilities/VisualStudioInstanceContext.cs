using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.VisualStudio.Test.Utilities
{
    /// <summary>
    /// Represents a wrapper of <see cref="VisualStudioInstance"/> that is given to a specific test. This should
    /// be disposed by the test to ensure the test's actions are cleaned up during the test run so the instance is
    /// usable for the next test.
    /// </summary>
    public sealed class VisualStudioInstanceContext : IDisposable
    {
        private readonly VisualStudioInstance _instance;
        private readonly VisualStudioInstanceFactory _instanceFactory;

        internal VisualStudioInstanceContext(VisualStudioInstance instance, VisualStudioInstanceFactory instanceFactory)
        {
            _instance = instance;
            _instanceFactory = instanceFactory;
        }

        public VisualStudioInstance Instance => _instance;

        public void Dispose()
        {
            try
            {
                _instance.CloseAndDeleteOpenSolution();
                _instanceFactory.NotifyCurrentInstanceContextDisposed(canReuse: true);
            }
            catch (Exception)
            {
                // If the cleanup process fails, we want to make sure the next test gets a new instance. However,
                // we still want to raise this exception to fail this test
                _instanceFactory.NotifyCurrentInstanceContextDisposed(canReuse: false);
                throw;
            }
        }
    }
}
