
This is a simple compiler perf benchmark suite. Run the 'run-perf.ps1'
script to run the suite.

Currently the only test that is supported is running csc.exe on CoreCLR
against a frozen copy of the source code for an old version of
Microsoft.CodeAnalysis.dll. The benchmark will gather perf numbers for
the current commit (HEAD) and the parent commit (HEAD^) and compare
them.
