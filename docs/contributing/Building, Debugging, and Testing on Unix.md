# Building, Debugging and Testing on Unix
This guide is meant to help developers setup an environment for debugging / contributing to Roslyn from Linux. 
Particularly for developers who aren't experienced with .NET Core development on Linux. 

## Working with the code
1. Install the [.NET Core SDK](https://www.microsoft.com/net/download/core)
1. Clone git@github.com:dotnet/roslyn.git
1. Run `./build.sh --restore`
1. Run `./build.sh --build`

## Running Tests
The unit tests can be executed by running `./build.sh --test`

## GitHub
The best way to clone and push is to use SSH. On Windows you typically use HTTPS and this is not directly compatible
with two factor authentication (requires a PAT). The SSH setup is much simpler and GitHub has a great HOWTO for 
getting this setup.

https://help.github.com/articles/connecting-to-github-with-ssh/

## Debugging test failures
The best way to debug is using lldb with the SOS plugin. This is the same SOS as used in WinDbg and if you're familiar
with it then lldb debugging will be pretty straight forward. 

Here are the CoreCLR guidelines for Linux debugging:

- https://github.com/dotnet/coreclr/blob/master/Documentation/botr/xplat-minidump-generation.md
- https://github.com/dotnet/coreclr/blob/master/Documentation/building/debugging-instructions.md#debugging-core-dumps-with-lldb.

Corrections:
- LLDB and createdump must be run as root
- `dotnet tool install -g dotnet-symbol` must be run from `$HOME` 

Furthermore the version of SOS that comes with the runtime will likely not work with the lldb you have. In order to 
use SOS you will need to build it by hand

1. Clone git@github.com:dotnet/diagnostics.git
1. Run `./build.sh`

This will produce libsosplugin.so that can be used in lldb. Once it's built you can start lldb with the following 
command line (replace `/home/jaredpar` as appropriate):

``` bash
> sudo lldb -o "plugin load /home/jaredpar/code/diagnostics/artifacts/Debug/bin/Linux.x64/libsosplugin.so" 
/home/jaredpar/code/roslyn/Binaries/Tools/dotnet/dotnet
```

From there you should be able to attach to running processes or load up coredumps.

## Ubuntu 18.04
The recommended OS for developing Roslyn is Ubuntu 18.04. This guide was written using Ubuntu 18.04 but should be 
applicable to most Linux enviroments. Ubuntu 18.04 was chosen here due to it's support for enhanced VMs in Hyper-V. 
This makes it easier to use from a Windows machine.

### Hyper-V
When using Hyper-V to develop you will want to enable enhanced mode. This allows for the VM to be full screen, have 
clipboard access and support additional devices. Ubuntu 18.04 is the first Linux OS supported in this mode. The 
mileage with other distros may vary.

Enhanced mode support is still fairly new and does require a few manual steps. They are all covered in this 
blog post:

https://blogs.technet.microsoft.com/virtualization/2018/02/28/sneak-peek-taking-a-spin-with-enhanced-linux-vms/

Following the steps on that blog post will get you to a point where you can the features you are looking for. There are
 a couple of deviations from the instructions there:

- There is no longer a config-user.sh script
- cd into `ubuntu/18.04` instead of `ubuntu/16.04`

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
