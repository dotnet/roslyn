 
**Code generation differences compared to previous compilers.**

**NOTE** - **The form and shape of the IL stream as produced by the compiler is not a public contract!**
Compiler is only responsible for producing IL that is semantically equivalent to the C# source code.
Any dependencies on exact form of the output may be broken by new versions or even bug fixes.

It may not be comprehensive since the changes are a moving target and there could be some changes that are not too obvious or significant to mention. This document is always in progress...

**What is the purpose of this document?**
* JIT compilers in general strive to handle any valid IL. However, the realities are that most optimizations and most tests are directed towards "typical" codegen and therefore there is interest in any changes in compiler output so that JITs could make sure there is adequate support.
* There is a class of tools that rely on decompilation techniques. Generally such tools *choose* to be  broken by specializing to particular compiler outputs. Adjusting to new patterns is just something they have to deal with as a part of the lifecycle.
* There is also a good deal of academic interest in the changes.

**So here is a rough list of changes:** 
There are two sections here - Release codegen and Debug. They are separate because the changes are typically different in nature since they are motivated by different goals. 
Generally Release codegen strives to be the most efficient and compact representation of the sources semantics, while the Debug codegen values debuggability.
When efficiency and debuggability are at conflict, Release and Debug make different choices.

**== Release (optimized)**

•	The async codegen was for the most part redone in Roslyn.
 - The capturing is smarter now when optimizations are enabled since it relies on precise data flow analysis. Basically only locals that cross awaits need to be lifted. Old compiler used more conservative approach and would often capture locals unnecessarily.
 - The stack spilling is completely different. Instead of always present object array slot, we generate strongly typed reusable slots, but only if needed.

•	Iterators - the genral principle is the same, but there were some minor refinements in the state machine.
 - Local capturing is based on precise data flow analysis and may result in fewer local lifted into iterator class. Generally only locals whose values are alive across yield statements need to be captured.
 - Valid states are now all positive and invalid states are negative. The common path in the  iterator body switches on valid states and there are benefits from the set of those states being contiguous.
 
•	Lambdas had some minor changes in caching strategy and a change in representation of non-lifting lambdas.
- The caching strategy has changed to a more compact pattern that also results in fewer accesses to the caching field (in case JIT does not optimize multiple reads). 
Instead of an equivalent of 
 
```C#
 if (cacheField == null)
 {
   cacheFiled = {allocate new lambdaDelegate};
 }
 return cacheField;
```
We now generate something close to

```C#
return cacheField ?? (cacheFiled = {allocate new lambdaDelegate});
```

- non-lifting Lambda expressions are now implemented as an instance methods on singleton display classes. Since the entry point to the delegate is the instance "Invoke" method, it is cheaper at runtime to dispatch delegate invocations to the underlying implementing method if such method is also an instance method with exactly same formal signature as "Invoke".
- delegates for non-lifting Lambdas in generic methods can now be cached by Roslyn. That is mostly a welcome sideeffect of the change above.

•	The string switch codegen in Roslyn is completely new. Roslyn does not use dictionaries to avoid allocations and a potentially huge penalty when a string switch is execute for the first time. Roslyn uses a private function that maps strings to hash codes and a numeric switch. In some sense this is a partial inlining of the former technic that used static dictionaries.

•	Array initializers are slightly more compact – in most cases extra temporary for the array instance is avoided and dup is used instead. (I think VB always did this, but this is new for C#)

•	Leaves from nested try in many cases would not cascade through outer regions and just leave directly to the outmost region. (VB was doing that always, now C# does this too since this part of codegen is in shared library and it is also more compact)
Branch threading in general may handle few more cases compared to the old compiler. Leave-to-leave case is probably the most noticeable.

•	Numeric switches – 
- some minor changes in re-biasing and range checking in switches with nonzero smallest key. 
- Slightly different strategy in choosing between binary search and computed switch buckets. 
At high level switch codegen follows the same pattern as with old compiler, but we fixed some off-by-one errors that could result in unbalanced decision trees in some cases. 
The end result is that overall IL could be different. Hopefully better.

•	Some unnecessary locals could be eliminated or used on the stack when compiled with /o+.
This is not new, old compiled did this as well, but implementation of this optimization has changed.
There are few cases where old compiler was “smarter” since it did this optimization earlier (and caused numerous inconveniences to later stages). 
Generally the new approach handles more scenarios, so you may notice in some cases more dups used and fewer locals.

**Cascading optimizations** – note that it is possible for one optimization to enable another that may not be by itself new. 



**== Debug (not optimized)**

TBW
