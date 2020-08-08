Compiler Server
===============

As a performance optimization, the Roslyn C# and VB compilers support using a
server process to execute compilations instead of executing the compilation
in-process. Engineering has focused on three performance benefits from the server:

1. Re-using a single process to do compilation amortizes the cost of JITing the
   compiler code. This is usually the single largest performance benefit.
1. Roslyn requires assembly references to be hydrated into a rich object
   model to perform compilation. The compiler server holds a fixed-size cache of
   metadata symbols, which can help compilation times if most compilations share
   a sizable portion of metadata references (e.g., mscorlib).
1. On Windows process creation itself is expensive, so dispatching compilations
   directly from MSBuild saves creations of csc.exe.

## Usage

The primary way to use the compiler server is via the
`Microsoft.Build.Tasks.CodeAnalysis` MSBuild task, which ships in both the
`dotnet` SDK and Visual Studio. The build task contains a build client which,
rather than executing the `csc.exe` or `vbc.exe`, creates the
`VBCSCompiler.exe` process and dispatches the command line arguments to the
server directly.

If you're not using MSBuild, there's also a flag, `/shared` which can be
passed directly to `csc.exe`, which causes `csc.exe` to behave as a client
and dispatches the actual compilation to the server process. In this mode
`csc.exe` doesn't perform any computation itself, it just dispatches the
command line to the server process (starting one if one does not already
exist), and then reports the results of the compilation (including any
warnings/errors produced).

Importantly, the server is a performance optimization. If server compilation
fails for any reason, the compiler will fall back to stand-alone compilation.
For MSBuild, this means creating a standard `csc.exe` process. For `/shared`
this means continuing the compilation in-process, as though `/shared` were
not passed. No diagnostics are produced in this case, as there are very
common environmental reasons for any given server compilation to fail.

This also implies that it is always safe to kill the compiler server process,
as the client will always recover gracefully.

## Architecture

Roslyn supports a client-server protocol where multiple clients can dispatch
to multiple servers and multiple servers can run concurrently on the machine.
The client and server must run on the same machine, as the client and server
only exchange a command line, not real assets.

## Troubleshooting

To diagnose problems with the server compilation, you can enable an
environment variable that writes a diagnostic log file:

```
(UNIX): export RoslynCommandLineLogFile=/path/to/log/file
(Windows CMD, using directory path): setx RoslynCommandLineLogFile="C:\path\to\log"
(Windows CMD, using file path): setx RoslynCommandLineLogFile="C:\path\to\log\file.log"
```

This file contains diagnostic logging for both the client and server,
which you can distinguish using the `PID` tag for each diagnostic line.
Sample output should look something like this:

```
--- PID=90433 TID=16 Ticks=1048119217: Attempt to open named pipe 'angocke.F.MW9E3dnrZddlsgI8MNFfpJyup'
--- PID=90433 TID=16 Ticks=1048119217: Attempt to connect named pipe 'angocke.F.MW9E3dnrZddlsgI8MNFfpJyup'
--- PID=90436 TID=1 Ticks=244465170: Keep alive timeout is: 600000 milliseconds.
--- PID=90436 TID=1 Ticks=244465182: Constructing pipe 'angocke.F.MW9E3dnrZddlsgI8MNFfpJyup'.
--- PID=90436 TID=1 Ticks=244465182: Successfully constructed pipe 'angocke.F.MW9E3dnrZddlsgI8MNFfpJyup'.
```

Here PID `90433` is the client, as it's attempting to open a connection
(named pipe), while PID `90436` is the server, as it is constructing the
named client. If it succeeds, the result looks something like:

```
--- PID=90436 TID=8 Ticks=244476082: ****C# Compilation complete.
****Return code: 0
****Output:
Microsoft (R) Visual C# Compiler version 2.9.0.63018 (e3fa7d6a)
Copyright (C) Microsoft Corporation. All rights reserved.
```

Otherwise, you may be able to discover what's going wrong from other output,
like exception messages.
