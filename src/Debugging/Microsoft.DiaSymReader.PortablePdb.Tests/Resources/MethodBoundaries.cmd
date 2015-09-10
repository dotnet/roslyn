csc /target:library /debug+ /optimize- /features:pdb=portable /features:deterministic MethodBoundaries.cs
copy /y MethodBoundaries.pdb MethodBoundaries.pdbx
copy /y MethodBoundaries.dll MethodBoundaries.dllx

csc /target:library /debug+ /optimize- /features:deterministic MethodBoundaries.cs


