GetDeterministicKey
===

**This is meant to be the text of the issue I will eventually file for the API**

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
updated. Even when proper equality checks are in place this does not help 
distributed computing where equality must be decided across different processes.

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
+       public string GetDeterministicKey(
+           ImmutableArray<AdditionalText> additionalTexts = default,
+           ImmutableArray<DiagnosticAnalyzer> analyzers = default,
+           ImmutableArray<ISourceGenerator> generators = default,
+           EmitOptions? emitOptions = null,
+           DeterministicKeyOptions options = DeterministicKeyOptions.Default)
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

The return of `GetDeterministicKey` is an opaque string that full represents 
the content of the `Compilation` contents. Two `Compilation` which produce 
different output, diagnostics or binaries, will have different strings returned
for this function. 

The return of `GetDeterministicKey` can, and by default will, change between versions
of the compiler. That is true of both the content of the string as well as the 
underlying format. The content must change because part of the input to compilation
is the version of the compiler. The format will change as desired by the 
implementation.  Consumers should not take any dependency on the format of this 
string other than it being an effective hash of the `Compilation` it came from.

The string returned here will be human readable and visually diffable. It will
not be a minimal representation though. The content can, and is expected to be,
compressed further with a hashing function such as SHA-256. 

For example here is the proposed return for the following `net5.0` program:

```c#
System.Console.WriteLine("Hello World");
```

```json
{
  "options": {
    "outputKind": "ConsoleApplication",
    "scriptClassName": "Script",
    "publicSign": false,
    "checkOverflow": false,
    "platform": "AnyCpu",
    "optimizationLevel": "Release",
    "generalDiagnosticOption": "Default",
    "warningLevel": 9999,
    "deterministic": false,
    "debugPlusMode": false,
    "referencesSupersedeLowerVersions": false,
    "reportSuppressedDiagnostics": false,
    "nullableContextOptions": "Disable",
    "unsafe": false,
    "topLevelBinderFlags": "None"
  },
  "syntaxTrees": [
    {
      "fileName": "",
      "text": {
        "checksum": "1b565cf6f2d814a4dc37ce578eda05fe0614f3d",
        "checksumAlgorithm": "Sha1",
        "encoding": "Unicode (UTF-8)"
      },
      "parseOptions": {
        "languageVersion": "CSharp9",
        "specifiedLanguageVersion": "Default"
      }
    }
  ],
  "references": [ 
      // omitted for brevity 
  ]
}
```

The full output can be seen [here](https://gist.github.com/jaredpar/654d84f64de2d728685a7d4ccde944e7)

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

## Work Remaining


