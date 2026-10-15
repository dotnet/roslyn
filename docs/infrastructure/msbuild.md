# MSBuild usage

## Supporting different MSBuilds

This repo must support the ability to build with a number of different MSBuild configurations:

- MSBuild via Visual Studio: this happens when developers open Roslyn.slnx in Visual Studio and execute the build action.  This uses desktop MSBuild to drive the solution. 
- MSBuild via CLI: the cross platform portions of the repo are built via the CLI.  This is a subset of the code contained in Roslyn.slnx. 
- MSBuild xcopy: an xcopyable version of MSBuild that is used to run many of our Jenkins legs.  It allows Roslyn to build and run tests on a completely fresh Windows image (no pre-reqs).  The [xcopy-msbuild](https://github.com/jaredpar/xcopy-msbuild) project is responsible for building this image. 
- BuildTools: this is a collection of tools produced by [dotnet/buildtools](https://github.com/dotnet/buildtools) which build a number of dotnet repos. 

This places a small burden on our repo to keep our build props / targets files simple to avoid any odd conflicts.  This is rarely an issue at this point.

## Picking MSBuild

Given our repo supports multiple MSBuild versions, it must pick one to use when building, restoring, etc ...  The preference list is as follows:

1. Developer command prompt: when invoked from inside a developer command prompt the associated MSBuild will be used. 
1. Machine MSBuild: when invoked on a machine with MSBuild 15.0 installed, the first mentioned instance will be used. 
1. XCopy MSBuild: fallback when no other option is available

## Opt-in graph builds

Use `Build.cmd -restore -pack -graph -binaryLog` on Windows, or
`./build.sh --restore --build --pack --graph --binaryLog` on Linux and macOS.
Graph mode is opt-in; the default build behavior is unchanged. Solution filters
can be used for compilation, for example
`Build.cmd -restore -graph -solution Compilers.slnf`.
Repository packaging and insertion validation require the full solution.

Restore runs before graph construction. Compilation and packaging use separate
MSBuild `-graph` invocations; packaging uses `NoBuild=true` to consume the graph
compilation outputs without recursively compiling projects again. Arcade still
drives the surrounding restore, post-build, signing, and publishing phases.
`-rebuild` uses the graph's Rebuild target before packaging. Bootstrap and analyzer
options continue to apply to the solution build.

With binary logging enabled, the normal log records orchestration, `Graph-`
prefixed logs record solution compilation, and `Pack-Graph-` prefixed logs record
packaging. Inspect the graph logs when diagnosing dependency scheduling, runtime
property propagation, or packaging failures. Static-graph NuGet restore alone is
not evidence that compilation or packaging used graph scheduling.
When running BuildBoss after a graph build with the default log names, pass
`--check-graph-build-logs` to check both graph logs for double writes as well as
the orchestration log. Missing graph logs fail this check.

Graph mode is not supported for official, product, VMR, or source-only builds.
The `Correctness_Build_Artifacts` CI job exercises graph compilation and packaging
and retains its existing artifact and generated-source checks.
