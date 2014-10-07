using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal interface ITaskSet
    {
        Task AddTask(Action<ITaskSet> action, CancellationToken cancellationToken);
        Task AddTask(Func<ITaskSet, Task> action, CancellationToken cancellationToken);
    }
}
