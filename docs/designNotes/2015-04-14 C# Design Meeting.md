C# Design Meeting Notes for Apr 14
==================================

Discussion thread for these notes can be found at https://github.com/dotnet/roslyn/issues/2134.

Bart De Smet visited from the Bing team to discuss their use of Expression Trees, and in particular the consequences of their current shortcomings.

The Expression Tree API today is not able to represent all language features, and the language supports lambda conversions even for a smaller subset than that.



Background
==========

Certain Bing services, such as parts of Cortana, rely on shipping queries between different machines, both servers in the cloud and client devices.

In a typical scenario a lambda expression tree is produced in one place, ideally via the conversion from lambda expressions to expression trees that exists in C#. The expression tree is then serialized using a custom serialization format, and sent over the wire. At the receiving end it will often be stitched into a larger expression tree, which is then compiled with the `Compile` method and executed.

Along the way, several transformations are often made on the trees, for efficiency reasons etc. For instance, rather than invoke a lambda its body can often be inlined in the enclosing tree that the lambda gets stitched into.

The serialization format is able to carry very specific type information along with the code, but can also represent the code loosely. A looser coupling makes for code that is more resilient to "schema" differences between the nodes, and also allows for use of types and functions that aren't present where the lambda is concocted. However, it also makes it harder to stich things back up right on the other side.



Shortcomings in the Expression Tree API
=======================================

The expression tree API was introduced with Linq and C# 3.0, and was extended to support the implementation infrastructure of `dynamic` along with C# 4.0. However, it has not been kept up to date with newer language features.

This is a list of ones that are confounding to the Bing team.


Dynamic
-------

Even though the API was extended for the benefit of the dynamic feature, the feature itself ironically is not well represented in the API. This is not a trivial undertaking: in Expression Trees today, all invoked members are represented using reflection structures such as `MemberInfo`s etc. Dynamic invocations would need a completely different representation, since they are by definition not bound by the time the expression tree is produced.

In the Bing scenario, dynamic would probably help a lot with representing the loosely coupled invocations that the serialization format is able to represent.

Alternatively, if the C# language added something along the "lightweight dynamic" lines that were discussed (but ultimately abandoned) for C# 6, the representation of that in Expression Trees would probably yield similar benefit with a fraction of the infrastructure.


Await
-----

Representing await would probably be easy in the Expression Tree API. However, the implementation of it in `Expression.Compile` would be just as complex as it is in the C# and VB compilers today. So again, this is a significant investment, though one with much less public surface area complexity than `dynamic`.

Needless to say, distributed scenarios such as that of Bing services have a lot of use for `await`, and it can be pretty painful to get along without it.


Null conditional operators and string interpolation
---------------------------------------------------

These should also be added as Expression Tree nodes, but could probably be represented as reducible nodes. Reducible nodes are a mechanism for describing the semantics of a node in terms of reduction to another expression tree, so that the `Compile` method or other consumers don't have to know about it directly.


Higher level statements
-----------------------

While expression trees have support for statements, those tend to be pretty low level. Though there is a `loop` node there are no `for` or `foreach` nodes. If added, those again could be reducible nodes.



Shortcomings in the languages
=============================

C# and VB are even more limited in which lambdas can be converted to expression trees. While statements are part of the Expression Tree API, the languages will not convert them. Also, assignment operators will not be converted.

This is a remnant of the first wave of Linq, which focused on allowing lambdas for simple, declarative queries, that could be translated to SQL.

There has traditionally been an argument against adding more support to the languages based on the pressure this would put on existing Linq providers to support the new nodes that would start coming from Linq queries in C# and VB.

This may or may not ever have been a very good argument. However, with Roslyn analyzers now in the world, it pretty much evaporates. Any Linq provider that wants to limit at design time the kinds of lambdas allowed, can write a diagnostic analyzer to check this, and ship it with their library.

None of this support would require new syntax in the language, obviously. It is merely a matter of giving fewer errors when lambdas are converted to expression trees, and of mapping the additional features to the corresponding nodes in what is probably a straightforward fashion.



Conclusion
==========

There is no remaining design reason we can think of why we shouldn't a) bring the Expression Tree API up to date with the current state of the languages, and b) extend the language support accordingly.

The main issue here is simply that it is likely to be a huge undertaking. We are not sure that the sum of the scenarios will warrant it, when you think about the opportunity cost for other language evolution. Bing is a very important user, but also quite probably an atypical one.

So in summary, as a language design team, we certainly support completing the picture here. But with respect to priorities, we'd need to see a broader call for these improvements before putting them ahead of other things.
