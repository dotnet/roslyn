C# Design Meeting Notes for May 20
==================================

Discussion for this issue can be found at https://github.com/dotnet/roslyn/issues/3911.

_Quote of the day:_ The slippery slope you are talking about is that if we satisfy our customers they'll want us to satisfy them some more.


Agenda
------

Today we discussed whether and how to add local functions to C#, with the aim of prototyping the feature in the near future. Proposal at issue #259. Some details are discussed in issue #2930.

1. Local functions



Local Functions
===============

We agree that the scenario is useful. You want a helper function. You are only using it from within a single function, and it likely uses variables and type parameters that are in scope in that containing function. On the other hand, unlike a lambda you don't need it as a first class object, so you don't care to give it a delegate type and allocate an actual delegate object. Also you may want it to be recursive or generic, or to implement it as an iterator.


Lambdas or local functions?
---------------------------

There are two ways of approaching it:

* Make lambdas work for this
* Add local function syntax to the language

On the face of it, this seems like it would be better to reuse an existing feature. After all, we are not looking to address a new problem but just make existing code more convenient to write. In VB this scenario works pretty nicely with lambdas already - can't we just take a page out of VB's book?

Well, it turns out you need a plethora of little features to achieve the full effect with lambdas:

* lambdas would need to have an intrinsic (compiler-generated) delegate type that we can infer for them when they are not target typed to a specific delegate type:

``` c#
var f = (int x) => x * x; // infers a compiler generated delegate type for int -> int.
```

* lambdas need to be able to recursively call themselves through a variable name they get assigned to. This introduces a problem with inferring a return type:

``` c#
var f = (int x, int c) => c = 0 ? x : f(x) * x; // can we reasonably infer a return type here?
var f = (int x, int c) => (int)(c = 0 ? x : f(x) * x); // or do we need to cast the result?
```

* lambdas need to be able to be iterators. Since they don't have a specified return type, how can we know if the iterator is supposed to be `IEnumerable<T>`, `IEnumerator<T>`, `IEnumerable` or `IEnumerator`?

``` c#
var f = (int x, int c) => { for (int i = 0; i < c; i++) { yield return x; } }; // default to IEnumerable<T>?
```

* We'd want lambdas to be generic. What's the syntax for a generic lambda - where do the type parameters go? Presumably in front of the parameter list? And wait a minute, we don't even have a notion of delegate types for generic functions!

``` c#
var f = <T>(IEnumerable<T> src) => src.FirstOrDefault(); // Is this unambiguous? What's the delegate type?
```

VB does a subset of these, probably enough to get by, but all in all for C# it seems both the better and easier path to simply let you define a function in method scope.

On top of this is the performance aspect: the lambda approach implies a lot of allocations: one for the delegate and one for the closure object to capture surrounding variables and type parameters. 

Sometimes one or both of these can be optimized away by a clever compiler. But with functions, the delegate is never there (unless you explicitly decide to create one when you need it), and if the function itself is not captured as a delegate, the closure can be a struct on the stack.

**Conclusion**: Local functions are the better choice. Let's try to design them.


Syntax
------

The syntax of a local functions is exactly as with a method, except that it doesn't allow the syntactic elements that are concerned  with it being a member. More specifically: 

* No attributes
* No modifiers except `async` and `unsafe`
* The name is always just an identifier (not `Interface.MemberName`)
* The body is never just `;` (always `=> ...` or `{ ... }`)

We'll consider *local-function-declaration* to be a third kind of *declaration-statement*, next to *local-variable-declaration* and *local-constant-declaration*. Thus it can appear as a statement at the top level of a block, but cannot in itself be e.g. a branch of an if statement or the body of a while statement.

Local functions need to be reconciled with top level functions in script syntax, so that they work as similarly as possible. Nested local functions in blocks, just like nested local variables, would truly be local functions even in script.


Examples
-------- 

A classical example is that of doing argument validation in an iterator function. Because the body of an iterator method is executed lazily, a wrapper function needs to do the argument checking. The actual iterator function can now be a local function, and capture all the arguments to the wrapper function:

``` c#
public static IEnumerable<T> Filter<T>(IEnumerable<T> source, Func<T, bool> predicate)
{
    if (source == null) throw new ArgumentNullException(nameof(source));
    if (predicate == null) throw new ArgumentNullException(nameof(predicate));

    IEnumerable<T> Iterator()
    {
        foreach (var element in source)
        if (predicate(element))
        yield return element;
    }
    return Iterator();
}

```

An example of a recursive local function would be a Quicksort, for instance:

``` c#
public static void Quicksort<T>(T[] elements) where T : IComparable<T>
{
    void Sort(int start, int end)
    {
        int i = start, j = end;
        var pivot = elements[(start + end) / 2];

        while (i <= j)
        {
            while (elements[i].CompareTo(pivot) < 0) i++;
            while (elements[j].CompareTo(pivot) > 0) j--;
            if (i <= j)
            {
                T tmp = elements[i];
                elements[i] = elements[j];
                elements[j] = tmp;
                i++;
                j--;
            }
        }
        if (start < j) Sort(elements, start, j);
        if (i < end) Sort(elements, i, end);
    }

    Sort(elements, 0, elements.Length - 1);
}
```

