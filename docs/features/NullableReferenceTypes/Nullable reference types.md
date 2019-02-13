# Proposal: Static null checking in C&num;

Null reference exceptions are rampant in languages like C\#, where any reference type can reference a null value. Some type systems separate types into a `T` that cannot be null and an `Option<T>` (or similar) that can be null but cannot be dereferenced without an explicit null check that unpacks the non-null value.

This approach is attractive, but is difficult to add to a language where every type has a default value. For instance, a newly created array will contain all nulls. Also, such systems are notorious for problems around initialization and cyclic data structures, unless non-null types are permitted to at least temporarily contain null.

On top of these issues come challenges stemming from the fact that C\# already has null-unsafe types, that allow both null values and dereferencing. How can a safer approach be added to the language without breaking changes, without leaving the language more complex than necessary and with a natural experience for hardening existing code against null errors in your own time?

# Approach: Nullable reference types plus nullability warnings

The approach suggested here consists of a couple of elements:

* Add a notion of safe nullable reference types `T?` to the language, in addition to the current ones `T` which we will now call non-nullable.
* Detect and warn on cases where nullable types are dereferenced, or when null is values are assigned to (or left in) non-nullable variables.
* Offer opt-in means to deal with breaking behavior and compat across versions and assemblies.

The feature will not provide airtight guarantees, but should help find most nullability errors in code. It can be assumed that most values are intended *not* to be null, so this scheme provides the minimal annotation overhead. 

Nullable reference types are helpful, in that they help find code that may dereference null and help guard it with null checks. Non-nullability warnings are helpful in that they help prevent variables from inadvertently containing an null value.

# Nullable and non-nullable reference types

For every non-nullable reference type `T`, there is now a corresponding nullable reference type `T?`. 

Syntactically speaking, this doesn't add much, since nullable value types have the same syntax. However, a few syntactic corner cases are new, like `T[]?`.

From a semantic viewpoint, `T` and `T?` are equivalent in every way: they are the same type. The *only* way in which they differ is in the warnings caused by their use. 

For type inference purposes `T` and `T?` are considered the same type. If a reference type `T` is the result of type inference, and at least one of the candidate reference types is nullable, then the result is nullable too.

If an expression is by declaration of type `T?`, it will still be considered to be of type `T` if it occurs in a context where by flow analysis we consider it known that it is not null. 

## Warnings for nullable reference types

Values of nullable reference types should not be used in a connection where a) they are not known to (probably) contain a non-null value and b) the use would require them to be non-null. Such uses will be flagged with a warning.

a) means that a flow analysis has determined that they are very likely to not be null. There will be specific rules for this flow analysis, similar to those for definite assignment. It is an open question which variables are tracked by this analysis. Just locals and parameters? All dotted names?

b) means dereferencing (e.g. with dot or invocation) or implicitly converting to a non-nullable reference type.

## Warnings for non-nullable reference types

Variables of non-nullable reference types should not be assigned the literal `null` or `default(T)`; nor should nullable value types be boxed to them.  Such uses will result in a warning. 

Additionally, fields of non-nullable reference type must be protected by their constructor so that they are a) not used before they are assigned, and b) assigned before the constructor returns. Otherwise a warning is issued. (As an alternative to (a) we can consider allowing use before assignment, but in that case treating the variable as nullable.) 

Note that there is no warning to prevent new arrays of non-nullable reference type from keeping the null elements they are initially created with. There is no good static way to ensure this. We *could* consider a requirement that *something* must be done to such an array before it can be read from, assigned or returned; e.g. there must be at least one element assignment to it, or it must be passed as an argument to something that could potentially initialize it. That would at least catch the situation where you simply forgot to initialize it. But it is doubtful that this has much value.

# Generics

Constraints can be both nullable and non-nullable. The default constraint for an unconstrained type parameter is `object?`.

A warning is issued if a type parameter with at least one non-nullable reference constraint is instantiated with a nullable reference type.

A type parameter with at least one non-nullable constraint is treated as a non-nullable type in terms of warnings given.

