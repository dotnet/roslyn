Analyzers / Generators
===

Many of our unit tests need to define and consume custom analyzers and generators. This is a problem because most of our unit test assemblies multi-target between `net472` and the latest .NET Core. The compiler pushes customers to define their analyzers / generators against `netstandard2.0`. For generators the compiler actually issues a diagnostic when targeting `net472` and eventually this will be an error.

Analyzers and Generators that are defined for testing purposes therefore should be defined in this project. It is the only project in our test set that actually targets `netstandard2.0` hence the only place we can safely define analyzers / generators going forward. 
