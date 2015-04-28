This guide will walk you through setting up a Linux / Mac box for Roslyn development.  

# Caveats

Linux and Mac support for developing Roslyn is very much a work in progress.  Not everything is supported at the moment and the steps detailed on this page will change very frequently.  If this is an area you are interested in then please check back frequently for updates. 

# Acquiring Mono 

Roslyn requires bug fixes not present in the latest release of mono.  Hence you will need to acquire mono from a different channel in order to develop on Roslyn.  

The universal working method for Linux and Mac is to build Mono from source.  This actually quite straightforward as Mono has boiled this down to a simple set of steps which is described here:

- [Compiling Mono on Mac](http://www.mono-project.com/docs/compiling-mono/mac/#building-mono-from-a-git-source-code-checkout)

The Mono master branch has merged in all of the necessary fixes and should work.  Roslyn though currently tests against a very specific build hence this will produce the most reliable results.  
``` 
$> cd mono
$mono> git remote add jaredpar git@github.com:jaredpar/mono.git
$mono> git checkout -b build-roslyn jaredpar/build-roslyn
```
### Debian

These instructions are written assuming the Ubuntu 14.04 LTS, since that's the distro the team uses.

The easiest way to acquire Mono on Ubuntu, or any Debian based system, is to use the provided [continuous integration packages](http://www.mono-project.com/docs/getting-started/install/linux/ci-packages).  Any recent package should do as all of the needed fixes are checked into the Mono master branch.  There is a script checked into our repro that automates getting the latest package:

```
$> cd roslyn
$roslyn> sudo ./build/linux/setup-snapshot.sh
```

After this script runs you can enable the latest mono snapshot from a shell prompt by running the following:

```
$> . mono-snapshot mono
```
# Configuring Mono

The standard Mono installation needs to be patched with the Portable Class Libraries in order to build parts of Roslyn.  The [setup-pcl.sh] script takes care of this.  Simply point it to the directory where mono is installed and it will take care of the rest.  

```
$> ./roslyn/builds/linux/setup-pcl.sh ~/builds/mono 
```

Note: This script may need to be used with `sudo` depending on where mono was installed. 

Additionally we need to update the certificate store so that NuGet can function correctly.  

```
$> sudo roslyn/builds/linux/setup-certs.sh
```

# Building Roslyn on Linux / Mac 

The first step is to ensure that the custom installed version of mono is first on your path.  This is what `xbuild` will grab when running our tools and compilers during the build.  

Next we need to restore NuGet packages.  Roslyn compiles itself via a NuGet package and it must be installed before the build will succeed:

```
$> mono src/.nuget/NuGet.exe restore src/Roslyn.sln -packagesdirectory packages
```

Once the NuGet packages are available CrossPlatform.sln can be built directly.

```
$> xbuild /p:SignAssembly=False /p:DebugSymbols=False src/CrossPlatform.sln
```

This will produce a number of binaries including `csc.exe` and `vbc.exe` in `Binaries\Debug`.  
