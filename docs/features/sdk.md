Moving dotnet/roslyn to the new SDK
=============

# Overview 

This document tracks our progress in moving Roslyn to the new SDK and project system.  The goal of
this initial push is explicitly to remove the use of project.json from our code base.  In doing 
many of our projects will move to target the new SDK, but not all.  Once this change is merged 
we will do further cleanup to move everything to the new SDK.

The projects in our repo that need to be converted break down into the following categories:

- PCL: This will be converted to the new SDK and Project System
- Desktop 4.6: Package consumption will change to PackageReference 
- Desktop 2.0: Package consumption will change to PackageReference

The conversion of our projects is being almost entirely automated with the [ConvertPackageRef Tool](https://github.com/jaredpar/ConvertPackageRef).  
This automation is always checked in as a single commit titled "Generated code".  It is frequently 
replaced via a rebase as the tool improves.

# Branch State 

All deliberately disabled features are marked with the comment "DO NOT MERGE".  These entries will 
be removed + fixed before merging

## Working

- Developer work flow: Restore.cmd, Build.cmd, Test.cmd
- Editing in Visual Studio 

## Jenkins Legs

This set of Jenkins legs is considered functional and must pass on every merge:

- Unit tests debug / release on x86 / amd64 
- Build correctness
- Microbuild
- CoreClr
- Linux
- Integration Tests

## Big Items

These are the big items that are broken and need some work

- RepoUtil: needs to understand PackageReference and can be massively simplified now
- Delete the DeployCompilerGeneratorToolsRuntime project.  Not needed anymore 

