Used Assembly References
=========================

The *Used Assembly References* feature provides a ```GetUsedAssemblyReferences``` API on a ```Compilation``` to obtain a set
of metadata assembly references that are considered to be used by the compilation. For example, if a type declared in a
referenced assembly is referenced in source code within the compilation, the reference is considered to be used. Etc.

See https://github.com/dotnet/roslyn/issues/37768 for more information.