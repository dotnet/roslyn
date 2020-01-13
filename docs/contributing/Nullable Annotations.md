# Nullable Annotations

This document describes how nullable annotations should be approached in the 
Roslyn code base. The default is to simply follow [the same guidance](https://github.com/dotnet/runtime/blob/master/docs/coding-guidelines/api-guidelines/nullability.md)
as the [dotnet/runtime](github.com/dotnet/runtime) repository. This document
serves to detail the places where the guidance differs for Roslyn and 
re-emphasize rules that come up frequently.

## Annotation Guidance 

- **DO** annotate all new code
- **DO** use the null suppression operator in places where a method body has
ensured a value cannot be `null` but the compiler is unable to track it. 
Example:
```cs
struct SyntaxTrivia {
    void Example() {    
        if (this.HasStructure) {
            Use(this.GetStructure()!)
        }
    }
}
```
- **DO** use `Assert` to validate state that is passed into the method
```cs
void M1(SyntaxNode node) {
    RoslynDebug.Assert(node.Parent is object);
    ...
    M2(node.Parent);
}
```
- **AVOID** annotating and refactoring code in the same commit. This can
significantly increase the cost of reviewing. Prefer instead to separate 
annotations and significant refactorings into separate commits. Small changes,
and modernizing style (change `default(SyntaxToken)` to `default`) are
acceptable.
- **DO NOT** be concerned with the validity of annotations after an object has 
been Dispose'd or otherwise entered into an invalid state. For example
manipulating a pooled object after it's been freed. This is a violation of the 
objects contract and annotations aren't meaningful here.
- **DO NOT** be concerned with the validity of an enumerator's Current 
annotations when used before MoveNext has been called or after MoveNext has 
returned false.  As with Dispose, such use is considered invalid, and we don't
want to harm the correct consumption of Current by catering to incorrect
consumption.

## Breaking Change Guidance
At this point in time nullable annotations are not considered as a part of our 
compatibility bar. That is nullable annotations can change as needed or desired
by the Roslyn team and it is not considered a compat change even if it
introduces new warnings into consumer code.

This is necessary as Roslyn is a very large code base and it will take a 
significant amount of time to annotate the code base. Even Public API 
annotations only is a siginifcant amount of work and, without the 
implementations also being annotated, there won't be significant confidence the
annotations are correct.

As specific libraries complete their nullable annotations then the annotations
will be [tracked in API files](https://github.com/dotnet/roslyn-analyzers/pull/3125)
and the library names will be added to this document as being complete.

At that point nullable annotation changes will still be allowed but there will
be a **significantly** higher bar to making such changes.



