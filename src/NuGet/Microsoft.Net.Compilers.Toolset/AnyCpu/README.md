![Metalama Logo](https://raw.githubusercontent.com/postsharp/Metalama/master/images/metalama-by-postsharp.svg)

The `Metalama.Compiler` package is a fork of the `Microsoft.Net.Compilers.Toolset` package that adds support for source transformers through the `ISourceTransformer` interface.
 
You should normally never reference this package from a project. Instead, you should reference the `Metalama.Framework` package, which references `Metalama.Compiler`.

For a map of the NuGet packages that compose Metalama, see https://doc.metalama.net/deployment/packages.