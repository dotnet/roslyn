using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Threading.Tasks
{
    [Tasklike(typeof(FactoryTaskMethodBuilder))]
    public class FactoryTask
    {
        public IAsyncStateMachine sm_original;
        public Task SpawnInstance()
        {
            // Create a copy of the state-machine struct
            var sm = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(sm_original, new object[] { }) as IAsyncStateMachine;

            // create a new builder
            var builder = new FactoryTaskMethodBuilder { _builder = AsyncTaskMethodBuilder.Create() };
            (sm.GetType().GetField("<>t__builder") ?? sm.GetType().GetField("$Builder")).SetValue(sm, builder);
            builder._builder.SetStateMachine(sm);

            // kick off this instance of the async method
            sm.MoveNext();

            // return the task for this instance
            return builder._builder.Task;
        }
    }
}

namespace System.Runtime.CompilerServices
{
    class FactoryTaskMethodBuilder
    {
        // When the user calls the FactoryTask-returning async method stub, it merely creates the state machine struct,
        // calls Create() to create an instance of the builder as a field of the struct, calls Start() on it which
        // does nothing, and returns Task. All it does is stash away a (necessarily boxed) reference to the state machine struct.
        public FactoryTask _task;

        public static FactoryTaskMethodBuilder Create() => new FactoryTaskMethodBuilder();
        public void Start<TSM>(ref TSM sm) where TSM : IAsyncStateMachine { _task = new FactoryTask { sm_original = sm }; }
        public FactoryTask Task => _task;

        // Later on, each time the user calls SpawnInstance, that creates a fresh copy of the state machine struct,
        // with a fresh copy of the builder. (The same builder type has to do double-duty because of how the compiler uses types).
        // This fresh copy of the builder will deal with SetStateMachine/AwaitOnCompleted/SetResult calls from the MoveNext method.
        public AsyncTaskMethodBuilder _builder;

        public void SetStateMachine(IAsyncStateMachine sm) { _builder.SetStateMachine(sm); }
        public void SetResult() { _builder.SetResult(); }
        public void SetException(Exception ex) { _builder.SetException(ex); }
        public void AwaitOnCompleted<TA, TSM>(ref TA a, ref TSM sm) where TA : INotifyCompletion where TSM : IAsyncStateMachine => _builder.AwaitOnCompleted(ref a, ref sm);
        public void AwaitUnsafeOnCompleted<TA, TSM>(ref TA a, ref TSM sm) where TA : ICriticalNotifyCompletion where TSM : IAsyncStateMachine => _builder.AwaitUnsafeOnCompleted(ref a, ref sm);
    }
}
