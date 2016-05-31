# C# Design Notes for Sep 8, 2015

Discussion for these notes can be found at https://github.com/dotnet/roslyn/issues/5383.

## Agenda

1. Check-in on some of our desirable features to see if we have further thoughts
2. Start brainstorming on async streams


# Check-in on features 

## Bestest betterness
 
Overload resolution will sometimes pick a candidate method only to lead to an error later on. For instance, it may pick an instance method where only a static one is allowed, a generic method where the constraints aren't satisfied or an overload the relies on a method group conversion that will ultimately fail.

The reason for this behavior is usually to keep the rules simple, and to avoid accidentally picking another overload than what you meant. However, it also leads to quite a bit of frustration. It might be time to sharpen the rules.

## Private protected

In C# 6 we considered adding a new accessibility level with the meaning protected *and* internal (`protected internal` means protected *or* internal), but gave up because it's hard to settle on a syntax. We wanted to use `private protected` but got a lot of loud push back on it.

No-one has come up with a better syntax in the meantime though. We are inclined to think that `private protected` just takes a little getting used to. We may want to try again.

## Attributes

There are a number of different features related to attributes. We should return to these in a dedicated meeting looking at the whole set of proposals together:

* Generic attributes: They seem to be allowed by the runtime, but for some reason are disallowed by the language
* Compile-time only attributes: For attributes that are intended to be used only before IL gen, it'd be nice to have them elided and not bloat metadata
* Attributes in more places: There are more places where it makes sense to allow attributes, such as on lambda expressions. If they are compile-time only, they can even be in places where there is no natural code gen.
* Attributes of more types: For attributes that are not going into code gen, we could relax the restrictions on the types of their constructor parameters.

A lot of these would be particularly helpful to Roslyn based tools such as analyzers and metaprogramming.

## Local extern functions

We should allow local functions to be extern. You'd almost always want to wrap an extern method in a safe to call wrapper method. 

## Withers for arbitrary types

If we want to start focusing more on immutable data structures, it would be nice if there was a language supported way of creating new objects from existing ones, changing some subset of properties along the way:

``` c#
var newIfStatement = ifStatement with { Condition = newCondition, Statement = newStatement }; // other properties copied 
```

Currently the Roslyn code base, for example, uses the pattern of "withers", which are methods that return a new object with one property changed. There needs to  be a wither per property, which is quite bloatful, and in Roslyn made feasible by having them automatically generated. Even so, changing multiple properties is suboptimal:

``` c#
var newIfStatement = ifStatement.WithCondition(newCondition).WithStatement(newStatement); 
```

It would be nice if we could come up with an efficient API pattern to support a built-in, efficient `with` expression.

Related to this, it would also be nice to support object initializers on immutable objects. Again, we would need to come up with an API pattern to support it; possibly the same that would support the with expression.

## Params IEnumerable

This is a neat little feature that we ultimately rejected or didn't get to in C# 6. It lets you do away with the situation where you have to write two overloads of a method, one that takes an `IEnumerable<T>` (for generality) and another one that takes `params T[]` and calls the first one with its array.

The main problem raised against params IEnumerable is that it encourages an inefficient pattern for how parameters are captured: An array is allocated even when there are very few arguments (even zero!), and the implementation then accesses the elements through interface dispatches and further allocation (of an IEnumerator).

Probably this won't matter for most people - they can start out this way, and then build a more optimal pattern if they need to. But it might be worthwhile considering a more general language pattern were folks can build a params implementation targeting something other than arrays.


# Async streams

We shied back from a notion of asynchronous sequences when we originally introduced async to C#. Part of that was to see whether there was sufficient demand to introduce framework and language level concepts, and get more experience to base their design on. But also, we had some fear that using async on a per-element level would hide the true "chunky" degree of asynchrony under layers of fine-grained asynchronous abstractions, at great cost to performance.

## IAsyncEnumerable

At this point in time, though, we think that there is definitely demand for common abstractions and language support: foreach'ing, iterators, etc. Furthermore we think that the performance risks - allocation overhead in particular - of fine grained asynchrony can large be met with a combination of the compiler's existing optimizations and a straightforward asynchronous "translation" of `IEnumerable<T>` into `IAsyncEnumerable<T>`:

``` c#
public interface IAsyncEnumerable<T>
{
  public IAsyncEnumerator<T> GetEnumerator();
}

public interface IAsyncEnumerator<T>
{
  public T Current { get; }
  public Task<bool> MoveNextAsync();
}
```

The only meaningful difference is that the `MoveNext` method of the enumerator interface has been made async: it returns `Task<bool>` rather than `bool`, so that you need to await it to find out whether there is a next element (which you can then acquire from the `Current` property) or the sequence is finished.

## Allocations

Let's assume that you are foreach'ing over such an asynchronous sequence, which is buffered behind the scenes, so that 99.9% of the time an element is available locally and synchronously. Whenever a `Task` is awaited that is already completed, the compiler avoids the heavy machinery and just gets the value straight out of the task without pause. If *all* awaited Tasks in a given method call are already completed, then the method will never allocate a state machine, or a delegate to store as a continuation, since those are only constructed the first time they are needed. 

Even when the async method reaches its return statement synchronously, without the awaits having ever paused, it needs to construct a Task to return. So normally this would still require one allocation. However, the helper API that the compiler uses for this will actually cache completed Tasks for certain common values, including `true` and `false`. In summary, a `MoveNextAsync` call on a sequence that is buffered would typically not allocate anything, and the calling method often wouldn't either.

