# Building, Debugging and Testing on Unix
This guide is meant to help developers setup an environment for debugging / contributing to Roslyn from Linux.
Particularly for developers who aren't experienced with .NET Core development on Linux.

## Working with the code
1. Ensure the commands `git` and `curl` are available
1. Clone git@github.com:dotnet/roslyn.git
1. Run `./build.sh --restore`
1. Run `./build.sh --build`

## Working in Visual Studio Code
1. Install [VS Code](https://code.visualstudio.com/Download)
    - After you install VS Code, install the [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)
    - Important tip: You can look up editor commands by name by hitting *Ctrl+Shift+P*, or by hitting *Ctrl+P* and typing a `>` character. This will help you get familiar with editor commands mentioned below. On a Mac, use *⌘* instead of *Ctrl*.
2. Install the [.NET 7.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0).
3. You can build from VS Code by running the *Run Build Task* command, then selecting an appropriate task such as *build* or *build current project* (the latter builds the containing project for the current file you're viewing in the editor).
4. You can run tests from VS Code by opening a test class in the editor, then using the *Run Tests in Context* and *Debug Tests in Context* editor commands. You may want to bind these commands to keyboard shortcuts that match their Visual Studio equivalents (**Ctrl+R, T** for *Run Tests in Context* and **Ctrl+R, Ctrl+T** for *Debug Tests in Context*).

## Running Tests
The unit tests can be executed by running `./build.sh --test`.

To run all tests in a single project, it's recommended to use the `dotnet test path/to/project` command.

## GitHub
The best way to clone and push is to use SSH. On Windows you typically use HTTPS and this is not directly compatible
with two factor authentication (requires a PAT). The SSH setup is much simpler and GitHub has a great HOWTO for
getting this setup.

https://help.github.com/articles/connecting-to-github-with-ssh/

## Debugging test failures
The best way to debug is using lldb with the SOS plugin. This is the same SOS as used in WinDbg and if you're familiar
with it then lldb debugging will be pretty straight forward.

The [dotnet/diagnostics](https://github.com/dotnet/diagnostics) repo has more information:

- [Getting LLDB](https://github.com/dotnet/diagnostics/blob/main/documentation/lldb/linux-instructions.md)
- [Installing SOS](https://github.com/dotnet/diagnostics/blob/main/documentation/installing-sos-instructions.md)
- [Using SOS](https://github.com/dotnet/diagnostics/blob/main/documentation/sos-debugging-extension.md)

CoreCLR also has some guidelines for specific Linux debugging scenarios:

- https://github.com/dotnet/coreclr/blob/main/Documentation/botr/xplat-minidump-generation.md
- https://github.com/dotnet/coreclr/blob/main/Documentation/building/debugging-instructions.md#debugging-core-dumps-with-lldb.

Corrections:
- LLDB and createdump must be run as root
- `dotnet tool install -g dotnet-symbol` must be run from `$HOME`

### Core Dumps
The CoreClr does not used the standard core dumping mechanisms on Linux. Instead you must specify via
environment variables that you want a core dump to be produced. The simplest setup is to do the following:

```
> export COMPlus_DbgEnableMiniDump=1
> export COMPlus_DbgMiniDumpType=4
```

This will cause full memory dumps to be produced which can then be loaded into LLDB.

A preview of [dotnet-dump](https://github.com/dotnet/diagnostics/blob/main/documentation/dotnet-dump-instructions.md) is also available for interactively creating and analyzing dumps.

### GC stress failures
When you suspect there is a GC failure related to your test then you can use the following environment variables
to help track it down.

```
> export COMPlus_HeapVerify=1
> export COMPlus_gcConcurrent=1
```

The `COMPlus_HeapVerify` variable causes GC to run a verification routine on every entry and exit. Will crash with
a more actionable trace for the GC team.

The `COMPlus_gcConcurrent` variable removes concurrency in the GC. This helps isolate whether this is a GC failure
or memory corruption outside the GC. This should be set after you use `COMPLUS_HeapVerify` to determine it is
indeed crashing in the GC.

Note: this variables can also be used on Windows as well.

## Ubuntu 18.04
The recommended OS for developing Roslyn is Ubuntu 18.04. This guide was written using Ubuntu 18.04 but should be
applicable to most Linux environments. Ubuntu 18.04 was chosen here due to it's support for enhanced VMs in Hyper-V.
This makes it easier to use from a Windows machine: full screen, copy / paste, etc ...

### Hyper-V
Hyper-V has a builtin Ubuntu 18.04 image which supports enhanced mode. Here is a tutorial for creating
such an image:

https://docs.microsoft.com/en-us/virtualization/hyper-v-on-windows/quick-start/quick-create-virtual-machine

When following this make sure to:
1. Click Installation Source and uncheck "Windows Secure Boot"
1. Complete the Ubuntu installation wizard. Full screen mode won't be available until this is done.

Overall this takes about 5-10 minutes to complete.

### Source Link
Many of the repositories that need to be built use source link and it crashes on Ubuntu 18.04 due to dependency changes.
To disable source link add the following to the `Directory.Build.props` file in the root of the repository.

``` xml
<EnableSourceControlManagerQueries>false</EnableSourceControlManagerQueries>
<EnableSourceLink>false</EnableSourceLink>
<DeterministicSourcePaths>false</DeterministicSourcePaths>
```
### Prerequisites

Make sure to install the following via `apt install`

- clang
- lldb
- cmake
- xrdp
