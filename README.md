## Metalama.Compiler

The Metalama compiler is a fork of [Roslyn](https://github.com/dotnet/roslyn) (the C# compiler) which allows you to execute "source transformers". Source transformers are similar to [source generators](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/), except that they allow any changes to the source code, not just additions.

[![CI badge](https://github.com/postsharp/Metalama.Compiler/workflows/Full%20Pipeline/badge.svg)](https://github.com/postsharp/Metalama.Compiler/actions?query=workflow%3A%22Full+Pipeline%22)

### See also

* [API](src/Metalama/doc/API.md)
* [Building](src/Metalama/doc/Building.md)
* [Component diagram](src/Metalama/doc/Component%20diagram.md)
* [Properties](src/Metalama/doc/Properties.md)
* [Modifications and additions](src/Metalama/doc/Modifications.md)
* [Merging from new Roslyn branches](src/Metalama/doc/Merging.md)

### Notes

!!! The exact version of .NET SDK as set in the global.json needs to be installed. Errors comming from not having this installed are misleading. !!!