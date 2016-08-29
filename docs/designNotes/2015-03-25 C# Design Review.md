C# Language Design Review, Mar 25, 2015
=======================================

Discussion thread for these notes can be found at https://github.com/dotnet/roslyn/issues/1921.

We've recently changed gears a little on the C# design team. In order to keep a high design velocity, part of the design team meets one or two times each week to do detailed design work. Roughly monthly the full design team gets together to review and discuss the direction. This was the first such review.


Agenda
------

1. Overall direction
2. Nullability features
3. Performance and reliability features
4. Tuples
5. Records
6. Pattern matching
1. Overall direction
====================

In these first two months of design on C# 7 we've adopted a mix of deep dive and breadth scouring. There's agreement that we should be ambitious and try to solve hard problems, being willing to throw the result away if it's not up to snuff. We should keep an open mind for a while still, and not lock down too soon on a specific feature set or specific designs.

 

2. Nullability features
=======================

Non-nullable types are the number one request on UserVoice. We take it that the *underlying problem* is trying to avoid null reference exceptions. Non-nullable types are at best only part of the solution to this. We'd also need to help prevent access when something is *nullable*.

We've looked at this over a couple of design meetings (#1303, #1648). Ideally we could introduce non-null types, such as `string!` that are guaranteed never to be null. However, the problems around initialization of fields and arrays, etc., simply run too deep. We can never get to full guarantees.

We've been mostly looking at implementation approaches that use type erasure, and that seems like a promising approach. 

However, the thing we need to focus on more is this: when you get a nullability warning from this new feature, how do you satisfy the compiler? If you need to use unfamiliar new language features or significant extra syntax to do so, it probably detracts from the feature.

Instead we should at least consider an flow-based approach, where the "null-state" of a variable is tracked based on tests, assignments etc. 

``` c#
if (x == null) return;
// not null here
```

It's an open question how far we would go. Would we track only locals and parameters, or would we also keep track of fields?

``` c#
if (foo.x == null) ...
```

This is more problematic, not just because of the risk of other threads changing the field, but also because other code may have side effects on the field or property.

TypeScript uses information about type guards to track union types in if branches, but it's not full flow analysis, and works only for local variables. Google Closure is more heuristics based, and is happy to track e.g. `foo.bar.baz` style patterns.

A core nuisance with nullability checking is that it raises a wealth of compat questions that limit the design in different ways. There may need to be some sort of opt-in to at least some of the diagnostics you'd get, since you wouldn't want them if you were just recompiling old code that used to "work".



3. Performance and reliability
==============================

The list produced at a recent design meeting (#1898) looks sensible.

val / readonly
--------------

We should cross check with Scala on their syntax.


ref return / locals
-------------------

Lots of compexitity - we question whether it is worth the cost?


Never type
----------

The type system approach is interesting, and allows throw expressions.

Method contracts
----------------

`requires` / `ensures`, show up in docs, etc. This looks great. The biggest question is what happens on failure: exceptions? fail fast?


Slices
------

We got strong feedback that array slices are only interesting if we unify them with arrays. Otherwise there's yet another bifurcation of the world. There's *some* value to have a `Slice<T>` struct type just show up in the Framework. But it really doesn't seem worth it unless it's a runtime feature. That unification is really hard to achieve, and would require CLR support. It's valuable enough to try to pursue even with high likelihood of failure.

Slicing as a language syntax could also be "overloadable" - on IEnumerables for instance.

In Go, if you treat a slice as an object, it gets boxed. 


Lambda capture lists
--------------------

Not interesting as a feature, but the idea of allowing attributes on lambdas might fly. 


Immutable types
---------------

General concern that this doesn't go far enough, is lying to folks, etc. It tries to have strong guarantees that we can't make.

But a lot of people would appreciate *something* here, so the scenario of immutability should continue to motivate us.


Destructible types
------------------

The scenario is good, not the current proposal.



4. Tuple types
==============

There's agreement on wanting the feature and on the syntax (#347, #1207).

We probably prefer a value type version of Tuple<T>. Of course those would be subject to tearing, like all structs. We're willing to be swayed.

There are performance trade offs around allocation vs copying, and also around generic instantiation. We could do some experiments in F# source code, which already has tuples.



5. Records
==========

See #180, #206, #396, #1303, #1572.

In the current proposal, we should just give up on the ability to name constructor parameters and members differently. The motivation was to be able to upgrade where parameter names start with lower case and member names upper case, but it's not worth the complexity.

Should it have `==` and `!=` that are value based? Clashes a little with the ability to make them mutable.

If I introduce extra state, then I have to write my own GetHashCode. That seems unfortunate.

All the gunk today is part of why Roslyn uses XML to generate its data structure. A test of success would be for the Roslyn syntax trees to be concise to write in source code.

A big issue here is incremental non-destructive modification. Roslyn follows the pattern of "Withers", a method for each property that takes a new value for that property and returns a new object that's a copy of the old one except for that property. Withers are painfully verbose to declare, and ideally this feature would offer a solution to that.

Serialization has to work *somehow*, even though many of the members will be generated.

We should not be *too* concerned about the ability to grow up to represent all kinds of things. Start from it being the POD feature, and work from there.



6. Pattern matching
===================

See #180, #206, #1572.

Whether introduced variables are mutable or not is not a key question: we can go with language uniformity or scenario expectation.

Integral value matching is an opportunity to generalize. The pattern 3 may match the value 3 of all integral types rather than just the int 3.

Named matching against all objects and positional against ones that define a positional match. Are recursive patterns necessary? No, but probably convenient and there's no reason not to have them.

Pattern matching could be shoe-horned into current `switch` statements as well as `is` expressions. And we could have a switching *expression* syntax as well:

``` c#
var x = match(e) { p => e, p => e, * => e }
```
An expression version would need to be checked by the compiler for completeness. A little clunky, but much more concise than using a switch.

Similar to a current pattern in the Roslyn code base:

``` c#
Match<T1, T2, TResult>(Func<T1, TResult> f1, Func<T2, TResult> f2) { ... }
var x = Match((string s) => e, (int i) => e);
```

Maybe the fat arrow is not right. We need to decide on syntax. Another option is to use the case keyword instead.
