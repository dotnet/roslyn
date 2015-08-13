csc /target:library /debug+ /features:pdb=portable /optimize- /features:deterministic Documents.cs
copy /y Documents.pdb Documents.pdbx
copy /y Documents.dll Documents.dllx

csc /target:library /debug+ /optimize- /features:deterministic Documents.cs


