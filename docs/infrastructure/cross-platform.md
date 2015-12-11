# Cross Platform Instructions

This guide will walk you through setting up a Linux / Mac box for Roslyn development.  

## Caveats

Linux and Mac support for developing Roslyn is very much a work in progress.  Not everything is supported at the moment and the steps detailed on this page will change very frequently.  If this is an area you are interested in then please check back frequently for updates.

## Acquiring Mono

Roslyn requires bug fixes not present in the latest release of mono.  Hence you will need to acquire mono from a different channel in order to develop on Roslyn.  

### Azure drops

The easiest way to acquire a compatible version is to download it from our Azure storage.  This is the version used on our CI system and hence will work with the latest sources:

- [Mac Mono Bundle](https://dotnetci.blob.core.windows.net/roslyn/mono.mac.1.tar.bz2)
- [Linux Mono Bundle](https://dotnetci.blob.core.windows.net/roslyn/mono.linux.1.tar.bz2)

This file must be unzipped into `/tmp` in order to function correctly.  Once unzipped simply add the `/tmp/mono.mac.1/bin` folder to `$PATH` and you should be able to build, edit and run test for CrossPlatform.sln.  

### Build from source

The universal working method for Linux and Mac is to build Mono from source.  This actually quite straightforward as Mono has boiled this down to a simple set of steps which is described here:

- [Compiling Mono on Mac](http://www.mono-project.com/docs/compiling-mono/mac/#building-mono-from-a-git-source-code-checkout)

The Mono master branch has merged in all of the necessary fixes and should work.  Roslyn though currently tests against a very specific build hence this will produce the most reliable results.  

```
$> cd mono
$mono> git remote add jaredpar git@github.com:jaredpar/mono.git
$mono> git fetch jaredpar
$mono> git checkout -b build-roslyn jaredpar/build-roslyn
```

Roslyn depends on the Portable Class Libraries to build which is not standard on Mono.  Hence the installation of Mono being used must be patched in order to build Roslyn.  The [setup-pcl.sh](https://github.com/dotnet/roslyn/blob/master/build/linux/setup-pcl.sh) script takes care of this.  

```
$> ./roslyn/build/linux/setup-pcl.sh ~/builds/mono
```

## Configuring Mono

Note: This script may need to be used with `sudo` depending on where mono was installed.

Additionally we need to update the certificate store so that NuGet can function correctly.  

```
$> sudo roslyn/build/linux/setup-certs.sh
```

## Building Roslyn on Linux / Mac

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
