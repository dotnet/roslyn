using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System
{
    [Tasklike(typeof(Observable1Builder<>))]
    interface IObservable1<T> : IObservable<T> { }

    [Tasklike(typeof(ObservableNBuilder<>))]
    interface IObservableN<T> : IObservable<T> { }
}



namespace System.Runtime.CompilerServices
{
    class ConcreteObservable1<T> : IObservable1<T>
    {
        public Task<T> _task;
        public IDisposable Subscribe(IObserver<T> observer) => _task.ToObservable().Subscribe(observer);
    }

    class Observable1Builder<T>
    {
        private AsyncTaskMethodBuilder<T> _builder;
        private ConcreteObservable1<T> _task;

        public static Observable1Builder<T> Create() => new Observable1Builder<T>() { _builder = AsyncTaskMethodBuilder<T>.Create() };
        public void Start<TSM>(ref TSM sm) where TSM : IAsyncStateMachine { _builder.Start(ref sm); }
        public IObservable1<T> Task => (_task = _task ?? new ConcreteObservable1<T> { _task = _builder.Task });

        public void SetStateMachine(IAsyncStateMachine sm) { }
        public void SetResult(T result) => _builder.SetResult(result);
        public void SetException(Exception ex) => _builder.SetException(ex);
        public void AwaitOnCompleted<TA, TSM>(ref TA a, ref TSM sm) where TA : INotifyCompletion where TSM : IAsyncStateMachine => _builder.AwaitOnCompleted(ref a, ref sm);
        public void AwaitUnsafeOnCompleted<TA, TSM>(ref TA a, ref TSM sm) where TA : ICriticalNotifyCompletion where TSM : IAsyncStateMachine => _builder.AwaitUnsafeOnCompleted(ref a, ref sm);
    }

    class ConcreteObservableN<T> : IObservableN<T>
    {
        public IAsyncStateMachine sm_original;

        public IDisposable Subscribe(IObserver<T> observer)
        {
            // Create a copy of the state-machine struct
            var sm = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(sm_original, new object[] { }) as IAsyncStateMachine;

            // create a new builder
            var builder = new ObservableNBuilder<T> { _observer = observer };
            (sm.GetType().GetField("<>t__builder") ?? sm.GetType().GetField("$Builder")).SetValue(sm, builder);

            // kick off this instance of the async method
            sm.MoveNext();

            return new Subscriber<T>() { _builder = builder };
        }
    }

    class Subscriber<T> : IDisposable
    {
        public ObservableNBuilder<T> _builder;
        public void Dispose() { _builder._observer = null; }
    }


    class ObservableNBuilder<T>
    {
        // Like FactoryTask, the first few methods are about saving a copy of the state machine
        public ConcreteObservableN<T> _task;

        public static ObservableNBuilder<T> Create() => new ObservableNBuilder<T>();
        public void Start<TSM>(ref TSM sm) where TSM : IAsyncStateMachine { _task = new ConcreteObservableN<T> { sm_original = sm }; }
        public IObservableN<T> Task => _task;


        // Later on, each time the user calls .Subscribe(), that will create a fresh copy of the state machine struct
        // with a fresh copy of this builder.
        public IObserver<T> _observer;

        public void SetStateMachine(IAsyncStateMachine sm) { }
        public void SetResult(T result) => _observer?.OnCompleted();
        public void SetException(Exception ex) => _observer?.OnError(ex);
        public void AwaitOnCompleted<TA, TSM>(ref TA a, ref TSM sm) where TA : INotifyCompletion where TSM : IAsyncStateMachine { throw new NotImplementedException(); }
        public void AwaitUnsafeOnCompleted<TA, TSM>(ref TA a, ref TSM sm) where TA : ICriticalNotifyCompletion where TSM : IAsyncStateMachine
        {
            if (a is AsyncObservableYieldAwaitable)
            {
                var a1 = (AsyncObservableYieldAwaitable)(object)a;
                if (_observer == null) a1._issubscribed = false;
                else _observer.OnNext((T)a1._value);
                sm.MoveNext();
                return;
            }
            else
            {
                a.OnCompleted(sm.MoveNext);
            }
        }
    }

    public static class AsyncObservable
    {
        public static AsyncObservableYieldAwaitable Yield<T>(T value) => new AsyncObservableYieldAwaitable { _value = value };
    }

    public class AsyncObservableYieldAwaitable : ICriticalNotifyCompletion
    {
        public object _value;
        public bool _issubscribed = true;
        public AsyncObservableYieldAwaitable GetAwaiter() => this;
        public bool IsCompleted => false;
        public void GetResult() { if (!_issubscribed) throw new OperationCanceledException(); }
        public void OnCompleted(Action continuation) { }
        public void UnsafeOnCompleted(Action continuation) { }
    }
}

