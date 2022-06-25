# Nullable Annotations

This document describes how nullable annotations should be approached in the 
Roslyn code base. The default is to simply follow [the same guidance](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/api-guidelines/nullability.md)
as the [dotnet/runtime](https://github.com/dotnet/runtime) repository. This document
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
            Use(this.GetStructure()!);
        }
    }
}
```
- **DO** use explicit validation such as `Debug.Assert` or 
`Contract.ThrowIfNull` to capture internal program invariants.
```cs
void M1(SyntaxNode node) {
    Debug.Assert(node.Parent is object);
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
been disposed or otherwise entered into an invalid state. For example
manipulating a pooled object after it's been freed. This is a violation of the 
objects contract and annotations aren't meaningful here.
- **DO NOT** be concerned with the validity of an enumerator's `Current` 
annotations when used before `MoveNext` has been called or after `MoveNext` has 
returned false. As with `Dispose`, such use is considered invalid, and we don't
want to harm the correct consumption of `Current` by catering to incorrect
consumption.
- **DO** use the adjective `Require` when defining a member that exist only
to provide a non-null returning version of a method that returns a nullable 
reference type. For example: `GetRequiredSemanticModel` model returns a non-null 
value from `GetSemanticModel`


## Breaking Change Guidance
Roslyn is a large code base and it will take significant time for us to fully 
annotate the code base. This will likely take several releases for our larger
libraries to complete. During this time those libraries will not consider 
nullable annotations to be a part of the compatibility bar. That is 
nullable annotations will change without an entry in the breaking change 
document, no compat review, etc ...

Once the nullable annotations in a given library reach critical mass we will 
begin tracking nullable annotations as a part of our compatibility bar. The 
Roslyn team still reserves the right to change annotations if the language or
underlying platforms we build on effectively require us to do so. However such
changes will have a higher degree of scrutiny and will need approval from 
members of the API compat council.

As specific libraries complete their nullable annotations then the annotations
will be [tracked in API files](https://github.com/dotnet/roslyn-analyzers/pull/3125)
and the library names will be added to this document as being complete.



