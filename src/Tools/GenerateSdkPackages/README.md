# Generate SDK Packages

This is a collection of tools for generating a set of NuGet packages for the VS SDK and updating our repo to consume them.  This is a temporary solution until we work with the VS SDK team to help address a couple of issues with how their packages are produced.

- make-all.ps1: Generates all of the NuGet packages we need for the VS SDK
- change-all.ps1: Changes all our project.json files to reference a new VS SDK version 


