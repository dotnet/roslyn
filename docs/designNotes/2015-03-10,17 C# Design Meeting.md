C# Design Meeting Notes for Mar 10 and 17, 2015
===============================================

Discussion thread for these notes can be found at https://github.com/dotnet/roslyn/issues/1648.

Agenda
------

These two meetings looked exclusively at nullable/non-nullable reference types. I've written them up together to add more of the clarity of insight we had when the meetings were over, rather than represent the circuitous path we took to get there.

1. Nullable and non-nullable reference types
2. Opt-in diagnostics
3. Representation
4. Potentially useful rules
5. Safely dereferencing nullable reference types
6. Generating null checks
1. Nullable and non-nullable reference types
============================================

The core features on the table are nullable and non-nullable reference types, as in `string?` and `string!` respectively. We might do one or both (or neither of course).

The value of these annotations would be to allow a developer to express intent, and to get errors or warnings when working against that intent.



2. Opt-in diagnostics
=====================

However, depending on various design and implementation choices, some of these diagnostics would be a breaking change to add. In order to get the full value of the new feature but retain backward compatibility, we therefore probably need to allow the enforcement of some or most of these diagnostics to be *opt-in*. That is certainly an uncomfortable concept, and adding switches to the language changing its meaning is not something we have much of an appetite for. 

However, there are other ways of making diagnostics opt-in. We now have an infrastructure for custom analyzers (built on the Roslyn infrastructure). In principle, some or all of the diagnostics gained from using the nullability annotations could be custom diagnostics that you'd have to switch on.

The downside of opt-in diagnostics is that we can forget any pretense to guarantees around nullability. The feature would help you find more errors, and maybe guide you in VS, but you wouldn't be able to automatically trust a `string!` to not be null.

There's an important upside though, in that it would allow you to gradually strengthen your code to nullability checks, one project at a time.



3. Representation
================= 

The representation of the annotations in metadata is a key decision point, because it affects the number of diagnostics that can be added to the language itself without it being a breaking change. There are essentially four options:

1. Attributes: We'd have `string?` be represented as `string` plus an attribute saying it's nullable. This is similar to how we represent `dynamic` today, and for generic types etc. we'd use similar tricks to what we do for `dynamic` today.

2. Wrapper structs: There'd be struct types `NullableRef<T>` and `NonNullableRef<T>` or something like that. The structs would have a single field containing the actual reference.

3. Modreq's: These are annotations in metadata that cause an error from compilers that don't know about them. 

4. New expressiveness in IL: Something specific to denote these that only a new compiler can even read.

We can probably dispense with 3 and 4. We've never used modreq's before, and who knows how existing compilers (of all .NET languages!) will react to them. Besides, they cannot be used on type arguments, so they don't have the right expressiveness. A truly new metadata annotation has similar problems with existing compilers, and also seems like overkill. 

Options 1 and 2 are interesting because they both have meaning to existing compilers.

Say a library written in C# 7 offers this method:

``` c#
public class C
{
    string? M(string! s) { ... }
}
```

With option 1, this would compile down to something like this:

``` c#
public class C
{
    [Nullable] string M([NonNullable] string s) { ... }
}
```

A consuming program in C# 6 would not be constrained by those attributes, because the C# 6 compiler does not know about them. So this would be totally fine:

``` c#
var l = C.M(null).Length;
```

Unfortunately, if something is fine in C# 6 it has to also be fine in C# 7. So C# 7 cannot have rules to prevent passing null to a nonnullable reference type, or prevent dereferencing a nullable reference type!

That's obviously a pretty toothless - and hence useless - version of the nullability feature in and of itself, given that the value was supposed to be in getting diagnostics to prevent null reference exceptions! This is where the opt-in possibility comes in. Essentially, if we use an attribute encoding, we need all the diagnostics that make nullability annotations useful be opt-in, e.g. as custom diagnostics.

With option 2, the library would compile down to this:

``` c#
public class C
{
    NullableRef<string> M(NonNullableRef<string> s) { ... }
}
```

