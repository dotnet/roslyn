Deterministic Inputs
====================

We are aiming to make the compilers ultimately deterministic (https://github.com/dotnet/roslyn/issues/372). What that means is that the "same inputs" will cause the compilers to produce the "same outputs". 

The following are considered inputs to the compiler for the purpose of determinism:

- The sequence of command-line parameters
- The contents of the compiler's `.rsp` response file.
- The precise version of the compiler used, and its referenced assemblies
- Current full directory path (you can reduce this to a relative path; see https://github.com/dotnet/roslyn/issues/949)
- (Binary) contents of all files explicitly passed to the compiler, directly or indirectly, including
  - source files
  - referenced assemblies
  - referenced modules
  - resources
  - the strong name key file
  - `@` response files
  - Analyzers
  - Rulesets
  - "additional files" that may be used by analyzers
- The current culture (for the language in which diagnostics and exception messages are produced).
- The current OS code page if `/codepage` is not specified and any of the input source files do not have BOM and are not UTF-8 encoded.
- The existence, non-existence, and contents of files on the compiler's search paths (specified, e.g. by `/lib` or `/recurse`)
- The CLR platform on which the compiler is run:
  - The result of `double` arithmetic performed for constant-folding may use excess precision on some platforms.
  - The compiler uses Unicode tables provided by the platform.
- The version of the zlib library that the CLR uses to implement compression (when `/embed` or `/debug:embedded` is used).
- The value of `%LIBPATH%`, as it can affect analyzer dependency loading.

At the moment the compiler also depends on the time of day and random numbers for GUIDs, so it is not deterministic unless you specify `/deterministic`.
