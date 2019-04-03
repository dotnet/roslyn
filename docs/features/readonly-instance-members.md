# Readonly Instance Members

Championed Issue: <https://github.com/dotnet/csharplang/issues/1710>

## Summary
[summary]: #summary

Provide a way to specify individual instance members on a struct do not modify state, in the same way that `readonly struct` specifies no instance members modify state.

It is worth noting that `readonly instance member` != `pure instance member`. A `pure` instance member guarantees no state will be modified. A `readonly` instance member only guarantees that instance state will not be modified.

All instance members on a `readonly struct` are implicitly `readonly instance members`. Explicit `readonly instance members` declared on non-readonly structs would behave in the same manner. For example, they would still create hidden copies if you called an instance member (on the current instance or on a field of the instance) which was itself not-readonly.

## Design
[design]: #design

Allow a user to specify that an instance member is, itself, `readonly` and does not modify the state of the instance (with all the appropriate verification done by the compiler, of course). For example:

```csharp
public struct Vector2
{
    public float x;
    public float y;

    public readonly float GetLengthReadonly()
    {
        return MathF.Sqrt(LengthSquared);
    }

    public float GetLength()
    {
        return MathF.Sqrt(LengthSquared);
    }

    public readonly float GetLengthIllegal()
    {
        var tmp = MathF.Sqrt(LengthSquared);

        x = tmp;    // Compiler error, cannot write x
        y = tmp;    // Compiler error, cannot write y

        return tmp;
    }

    public float LengthSquared
    {
        readonly get
        {
            return (x * x) +
                   (y * y);
        }
    }
}

public static class MyClass
{
    public static float ExistingBehavior(in Vector2 vector)
    {
        // This code causes a hidden copy, the compiler effectively emits:
        //    var tmpVector = vector;
        //    return tmpVector.GetLength();
        //
        // This is done because the compiler doesn't know that `GetLength()`
        // won't mutate `vector`.

        return vector.GetLength();
    }

    public static float ReadonlyBehavior(in Vector2 vector)
    {
        // This code is emitted exactly as listed. There are no hidden
        // copies as the `readonly` modifier indicates that the method
        // won't mutate `vector`.

        return vector.GetLengthReadonly();
    }
}
```

Readonly can be applied to property accessors to indicate that `this` will not be mutated in the accessor.

```csharp
public int Prop
{
    readonly get
    {
        return this._prop1;
    }
}
```

When `readonly` is applied to the property syntax, it means that all accessors are `readonly`.

```csharp
public readonly int Prop
{
    get
    {
        return this._store["Prop2"];
    }
    set
    {
        this._store["Prop2"] = value;
    }
}
```

Similar to the rules for property accessibility modifiers, redundant `readonly` modifiers are not allowed on properties.

```csharp
public readonly int Prop1 { readonly get => 42; } // Not allowed
public int Prop2 { readonly get => this._store["Prop2"]; readonly set => this._store["Prop2"]; } // Not allowed
```

Readonly can only be applied to accessors which do not mutate the containing type.

```csharp
public int Prop
{
    readonly get
    {
        return this._prop3;
    }
    set
    {
        this._prop3 = value;
    }
}
```

### Auto-properties
Readonly can be applied to auto-implemented properties or `get` accessors. However, the compiler will treat all auto-implemented getters as readonly regardless of whether a `readonly` modifier is present.

```csharp
// Allowed
public readonly int Prop1 { get; }
public int Prop2 { readonly get; }
public int Prop3 { readonly get; set; }

// Not allowed
public readonly int Prop4 { get; set; }
public int Prop5 { get; readonly set; }
```

### Events
Readonly can be applied to manually-implemented events, but not field-like events. Readonly cannot be applied to individual event accessors (add/remove).

```csharp
// Allowed
public readonly event Action<EventArgs> Event1
{
    add { }
    remove { }
}

// Not allowed
public readonly event Action<EventArgs> Event2;
public event Action<EventArgs> Event3
{
    readonly add { }
    remove { }
}
public static readonly event Event4
{
    add { }
    remove { }
}
```

Some other syntax examples:

* Expression bodied members: `public readonly float ExpressionBodiedMember => (x * x) + (y * y);`
* Generic constraints: `public readonly void GenericMethod<T>(T value) where T : struct { }`

The compiler would emit the instance member, as usual, and would additionally emit a compiler recognized attribute indicating that the instance member does not modify state. This effectively causes the hidden `this` parameter to become `in T` instead of `ref T`.

This would allow the user to safely call said instance method without the compiler needing to make a copy.

Some more "edge" cases that are explicitly permitted:

* Explicit interface implementations are allowed to be `readonly`.
* Partial methods are allowed to be `readonly`. Both signatures or neither must have the `readonly` keyword.

The restrictions would include:

* The `readonly` modifier cannot be applied to static methods, constructors or destructors.
* The `readonly` modifier cannot be applied to delegates.
* The `readonly` modifier cannot be applied to members of class or interface.

## Compiler API

The following public API will be added:

- `bool IMethodSymbol.IsDeclaredReadOnly { get; }` indicates that the member is readonly due to having a readonly keyword, or due to being an auto-implemented instance getter on a struct.
- `bool IMethodSymbol.IsEffectivelyReadOnly { get; }` indicates that the member is readonly due to IsDeclaredReadOnly, or by the containing type being readonly, except if this method is a constructor.

It may be necessary to add additional public API to properties and events. This poses a challenge for properties because `ReadOnly` properties have an existing, different meaning in VB.NET and the `IMethodSymbol.IsReadOnly` API has already shipped to describe that scenario. This specific issue is tracked in https://github.com/dotnet/roslyn/issues/34213.
