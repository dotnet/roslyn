C# Design Meeting Notes for Mar 24, 2015
========================================

Discussion thread on these notes is at https://github.com/dotnet/roslyn/issues/1898.

*Quote of the Day:* If we have slicing we also need dicing!


Agenda
------

In this meeting we went through a number of the performance and reliability features we have discussed, to get a better reading on which ones have legs. They end up falling roughly into three categories:

* Green: interesting - let's keep looking
* Yellow: there's something there but this is not it
* Red: probably not

As follows:

1. ref returns and locals <*green*> (#118)
2. readonly locals and parameters <*green*> (#115)
3. Method contracts <*green*> (#119)
4. Does not return <*green*> (#1226)
5. Slicing <*green*> (#120)
6. Lambda capture lists <*yellow - maybe attributes on lambdas*> (#117)
7. Immutable types <*yellow in current form, but warrants more discussion*> (#159)
8. Destructible types <*yellow - fixing deterministic disposal is interesting*> (#161)
9. Move <*red*> (#160)
10. Exception contracts <*red*>
11. Static delegates <*red*>
12. Safe fixed-size buffers in structs <*red*> (#126)

Some of these were discussed again (see below), some we just reiterated our position.



1. Ref returns and locals
=========================

At the implementation level these would require a verifier relaxation, which would cause problems when down targeting in sandboxing scenarios. This may be fine.

At the language level, ref returns would have to be allowed on properties and indexers only if they do not have a setter. Setters and ref would be two alternative ways of allowing assignment through properties and indexers. For databinding scenarios we would need to check whether reflection would facilitate such assignment through a ref.

A danger of ref returns in public APIs: say you return a ref into the underlying array of e.g. a list, and the list is grown by switching out the underlying array. Now someone can get a ref, modify the collection, follow the ref and get to the wrong place. So maybe ref returns are not a good thing on public API boundary.

There's complexity around "safe to return": You should only return refs that you received as parameters, or got from the heap. This leads to complexity around allowing reassignment of ref locals: how do you track whether the ref they are pointing to is "safe to return" or not? We'd have to either

* add syntax to declare which kind the local points to (complex)
* allow only one of the kinds to be assigned to locals (restrictive)
* track it as best we can through flow analysis (magic) 

There's complexity in how refs relate to readonly. You either can't take a ref to a readonly, or you need to be able to express a readonly ref through which assignments is illegal. The latter would need explicit representation in metadata, and ideally the verifier would enforce the difference.

This can't very well be a C# only feature, at least if it shows up in public APIs. VB and F# would need to at least know about it.

This feature would be a decent performance win for structs, but there aren't a lot of structs in the .NET ecosystem today. This is a chicken-and-egg thing: because structs need to be copied, they are often too expensive to use. So this feature could lower the cost of using structs, making them more attractive for their other benefits.

Even so, we are still a bit concerned that the scenario is somewhat narrow for the complexity the feature adds. The proof will have to be in the use cases, and we're not entirely convinced about those. It would be wonderful to hear more from the community. This is also a great candidate for a prototype implementation, to allow folks to experiment with usability and performance.



2. Readonly locals and parameters
=================================

At the core this is a nice and useful feature. The only beef we have with it is that you sort of want to use `readonly` to keep your code safe, and you sort of don't because you're cluttering your code. The `readonly` keyword simply feels a bit too long, and it would be nice to have abbreviations at least in some places. 

For instance `readonly var` could be abbreviated to `val` or `let`. Probably `val` reads better than `let` in many places, e.g. declaration expressions. We could also allow `val` as an abbreviation for `readonly` even in non-var situations. 

In Swift they use `let` but it reads strange in some contexts. In Swift it's optional in parameter positions, which helps, but we couldn't have that for back compat reasons.

This is promising and we want to keep looking at it.



4. Does Not Return
==================

It would be useful to be able to indicate that a method will never return successfully. It can throw or loop.

The proposal is to do it as an attribute, but there would be more value if it was part of the type system. Essentially it replaces the return type, since nothing of that type is ever returned. We could call it `never`. The `never` type converts to pretty much anything.

This would allow us to add throw *expressions* in the language - their type would be `never`.

Having it in the type system allows e.g. returning `Task<never>`, so that you can indicate an async method that will only ever produce an exception, if anything.

Because of the Task example you do want to allow `never` in generics, but that means you could have generic types that unwittingly operate on never values, which is deeply strange. This needs to be thought about more.

If through nasty tricks you get to a point in the code that according to never types should not be reachable, the code should probably throw.

A common usage would be helper methods to throw exceptions. But throw as an expression is the most useful thing out of this.

 

6. Attributes on lambdas
========================

Why? Guiding an analyzer, e.g. to prevent variable capture. Syntactically it might collide with XML literals in VB.

We could probably hack it in. The attribute would be emitted onto the generated method.

