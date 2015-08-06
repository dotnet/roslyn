csc /target:library /debug+ /features:pdb=portable  /optimize- /features:deterministic Documents.cs
copy /y Documents.pdb Documents.pdbx
del Documents.pdb
