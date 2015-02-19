Static Type Constraints
=======================

The Roslyn C# compiler does not implement all of the required constraints around static types as required by the C# language specification.

The native compiler gave an error when a type argument was inferred to be a static type, for example in a call like

```cs
M(default(StaticType))
```

Now this was only possible because the native compiler does not enforce the restriction that you can’t use a static type in a default expression. That is the first bug (#344) and won’t be fixed in Roslyn due to compatibility.

Now that the inferred type argument is a static type, the native compiler gave an error for that. While the language spec says that it is illegal to use a static type as a generic type argument, it isn’t clear if that was intended to apply to inferred type arguments. In any case, the native compiler would give an error in the above call due to the (inferred) type parameter being a static type.

Roslyn only implemented the constraint when binding explicit type arguments, so Roslyn didn’t diagnose this. This was bug the second bug (#345). I “fixed” that in Roslyn by moving the check from the place where we bind the type argument to the place where we check type arguments against the type parameter constraints.

Now as a separate matter the native compiler did not give an error when a static type was used as a type argument in a dynamic call, while Roslyn would give an error:

```cs
((dynamic)3).M<StaticType>()
```

This is the third bug (#511), and while Roslyn relies on the spec for its behavior, it is a breaking change from the native compiler. But as it turns out, this compat issue is fixed by the fix described above, because the type argument in a dynamic call is not checked against any type parameter constraints. So Roslyn now fails to diagnose this situation (i.e. doesn’t implement the required language rules) but at least it is compatible with the native compiler here.

I’m on a horse.
