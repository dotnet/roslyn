csc /target:library /debug:portable /optimize- /deterministic Async.cs
copy /y Async.pdb Async.pdbx
copy /y Async.dll Async.dllx

csc /target:library /debug+ /optimize- /deterministic Async.cs


