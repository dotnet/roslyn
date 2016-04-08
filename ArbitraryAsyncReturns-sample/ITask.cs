using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    [Tasklike(typeof(ITaskMethodBuilder<>))]
    interface ITask<out T>
    {
        ITaskAwaiter<T> GetAwaiter();
    }
}

namespace System.Runtime.CompilerServices
{

    interface ITaskAwaiter<out T> : INotifyCompletion, ICriticalNotifyCompletion
    {
        bool IsCompleted { get; }
        T GetResult();
    }

    struct ITaskMethodBuilder<T>
    {
        private class ConcreteITask<U> : ITask<U>
        {
            private readonly Task<U> _task;
            public ConcreteITask(Task<U> task) { _task = task; }
            public ITaskAwaiter<U> GetAwaiter() => new ConcreteITaskAwaiter<U>(_task.GetAwaiter());
        }

        private class ConcreteITaskAwaiter<U> : ITaskAwaiter<U>
        {
            private readonly TaskAwaiter<U> _awaiter;
            public ConcreteITaskAwaiter(TaskAwaiter<U> awaiter) { _awaiter = awaiter; }
            public bool IsCompleted => _awaiter.IsCompleted;
            public U GetResult() => _awaiter.GetResult();
            public void OnCompleted(Action continuation) => _awaiter.OnCompleted(continuation);
            public void UnsafeOnCompleted(Action continuation) => _awaiter.UnsafeOnCompleted(continuation);
        }

        private AsyncTaskMethodBuilder<T> _taskBuilder;
        private ConcreteITask<T> _task;

        public static ITaskMethodBuilder<T> Create() => new ITaskMethodBuilder<T>() { _taskBuilder = AsyncTaskMethodBuilder<T>.Create() };
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine => _taskBuilder.Start(ref stateMachine);
        public void SetStateMachine(IAsyncStateMachine stateMachine) => _taskBuilder.SetStateMachine(stateMachine);
        public void SetResult(T result) => _taskBuilder.SetResult(result);
        public void SetException(Exception exception) => _taskBuilder.SetException(exception);
        public ITask<T> Task => (_task == null) ? _task = new ConcreteITask<T>(_taskBuilder.Task) : _task;
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine => _taskBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine => _taskBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
    }
}