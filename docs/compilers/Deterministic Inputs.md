Deterministic Inputs
====================

We are aiming to make the compilers ultimately deterministic (https://github.com/dotnet/roslyn/issues/372). What that means is that the "same inputs" will cause the compilers to produce the "same outputs". 

The following will be considered inputs to the compiler for the purpose of determinism:

- The sequence of command-line flags
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
- The current culture
- The default encoding (or the current code page) if the encoding is not specified
- The existence, non-existence, and contents of files on the compiler's search paths (specified, e.g. by `/lib` or `/recurse`)
- The CLR platform on which the compiler is run (e.g. the result of `double` arithmetic performed for constant-folding may use excess precision on some platforms).

At the moment the compiler also depends on the time of day and random numbers for GUIDs, so it is not deterministic unless you specify `/features:deterministic`.