Again it captures parameters and type parameters of the enclosing method, while calling itself recursively.

For optimization purposes, some async methods are implemented with a "fast path" where they don't allocate a state machine or a resulting `Task` unless they discover that it's necessary. These aren't themselves `async`, but can have a nested local async function that they call when necessary, returning the resulting `Task`. Something along the lines of:

``` c#
public Task<byte> GetByteAsync()
{
    async Task<byte> ActuallyGetByteAsync()
    {
        await buffer.GetMoreBytesAsync();
        byte result;
        if (!buffer.TryGetBufferedByte(out result)) throw ...; // we just got more
        return result;
    }

    byte result;
    if (!buffer.TryGetBufferedByte(out result)) return ActuallyGetByteAsync(); // slow path

    if (taskCache[result] == null) taskCache[result] = Task.FromResult(result);
    return taskCache[result];
}
```

By the way, we just did `taskCache[result]` three times there in the last two lines, each with its own bounds check. We could maybe optimize a little with a local function taking a ref:

``` c#
    Task<byte> GetTask(ref Task<byte> cache)
    {
        if (cache == null) cache = Task.FromResult(result);
        return cache;
    }
    return GetTask(ref taskCache[result]); // only indexing once
```

Of course if we *also* add ref locals to the language, such trickery would not be necessary.


Scope and overloading
---------------------

In various ways we need to choose whether local functions are more like methods or more like local variables:

* Local variables only allow one of a given name, whereas methods can be overloaded
* Local variables shadow anything of the same name in outer scopes, whereas method lookup will keep looking for applicable methods
* Local variables are not visible (it is an error to use them) before their declaration occurs, whereas methods are.

There are probably scenarios where it would be useful to overload local functions (or at least more elegant not to have to give them all separate names). You can also imagine wanting to augment a set of existing overloads with a few local ones. 

However, it is somewhat problematic to allow local functions to be visible before their declaration: they can capture local variables, and it would provide an easy loophole for manipulating those variables before their declaration:

``` c#
f(3);
int x; // x is now 3!
void f(int v) { x = v; }
```

Such pre-declaration manipulation is certainly possible today, but it requires more sneaky use of lambdas or goto statements, e.g.:

``` c#
    goto Assign;
Before:
    goto Read;
Declare:
    int x = 5;
    goto Read;
Assign:
    x = 3;
    goto Before;
Read:
    WriteLine(x);
    if (x == 3) goto Declare;
```

This prints 3 and 5. Yeah, yuck. We should think twice before we allow local functions to make this kind of thing so much easier that you might likely do it by mistake.

On the other hand, there's no nice way to write mutually recursive local functions, unless the first can also see the second. (The workaround would be to nest one within the other, but that's horrible).

For now, for prototyping purposes, we'll say that local functions are more like local variables: there can only be one of a given name, it shadows same-named things in outer scopes, and it cannot be called from a source location that precedes its declaration. We can consider loosening this later.

Definite assignment analysis of locals captured by local functions should be similar to lambdas for now. We can think of refining it later.


Type inference
--------------

Lambdas have a mechanism for inferring their return type. It is used for instance when a lambda is passed to a generic method, as part of that generic method's type inference algorithm.

We could allow local functions to be declared with `var` as their return type. Just as `var` on local variables doesn't always succeed, we could fail whenever finding the return type of the local function is too hard; e.g. if it is recursive or an iterator.

``` c#
var fullName(Person p) => $"{p.First} {p.Last}"; // yeah, it's probably going to be a string.
```

We don't need this for prototyping, but it is nice to consider for the final design of the feature.


Declaration expressions
-----------------------

One thing lambdas have going for them is that they can be embedded in expression contexts, whereas local declarations currently can't, though we had a proposal for declaration expressions in C# 6. If we were to do some sort of let expressions, local functions should ideally work alongside local variables. The proposed C# 6 scheme would work for that:

``` c#
return (IEnumerable<T> Impl() { ... yield return ... }; Impl()); // Or no semicolon?
```

Why would you do that? For encapsulation, but especially if you're in a big expression context where pulling it out is too far. Imagine a string-interpolated JSON literal that has an array inside it that I want to construct with an iterator:

``` c#
return $@"""hello"": ""world""
          ... // 30 lines of other stuff
          ""list"" : { (
              IEnumerable<int> GetValues() {
                  ... yield return ...
              };
              JsonConvert.SerializeObject(GetValues())) }";
```

We don't have to think about this now, but it is nice to know that it's possible if we ever do declaration expressions to include local functions as one of the things you can declare in an expression context.


Slippery slope
--------------

Local classes might be the next ask, but we perceive them as much less frequent, and other features we're discussing would make them even less so.
