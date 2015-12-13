# Cross Platform Instructions

This guide will walk you through setting up a Linux / Mac box for Roslyn development.  

## Caveats

Linux and Mac support for developing Roslyn is very much a work in progress.  Not everything is supported at the moment and the steps detailed on this page will change very frequently.  If this is an area you are interested in then please check back frequently for updates.

## Building using a pre-made toolset

Right now Roslyn builds on *nix using a mix of Mono and CoreCLR. Patching the right Mono version and acquiring all the tools
can be very difficult, so we've saved pre-built versions on Azure.

Running `make` should download all these toolset binaries and kick off a build using MSBuild running on Mono.

The resulting binaries should end up in the Binaries directory. Specifically, the CoreCLR-compatible C# and VB compilers
(and the necessary CoreCLR runtime) should end up in Binaries/Debug/csccore and Binaries/Debug/vbccore, respectively.
