# Enhanced Using

Enhanced using consists to two related features that aim to make the ```IDisposable``` coding pattern easier to participate in for implementers, and easier to consume for end users.

__See__: the [corresponding proposal](https://github.com/dotnet/csharplang/blob/d2ce4cc3e17708e7e1d062bf40da0901a744fa3b/proposals/using.md) in CSharpLang.

_Pattern based dispose_ aims to make participation in the disposable pattern easier by relaxing the restrictions on what types can be used in a ```using(...){ }``` construct. 

_Using declarations_ aim to make consuming a disposable type simpler by allowing ```using``` to be added to a variable declaration. 


## Pattern based dispose

Previously a type must derive from ```IDiposable``` / ```IAsyncDisposable``` and implement the ```void Dispose()``` / ```Task DisposeAsync()``` methods. With pattern-based dispose, a type can be considered 'disposable' if it meets certain structural requirements. Specifically:

For ```using``` statements:  

    "A reachable, void returning method called Dispose, that can be called with zero parameters"

For ```await using``` statements: 

    "A reachable, Task-like returning method called DisposeAsync, that can be called with zero parameters"

Where reachable means legal to call from the site of the ```[await] using(...)``` under normal accessibility rules.

The ```Dispose``` / ```DisposeAsync``` methods are discovered using [Generalized Pattern Lookup](pattern-methods.md). In general if you could write ```c.Dispose()``` at the site of the ```using``` syntax and have it be a valid call under normal language rules, then the type is considered to be disposable. This includes implementation of ```Dispose``` via extension method, or methods with default or ```params``` parameters. 

In the situation where a type can be implicitly converted to ```IDisposable``` and also fits the disposable pattern, then ```IDisposable``` will be preferred. While this takes the opposite approach of foreach (pattern preferred over interface) it is necessary for backwards compatibility.


### Null behaviors

Today ```using(null){ }``` is valid syntax and the using is elided completely.  ```using(IDisposable x = null){ }``` is also valid; the type is first checked for null before calling Dispose: when the using expression produces a null value there is no ```NullReferenceException``` thrown, and no Dispose method is called. This is extended to pattern based _instance_ ```Dispose``` methods.

For extension method ```Dispose``` implementations the second rule is _not_ followed. Today if you call an extension method on a null valued receiver, the method is still called with the ```this``` parameter set to null. Pattern-based dispose follows this behavior, meaning an extension method ```Dispose``` will always be called but with a ```null``` this parameter if type in the dispose is null-valued.

### Nullable value type behavior
For nullable value types the behavior today is to call ```Dispose``` on the _underlying_ type if and only if the type has a value (i.e. ```if(t.HasValue){ t.GetValueOrDefault().Dispose() }``` ). For pattern-based instance ```Dispose``` methods this behavior is preserved. 

For extension methods, this behavior is _not_ followed. If an extension method with a ```this``` parameter of ```x?``` can be found, it will be called, regardless of whether the nullable value has a value or not, that is it may be called with a null value if ```HasValue``` returns false.  An extension method with a ```this``` parameter of ```x``` does _not_ make the type disposable. That is, an extension method on the underlying type is not enough to make a nullable-value type disposable. This is consistent with the behavior of extension methods today and the concept of calling ```x.Dispose()```: an extension method on the underlying type is not callable on the nullable value type.

### Lowering
The lowered forms of a using statement thus depend on how ```Dispose``` was implemented, and the type which is being disposed.

In general: 
```c#
using(var x = ...){ // statements }
```

is lowered to
```c#
var x = ...; //expression
try
{
    // statements
}
finally
{
    if(x != null) x.Dispose(); // disposal
}
```

With the following specializations:

Expression value|Dispose implementation| call emitted
----------|----------------------|-----------------
```null``` | All implementations  | _elided_
Reference type | IDisposable | ```if (x != null) ((IDisposable)x).Dispose();```
Reference type | Instance pattern method | ```if (x != null) x.Dispose();```
Reference type | Extension method | ```ExtensionStaticClass.Dispose(x);```
Value type | All implementations | ```x.Dispose();```
Nullable value type | IDisposable | ``` if(x.HasValue) ((IDisposable)x.GetValueOrDefault()).Dispose();```
Nullable value type | Instance pattern method |  ``` if(x.HasValue) x.GetValueOrDefault().Dispose();```
Nullable value type | Extension method | ``` ExtensionStaticClass.Dispose(x) ```

## Using Declarations

This has the same effect as wrapping the declaration in a standard ```using(...){ }``` syntax, with the lifetime of the wrapped declaration being the same as that of the enclosing block.

_TODO_