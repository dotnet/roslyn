## Building the Caravela compiler

To start working with the Caravela compiler, first run `restore && build` from the root of the repo.

You can then open Compilers.sln in VS 2019 version 16.8 Preview 6 or newer. (Roslyn.sln will work too, but is unnecessarily big for most tasks.)

To build the Caravela compiler packages, you can either run `build -pack`, or you can use the Pack command in VS on the Caravela.Compiler project and the Caravela.Compiler.Sdk project. The built packages are in artifacts\packages\\$Configuration\Shipping.

To build a release version of the Caravela compiler:

1. Set the version in eng\Versions.props.
2. Run `build -c Release -pack /p:DotNetFinalVersionKind=release`. Without the last parameter, any pack produces a prerelease version, e.g. 3.8.0-dev.