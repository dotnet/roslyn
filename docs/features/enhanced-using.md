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

The design philopshy of the using statement is to only perform an action when there is a resource to be disposed. As such, null values are allowed, but no actions are performed. ```using(null){ }``` is valid syntax and the using is elided completely.  ```using(IDisposable x = null){ }``` is also valid; the type is first checked for null before calling Dispose: when the using expression produces a null value there is no ```NullReferenceException``` thrown, and no Dispose method is called. This is extended as-is to pattern-based ```Dispose``` methods.

For extension method ```Dispose``` implmentations this means they will explictly *not* be invoked when the receiver is null, even though an equivilent call in code of ```x.Dispose()``` would result in the call being made with a null ```this``` parameter. This was a language decision to maintain the behavior that ```using``` perfoms no action when the resource being used is null valued.

### Nullable value type behavior

For nullable value types the behavior today is to call ```Dispose``` on the _underlying_ type if and only if the type has a value (i.e. ```if(t.HasValue){ t.GetValueOrDefault().Dispose() }``` ). This allows lifted types to be used as if they were the underlying type, and dispose is only called in the case they are not null. This behavior is extended to pattern based ```Dispose``` methods.

Because dispose is only called on the underlying type when it is not null, an extension method on the lifted ```Type?``` is not enough to make ```Type``` disposable, and will not be called even when the type in the using is ```Type?```. This follows the same behavior as the null-conditional operator ```?.```, where only an extension method of the underlying type will be invoked and only when the nullable value type is not null.  

### Interactions with generalized pattern matching

The explicit dispose rules around nullability and nullable-value types would seem to contradict the earlier statements that the ```Dispose``` method is looked up using Generalized Pattern Lookup. For instance in the case of ```using(int? x = 3){...}``` where an extension method on both ```int?``` and ```int``` is available, it would be expected that Generalized Pattern Lookup would prefer the more specific case of ```int?``` which is directly against the explained nullable-value type behavior.

This is remedied by performing Generalized Pattern Lookup on the _underlying_ type for nullable value-types. This means that for ````using(int? ...){}``` the lookup is actually performed on ```int```, and Generalized Pattern Lookup will find a single extension for ```int```. 

For nullable-value types that implement via instance pattern dispose, searching on the nullable type will fail (as there is no method where the receiver is ```Type?```), so looking up on the underlying type is actually required to successfully look it up. The using statement will unwrap the nullable ```Type?``` to a ```Type``` before calling the method in the case where it is not null, ensuring the looked up method has the correct receiver.


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
Reference type | Extension method | ```i (x != null) ExtensionStaticClass.Dispose(x);```
Value type | All implementations | ```x.Dispose();```
Nullable value type | IDisposable | ``` if(x.HasValue) ((IDisposable)x.GetValueOrDefault()).Dispose();```
Nullable value type | Instance pattern method | ``` if (x.HasValue) x.GetValueOrDefault().Dispose();```
Nullable value type | Extension method | ``` if (x.HasValue) ExtensionStaticClass.Dispose(x.GetValueOrDefault()) ```

## Using Declarations

This has the same effect as wrapping the declaration in a standard ```using(...){ }``` syntax, with the lifetime of the wrapped declaration being the same as that of the enclosing block.

_TODO_