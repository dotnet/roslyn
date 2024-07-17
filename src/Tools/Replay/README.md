# Replay

This is a tool for replaying the compilation events from a [binary log][binary-log] directly into the compiler server. This is very useful for profiling the compiler as it removes the overhead of msbuild from the logs. This gives a much simpler view of the compiler's performance.

The other advantage is the tool allows for easy testing of performance changes to the compiler. Developers can have two branches of the code, one with performance changes and one without, then run `replay` against the same binary log and analyze the performance differences.

Note: this is only supported for replaying binary logs on the same machine they were built.

## Usage

The first step is to run a build locally and collect a binary log. For example:

```cmd
e:\code\example> dotnet msbuild -bl -v:m -m Example.sln
```

Then run the replay tool against that log.

```cmd
e:\code\roslyn> cd src\Tools\Replay
e:\code\roslyn\src\Tools\Replay> dotnet run --framework net472 --configuration Release e:\code\example\msbuild.binlog
```

This runs all of the compilation events in the binary log against the compiler and outputs the results to `e:\temp`.

[binary-log]: https://github.com/dotnet/msbuild/blob/main/documentation/wiki/Binary-Log.md

## Example Usage

### dotnet trace

To profile with `dotnet trace` first run with the `-w` option to get the PID of the compiler server. Then start up `dotnet trace` against that PID and have replay continue

Console 1

```cmd
e:\code\roslyn\src\Tools\Replay> dotnet run --framework net8.0 --configuration Release e:\code\example\msbuild.binlog -w
Binary Log: E:\code\example\msbuild.binlog
Client Directory: E:\code\roslyn\artifacts\bin\Replay\Release\net8.0\
Output Directory: E:\code\roslyn\src\Tools\Replay\output
Pipe Name: 0254ccf8-294e-4b8f-a606-70f105b9e4a1
Parallel: 6

Starting server
Process Id: 48752
Press any key to continue
```

Console 2

```cmd
e:\users\jaredpar> dotnet trace collect --profile gc-verbose -p 48752 --providers Microsoft-CodeAnalysis-General

Provider Name                           Keywords            Level               Enabled By
Microsoft-CodeAnalysis-General          0x0000000000000000  Informational(4)    --providers
Microsoft-Windows-DotNETRuntime         0x0000000000008003  Verbose(5)          --profile

Process        : C:\Program Files\dotnet\dotnet.exe
Output File    : C:\Users\jaredpar\dotnet.exe_20240502_083035.nettrace

[00:00:01:13]   Recording trace 379.5907 (MB)
```

Then go back to Console 1 and press any key to continue. The trace will automatically stop once the replay operation is complete.