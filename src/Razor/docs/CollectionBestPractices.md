# Collection Best Practices in Razor

- [Imperative Collections](#imperative-collections)
- [Immutable Collections](#immutable-collections)
  - [Using Builders](#using-builders)
  - [`ImmutableArray<T>`](#immutablearrayt)
    - [Using `ImmutableArray<T>.Builder`](#using-immutablearraytbuilder)
  - [Frozen Collections](#frozen-collections)
- [Array Pools](#array-pools)
- [Object Pools](#object-pools)
- [✨It’s Magic! `PooledArrayBuilder<T>`](#its-magic-pooledarraybuildert)
- [Using LINQ](#using-linq)
  - [Best Practices](#best-practices)
- [Meta Tips](#meta-tips)

# Imperative Collections
- .NET provides many collection types with different characteristics for different purposes.
- The collections from the System.Collections namespace should be avoided. Never use these unless in some legacy scenario.
- The collections in System.Collections.Generic are considered the “work horse” collection types for .NET and are
  suitable for most purposes. They have years of hardening that make them highly efficient choices for most work.
- Popular imperative collection types include the ones we all use on a regular basis `List<T>`, `HashSet<T>`,
  `Dictionary<TKey, TValue>`, `Stack<T>`.
- System.Collections.Concurrent contains collections that are designed for use when thread-safety is needed.
  In general, these should only be used in particular situations.

> [!WARNING]
> **Beware of collection growth**
>
> The imperative collections generally have more internal storage than needed to allow more items to be added. (This is
> what is meant by "capacity” vs. “count”). When enough items are added, the internal storage will need to grow. This
> requires creating larger storage, releasing the previous storage for garbage collection, and copying the existing
> contents into it, which consumes CPU time. For a larger collection, this can potential happen many times, so it’s
> important to set the capacity up front to avoid unnecessary internal storage growth.

> [!WARNING]
> **Avoid exposing collection interfaces**
>
> Avoid exposing collections directly via interfaces, such as `IReadOnlyList<T>` and
> `IReadOnlyDictionary<TKey, TValue>`. The primary reason for this is that these interfaces can result in allocations
> when they are foreach’d. In general, collections provide a struct enumerator that can be used to foreach that
> collection without allocating an `IEnumertor<T>` on the heap. However, when going through a collection interface,
> there isn’t a struct enumerator, so an allocation is likely required to foreach. In fact, many collections, such as
> `List<T>`, are implemented to just return their struct enumerator when accessed via collection interfaces, resulting
> in an allocation when the struct enumerator is boxed.
> - If exposing a collection is necessary, consider whether it might be better to expose a more optimal read-only
>   collection. Instead of `IReadOnlyList<T>`, consider [`ImmutableArray<T>`](#immutablearrayt).
> - There aren’t many other options when an API calls for exposing an `IReadOnlyDictionary<TKey, TValue>`. In these
>   cases, consider whether it might be better to just avoid exposing the collection altogether and provide APIs that
>   access it. Or, in some cases, it might be necessary to create entirely new collection types. (This is why Razor
>   `TagHelperDescriptors` expose a `MetadataCollection`.)

> [!WARNING]
> **Be mindful of ToArray()**
>
> Calling `ToArray()` on a collection will create a new array and copy content from the collection into it. So, when
> the exact capacity is known up front, it is an anti-pattern to create a `List<T>` without that capacity, fill it
> with items and then call `ToArray()` at the end. This results in extra allocations that could be avoided by creating
> an array and filling it.

# Immutable Collections
- The .NET immutable collections are provided by the System.Collections.Immutable NuGet package, which provides
  implementations for .NET, .NET Framework, and .NET Standard 2.0.
- The collections in the System.Collections.Immutable namespace have a very specific purpose. They are intended to be
  *persistent* data structures; that is, a data structure that always preserves the previous version of itself when it
  is modified. Such data structures are effectively immutable, but in hindsight, maybe it would have been better for
  this namespace to have been called, System.Collections.Persistent?
  - The term “persistent data structure” was introduced by the 1986 paper,
    “Making Data Structures Persistent” ([PDF](https://www.cs.cmu.edu/~sleator/papers/making-data-structures-persistent.pdf)).
  - A highly influential book in the area of persistent data structures is “Purely Functional Data Structures” (1999)
    by Chris Okasaki ([Amazon](https://www.amazon.com/Purely-Functional-Data-Structures-Okasaki/dp/0521663504)).
    Okasaki’s original dissertation is available from CMU’s website ([PDF](https://www.cs.cmu.edu/~rwh/students/okasaki.pdf)).
- Because of their persistency, nearly all of the immutable collections have very different implementations than their
  imperative counterparts. For example, `List<T>` is implemented using an array, while `ImmutableList<T>` is implemented
  using a binary tree.
- Mutating methods on an immutable collection perform “non-destructive mutation”. Instead, of mutating the underlying
  object, a mutating method like `Add` produces a new instance of the immutable collection. This is similar to how the
  `String.Replace(...)` API is used.
- Significant effort has been made to ensure that immutable collections are as efficient as they can be. However, the
  cost of persistence means that immutable collections are generally assumed to be slower than imperative counterparts.

> [!CAUTION]
>
> Because the immutable collections are often implemented using binary trees to achieve persistence, the asymptotic
> complexity of standard operations can be very surprising. For example, `ImmutableDictionary<TKey, TValue>` access is
> O(log n) rather than the usual O(1) that would be expected when accessing a hash table data structure, such as
> `Dictionary<TKey, TValue>`. A similar difference in performance characteristics exists across the various collection
> types. The following table shows the complexity of accessing a few popular collections types using their indexer.
>
> | Immutable collection type                 | Complexity | Imperative collection type       | Complexity |
> | ----------------------------------------- | ---------- | -------------------------------- | ---------- |
> | `ImmutableDictionary<TKey, TValue>`       | O(log n)   | `Dictionary<TKey, TValue>`       | O(1)       |
> | `ImmutableHashSet<T>`                     | O(log n)   | `HashSet<T>`                     | O(1)       |
> | `ImmutableList<T>`                        | O(log n)   | `List<T>`                        | O(1)       |
> | `ImmutableSortedDictionary<TKey, TValue>` | O(log n)   | `SortedDictionary<TKey, TValue>` | O(log n)   |

> [!CAUTION]
> **ToImmutableX() extension methods are not “freeze” methods!**
>
> The System.Immutable.Collections package provides several extension methods that produce an immutable collection from
> an existing collection or sequence. These methods aren’t optimized to reuse the internal storage of other collections
> in any way. Because of this, the following code is an anti-pattern. In this example, each element is added to a
> `HashSet<int>` and then the elements of that set are added to a new `ImmutableHashSet<int>`.
>
> ```C#
> var array = new[] { "One", "Two", "Two", "One", "Three" };
> var set = new HashSet<int>(array).ToImmutableHashSet();
> ```

## Using Builders
- When creating an immutable collection with a lot of mutation, use a builder. Builders are optimized to populate the
  internal storage of an immutable collection.
- The following code achieves the expected result but inefficiently creates several intermediate `ImmutableList<int>`
  instances.

```C#
ImmutableList<int> CreateList()
{
    var list = ImmutableList<int>.Empty;
    for (var i = 0; i < 10; i++)
    {
      list = list.Add(i);
    }

    return list;
}
```

- The version below populates an `ImmutableList<int>.Builder` and creates just a single `ImmutableList<int>` instance
  at the end.

```C#
ImmutableList<int> CreateList()
{
    var builder = ImmutableList.CreateBuilder<int>();

    for (var i = 0; i < 10; i++)
    {
        builder.Add(i);
    }

    return builder.ToImmutable();
}
```

## `ImmutableArray<T>`
- `ImmutableArray<T>` is very different than the other immutable collections. It is the only struct collection type,
  and is not optimized for persistence. (In hindsight, perhaps a more appropriate name would have been
  `FrozenArray<T>`?)
- `ImmutableArray<T>` is a relatively simple struct that provides read-only access to an internal array.

> [!WARNING]
> **Be aware of copies!**
>
> In order to maintain its immutability semantics, `ImmutableArray<T>` *always* creates a copy of the array it is
> wrapping internally. If it didn’t, external changes to the array would be reflected in the `ImmutableArray<T>`.
>
> Because a new array copy is created for every `ImmutableArray<T>` it is important to be mindful of chaining methods
> that produce immutable arrays to avoid unnecessary intermediate array copies.
>
> In addition, as of System.Immutable.Collections 8.0.0, there is a new `ImmutableCollectionsMarshal` class that can
> provide access to the internal array of an `ImmutableArray<T>` or to create an new `ImmutableArray<T>` that wraps an
> existing array without copying. These can be used in high performance scenarios, but should be employed carefully to
> avoid introducing subtle bugs.

- Because `ImmutableArray<T>` is a struct that wraps a single field of a reference type, it is essentially free to copy
  at runtime. However, this also leaves a bit of a usability wart because, as a struct, an `ImmutableArray<T>` reference
  can never be null, but it can has its default, zeroed-out value where the internal array reference is null. For this
  reason, an `IsDefault` property is provided to check if an `ImmutableArray<T>` is actually wrapping an array.
- `ImmutableArray<T>` *can* be used as a persistent data structure via non-destructive mutation, but mutating methods
  are generally implemented to copy the elements of the internal array. For example, `Add` will create a copy of the
  internal array storage with an additional element and return it as an `ImmutableArray<T>`.

> [!NOTE]
> **A Little History**
>
> `ImmutableArray<T>` was not part of System.Collections.Immutable when originally conceived. It was developed out of
> necessity by Roslyn to expose array data while avoiding the inherent problems of exposing an array. (At the time,
> .NET arrays didn’t even implement `IReadOnlyList<T>`, which didn’t ship until .NET Framework 4.5.)
> System.Collections.Immutable itself was inspired by the many persistent data structures used internally by Roslyn and
> was intended to be used within Visual Studio for asynchronous code. However, the NuGet package became so popular that
> it was ultimately pulled into the .NET runtime.

### Using `ImmutableArray<T>.Builder`
- The Builder type for `ImmutableArray<T>` provides a couple of features not provided by other immutable collection
  builders.
- `ToImmutable()`: Like other builders, creates a new `ImmutableArray<T>` that wraps a copy of the filled portion of
  internal array buffer used by the builder.
- `MoveToImmutable()`: Creates a new `ImmutableArray<T>` that wraps the internal array buffer used by the builder. Note
  that this requires that the builder’s capacity is the same as its count. In other words, the builder’s internal array
  buffer must be completely filled, or this will throw an `InvalidOperationException`. If the operation is successful,
  the internal buffer is set to an empty array.
- `DrainToImmutable()`: This is sort of like a combination of `ToImmutable()` and `MoveToImmutable()`. This operation
  “drains” the builder by checking if the capacity equals the count. If true, it returns a new `ImmutableArray<T>` that
  wraps the internal array buffer. If false, it returns a new `ImmutableArray<T>` that wraps a copy of the filled
  portion of the internal array buffer. In either case, the internal buffer is set to an empty array. Generally,
  code should be calling ToImmutableAndClear instead of DrainToImmutable, as that better handles interactions with
  pools in that it doesn't throw away the backing array when the size is not equal to the capacity.

> [!CAUTION]
> **Immutable collections as static data**
>
> Because of their performance characteristics, most of the immutable collections are *not* suitable for static
> collections. In fact, `ImmutableArray<T>` is really the only immutable collection that should be used for static data,
> since accessing it is essentially the same as accessing an array.
>
> When creating a static lookup table it can be tempting to reach for an `ImmutableHashSet<T>` or an
> `ImmutableDictionary<TKey, TValue>`, but that temptation should be resisted! Lookup will always be slower than using
> he imperative counterpart because of the internal tree structures employed for immutable collections.
>
> There are several tricks that can be used to encapsulate imperative collections as static data. For example, a nested
> static class could hide a `HashSet<T>` or `Dictionary<TKey, TValue>` behind static methods that access the
> collections. However, a better solution available today is to use a [frozen collection](#Frozen-Collections).

## Frozen Collections
- The System.Collections.Frozen namespace became available starting with version 8.0.0 of the
  System.Collections.Immutable NuGet package.
- Currently, there are two frozen collection types: `FrozenSet<T>` and `FrozenDictionary<TKey, TValue>`.
- The frozen collections are not persistent; in fact, they can’t be mutated at all! Instead, frozen collections are
  optimized for faster lookup operations — faster than their imperative counterparts.
- Frozen collections provide faster lookup by performing up-front analysis and selecting an optimal implementation for
  the content. This means that they are much more expensive to create.
- Because of their higher creation cost and improved lookup performance, frozen collections are best suited for
  static data.

# Array Pools
- When a temporary array is needed to perform work and the lifetime of the array is bounded, consider acquiring a
  pooled array. `ArrayPool<T>` can be used to acquire an array of some minimum length that can be returned to the pool
  when the work is done.

> [!WARNING]
> **Be mindful of the array size!**
>
> The size of an array acquired from an `ArrayPool<T>` is guaranteed to be at least as large as the minimum length that
> was requested. However, it is likely that a larger array will have been returned. So, care should be taken to avoid
> using the acquired array’s length, unless that’s what’s needed.

- Razor provides a handful of helper extension methods that acquire pooled arrays and return them within the scope of a
using statement:

```C#
var pool = ArrayPool<char>.Shared;

using (pool.GetPooledArray(minimumLength: 42, out var array)
{
    // When using array but be careful that array.Length >= minimumLength.
}

using (pool.GetPooledArraySpan(minimumLength: 42, out var span)
{
    // span is array.AsSpan(0, minimumLength) to help avoid subtle bugs.
}
```

# Object Pools
- Razor provides object pooling facilities based on
  [Microsoft.Extensions.ObjectPool](https://www.nuget.org/packages/Microsoft.Extensions.ObjectPool/) (which was
  originally based on Roslyn’s `ObjectPool<T>`) along with several premade pools for many collection types in the
  [Microsoft.AspNetCore.Razor.PooledObjects](https://github.com/dotnet/razor/tree/5c0677ad275e64300b897de0f6e8856ebe13f07b/src/Shared/Microsoft.AspNetCore.Razor.Utilities.Shared/PooledObjects)
  namespace. These can be used to acquire temporary collections to use for work and return when finished.

```C#
using var _ = ListPool<int>.GetPooledObject(out var list);

// Use list here. It'll be returned to the pool at the end of the using
// statement's scope.
```

- Pooled collections provide a couple of benefits.
  1. Pooled collections decrease pressure on the garbage collector by reusing collection instances.
  2. Pooled collections avoid growing a collection’s internal storage. For example, when the `List<int>` acquired from
     `ListPool<int>` in the code sample above is returned to the pool, it will be cleared. However, the capacity of its
     internal storage will only be trimmed if it is larger than 512. So, lists acquired from the pool are likely to
     already have a larger capacity than needed for most work.

> [!WARNING]
> **Don't allow pooled objects to escape their scope!**
>
> Consider the following code:
>
> ```C#
> List<int> M()
> {
>     using var _ = ListPool<int>.GetPooledObject(out var list);
>
>     // use list...
>
>     return list;
> }
> ```
>
> The compiler won't complain if a pooled `List<int>` escapes its scope. In the code above, the `List<int>` will be
> returned to the pool at the end of the using statement's scope but is returned from the method. This results
> several problems:
>
> 1. The list will be cleared when returned to the pool. So, the caller will find it to be empty.
> 2. If the caller adds items to the list, other code acquiring a pooled list might receive the mutated list!
> 3. Likewise, if the caller holds onto the list, other code acquiring a pooled list might receive the same list and
>    mutate it!
>
> In essence, a pooled object that escapes its scope can corrupt the pool in came from.

# ✨It’s Magic! `PooledArrayBuilder<T>`

- Razor’s [`PooledArrayBuilder<T>`](https://github.com/dotnet/razor/blob/5c0677ad275e64300b897de0f6e8856ebe13f07b/src/Shared/Microsoft.AspNetCore.Razor.Utilities.Shared/PooledObjects/PooledArrayBuilder%601.cs)
  is heavily inspired by Roslyn’s [`TemporaryArray<T>`](https://github.com/dotnet/roslyn/blob/d176f9b5a7220cd95a6d5811ba1c49ac392a2fdc/src/Compilers/Core/Portable/Collections/TemporaryArray%601.cs).
- The important feature of this type (and the reason we’ve started using it all over Razor) is that it stores the first
  4 elements of the array being built inline as fields. After 4 elements have been added, it will acquire a pooled
  `ImmutableArray<T>.Builder`. This makes it extremely cheap to use for small arrays and reduces pressure on the object
  pools.
- Because `PooledArrayBuilder<T>` is a struct, it must be passed by-reference. Otherwise, any elements added by a method
  it’s passed to won’t be reflected back at the call-site.
- To avoid writing buggy code that accidentally copies a `PooledArrayBuilder<T>`, it is marked with a `[NonCopyable]`
  attribute. A Roslyn analyzer tracks types decorated with that attribute and ensures that instances are never copied.
- Because `PooledArrayBuilder<T>` _may_ acquire a pooled `ImmutableArray<T>.Builder`, it is disposable and should
  generally be created within a using statement. However, that makes it a bit more awkward to pass by reference, so a
  special `AsRef()` extension method is provided.
- In the following code example, an `ImmutableArray<int>.Builder` will never be acquired from the pool because the
  `PooledArrayBuilder<int>` only ever contains three elements.

```C#
ImmutableArray<string> BuildStrings()
{
    using var builder = new PooledArrayBuilder<string>();
    AddElements(ref builder.AsRef());

    return builder.ToImmutableAndClear();
}

void AddElements(ref PooledArrayBuilder<string> builder)
{
    builder.Add("One");
    builder.Add("Two");
    builder.Add("Three");
}
```

# Using LINQ
- LINQ (that is, LINQ to Objects) is a bit of a tricky subject. It has been used extensively throughout Razor for a long
  time. It’s certainly not off limits but should be used with an understanding of the hidden costs:
  - Every lambda expression represents at least one allocation — the delegate that holds it.
  - A lambda that accesses variables or instance data from an outer scope will result in a closure being allocated each
    time the delegate is invoked.
  - Many LINQ methods allocate an iterator instance.
  - Because Razor tooling runs in Visual Studio, it runs on .NET Framework and doesn’t benefit from many LINQ
    optimizations made in modern .NET.
  - Because LINQ methods target `IEnumerable<T>` instances, they can trigger additional allocations depending on how
    `GetEnumerator()` is implemented. For example, a simple call like `Queue<T>.Any()` might seem innocuous—it doesn’t
    even have a lambda! However, the implementation of
    [`Enumerable.Any()`](https://referencesource.microsoft.com/#System.Core/System/Linq/Enumerable.cs,1288) on .NET
    Framework doesn’t have any fast paths and simply calls `GetEnumerator()`. So, `Any()` boxes `Queue<T>`’s struct
    enumerator, resulting an allocation every time it’s called. In a tight loop, that could be disastrous!
  - LINQ can obfuscate algorithmic complexity. It can be hard to see that introducing a LINQ expression has made an
    algorithm O(n^2).

## Best Practices
- Consider whether LINQ could have a negative performance impact for a particular scenario. Is this a hot path? Is it
  happening in a loop?
- Always try to use static lambdas to ensure closures aren’t created and delegates are cached and reused.
- What collection type is being targeted? Do we have specialized LINQ methods that could be used? Razor provides a few
  for `ImmutableArray<T>` and `IReadOnlyList<T>`.

# Using Collection Expressions
- C# 12 introduced collection expressions as a language-level abstraction to generate collection-based code. It is a
  goal of collection expressions to produce efficient code.
- Collection expressions are generally very good. They are especially helpful for combining collections or even query
  expressions.

```C#
int[] Combine(List<int> list, HashSet<int> set)
{
    return [..list, ..set];
}

int[] Squares(List<int> list, HashSet<int> set)
{
    return [
        ..from x in list select x * x,
        ..from x in set select x * x
    ];
}
```

> [!WARNING]
> **Considerations when using collection expressions**
>
> - Sometimes, a collection expression might create a new temporary collection instance, such as a `List<T>`. However,
>   it will not acquire a temporary collection from Razor’s object pools ([SharpLab](https://sharplab.io/#v2:EYLgxg9gTgpgtADwGwBYA0ATEBqAPgAQAYACfARhQG4BYAKHwGZSAmYgYWIG87jfSmAlgDsALgG0AusQCyACnIMAPMJEA+YgGcYARwCuMIWBgBKLjz4X8AdmJiAdHc079hmBJq0LAXzpegA=)).
> - There are pathological collection expressions to be avoided. For example, never use a collection expression to
>   replace a call to `ImmutableArray<T>.Builder.ToImmutable()` ([SharpLab](https://sharplab.io/#v2:EYLgxg9gTgpgtADwGwBYA0ATEBqAPgAQAYACfARgDoBhCAG1pjABcBLCAOwGcKBJAWz4BXJgENgDANwBYAFD4AzKQBMxKsQDes4ttKL+Q0eJgBBKFBEBPADwt2TAHzEAsgApbTANoBdYiLOWASg0tHVCANz9iYEEWWgwYKGIAXmJ9YTEGU3MLalgRJhgAIRi4hJs7excA6RlQ0OjY+KgKYwwMACURdgBzGBc/bOqQuuJhuvwAdmIPCgookqavGtCAX1kVoA=)).
>   When using a collection expression in a new scenario or with an uncommon type, it's a good idea to try it out on
>   https://sharplab.io first.

- Empty collection expression generally produces very efficient code and can be used without concern ([SharpLab](https://sharplab.io/#v2:EYLgtghglgdgPgAQEwEYCwAoBAGABAlAOgGEB7AG3IFMBjAFylJgGcBuTHfFAFnazwIkK1eoxaEAkmDABXOhGDU+nQQBlYAR2UCiAJRkwGYKkLAAHKNQBOAZSpWAblBpU2mDgGZ8SXMVwBvTFxg/C8EblwAWQAKAEoAoJCkgDkUaIBRGBljKwVqQnTzOgBPAB5YOgA+ONi+JJS0gEErXOKCorKK6tjaxPrcVOiAbQBdXox+4OSkYbG6yeSPWfGkgF8+3A2EMIjBgg9yw0rcZioNGSoYF3jAiZD1u+CtnYGZqVl5RSpm1sOq3AgLQgxRuGweSWe+F2S2IMmYdFIYF0VAgABMAPIwcjFdTw3DkKDw0GPXAPB6YIZkSi0BhMABCMksqPs0RKZiopAAZtFYfDEci0Zjsbi6LEADS4ABExCsKLoVElsRGnhOKOoqO8vjhCKRKIxWJxhLo0TMgIgYGYuAF+uxNlNMD+xwcEHIF2Y8RAuAk1qFhvhjswtySZisUGd8twsrRTGxuAqo1wAH1na7XLgALy4FNuwgAFVIP2BcT4kIquDoAAtCUMy7BmQAPEYZ47Jl1umswBsjebBbZxwy+UgGOjNpPZ1yEVSXADmlZLJL7Eky2XsEARVkduAA4lQ6MucmvSFY4hsg5NOUeUTQK7hos6rFm21R+2On+6Nkkz5MQgQUPgAOyPqmPb1OC9zuCSS5ZAe65cB4hA7nu0GruuJ4kkk6bHIh+4oUexYQUkfYEAAbFqfK6oKBoir4UbytEPoGnaEAOl0Jz2rEH4hJhuAwFQADu0TMOxfCrEAA=)).
- It is expected that collection expressions will improve over time. At the time of writing, there are
  [several open issues](https://github.com/dotnet/roslyn/issues?q=is%3Aissue+is%3Aopen+%22collection+expression%22+label%3AArea-Compilers+label%3A%22Code+Gen+Quality%22)
  tracking collection expression enhancements.

# Meta Tips

- Always be aware of the memory layout, features, and performance characteristics of the data structure you are using.
- If you have an implementation question for a .NET collection type, check out the source code using the
  [.NET Source Browser](https://source.dot.net/) for modern .NET, or the
  [.NET Framework Reference Source](https://referencesource.microsoft.com/). And of course, the .NET runtime repo is
  available at [dotnet/runtime](https://github.com/dotnet/runtime).
- Several reflection-based tools exist for exploring .NET assemblies, such as
  [ILSpy](https://github.com/icsharpcode/ILSpy) or dotPeek (from JetBrains).
- Use https://sharplab.io to see how code will be compiled. This can be especially useful for collection expressions,
  which are usually very efficient do have pathological cases to avoid.