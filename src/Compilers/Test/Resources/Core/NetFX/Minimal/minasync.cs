using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace System
{
    public delegate void Action();
}

namespace System.Threading.Tasks
{
    public class Task : IAsyncResult, IDisposable
    {
        public Awaiter GetAwaiter() => null;
    }

    public class Task<T> : IAsyncResult, IDisposable
    {
        public Awaiter<T> GetAwaiter() => null;
    }

    public class Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action continuation) { }
        public bool IsCompleted => false;
        public void GetResult() { }
    }

    public class Awaiter<T> : INotifyCompletion
    {
        public void OnCompleted(Action continuation) { }
        public bool IsCompleted => false;
        public T GetResult() => default(T);
    }
}

namespace System.Runtime.CompilerServices
{
    public interface INotifyCompletion
    {
        void OnCompleted(Action continuation);
    }

    public interface ICriticalNotifyCompletion : INotifyCompletion
    {
        void UnsafeOnCompleted(Action continuation);
    }

    public interface IAsyncStateMachine
    {
        void MoveNext();
        void SetStateMachine(IAsyncStateMachine stateMachine);
    }

    public struct AsyncVoidMethodBuilder
    {
        public static AsyncVoidMethodBuilder Create() { throw null; }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
        }

        public void SetResult() { }
        public void SetException(Exception exception) { }
    }

    public struct AsyncTaskMethodBuilder
    {
        public static AsyncTaskMethodBuilder Create() { throw null; }

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
        }

        public Task Task => null;
        public void SetResult() { }
        public void SetException(Exception exception) { }
    }

    public struct AsyncTaskMethodBuilder<TResult>
    {
        public static AsyncTaskMethodBuilder<TResult> Create() => default(AsyncTaskMethodBuilder<TResult>);

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
        }

        public Task<TResult> Task => null;
        public void SetResult(TResult result) { }
        public void SetException(Exception exception) { }
    }
}
