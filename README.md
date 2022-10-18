## Metalama.Compiler

The Metalama compiler is a fork of [Roslyn](https://github.com/dotnet/roslyn) (the C# compiler) which allows you to execute "source transformers". Source transformers are similar to [source generators](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/), except that they allow any changes to the source code, not just additions.

### See also

* [API](src/Metalama/doc/API.md)
* [Building](src/Metalama/doc/Building.md)
* [Component diagram](src/Metalama/doc/Component%20diagram.md)
* [Properties](src/Metalama/doc/Properties.md)
* [Modifications and additions](src/Metalama/doc/Modifications.md)
* [Merging from new Roslyn branches](src/Metalama/doc/Merging.md)

### Notes
* THIS IS NOT THE PACKAGE YOU ARE LOOKING FOR. If you want to add Metalama to your project, add a reference to the package named `Metalama.Framework`.
* Referencing Metalama Compiler package causes the project to be built using the Metalama Compiler contained in the package (a fork of Roslyn), as opposed to the version of the C# compiler installed with .NET SDK.
* When referencing Metalama Compiler pacakge, i.e. using Metalama Compiler instead of standard C# compiler, the exact version of .NET SDK as set in the global.json needs to be installed. Errors comming from not having this installed are misleading.