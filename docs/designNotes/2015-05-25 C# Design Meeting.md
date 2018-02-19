# C# Design Meeting Notes for May 25, 2015

Discussion for these notes can be found in https://github.com/dotnet/roslyn/issues/3912.

## Agenda

Today we went through a bunch of the proposals on GitHub and triaged them for our list of features in issue #2136. Due to the vastness of the list, we needed to use *some* criterion to sort by, and though far from ideal we ordered them by number of comments, most to least.

Here's where we landed, skipping things that were already "Strong interest". Some are further elaborated in sections below.

1. Method contracts <*Stay at "Some interest"*>(#119)
2. Destructible types <*Stay at "Probably never"*> (#161)
3. Params IEnumerable <*Stay at "Small but Useful*>(#36)
4. Multiple returns <*Addressed by tuples. Close.*> (#102)
5. More type inference <*Not a coherent proposal. Close*> (#17)
6. Readonly parameters and locals <*Stay at "Some interest"*>(#115)
7. Implicitly typed lambdas <*Add at "Probably never"*>(#14)
8. Immutable types <*Stay at "Some interest*>(#159)
9. Object initializers for immutable objects <*Add at "Some interest"*>(#229)
10. First item is special <*Add at "Never"*>(#131)
11. Array slices <*Keep at "Interesting but needs CLR support"*>(#120)
12. Vararg calling convention <*Merge with params IEnumerable*>(#37)
13. XML Literals <*Add to "Never"*>(#1746)
14. Local Functions <*Move to "Some interest*>(#259)
15. Covariant returns <*Stay at "Some interest*>(#357)

# Params IEnumerable

This needs more thinking - let's not just implement the straightforward design. There are perf issues, for instance, around implementing through the `IEnumerable<T>` interface instead of arrays directly.

# More type inference

Not a coherent proposal. But even if there was one, we probably wouldn't want it in C#.

# Implicitly typed lambdas

These are mostly subsumed by local functions, which we'd rather do. It has some individual usefulness but not much synergy.

# Object initializers for immutable objects

We want to think this together with withers, not sure what form it would take.

# first item in loops is special

We recognize the scenario but it's not worthy of a feature.

# vararg calling convention

Roll it in with params IEnumerable discussion for investigation.

# XML literals

Never! We won't bake in a specific format.

