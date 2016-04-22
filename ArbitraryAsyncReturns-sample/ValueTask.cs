using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.Threading.Tasks
{
    [Tasklike(typeof(ValueTaskMethodBuilder<>))]
    public struct ValueTask<TResult> : IEquatable<ValueTask<TResult>>
    {
        // A ValueTask holds *either* a value _result, *or* a task _task. Not both.
        // The idea is that if it's constructed just with the value, it avoids the heap allocation of a Task.
        internal readonly Task<TResult> _task;
        internal readonly TResult _result;
        public ValueTask(TResult result) { _result = result; _task = null; }
        public ValueTask(Task<TResult> task) { _task = task; _result = default(TResult);  if (_task == null) throw new ArgumentNullException(nameof(task)); }
        public static implicit operator ValueTask<TResult>(Task<TResult> task) => new ValueTask<TResult>(task);
        public static implicit operator ValueTask<TResult>(TResult result) => new ValueTask<TResult>(result);
        public override int GetHashCode() => _task != null ? _task.GetHashCode() : _result != null ? _result.GetHashCode() : 0;
        public override bool Equals(object obj) => obj is ValueTask<TResult> && Equals((ValueTask<TResult>)obj);
        public bool Equals(ValueTask<TResult> other) => _task != null || other._task != null ? _task == other._task : EqualityComparer<TResult>.Default.Equals(_result, other._result);
        public static bool operator ==(ValueTask<TResult> left, ValueTask<TResult> right) => left.Equals(right);
        public static bool operator !=(ValueTask<TResult> left, ValueTask<TResult> right) => !left.Equals(right);
        public Task<TResult> AsTask() => _task ?? Task.FromResult(_result);
        public bool IsCompleted => _task == null || _task.IsCompleted;
        public bool IsCompletedSuccessfully => _task == null || _task.Status == TaskStatus.RanToCompletion;
        public bool IsFaulted => _task != null && _task.IsFaulted;
        public bool IsCanceled => _task != null && _task.IsCanceled;
        public TResult Result => _task == null ? _result : _task.GetAwaiter().GetResult();
        public ValueTaskAwaiter<TResult> GetAwaiter() => new ValueTaskAwaiter<TResult>(this);
        public ConfiguredValueTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext) => new ConfiguredValueTaskAwaitable<TResult>(this, continueOnCapturedContext: continueOnCapturedContext);
        public override string ToString() => _task == null ? _result.ToString() : _task.Status == TaskStatus.RanToCompletion ? _task.Result.ToString() : _task.Status.ToString();
    }
}



namespace System.Runtime.CompilerServices
{
    struct ValueTaskMethodBuilder<TResult>
    {
        // This builder contains *either* an AsyncTaskMethodBuilder, *or* a result.
        // At the moment someone retrieves its Task, that's when we collapse to the real AsyncTaskMethodBuilder
        // and it's task, or just use the result.
        internal AsyncTaskMethodBuilder<TResult> _taskBuilder; internal bool GotBuilder;
        internal TResult _result; internal bool GotResult;

        public static ValueTaskMethodBuilder<TResult> Create() => new ValueTaskMethodBuilder<TResult>();
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            EnsureTaskBuilder();
            _taskBuilder.SetStateMachine(stateMachine); // must be included if my builder is a struct; must be omitted if my builder is a class
        }
        public void SetResult(TResult result)
        {
            if (GotBuilder) _taskBuilder.SetResult(result);
            else _result = result;
            GotResult = true;
        }
        public void SetException(Exception exception)
        {
            EnsureTaskBuilder();
            _taskBuilder.SetException(exception);
        }
        private void EnsureTaskBuilder()
        {
            if (!GotBuilder && GotResult) throw new InvalidOperationException();
            if (!GotBuilder) _taskBuilder = AsyncTaskMethodBuilder<TResult>.Create();
            GotBuilder = true;
        }
        public ValueTask<TResult> Task
        {
            get
            {
                if (GotResult && !GotBuilder) return new ValueTask<TResult>(_result);
                EnsureTaskBuilder();
                return new ValueTask<TResult>(_taskBuilder.Task);
            }
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            EnsureTaskBuilder();
            _taskBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            EnsureTaskBuilder();
            _taskBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }
    }

    public struct ValueTaskAwaiter<TResult> : ICriticalNotifyCompletion
    {
        private readonly ValueTask<TResult> _value;
        internal ValueTaskAwaiter(ValueTask<TResult> value) { _value = value; }
        public bool IsCompleted => _value.IsCompleted;
        public TResult GetResult() => (_value._task == null) ? _value._result : _value._task.GetAwaiter().GetResult(); 
        public void OnCompleted(Action continuation) => _value.AsTask().ConfigureAwait(continueOnCapturedContext: true).GetAwaiter().OnCompleted(continuation);
        public void UnsafeOnCompleted(Action continuation) => _value.AsTask().ConfigureAwait(continueOnCapturedContext: true).GetAwaiter().UnsafeOnCompleted(continuation);
    }

    public struct ConfiguredValueTaskAwaitable<TResult>
    {
        private readonly ValueTask<TResult> _value;
        private readonly bool _continueOnCapturedContext;
        internal ConfiguredValueTaskAwaitable(ValueTask<TResult> value, bool continueOnCapturedContext) { _value = value; _continueOnCapturedContext = continueOnCapturedContext; }
        public ConfiguredValueTaskAwaiter GetAwaiter() => new ConfiguredValueTaskAwaiter(_value, _continueOnCapturedContext);
        public struct ConfiguredValueTaskAwaiter : ICriticalNotifyCompletion
        {
            private readonly ValueTask<TResult> _value;
            private readonly bool _continueOnCapturedContext;
            internal ConfiguredValueTaskAwaiter(ValueTask<TResult> value, bool continueOnCapturedContext) { _value = value; _continueOnCapturedContext = continueOnCapturedContext; }
            public bool IsCompleted => _value.IsCompleted;
            public TResult GetResult() => _value._task == null ? _value._result : _value._task.GetAwaiter().GetResult();
            public void OnCompleted(Action continuation) => _value.AsTask().ConfigureAwait(_continueOnCapturedContext).GetAwaiter().OnCompleted(continuation);
            public void UnsafeOnCompleted(Action continuation) => _value.AsTask().ConfigureAwait(_continueOnCapturedContext).GetAwaiter().UnsafeOnCompleted(continuation);
        }
    }
}
