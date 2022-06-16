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
