csc /target:library /debug:portable /optimize- /deterministic Documents.cs
copy /y Documents.pdb Documents.pdbx
copy /y Documents.dll Documents.dllx

csc /target:library /debug+ /optimize- /deterministic Documents.cs