The lesson is that fine-grained asynchrony is bad for performance if it is done in terms of `Task<T>` where completed Tasks are never or rarely cached, e.g. `Task<string>` or `Task<int>`. It should be done in terms of `Task<bool>` or even non-generic `Task`.

We think that there may or may not be scenarios where people want to get explicit about the "chunks" that data is transmitted in. If so, they can express this as `IAsyncEnumerable<Chunk<T>>` or some such thing. But there is no need to complicate asynchronous streaming by forcing people to deal with chunking by default.

## Linq bloat

Another concern is that there are many API's on `IEnumerable<T>` today; not least the Linq query operators `Select`, `Where` and so on. Should all those be duplicated for `IAsyncEnumerable<T>`?

And when you think about it, we are not just talking about one extra set of overloads. Because once you have asynchronously foreach'able streams, you'll quickly want the delegates applied by the query operators to *also* be allowed to be async. So we have potentially four combinations:

``` c#
public static IEnumerable<T> Where<T>(this IEnumerable<T> source, Func<T, bool> predicate);
public static IAsyncEnumerable<T> Where<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate);
public static IAsyncEnumerable<T> Where<T>(this IEnumerable<T> source, Func<T, Task<bool>> predicate);
public static IAsyncEnumerable<T> Where<T>(this IAsyncEnumerable<T> source, Func<T, Task<bool>> predicate);
```

So either we'd need to multiply the surface area of Linq by four, or we'd have to introduce some new implicit conversions to the language, e.g. from `IEnumerable<T>` to `IAsyncEnumerable<T>` and from `Func<S, T>` to `Func<S, Task<T>>`. Something to think about, but we think it is probably worth it to get Linq over asynchronous sequences one way or another.

Along with this, we'd need to consider whether to extend the query syntax in the language to also produce async lambdas when necessary. It may not be worth it - using the query operators may be good enough when you want to pass async lambdas.

## Language support

In the language we would add support for foreach'ing over async sequences to consume them, and for async iterators to produce them. Additionally (we don't discuss that further here) we may want to introduce a notion of `IAsyncDisposable`, for weach we could add an async version of the `using` statement.

One concern about async versions of language features such as foreach (and using) is that they would generate `await` expressions that aren't there in source. Philosophically that may or may not be a problem: do you want to be able to see where all the awaiting happens in your async method? If that's important, we can maybe add the `await` or `async` keyword to these features somewhere:

``` c#
foreach (string s in asyncStream) { ... } // or
foreach async (string s in asyncStream) { ... } // or
foreach (await string s in asyncStream) { ... } // etc.
```

Equally problematic is when doing things such as `ConfigureAwait`, which is important for performance reasons in libraries. If you don't have your hands on the Task, how can you `ConfigureAwait` it? The best answer is to add a `ConfigureAwait` extension method to `IAsyncEnumerable<T>` as well. It returns a wrapper sequence that will return a wrapper enumerator whose `MoveNextAsync` will return the result of calling `ConfigureAwait` on the task that the wrapped enumerator's `MoveNextAsync` method returns:

``` c#
foreach (string s in asyncStream.ConfigureAwait(false)) { ... }
```
 
For this to work, it is important that async foreach is pattern based, just like the synchronous foreach is today, where it will happily call any `GetEnumerator`, `MoveNext` and `Current` members, regardless of whether objects implement the official "interfaces". The reason for this is that the result of `Task.ConfigureAwait` is not a `Task`.

A related issue is cancellation, and whether there should be a way to flow a `CancellationToken` to the `MoveNextAsync` method. It probably has a similar solution to `ConfigureAwait`.

## Channels in Go

The Go language has a notion of channels, which are communication pipes between threads. They can be buffered, and you can put things in and take them out. If you put things in while the channel is full, you wait. If you take things out while it is empty, you wait.

If you imagine a `Channel<T>` abstraction in .NET, it would not have blocking waits on the endpoints; those would instead be asynchronous methods returning Tasks.

Go has an all-powerful language construct called `select` to consume the first available element from any set of channels, and choosing the logic to apply to that element based on which channel it came from. It is guaranteed to consume a value from only one of the channels.

It is worthwhile for us to look at Go channels, learn from them and consider to what extent a) we need a similar abstraction and b) it is connected to the notion of async streams.

Some preliminary thoughts: Channels and select statements are very easy to understand, conceptually. On the other hand they are somewhat low-level, and extremely imperative: there is a strong coupling from the consumer to the producer, and in practice there would typically only *be* one consumer. It seems like a synchronization construct like semaphores or some of the types from the DataFlow library.

The "select" functionality is interesting to ponder more generally. If you think about it from an async streams perspective, maybe the similar thing you would do would be to merge streams. That would need to be coupled with the ability to tie different functionality to elements from different original streams - or with different types. Maybe pattern matching is our friend here?

``` c#
foreach (var e in myInts.Merge(myStrings))
{
  switch (e)
  {
    case int i:
      ...
    case string s:
      ...
  }
}
```

Either way, it isn't as elegant by a long shot. If we find it's important, we'd need to consider language support.

Another important difference between IAsyncEnumerable and Channels is that an enumerable can have more than one consumer. Each enumerator is independent of the others, and provides access to all the members - at least from the point in time where it is requested. 


## Conclusion

We want to keep thinking about async streams, and probably do some prototyping.