Now the C# 6 program above would not compile. The C# 6 compiler would see structs that can't be null and don't have a Length. Whatever members those structs *do* have, though, would be accessible, so C# 7 would still have to accept using them as structs. (We could mitigate this by not giving the structs any public members).

For the most part, this approach would make the C# 6 program able to do so little with the API that C# 7, instead of adding *restrictions*, can allow *more* things than C# 6.

There are exceptions, though. For instance, casting any returned such struct to `object` would box it in C# 6, whereas presumably the desired behavior in C# 7 would be to unwrap it. This is exactly where the CLR today has special behavior, boxing nullable value types by first unwrapping to the underlying type if possible.

Also, having these single-field structs everywhere is likely going to have an impact on runtime performance, even if the JIT can optimize many of them away.

Probably the most damning objection to the wrapper structs is probably the degree to which they would hamper interoperation between the different variations of a type. For instance, the conversion from `string!` to `string` and on to `string?` wouldn't be a reference conversion at runtime. Hence, `IEnumerable<string!>` wouldn't convert to `IEnumerable<string>`, despite covariance. 

We are currently leaning strongly in the direction of an attribute-based representation, which means that there needs to be an opt-in mechanism for enforcement of the useful rules to kick in.



4. Potentially useful rules to enforce
======================================

**Don't dereference `C?`**: you must check for null or assert that the value is not null.

**Don't pass `null`, `C` or `C?` to `C!`:** you must check for null or assert that the value is not null.

**Don't leave `C!` fields unassigned:** require definite assignment at the end of the constructor. (Doesn't prevent observing null during initialization)

**Avoid `default(C!)`:** it would be null!

**Don't instantiate `C![]`:** it's elements would be null. This seems like a draconian restriction - as long as you only ever read fields from the array that were previously written, no-one would observe the default value. Many data structures wrapping arrays observe this discipline.

**Don't instantiate `G<C!>`:** this is because the above rules aren't currently enforced on even unconstrained type parameters, so they could be circumvented in generic types and methods. Again, this restriction seems draconian. No existing generic types could be used on nonnullable reference types. Maybe the generic types could opt in?

**Don't null-check `C!`:** oftentimes using e.g. `?.` on something that's already non-nullable is redundant. However, since non-nullable reference types *can* be null, maybe flagging such checks is not always so helpful?
  
We very much understand that these rules can't be perfect. The trade-off needs to be between adding value and allowing continuity with existing code.



5. Safely dereferencing nullable reference types
================================================

For nullable reference types, the main useful error would come from dereferencing the value without checking for null. That would often be in the shape of the null-conditional operator:

``` c#
string? s = ...;
var l = s?.Length;
var c = s?[3];
```

However, just as often you'd want the null test to guard a block of code, wherein dereferencing is safe. An obvious candidate is to use pattern matching:

``` c#
string? ns = ...;
if (ns is string! s) // introduces non-null variable s
{
    var l = s.Length;
    var c = s[3];
}
```

It is somewhat annoying to have to introduce a new variable name. However, in real code the expression being tested (`ns` in the above example) is more likely to be a more complex expression, not just a local variable. Or rather, the `is` expression is how you'd get a local variable for it in the first place.

More annoying is having to state the type again in `ns is string! s`. We should think of some shorthand, like `ns is ! s` or `ns is var s` or something else.

Whatever syntax we come up with here would be equally useful to nullable *value* types.



6. Generating null checks for parameters
========================================

There'd be no guarantees that a `string!` parameter actually isn't going to be null. Most public API's would probably still want to check arguments for null at runtime. Should we help with that by automatically generating null checks for `C!` parameters?

Every generated null check is performance overhead and IL bloat. So this may be a bad idea to do on every parameter with a non-nullable reference type. But we could have the user more compactly indicate the desire to do so. As a complete strawman syntax:

``` c#
public void M(string!! s) { ... }
```
Where the double `!!` means the type is non-nullable *and* a runtime check should be generated.

If we choose to also do contracts (#119), it would be natural for this feature to simply be a shorthand for a null-checking `requires` contract.
