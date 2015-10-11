csc /target:library /debug:portable /optimize- /deterministic Async.cs
copy /y Async.pdb Async.pdbx
del Async.pdb

