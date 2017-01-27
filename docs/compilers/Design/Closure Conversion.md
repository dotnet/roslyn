Closure Conversion in C#
========================

This document describes the how the C# compiler turns closures (anonymous and local functions) into top-level functions (methods).

If you aren't familiar with closure conversion, the introduction contains a walkthrough describing the transformations. Otherwise, you can skip to the Internals section to see how the actual transformations are done in Roslyn.

# Introduction

In the simplest case, this is trivial -- all closures are simply given new, unmentionable names and are lifted to the top level as static methods. The complexity comes when a closure captures a variable from its surrounding scope. At that point, we must not only move the closure to a method, we also have to create an "environment" to hold its captured variables and somehow deliver that environment into the context of the rewritten method.

There are two possible strategies that are both employed in different situations in the compiler. The first strategy is to pass the environment as an extra parameter to all method calls to the closure. For example,

```csharp
void M()
{
    int x = 0;
    int Local() => x + 1;
}
```

becomes

```csharp
void M()
{
    var env = new Environment();
    env.x = 0;
    <>__Local(env);
}

static int <>__Local(Environment env)
{
    return env.x + 1;
}

struct Environment
{
    int x;
}
```

Instead of referring to local variables, the rewritten closure now references fields on the environment. A `struct` environment is used to prevent extra allocations, since `struct`s are normally allocated on the stack. However, the astute reader may notice a problem with this conversion -- `struct`s are always copied when passed as arguments to a function call! This means that writes to the environment field will not always propogate back to the local variable in the method `M`. To work around this flaw, all Environment variables are passed as `ref` arguments to the rewritten closures.

The second strategy for rewriting closures is necessary when the closure interacts with external code. Consider the following program:

```csharp
void M(IEnumerable<int> e)
{
    int x = 0;
    var positive = e.Where(n => n > 0);
    ...
}
```

In this case we're passing a closure to the `IEnumerable<int>.Where` function, which is expecting a delegate of type `Func<int, bool>`. Note that that delegate type is immutable -- it is defined in external code and cannot be changed. Therefore, rewriting external callsites to take an extra `Environment` argument is impossible. We have to choose a different strategy for acquiring the environment.

What we do is use a `class` Environment for cases like this. With a `class` Environment, the previous environment can be rewritten as follows:

```csharp
void M(IEnumerable<int> e)
{
    var env = new Environment();
    env.x = 0;
    var positive = e.Where(env.<>__Local);
}

class Environment
{
    int x;

    bool <>__Local(int n) => n > 0;
}
```

Since the local variables are now fields in a class instance we can keep the same delegate signature and rely on field access to read and write the free variables.

This covers the transformations C# performs at a high level. The following section covers how these transformations are performed in detail.

# Internals