## csproj properties

The Caravela compiler can be configured by several custom MSBuild properties from the csproj file of a user project:

* `CaravelaEmitCompilerTransformedFiles`: Set to `true` to write transformed files to disk to the `obj/Debug` or `obj/Release` directory. The default is `true` if `CaravelaDebugTransformedCode` is enabled and `false` otherwise.
* `CaravelaCompilerTransformedFilesOutputPath`: Can be used to set the directory where transformed files are written instead of `obj/Debug`.
* `CaravelaCompilerTransformerOrder`: A semicolon-separated list of namespace-qualified names of transformers. This is necessary to set the execution order of transformers, if the order has not been fully specified by the transformers using [`[TransformerOrder]`](API.md#TransformerOrderAttribute).
* `CaravelaDebugTransformedCode`: Set to `true` to produce diagnostics and PDB sequence points in transformed code. Otherwise, locations are attempted to be mapped to original user code. The default is `false`.
* `CaravelaDebugCompiler`: Set to `true` to cause `Debugger.Launch()`.
* `CaravelaSourceOnlyAnalyzers` contains the list of analyzers that must execute on the source code instead of the transformed code. This is a comma-separated list which can contain the assembly name, an exact namespace (namespace inheritance rules do not apply) or the exact full name of an analyzer type.
* `CaravelaLicense` constains a comma-separated list of license keys. This allows the user to set the license key using an MSBuild property or using an environment variable.
* `CaravelaLicenseSources` constains a comma-separated list of license sources. It is used to disable license sources during testing. All license sources are enabled by default. The allowed values are:
  * `User` for license keys stored in user profile regitered using the PostSharp.Cli tooling or auto-registration.
  * `Property` for license keys set using the `CaravelaLicense` MSBuild property (or environmet variable).
* `CaravelaFirstRunLicenseActivatorEnabled`: Set to `false` to disable automatic license activation during first build. This is used for testing. The default value is `true`.

Note: If `CaravelaDebugTransformedCode` is set to `true`, but `EmitCompilerTransformedFiles` is explicitly set to `false` (and no custom `CompilerTransformedFilesOutputPath` is provided), then transformed sources should be used for debugging and diagnostics, but cannot be written to disk.

For debugging, this means transformed sources are embedded into the PDB. For diagnostics, this means the reported locations are nonsensical and the user is warned about this.