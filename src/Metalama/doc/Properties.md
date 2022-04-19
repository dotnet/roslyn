## csproj properties

The Metalama compiler can be configured by several custom MSBuild properties from the csproj file of a user project:

* `MetalamaEmitCompilerTransformedFiles`: Set to `true` to write transformed files to disk to the `obj/Debug/metalama` or `obj/Release/metalama` directory. The default is `true` if `MetalamaDebugTransformedCode` is enabled and `false` otherwise.
* `MetalamaCompilerTransformedFilesOutputPath`: Can be used to set the directory where transformed files are written instead of `obj/Debug`.
* `MetalamaCompilerTransformerOrder`: A semicolon-separated list of namespace-qualified names of transformers. This is necessary to set the execution order of transformers, if the order has not been fully specified by the transformers using [`[TransformerOrder]`](API.md#TransformerOrderAttribute).
* `MetalamaDebugTransformedCode`: Set to `true` to produce diagnostics and PDB sequence points in transformed code. Otherwise, locations are attempted to be mapped to original user code. The default is `false`.
* `MetalamaDebugCompiler`: Set to `true` to cause `Debugger.Launch()`.
* `MetalamaSourceOnlyAnalyzers` contains the list of analyzers that must execute on the source code instead of the transformed code. This is a comma-separated list which can contain the assembly name, an exact namespace (namespace inheritance rules do not apply) or the exact full name of an analyzer type.
* `MetalamaLicense` contains a comma-separated list of license keys. This allows the user to set the license key using an MSBuild property or using an environment variable.

Note: If `MetalamaDebugTransformedCode` is set to `true`, but `EmitCompilerTransformedFiles` is explicitly set to `false` (and no custom `CompilerTransformedFilesOutputPath` is provided), then transformed sources should be used for debugging and diagnostics, but cannot be written to disk.

For debugging, this means transformed sources are embedded into the PDB. For diagnostics, this means the reported locations are nonsensical and the user is warned about this.