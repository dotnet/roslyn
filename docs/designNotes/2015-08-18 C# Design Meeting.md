# C# Design Notes for Aug 18, 2015

Discussion for these notes can be found at https://github.com/dotnet/roslyn/issues/5033.

## Agenda

A summary of the design we (roughly) landed on in #5031 was put out on GitHub as #5032, and this meeting further discussed it.

1. Array creation
2. Null checking operator
3. Generics

# Array creation with non-nullable types

For array creation there is the question whether to allow (big hole) or disallow (big nuisance?) on non-nullable referance types. We'll leave it at allow for now, but may reconsider.

# Null checking operator

Casting to a non-nullable reference type would not, and should not, do a runtime null check. Should we, however, have an *operator* for checking null, throwing if the value *is* null, resulting in the non-null value if it isn't?

This seems like a good idea. The operator is postfix `!`, and it should in fact apply to values of nullable *value* types as well as reference types. It "upgrades" the value to non-nullable, by throwing if it is null.

``` c#
if (person.Kind == Student) 
{
  List<Course> courses = person.Courses!; // I know it's not null for a student, but the compiler doesn't.
  ...
}
```

The `!` operator naturally leads to `x!.y`, which is great! Although `!.` is two operators, it will feel as a cousin of `?.` (which is one operator). While the latter is conditional on null, the former just plows through. Naively, it implies two redundant null checks, one by `!` and one by `.`, but we'll optimize that of course. 

``` c#
if (person.Kind == Student) 
{
  var passed = !person.Courses!.Any(c => c.Grade == F);
  ...
}
```

Technically this would allow `x!?.y`, which comes quite close to swearing. We should consider warning when you use `?.` on non-null things.

VB may have a problem with post-fix `!`. We'll cross that bridge when we get there.


# Generics and nullability

Is it too heavyhanded to require `?` on constraints to allow nullable type arguments?

Often, when you have a constraint it is because you want to operate on instances. So it's probably good that the default is not nullable.

It may feel a bit egregious to require it on *all* the constraints of a type parameter, though. Should we put any `?`'s on the type parameter declaration instead of in the constraints? No, that is too weird and different. The case of multiple nullable constraints is probably sufficiently rare that it is reasonable to ask folks to put a `?` on each. In fact we should disallow having `?` on only some, since those question marks won't have an effect: they'll be cancelled by the non-nullable fellow constraints.

The proposal talks about allowing `?` on the use of type parameters to explicitly override their nullness. Maybe we should have an explicit `!` as well, to explicitly override in the other direction: non-nullable. Think for instance of a `FirstNonNull` method.

``` c#
T! FirstNonNull<T>(IList<T> list) { ... }
T? FirstOrDefault<T>(IList<T> list) { ... }
```

This means complexity slowly creeps into the proposal, thanks to generics. However, it seems those overrides are relatively rare, yet really useful when you need them. 

`T!` would only be allowed on type parameters, and only when they are not already non-null by constraint. 


