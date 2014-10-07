using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Roslyn.Services.Shared.Utilities
{
    internal class ThreadAffinitizedObject
    {
        private readonly int foregroundThreadId;
        protected readonly TaskScheduler ForegroundTaskScheduler;

        public ThreadAffinitizedObject()
        {
            this.foregroundThreadId = Thread.CurrentThread.ManagedThreadId;
            this.ForegroundTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }

        public bool IsForeground()
        {
            return Thread.CurrentThread.ManagedThreadId == foregroundThreadId;
        }

        public void AssertIsForeground()
        {
            Contract.ThrowIfFalse(Thread.CurrentThread.ManagedThreadId == foregroundThreadId);
        }

        public void AssertIsBackground()
        {
            Contract.ThrowIfTrue(Thread.CurrentThread.ManagedThreadId == foregroundThreadId);
        }
    }
}