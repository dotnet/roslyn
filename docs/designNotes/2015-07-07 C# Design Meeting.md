# C# Design Notes for Jul 7 2015

Discussion for these notes can be found at https://github.com/dotnet/roslyn/issues/5031.

Quotes of the day:

> "I don't think I've had a Quote of the Day for years <sigh>" 
> "What you just described is awful, so we can't go there!"

## Agenda

With Eric Lippert from Coverity as an honored guest, we looked further at the nullability feature.

1. Adding new warnings
2. Generics and nullability

# New warnings

The very point of this feature is to give new warnings about existing bugs. What's a reasonable experience here? Ideally folks will always be happy to be told more about the quality of the code. Of course there needs to be super straightforward ways of silencing warnings for people who just need to continue to build, especially when they treat warnings as errors (since those would be breaking). We've previously been burned by providing new warnings that then broke people.

There's a problem when those warnings are false positives, which though hopefully rare, is going to happen: I checked for null in a way the compiler doesn't recognize. Can I build an analyzer to turn warnings *off* if it knows better? Anti-warnings? That's a more general analyzer design question.

Coverity for instance have very advanced analysis to weed out false positives, and avoid the analysis becoming noisy.

# Generics

There's a proposal to treat generics in the following way: Both nullable and nonnullable types are allowed as type arguments. Nullability flows with the type. Type inference propagates nullability - if any input type is nullable the inferred type will be, too.

Constraints can be nullable or nonnullable. Any nonnullable constraints mean that only non-nullable reference types can satisfy them (without warning). Unconstrained generics is reinterpreted to mean constrained by `object?`, in order to continue to allow all safe types as type arguments. If a type parameter `T` is constrained to be nonnullable, `T?` can be used without warning.

Thus:  

``` c#
class C<T> where T : Animal? 
{
  public T x;
  public T? y; // warning
}
class D<T> where T : Animal 
{
  public T x;
  public T? y;
}

C<Giraffe>
C<Giraffe?>

D<Squirrel>
D<Squirrel?> // warning
```

Inside of generics, type parameters are always expected to possibly be nonnullable. Therefore assigning null or nullable values to them always yields a warning (except probably when they are from the *same* possibly nullable type parameter!).

This is nice and consistent. Unfortunately it isn't quite expressive enough. There are cases such as `FirstOrDefault` where we'd really want to return "the nullable version of T if it isn't already nullable". Assume an operator "`^`" (that shouldn't *actually* be the syntax) to mean take the nullable of any nonnullable reference type, leave it alone otherwise:

``` c#
public static T^ FirstOrDefault<T>(this IEnumerable<T> source);

var a = new string[] { "a", "b", "c" };
var b = new string[] {};

var couldBeNull = condition ? a.FirstOrDefault() : b.FirstOrDefault(); // string?
```

We would need to decide what the syntax for `^` *actually* is if we want to have that expressiveness.

`T^` can also serve the purpose of helping null-check code *inside* of a generic method or type. A `List<T>` type for instance could declare its storage array to be `T^[]` instead of `T[]` so that it warns you where you use it that the array elements could be null.

`T?` cannot exist when T is not constrained to either struct or class, because it means different things in the two cases, and we can't code gen across them.

Cast doesn't do null check, just suppresses warning.
