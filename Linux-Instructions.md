This guide will walk you through setting up a Linux / Mac box for Roslyn development.  

# Caveats

Linux and Mac support for developing Roslyn is very much a work in progress.  Not everything is supported at the moment and the steps detailed on this page will change very frequently.  If this is an area you are interested in then please check back frequently for updates. 

# Acquiring Mono 

Roslyn requires bug fixes not present in the latest release of mono.  Hence you will need to acquire mono from a different channel in order to develop on Roslyn.

### Linux

These instructions are written assuming the Ubuntu 14.04 LTS, since that's the distro the team uses.

The easiest way to acquire Mono on Ubuntu, or any Debian based system, is to use the provided [continuous integration packages](http://www.mono-project.com/docs/getting-started/install/linux/ci-packages).  Any recent package will do.  There is a script checked into our repro that automates getting the latest package:

```
$> cd roslyn
$roslyn> ./build/linux/setup-snapshot.sh
```

After this script runs you can enable the latest mono snapshot from a shell prompt by running the following:

```
$> . mono-snapshot mono
```

### Mac

The only supported way to get Mono on Mac for Roslyn development is to build from source.  Mono has boiled this down to a simple set of steps which is described here:

- [Compiling Mono on Mac](http://www.mono-project.com/docs/compiling-mono/mac/#building-mono-from-a-git-source-code-checkout)

At the moment mono master does not have all of the bug fixes Roslyn is consuming.  Hence you will need to pull from a different branch until these all get merged:

``` 
$> cd mono
$mono> git remote add jaredpar git@github.com:jaredpar/mono.git
$mono> git checkout -b build-roslyn jaredpar/build-roslyn
```

# Building Roslyn on Linux / Mac 

The first step is to ensure that the custom installed version of mono is first on your path.  This is what `xbuild` will grab when running our tools and compilers during the build.  

To build Roslyn run the following:

```
$> xbuild /p:SignAssembly=False /p:DebugSymbols=False src/Compilers/CSharp/csc/csc.csproj
$> xbuild /p:SignAssembly=False /p:DebugSymbols=False src/Compilers/VisualBasic/vbc/vbc.csproj
```

This will produce `csc.exe` and `vbc.exe` in `Binaries\Debug`.  
