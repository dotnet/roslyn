csc /target:library /debug:portable /optimize- /deterministic MethodBoundaries.cs
copy /y MethodBoundaries.pdb MethodBoundaries.pdbx
copy /y MethodBoundaries.dll MethodBoundaries.dllx

csc /target:library /debug+ /optimize- /deterministic MethodBoundaries.cs


