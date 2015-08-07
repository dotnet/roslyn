csc /target:library /debug+ /features:pdb=portable /optimize- /features:deterministic Async.cs
copy /y Async.pdb Async.pdbx
del Async.pdb