A type parameter with no non-nullable reference constraints is treated as *both* a nullable *and* a non-nullable reference type in terms of warnings given (since it could without warning have been instantiated with either). This means that *both* sets of warnings apply.

`?` is allowed to be applied to any type parameter `T`. For type parameters with the `struct` constraint it has the usual meaning. For all other type parameters it has this meaning, where `S` is the type with which `T` is instantiated:

* If `S` is a non-nullable reference type then `T?` refers to `S?`
* Otherwise, `S?` refers to `S`

Note: This rule is not elegant - in particular it is bad that the introduction of a `struct` constraint changes the meaning of `?`. But we believe we need it to faithfully express the type of common APIs such as `FirstOrDefault()`.

## Opting in and opting out

Some of the nullability warnings warn on code that exists without warnings today. There should be a way of opting out of those nullability warnings for compatibility purposes.

When opting *in*, assemblies generated should contain an assembly-level attribute with the purpose of signalling that nullable and non-nullable types in signatures should generate appropriate warnings in consuming code.

When consuming code references an assembly that does *not* have such a top-level attribute, the types in that assembly should be treated as *neither* nullable *nor* non-nullable. That is, neither set of warnings should apply to those types.

This mechanism exists such that code that was not written to work with nullability warnings, e.g. code from a previous version of C\#, does indeed not trigger such warnings. Only assemblies that opt in by having the compiler-produced attribute, will cause the nullability warnings to happen in consuming code accessing their signatures.

When warnings haven't been opted in to, the compiler should give some indication that there are likely bugs one would find by opting in. For instance, it could give (as an informational message, not a warning) a count of how many nullability warnings it would have given.

Even when a library has opted in, consuming code may be written with an earlier version of C\#, and may not recognize the nullability annotations. Such code will work without warning. To facilitate smooth upgrade of the consuming code, it should probably be possible to opt out of the warnings from *a given* library that will now start to occur. Again, such per-assebly opt-out could be accompanied by an informational message reminding that nullability bugs may be going unnoticed.

# Libraries and compatibility

An example: In my C\# client code, I use libraries A and B:

``` c#
// library A
public class A
{
  public static string M(string s1, string s2);
}

// library B
public class B
{
  public static object N(object o1, object o2);
}

// client C, referencing A and B
Console.WriteLine(A.M("a", null).Length);
Console.WriteLine(B.N("b", null).ToString());
```

Now library B upgrades to C\# 7, and starts using nullability annotations:

``` c#
// upgraded library B
public class B
{
  public static object? N(object o1, object o2); // o1 and o2 not supposed to be null
}

```

It is clear that my client code probably has a bug: apparently it was not supposed to pass null to B.N. However, the C\# 6 compiler knows nothing of all this, and ignores the assembly-level attribute opting in to it.

Now I upgrade to C\# 7 and start getting warnings on my call to B.N: the second argument shouldn't be null, and I shouldn't dot into the return value without checking it for null. It may not be convenient for me to look at those potential bugs right now; I just want a painless upgrade. So I can opt out of getting nullability warnings at all, or for that specific assembly. On compile, I am informed that I may have nullability bugs, so I don't forget to turn it on later.

Eventually I do, I get my warnings and I fix my bugs:

``` c#
Console.WriteLine(B.N("b", "")?.ToString());
```

Passing the empty string instead of null, and using the null-conditional operator to test the result for null.

Now the owner of library A decides to add nullability annotations:

``` c#
// library A
public class A
{
  public static string? M(string s1, string s2); // s1 and s2 shouldn't be null
}
```

As I compile against this new version, I get new nullability warnings in my code. Again, I may not be ready for this - I may have upgraded to the new version of the library just for bug fixes - and I may temporarily opt out for that assembly.

In my own time, I opt it in, get my warnings and fix my code. I am now completely in the new world. During the whole process I never got "broken" unless I asked for it with some explicit gesture (upgrade compiler or libraries), and was able to opt out if I wasn't ready. When I did opt in, the warnings told me that I used a library against its intentions, so fixing those places probably addressed a bug.