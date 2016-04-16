Compiler Specification
======================

The compiler specification details the supported (and semi-supported) surface area of the Roslyn VB and C# compilers. This includes

0. Command-line switches and their meaning
0. Breaking changes from previous versions of the compilers
0. Compiler behaviors that are (intentionally) contrary to the specification
0. Compiler features not described by the language specification
  - COM-specific and other Microsoft-specific features
  - "Well-known" attributes that affect compiler behavior
  - The "ruleset" file syntax and semantics
0. Features included for interoperability between C# and VB, for example
  - Named Indexers use from C#
0. Places where the compiler behavior diverges from the language specification
0. Limitations (e.g. identifier length)
0. History of language changes per version

The language specification itself is not included here.