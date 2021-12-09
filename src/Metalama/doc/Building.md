## Building the Metalama compiler

To start working with the Metalama compiler, first run `restore && build` from the root of the repo.

You can then open Compilers.sln in VS 2019 version 16.8 Preview 6 or newer. (Roslyn.sln will work too, but is unnecessarily big for most tasks.)

To build the Metalama compiler packages, you can either run `build -pack`, or you can use the Pack command in VS on the Metalama.Compiler project and the Metalama.Compiler.Sdk project. The built packages are in artifacts\packages\\$Configuration\Shipping.

To build a release version of the Metalama compiler:

1. Set the version in eng\Versions.props.
2. Run `build -c Release -pack`.