# Enhanced Using

Enhanced using consists of several related features that aim to make the disposable coding pattern easier to participate in for implementers, and easier to consume for end-users.

 - _[Using declarations](#Using-declarations)_ aim to make consuming a disposable type simpler by allowing ```using``` to be added to a local declaration. 

 - _[Pattern-based asynchronous disposal](#Pattern-based-asynchronous-disposal)_ allows a type to opt into asynchronous disposal without needing to implement the `IAsyncDisposable` interface.

 - _[Pattern-based disposal for ref structs](#Pattern-based-disposal-for-ref-structs)_ allows `ref struct`s to opt into disposal without needing to implement the `IDisposable` interface.

__See__: the [corresponding proposal](https://github.com/dotnet/csharplang/blob/master/proposals/csharp-8.0/using.md) in CSharpLang.

## Using Declarations

Using declarations allow a user to specify a `using` keyword as part of a local declaration statement:

```csharp
using var x = ...
// other statements
```

This is equivalent to declaring the local inside of a `using` statement at the same location:

```csharp
using (var x = ...) 
{
    // other statements
}
```

It is not valid to declare a using declaration without an initializer expression:

```csharp
using IDisposable x; //error CS0210: You must provide an initializer in a fixed or using statement declaration

```

The initializer expression must result in a type that is considered to be Disposable. That is, the expression must also be valid when used directly inside a `using` statement:

```csharp
using var x = <expression> 

using (<expression>) { } // expression must also be valid in a standard using statement
```

### Lifetime

The lifetime of the local extends to the scope in which it is declared; immediately prior to the variable going out of scope, it will be disposed.

```csharp
if (...)
{
    using var x = ...;
    
    // other statements

    // Dispose x
}
```

Using declarations in the same scope are disposed in the reverse order to which they are declared

```csharp
{
    using var x = ...;
    using var y = ...,  z = ...;

    // Dispose z
    // Dispose y
    // Dispose x
}
```

As with a local declared as part of a `using` statement, a using local is `readonly` and may not be re-assigned after declaration. 

```csharp
using var x = ...;
x = ...; // error CS1656: Cannot assign to 'x' because it is a 'using variable'
```

A using local may be used in the right hand side of an assignment or declaration. This means it is possible to capture a reference that will exist for longer than the lifetime of the using local. The reference will still be disposed when the using local is going out of scope. Interacting with the reference after disposal is undefined, but in most cases it is expected that it would result in an `ObjectDisposedException` being thrown.

```csharp
IDisposable y = null;
if (...)
{
    using var x = ...;
    y = x;
    // Dispose x
}
y.Dispose(); // undefined. ObjectDisposedException in most cases
```

It is possible to use an existing reference as the initializer of a using local declaration. As above, the reference will be disposed when the declared local is going out of scope, but the existing reference will still be available for use via its previous declaration.

```csharp
IDisposable y = ...;
if (...)
{
    using var x = y; 
    // Dispose x, which points to the same object as y
}
y.Dispose(); // undefined. ObjectDisposedException in most cases
```

### Asynchronous disposal 

When inside a method marked `async`, a user may optionally specify an additional `await` keyword prior to the `using` keyword, to indicate asynchronous disposal:

```csharp
await using var x = ...
```

Which is equivalent to
```csharp
await using (var x = ...) { }
```

The initializer expression must result in a type that is considered to be asynchronously disposable. That is, the expression must also be valid when used directly in an `await using` statement:

```csharp
await using var x = <expression> 

await using (<expression>) { } // expression must also be valid in a standard await using statement
```

Attempting to specify the `await` keyword on a using declaration in a method not marked with the `async` keyword results in a compiler error:

```csharp
public void M()
{
    await using var x = ...; //error CS4033: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
}
```

### Control flow 

In general, control flow is unaffected by the presence of a using declaration. However, there are some restrictions around the use of the `goto` statement when the statement and its target `label` lie either side of a using declaration.

It is forbidden to jump 'forward' to a location after a using declaration, when the `goto` or the target `label` are in the same or lower scope as the declaration.
```csharp
    goto label1; // error CS8648: A goto cannot jump to a location after a using declaration.
    using var x = ...;
label1:
    return;
```

```csharp
    goto label1; // ok. using declaration is in a lower scope than goto and label
    {
        using var x = ...;
    }
label1:
    return;
```

```csharp
    {
        {
            goto label1; // error CS8648: A goto cannot jump to a location after a using declaration.          
        }
        using var x = ...;
    }
label1:
    return;
```

It is forbidden to jump 'backwards' to a location before a using declaration, when the label is in the same scope as the using declaration. 
```csharp
label1:
        using var x = ...; //error CS8649: A goto cannot jump to a location before a using declaration within the same block.
        goto label1;
```

Jumping 'backwards' to a label at a higher scope is specifically permitted.

```csharp
label1:
    {
        using var x = ...;
        goto label1; // allowed. label1 is in a higher scope than the using declaration
    }
```

When jumping to a higher scope, any declared using variables will be disposed at the site of the `goto`. Variables not yet declared will **not** be disposed.
```csharp
label1:
    {
        using var x = ...;
        goto label1; // dispose x
        using var y = ...; 
    }
```

It is always permissible to jump to the same scope, when the `goto` and target `label` do not lie either side of a using declaration.
```csharp
    goto label1; // ok, not jumping over a using declaration
label1:
    using var x = ...;
label2:
    goto label2; // ok, not jumping over a using declaration
```


### Other restrictions on use

A using declaration may **not** appear directly inside of a case label. It may instead be used within a block inside of a case label:

```csharp
switch (...)
{
    case ...: 
        using var x = ...; // error CS8647: A using variable cannot be used directly within a switch section (consider using braces). 
        break;
    
    case ...:
        {
            using var y = ...; // ok

            // Dispose y
        }
        break;
}
```

A using declaration may **not** appear directly as part of an `out` variable declaration. It is easy to emulate this behavior however, by adding a using declaration immediately after the `out` variable:
```csharp
if(TryGetDisposable(out var x))
{
    using var y = x;

    // Dispose y, and thus x
}
```

## Pattern-based asynchronous disposal 

With pattern-based asynchronous disposal a type can be used in an `await using` statement without needing to explicitly implement `IAsyncDisposable` if it meets certain structural requirements. Specifically: 

    "A reachable, non-generic Task-like returning instance method called DisposeAsync, that can be called with zero explicit arguments"

Where reachable means legal to call from the site of the ```await using(...)``` under normal accessibility rules.

```csharp
public class C 
{
    public static async Task M()
    {     
        await using (AsyncDisposer a = new AsyncDisposer())
        { 
        }
    }
}

public class AsyncDisposer
{
    public async ValueTask DisposeAsync() => Console.WriteLine("DisposeAsync");
}
```

In the situation where a type can both be implicitly converted to `IAsyncDisposable` and also fits the asynchronous disposal pattern, `IAsyncDisposable` is chosen. 

### Optional arguments and params

Pattern-based asynchronous disposal methods may contain optional or `params` parameters and still meet the requirements of the pattern. In general if you could write `c.DisposeAsync()` at the site of the `await using` syntax and have it be a valid call under normal language rules, then the type is considered to be asynchronously disposable.

```csharp

public class AsyncDisposer
{
    public async ValueTask DisposeAsync(int x = 0, params object[] args) => Console.WriteLine("DisposeAsync"); // valid pattern candidate
}
```

### Extension methods

Extension methods may **not** be used to implement asynchronous disposal. The method must be a reachable _instance_ method in order to be considered as a valid candidate for the pattern.

### Nullable value type behavior

_Note_: This is currently not working as spec'd and is tracked by [#34701](https://github.com/dotnet/roslyn/issues/34701)

For nullable value types in a `using` statement, the behavior today is to call ```Dispose``` on the _underlying_ type if and only if the type has a value (i.e. ```if(t.HasValue){ t.GetValueOrDefault().Dispose() }``` ). This allows lifted types to be used as if they were the underlying type, and dispose is only called in the case they are not null. This behavior is extended to pattern-based ```DisposeAsync``` methods in `await using` statements.

```csharp
public class C 
{
    public static async Task M()
    {
        StructDisposer? a = null;
        await using (a) { } // DisposeAsync is not invoked
        
        StructDisposer? b = new StructDisposer();
        await using (b) { } // DisposeAsync is invoked
    }
}

public struct StructDisposer
{
    public async ValueTask DisposeAsync() => Console.WriteLine("DisposeAsync");
}
```


## Pattern-based disposal for ref structs

Today `ref struct`s can not participate in the `IDisposable` pattern as they can not implement an interface. This feature allows a `ref struct` to be considered disposable if it meets certain structural requirements. Specifically:

    "A reachable, void returning instance method called Dispose, that can be called with zero explicit arguments"

Where reachable means legal to call from the site of the `using(...)` under normal accessibility rules.

```csharp
using System;
public class C 
{
    public void M()
    {
        using var x = new Disposer();
    }
}

ref struct Disposer
{
    internal void Dispose() => Console.WriteLine("Disposed");
}
```

_Note:_ `ref struct`s can not be used in an `async` method and therefore can not participate in asynchronous disposal via `await using`

### Optional arguments and params

Pattern-based disposal methods may contain optional or `params` parameters and still meet the requirements of the pattern. In general if you could write `s.Dispose()` at the site of the `using` syntax and have it be a valid call under normal language rules, then the `ref struct` is considered to be disposable.

```csharp
public ref struct Disposer
{
    public void Dispose(int x = 0, params object[] args) => Console.WriteLine("Disposed"); // valid pattern candidate
}
```

### Extension methods

Extension methods may **not** be used to implement disposal. The method must be a reachable _instance_ method in order to be considered as a valid candidate for the pattern.