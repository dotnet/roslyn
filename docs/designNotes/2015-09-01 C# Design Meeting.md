C# Design Meeting Sep 1 2015
============================

Discussion for these notes can be found at https://github.com/dotnet/roslyn/issues/5233.

Agenda
------

The meeting focused on design decisions for prototypes. There's no commitment for these decisions to stick through to a final feature; on the contrary the prototypes are being made in order to learn new things and improve the design.

1. ref returns and ref locals
2. pattern matching


Ref returns and ref locals
==========================

Proposal in #118, Initial PR for prototype in #4042.

This feature is a generalization of ref parameters, and as such doesn't have much new, either conceptually or in how you work with it. As is already the case, refs cannot be null (they always reference a memory location), and you access the location they point to without explicitly dereferencing. The new thing is that you can return refs from methods and other functions, and that you can have locals that are refs to other memory locations.

There's a new notion of "safe-to-return". It would be bad if you could return a ref to your own stack frame, for instance. Therefore there's a simple analysis to always track if a ref is "safe-to-return" - and a compile time error if you return something that isn't.

A given method can get refs from a number of places:

1. refs to locals in this method
2. ref parameters passed to it
3. refs into heap objects
4. refs returned from calls
5. ref locals in this method

The 1st are never safe to return; the 2nd and 3rd always are. The 4th are safe to return if every ref passed *into* the call is safe to return - since we then know that the returned ref cannot be to a local of the calling (or called) method.

The 5th is more involved, as it depends on which semantics we adopt for ref locals.

Ref locals
----------

There are a couple of different questions to consider for ref locals:

1. Can they be reassigned or are they fixed to a location at declaration time (like ref parameters)?
2. When are they safe to return? Always? Never? Depends?

If ref locals can be reassignable *and* unsafe-to-return, that means that we have to worry about lifetime analysis:

``` c#
ref int r;
while (...)
{
  int x = ...;
  if (...) ref r = ref x;
}
WriteLine(r); // What now?
```

In other words we have to concern ourselves with the situation where locals are assigned to a ref that is longer lived than the local itself. We would have to detect it and either forbid it or resort to expensive techniques (similar to variable capture by lambdas) that  run counter to the perf you were probably hoping for by using refs in the first place.

On the other hand, if ref locals cannot be assigned after initialization, there will be no opportunity to assign variables to them that live in a more nested, and hence shorter-lived, scope. And if they are required to be safe-to-return, then *no* locals can be assigned to them, regardless of lifetime. Either of these seem more attractive!

We think that not being able to assign locals to ref locals is too limiting. For instance, it would be common to keep a struct in a local, and then call a function with a ref to it to locate and return a ref to some data nested in it. You'd want to store the result of that in a ref local:

``` c#
Node n = ...; struct
ref Node l = ref GetLeftMostSubNode(ref n);
l.Contents = ...; // modify leftmost node
```

So we definitely want to allow ref locals that are non-reassignable and unsafe-to-return.

However, it is also reasonable to want to call similar helper methods on refs that *are* safe to return, to store the results and to then return them:

``` c#
ref Node M(ref Node n)
{
  if (...)
  {
    ref Node l = ref GetLeftMostSubNode(ref n);
    l.Contents = ...; // modify leftmost node
    return ref l;
  }
  ...
}
```

It seems that we want ref locals to be safe-to-return or not *depending on what they are initialized with*.

We think that there is also some value in having reassignable ref locals. For instance, you can imagine a ref local being the current "pointer" in a loop over a struct array.

We can imagine a rule that simply says that ref locals are reassignable *except if they are initialized with an unsafe-to-return ref*. Then, only safe-to-return refs can be assigned to the reassignable ones, and we still don't have lifetime tracking issues.

On closer scrutiny, though, this scenario does raise harder issues. If you use a ref as a current pointer, what's its initial value? Do we need null refs in the language now? How do you increment it? Can it point "past the edge" at the end?

These are questions we don't want to answer right now for a somewhat hypothetical scenario.

**Conclusion**: for the propotype, ref locals are non-reassignable, and get their "safe-to-return" status from their mandatory ref initializer.

If there are scenarios not covered by this, we'll discover as folks start using the prototype and tell us.

By avoiding reassignment we avoid answering a couple of difficult questions:
* What's the syntax of a ref assignment? `ref x = y`? `ref x = ref y`? `x = ref y`?
* Are ref assignments expressions, and what does that mean?
* Null/default values or definite assignment analysis to prevent the need for them

`this` in structs
-----------------

For non-readonly structs, `this` is generally described as being like a ref parameter. However, considering it safe-to-return like ref parameters in struct methods leads to undesirable behavior: It lets a ref to `this` be returned from the method. Therefore a caller needs to consider a ref returned from a method on a struct local *not* safe to return (the returned ref could be the struct local itself or part of it). In essence, the ref `this` "contaminates" any ref-returning method called on a local value type.

Scenarios where this is a problem include using structs as wrapper types for heap objects.

Somewhat counterintuitively perhaps, the better rule is to consider `this` in struct methods unsafe-to-return. Since struct methods will never return a ref to the receiver, you can call them with safe-to-return ref parameters and get safe-to-return ref results.

This also solves a problem with generics, where you don't *know* if a receiver is going to be a struct. Now you don't have to program defensively against that; ref returning methods called on *anything* will be independent of their receiver with regards to safe-to-return determination.  

Other issues
------------

* We may want an unsafe language-level feature to convert between refs and pointers. We'll look at that later.
* PEVerify considers ref return unsafe. We need to chase that down, but for the prototype it's fine.
* It'd be useful to have a notion of "readonly refs" - refs through which you cannot write to the referenced storage. This is a separate (big) topic for later.


Pattern matching
================

Proposal in #206, initial PR in #4882.

Syntax: There are patterns, extended is-expressions, and (later) switch.
``` c#
pattern:
  *
  type identifier // scope is a separate issue
  type { id is pattern, ... } [identifier]
  type (pattern, ...)
  constant-expression

is-expression:
  relational-expression is pattern
```

For constants, we would have integral constants be flexible between types. Floats probably not. This has tons of levers, that we can obsess over, but in the prototype this is what we'll go with.

Relational operators in patterns? `e is > 3`? Maybe later.

For VB probably more issues. Save for later.

Scope for variables: in scope in nearest enclosing statement, *except* the else-block of an if-statement. This is a weird but useful exception that allows subsequent else-if's to introduce variables with the same name as previous ones in the conditions.

FOr now, the introduced variables are not reassignable. This could have both positive and negative perf impact. It's an opportunity to do "the right thing", but also a deviation from what we do with other variables. We'll try it in the prototype and see what it feels like.

Deconstruction and guards are interesting further topics that we don't want to pursue yet.

Record types are not part of this prototype.

Some concerns about the active pattern part being too weird or different. Best resolved by prototyping it and playing with it.

