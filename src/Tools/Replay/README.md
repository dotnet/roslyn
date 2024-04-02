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
e:\code\roslyn\src\Tools\Replay> dotnet run --framework net472 e:\code\example\msbuild.binlog -o e:\temp
```

This runs all of the compilation events in the binary log against the compiler and outputs the results to `e:\temp`.

[binary-log]: https://github.com/dotnet/msbuild/blob/main/documentation/wiki/Binary-Log.md