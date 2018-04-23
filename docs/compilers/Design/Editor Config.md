.editorconfig Compiler Support
==============================

Linked or "External" Files
==========================

The compiler has no notion of whether a source file is linked or brought in via some external source (e.g. NuGet package). This design is preserved for .editorconfig, where the only mechanism to apply editorconfig files to external files is to map or copy the files into an appropriate location in the filesystem, and then include that location in the pathspec for the editorconfig. In the case of a linked file, this can often be done via a relative path. For instance, say the code layout looks like the following:

```
Repo Root
  | --- LinkedFile.cs
  | --- Subproject
          | --- .editorconfig
          | --- SourceFile1.cs
          | --- SourceFile2.cs
```

In this example the subproject's `.editorconfig` will not apply to `LinkedFile.cs` if it only has the specification `[*.cs]`. If the user wants to explicitly map in the linked file such that the subproject's editorconfig applies, there would need to be a new `.editorconfig` file with a section that points to the linked file with a relative path specification. For instance,

```
Repo Root
  | --- .editorconfig
  | --- LinkedFile.cs
  | --- Subproject
          | --- .editorconfig
          | --- SourceFile1.cs
          | --- SourceFile2.cs
```

and the root .editorconfig would contain

```
[LinkedFile.cs]
option = value
```

For external files, like those provided by source NuGet packages, the NuGet package restore directory can itself be mapped into the repository path. Then, the relative path can be added to the `.editorconfig` like for linked files.
