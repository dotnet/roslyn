Used Assembly References
=========================

The *Used Assembly References* feature provides a ```GetUsedAssemblyReferences``` API on a ```Compilation``` to obtain a set
of metadata assembly references that are considered to be used by the compilation. For example, if a type declared in a
referenced assembly is referenced in source code within the compilation, the reference is considered to be used. Etc.

Note that documentation must be processed for best results. Types referenced in xml docs are missed when documentation isn't processed.
Conversely, all usings are assumed to be used when the usage analysis wasn't performed on doc comments.

See https://github.com/dotnet/roslyn/issues/37768 for more information.