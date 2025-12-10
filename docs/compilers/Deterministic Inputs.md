Deterministic Inputs
====================

The C# and VB compilers are fully deterministic when the `/deterministic` option is specified (this is the default in the .NET SDK). This means that the "same inputs" will cause the compilers to produce the "same outputs" byte for byte. 

The following are considered inputs to the compiler for the purpose of determinism:

- The sequence of command-line parameters (order is important)
- The precise version of the compiler used and the files included in its deployment: reference assemblies, rsp, etc ...
- Current full directory path (you can reduce this to a relative path; see https://github.com/dotnet/roslyn/issues/949)
- (Binary) contents of all files explicitly passed to the compiler, directly or indirectly, including
  - source files
  - referenced assemblies
  - referenced modules
  - resources
  - the strong name key file
  - `@` response files
  - Analyzers
  - Generators
  - Rulesets
  - "additional files" that may be used by analyzers and generators
- The current culture if `/preferreduilang` is not specified (for the language in which diagnostics and exception messages are produced).
- The current OS code page if `/codepage` is not specified and any of the input source files do not have BOM and are not UTF-8 encoded.
- The existence, non-existence, and contents of files on the compiler's search paths (specified, e.g. by `/lib` or `/recurse`)
- The CLR platform on which the compiler is run:
  - The result of `double` arithmetic performed for constant-folding may use excess precision on some platforms.
  - The compiler uses Unicode tables provided by the platform.
- The version of the zlib library that the CLR uses to implement compression (when `/embed` or `/debug:embedded` is used).
- The value of `%LIBPATH%`, as it can affect reference discovery if not fully qualified and how the runtime handles analyzer / generator dependency loading.
- The full path of source files although `/pathmap` can be used to normalize this between compiles of the same code in different root directories.

At the moment the compiler also depends on the time of day and random numbers for GUIDs, so it is not deterministic unless you specify `/deterministic`.

## Debugging Determinism Failures

When investigating determinism issues where the same code produces different outputs, you can use several techniques to identify the cause:

### 1. Generate a Deterministic Key File

Build with `/p:Features="debug-determinism"` to generate a `.key` file that documents all inputs to the compiler:

```bash
# For MSBuild projects
dotnet build /p:Features="debug-determinism"

# For direct compiler invocation
csc /features:debug-determinism YourFile.cs
```

This creates an additional output file (e.g., `MyAssembly.dll.key`) alongside your compiled assembly. The key file is a JSON document containing:
- Compiler version and runtime information
- All source file paths and content checksums
- Referenced assemblies with their MVIDs (Module Version IDs)
- Compilation options
- Parse options
- Emit options
- Analyzer and generator information

Compare the `.key` files from two supposedly identical builds to identify which inputs differ:

```bash
# Unix/Linux/Mac
diff build1/MyAssembly.dll.key build2/MyAssembly.dll.key

# Windows (using fc)
fc build1\MyAssembly.dll.key build2\MyAssembly.dll.key
```

### 2. Compare Metadata Using metadata-tools

Install the `mdv` (MetaData Viewer) tool from the [dotnet/metadata-tools](https://github.com/dotnet/metadata-tools) repository:

```bash
dotnet tool install mdv -g --prerelease --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json
```

Generate metadata dumps for your assemblies and compare them:

```bash
# Generate metadata output for each DLL
mdv MyAssembly1.dll > assembly1.txt
mdv MyAssembly2.dll > assembly2.txt

# Compare the outputs
diff assembly1.txt assembly2.txt
```

The metadata dump shows detailed information about types, methods, fields, attributes, and other metadata in the assembly. Differences in the metadata can help identify what changed between builds.

### 3. Compare Embedded Resources

If your assembly contains embedded resources, verify they are identical:

```bash
# Extract resources using ildasm or .NET tools
# For example, using ildasm on Windows:
ildasm /out=assembly1.il MyAssembly1.dll
ildasm /out=assembly2.il MyAssembly2.dll

# Then compare the .resources sections in the IL files
diff assembly1.il assembly2.il
```

Alternatively, use a tool like `ILSpy` or `dnSpy` to inspect and compare embedded resources visually.

### 4. Binary Diff of the DLL

As a last resort, perform a hex dump comparison of the actual DLL files:

```bash
# Unix/Linux/Mac - using xxd or hexdump
xxd MyAssembly1.dll > assembly1.hex
xxd MyAssembly2.dll > assembly2.hex
diff assembly1.hex assembly2.hex

# Windows - using fc with /b flag for binary comparison
fc /b MyAssembly1.dll MyAssembly2.dll

# Or using Format-Hex in PowerShell
Format-Hex MyAssembly1.dll | Out-File assembly1.hex
Format-Hex MyAssembly2.dll | Out-File assembly2.hex
Compare-Object (Get-Content assembly1.hex) (Get-Content assembly2.hex)
```

A hex diff shows the exact bytes that differ, which can help identify non-deterministic data like timestamps, GUIDs, or other embedded values.

### Common Causes of Non-Determinism

When debugging, look for these common issues:
- **Missing `/deterministic` flag**: Ensure `/deterministic` is enabled
- **Absolute paths**: Use `/pathmap` to normalize file paths
- **Timestamps**: Check if PDBs or other files embed build times
- **Different compiler versions**: Verify same compiler version is used
- **Different reference assembly versions**: Check MVIDs (Module Version IDs) in `.key` files
- **Environment variables**: Variables like `%LIBPATH%` can affect output
- **Source file encoding**: Ensure consistent encoding (UTF-8 with BOM recommended)
- **Generator/analyzer differences**: Verify same versions are loaded
