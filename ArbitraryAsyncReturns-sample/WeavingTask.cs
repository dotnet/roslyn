using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    [Tasklike(typeof(WeavingTaskMethodBuilder))]
    class WeavingTask
    {
        private Task _task;
        public WeavingTask(Task task) { _task = task; }
        public TaskAwaiter GetAwaiter() => _task.GetAwaiter();
    }
}

namespace System.Runtime.CompilerServices
{

    class WeavingConfiguration : ICriticalNotifyCompletion
    {
        public readonly Action _beforeYield, _afterYield;
        public WeavingConfiguration(Action beforeYield, Action afterYield) { _beforeYield = beforeYield; _afterYield = afterYield; }
        public WeavingConfiguration GetAwaiter() => this;
        public bool IsCompleted => false;
        public void UnsafeOnCompleted(Action continuation) { }
        public void OnCompleted(Action continuation) { }
        public void GetResult() { }
    }

    struct WeavingTaskMethodBuilder
    {
        private AsyncTaskMethodBuilder _taskBuilder;
        private WeavingTask _task;
        private WeavingConfiguration _config;

        public static WeavingTaskMethodBuilder Create() => new WeavingTaskMethodBuilder() { _taskBuilder = AsyncTaskMethodBuilder.Create() };
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine => _taskBuilder.Start(ref stateMachine);
        public void SetStateMachine(IAsyncStateMachine stateMachine) => _taskBuilder.SetStateMachine(stateMachine);
        public void SetResult() => _taskBuilder.SetResult();
        public void SetException(Exception exception) => _taskBuilder.SetException(exception);
        public WeavingTask Task => (_task == null) ? _task = new WeavingTask(_taskBuilder.Task) : _task;
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine => _taskBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            if (awaiter is WeavingConfiguration)
            {
                _config = (WeavingConfiguration)(object)awaiter;
                stateMachine.MoveNext();
                return;
            }
            var myAwaiter = new MyAwaiter(awaiter, _config);
            _taskBuilder.AwaitUnsafeOnCompleted(ref myAwaiter, ref stateMachine);
        }

        class MyAwaiter : ICriticalNotifyCompletion
        {
            private readonly ICriticalNotifyCompletion _awaiter;
            private readonly WeavingConfiguration _config;
            public MyAwaiter(ICriticalNotifyCompletion awaiter, WeavingConfiguration config) { _awaiter = awaiter; _config = config; }
            public void OnCompleted(Action continuation) => _awaiter.OnCompleted(continuation);
            public void UnsafeOnCompleted(Action continuation)
            {
                _config?._beforeYield?.Invoke();
                _awaiter.UnsafeOnCompleted(() =>
                {
                    _config?._afterYield?.Invoke();
                    continuation();
                });
            }
        }
    }
}