# Cross Platform Instructions

## Caveats

Linux and Mac support for developing Roslyn is very much a work in progress.  Not everything is supported at the moment and the steps detailed on this page will change very frequently.  If this is an area you are interested in then please check back frequently for updates.

## Building

Build all cross-platform projects with: 

```
cd <roslyn-git-directory>
./build.sh --restore
```

The script will install .NET Core to `.dotnet` directory if it is not found on `$PATH`. It will then restore required NuGet packages and build `Compilers.slnf` solution. The option `--restore` (or `-r`) is only needed when building for the first time, or when NuGet references change.

## Using the compiler

After building, there will be a `csc` in the `artifacts/bin/csc/Debug/netcoreapp3.1` and `artifacts/bin/csc/Debug/net472` directories. Use the former to run on .NET Core and the latter on Mono.

### Running `csc.exe` on Mono

Use `mono artifacts/bin/csc/Debug/net472/csc.exe -noconfig` to run the C# compiler.

`-noconfig` is needed because `csc.exe` by default references the `csc.rsp` file next to it. This is the Windows response file, so not all 
assemblies are present when running on Mono. Pass the `-noconfig` option to ignore this response file.
