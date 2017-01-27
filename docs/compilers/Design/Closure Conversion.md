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

Instead of referring to local variables, the rewritten closure now references fields on the environment. A `struct` environment is used to prevent extra allocations, since `struct`s are normally allocated on the stack. However, the astute reader may notice a problem with this conversion -- `struct`s are always copied when passed as arguments to a function call! This means that writes to the environment field will not always propogate back to the local variable in the method `M`. To work around this flaw, all environment variables are passed as `ref` arguments to the rewritten closures.


# Internals