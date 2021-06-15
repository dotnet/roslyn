GetDeterministicKey
===

## Background and Motivation
The `Compilation` type is fully deterministic meaning that given the same inputs
(`SyntaxTree`, `CompilationOptions`, etc ...) it will produce the same output. By 
that it means it will produce byte for byte equivalent binaries and the same 
diagnostics. This is extremely valuable in infrastructure because it allows 
for caching and opens the door for efficient distributed processing.

At the moment this is hard to leverage because consumers can't determine if 
two `Compilation` instances are equivalent for the purposes of determinism. 
Customers have to resort to hand written comparisons which requires a fairly 
intimate knowledge of the compiler (for example knowing what does and does not
impact determinism). Such solutions are not version tolerant; every time the 
compiler adds a new property that impacts determinism the solution must be 
updated.

The motivation here is to provide an API that returns a string based key for a
given `Compilation` such that for two equivalent `Compilation` instances the
key will also be equivalent. This will allow customers who want to leverage 
deterministic caching and communication to have a simple, and transmittable,
way to full describe the `Compilation` they are processing.

## Proposed API

```diff
namespace Microsoft.CodeAnalysis
{
    public class Compilation
    {
+       public string GetDeterministicKey(DeterministicKeyOptions options = default)
    }

    internal enum DeterministicKeyOptions
    {
+       /// <summary>
+       /// The default is to include all inputs to the compilation which impact the output of the 
+       /// compilation: binaries or diagnostics.
+       /// </summary>
+       Default = 0b0,

+       /// <summary>
+       /// Ignore all file paths, but still include file names, in the deterministic key.
+       /// </summary>
+       IgnorePaths = 0b0001,

+       /// <summary>
+       /// Ignore the versions of the tools contributing to the build: compiler version
+       /// runtime version, framework, os, etc ...
+       /// </summary>
+       IgnoreToolVersions = 0b0010,
    }
}
```

The return of `GetDeterministicKey` is an opaque string that represents a hash of the 
contents of the containing `Compilation`. Two `Compilation` which produce different 
output will have different strings returned for this function. 

The return of `GetDeterministicKey` can, and by default will, change between versions
of the compiler. That is true of both the content of the string as well as the 
underlying format. Consumers should not take any dependency on the content of this string
other than it being an effective hash of the `Compilation` it came from.

The hash returned here is not a minimal hash. The content can be compressed further
by running through a hashing function such as SHA-256. 

Note: I'm unsure if "hash" is the best term here. It's a string that effectively 
describes the content of the `Compilation`. In many ways it resembles a tree file 
in `git`. Very much open to better terminology here.

## Usage Examples

### Output caching
Consider that build caching on outputs could be implemented 

### LSIF

### IDE & Compiler communication
Consider that today when running Visual Studio today both the C# IDE and Compiler
are running "compilation" servers. The IDE is more focused on providing services
such as completion, semantic coloring, etc ... and the Compiler is focused on 
compiling. 

Under the hood though they are doing much of the same work: loading references, 
parsing files, binding, etc ... This is all duplicated work that eats up CPU 
cycles and RAM. 

Given the presence of this API it opens the door for build to actually leverage
the C# IDE for emit. Consider that the compiler server can use the output of 
`GetDeterministicKey` to efficiently, and correctly, communicate with the IDE 
server about the different `Compilation` they are each processing. It would be 
reasonable for the server to first check if the IDE can satisfy emit for a
`Compilation` before attempting to process it directly. 

Note: this scenario does require more work as the `Compilation` objects created
in the IDE and Compiler differ in subtle ways. Having this function though would
be motivation to clean these up such that they are equal.

## Alternative Designs

The interface approach

<!--
Were there other options you considered, such as alternative API shapes?
How does this compare to analogous APIs in other ecosystems and libraries?
-->

## Risks

Determinism is hard

