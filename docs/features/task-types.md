Async Task Types in C#
======================
Extend `async` to support _task types_ that match a specific pattern, in addition to the well known types
`System.Threading.Tasks.Task` and `System.Threading.Tasks.Task<T>`.

## Task Type
A _task type_ is a `class` or `struct` with an associated _builder type_ identified
with `System.Runtime.CompilerServices.AsyncMethodBuilderAttribute`.
The _task type_ may be non-generic, for async methods that do not return a value, or generic, for methods that return a value.

To support `await`, the _task type_ must have a corresponding, accessible `GetAwaiter()` method
that returns an instance of an _awaiter type_ (see _C# 7.7.7.1 Awaitable expressions_).
```cs
[AsyncMethodBuilder(typeof(MyTaskMethodBuilder<>))]
class MyTask<T>
{
    public Awaiter<T> GetAwaiter();
}

class Awaiter<T> : INotifyCompletion
{
    public bool IsCompleted { get; }
    public T GetResult();
    public void OnCompleted(Action completion);
}
```
## Builder Type
The _builder type_ is a `class` or `struct` that corresponds to the specific _task type_.
The _builder type_ can have at most 1 type parameter and must not be nested in a generic type.
The _builder type_ has the following `public` methods.
For non-generic _builder types_, `SetResult()` has no parameters.
```cs
class MyTaskMethodBuilder<T>
{
    public static MyTaskMethodBuilder<T> Create();

    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine;

    public void SetStateMachine(IAsyncStateMachine stateMachine);
    public void SetException(Exception exception);
    public void SetResult(T result);

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine;
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine;

    public MyTask<T> Task { get; }
}
```
## Execution
The types above are used by the compiler to generate the code for the state machine of an `async` method.
(The generated code is equivalent to the code generated for async methods that return `Task`, `Task<T>`, or `void`.
The difference is, for those well known types, the _builder types_ are also known to the compiler.)

`Builder.Create()` is invoked to create an instance of the _builder type_.

If the state machine is implemented as a `struct`, then `builder.SetStateMachine(stateMachine)` is called
with a boxed instance of the state machine that the builder can cache if necessary.

`builder.Start(ref stateMachine)` is invoked to associate the builder with compiler-generated state machine instance.
The builder must call `stateMachine.MoveNext()` either in `Start()` or after `Start()` has returned to advance the state machine.
After `Start()` returns, the `async` method calls `builder.Task` for the task to return from the async method.

Each call to `stateMachine.MoveNext()` will advance the state machine.
If the state machine completes successfully, `builder.SetResult()` is called, with  the method return value if any.
If an exception is thrown in the state machine, `builder.SetException(exception)` is called.

If the state machine reaches an `await expr` expression, `expr.GetAwaiter()` is invoked.
If the awaiter implements `ICriticalNotifyCompletion` and `IsCompleted` is false,
the state machine invokes `builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine)`.
`AwaitUnsafeOnCompleted()` should call `awaiter.OnCompleted(action)` with an action that calls `stateMachine.MoveNext()`
when the awaiter completes. Similarly for `INotifyCompletion` and `builder.AwaitOnCompleted()`.

## Overload Resolution
Overload resolution is extended to recognize _task types_ in addition to `Task` and `Task<T>`.

An `async` lambda with no return value is an exact match for an overload candidate parameter of non-generic _task type_,
and an `async` lambda with return type `T` is an exact match for an overload candidate parameter of generic _task type_. 

Otherwise if an `async` lambda is not an exact match for either of two candidate parameters of _task types_, or an exact match for both, and there
is an implicit conversion from one candidate type to the other, the from candidate wins. Otherwise recursively evaluate
the types `A` and `B` within `Task1<A>` and `Task2<B>` for better match.

Otherwise if an `async` lambda is not an exact match for either of two candidate parameters of _task types_,
but one candidate is a more specialized type than the other, the more specialized candidate wins.
