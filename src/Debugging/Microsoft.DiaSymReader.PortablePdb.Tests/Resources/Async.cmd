csc /target:library /debug+ /features:pdb=portable /optimize- /features:deterministic Async.cs
copy /y Async.pdb Async.pdbx
copy /y Async.dll Async.dllx

csc /target:library /debug+ /optimize- /features:deterministic Async.cs


