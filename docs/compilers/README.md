# Roslyn Compiler

## Specification

The compiler specification details the supported (and semi-supported) surface area of the Roslyn VB and C# compilers. This includes

0. Command-line switches and their meaning
1. Breaking changes from previous versions of the compilers
2. Compiler behaviors that are (intentionally) contrary to the specification
3. Compiler features not described by the language specification
    1. COM-specific and other Microsoft-specific features
    2. "Well-known" attributes that affect compiler behavior
    3. The "ruleset" file syntax and semantics
4. Features included for interoperability between C# and VB, for example
    1. Named Indexers use from C#
5. Places where the compiler behavior diverges from the language specification
6. Limitations (e.g. identifier length)
7. History of language changes per version

The language specification itself is not included here.

## Platforms

The compiler is officially supported in the following configurations:

- .NET Framework: x86, x64 and ARM64
- .NET Core:
  - Operating Systems: Windows, macOS, Linux
  - Architectures: x86, x64, and ARM64
- Mono Core: x86 and x64

The compiler takes contributions for other platforms, such as big-endian architectures, but does not officially support the compiler there.
