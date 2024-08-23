.NET and Terrapin Process
===
The Terrapin system is concerned with validating the provenance of NuPkgs in the .NET ecosystem. Specifically it needs to verify that DLLs in a NuPkg can be rebuilt using publicly available artifacts. 

This document describes the workflow that will be used by Terrapin for validating the provenance of .NET binaries, responsibilities of each team, as well as the tools the .NET team will deliver to enable the service to be successful.

## .NET Tools
The .NET Team will deliver a set of tools to the Terrapin team to provide the necessary validation. The idea in providing a series of tools here vs. a single all in one tool is to help better separate out the different responsibilities involved here and letting the appropriate team handle them:

1. Providing the list of artifacts necessary for validation to succeed (owned by .NET)
1. Acquiring the artifacts necessary for validation (owned by Terrapin)
1. Performing validation and making a determination of provenance (owned by .NET)

It's possible in the future we will end up providing a "all in one" experience tool but for the short term we'll be focusing on keeping these responsibilities and tools separate. 

### dotnet-build-manifest-generator
This is global tool which takes in a DLL / PDB file combination and will generate an artifacts manifest file which lists the set of artifacts necessary to perform a validation. This includes the version of `dotnet-build-validator` which needs to be used to perform the validation. 

This tool is meant to work with binaries produced by earlier versions of the .NET compiler. Every time the contents of the PDB provenance metadata change a new version of this tool will be produced that can account for the metadata. The expectation is that Terrapin will always have the latest version of this tool installed on their service.

### dotnet-build-validator
A global tool which takes in the following arguments: the PE file to validate, its accompanying PDB and a directory containing the artifacts gathered from the artifacts manifest file. The tool will then return whether the provided PDB file can have it's provenance validated.

The tool will have three output states:

1. Validation succeeded
1. Validation failed because the binaries do not match
1. Validation failed because a binary could not be produced

This tool will be sim-shipped with the compiler: every time there is a new compiler, there will be a new version of this tool that uses those compiler binaries. This is important because the compiler does not guarantee deterministic output between versions of the compiler. Even minor version differences of the compiler can produce different IL for the same source code if the code intersects a bug fix or optimization. In order for the validator to function with high fidelity Terrapin must use the version that sim-shipped with the compiler. 

The expectation is that Terrapin will install this as a local tool for a given validation event. That will allow for every validation event to use a different version of `dotnet-build-validator` without having to worry about version conflicts as it would if it were installed as a global tool.

## The Terrapin Service Workflow
The Terrapin service will validate a DLL by going through the following workflow for every DLL in a NuPkg file:

1. Execute `dotnet-build-manifest-generator` on the DLL + PDB combination. 
1. Setup the environment for validation:
    1. Transfer execution to the appropriate operating system listed in the manifest. All future actions are expected to run on this machine.
    1. Install the correct .NET Runtime on the target machine (if necessary)
1. Download the artifacts listed in the artifact manifest file as well as the specified version of `dotnet-build-validator`. 
1. Execute `dotnet-build-validator` providing the DLL, PDB and the directory where the artifacts were downloaded too.

The Terrapin service will download the artifacts into a directory created by Terrapin. The directory will have two directories to store source files and references respectively: sources and references. The name of the artifacts in those directories will be specified in the manifest file. That will put the burden of avoiding collisions and dealing with file path fixup on the compiler team.

## Artifacts Manifest File
The goal of `dotnet-build-manifest-generator` is to provide a manifest file to the Terrapin service which lists all of the artifacts necessary for a validation to be completed as well as the environment needed to run the validation. The set of artifacts that are necessary include:

1. The source files used in the compilation. 
1. The reference DLLs used in the compilation. 

At this time the expectation is Terrapin will need to recreate much of the environment used to create the binaries in order to perform provenance validation. This includes having a matching .NET Runtime version and operating system. This is because OS dependencies, like zip compression version, can impact the deterministic output of the compiler. It's possible our experiments in Terrapin will cause us to re-evaluate how these play into determinism but for now the design is to build validation on top of our existing assumptions here. 

To facilitate this the artifact manifest file will include all of the necessary environment information. This will include the OS RID, the .NET Runtime version and whether a desktop or NET Core runtime was used to run the compiler.

The artifacts manifest file will be a JSON file with the following format:

```json
{
    "sources": [
        { 
            "uri": "https://github.com/dotnet/roslyn/blob/f5517311e37a3883e09dc6daa5069bbc4f44c882/src/Compilers/Shared/Csc.cs",
            "destFileName": "c6fea380dac7392b945fa6c63466746f830c5cc3"
        }
    ],
    "references": [
        {
            "name": "System.Reflection.Metadata.dll",
            "mvid": "51ef3ce0-5a89-4325-b094-8a5970acc676",
            "destFileName": "51ef3ce0-5a89-4325-b094-8a5970acc676",
        }
    ],
    "environment": {
        "compiler-language": "csharp",
        "compiler-version": "3.8.0",
        "os": "windows",
        "os-version": "...",
        "dotnet-runtime": "net6.0.0"
    }
}
```

## Phases of validation 
There are still many open questions surrounding this effort like the impact of code pages, line endings as well as how strongly .NET runtime versions impact determinism of our outputs. The best way to answer these questions is to be pushing real customer code through the service to prove out the real impact of these problems. There are also just general process issues we need to work out between like cadence of delivery as well as just making sure all the pieces fit together. 

To help solve both of these we will be breaking this effort up into a series of milestones that increase in complexity. Each one moving us closer to our goals around provenance of all major NuPkgs in the ecosystem: 

The milestones are:

- Roslyn can successfully validate dotnet/roslyn on a machine that just built dotnet/roslyn. This will give us confidence the underlying provenance checks are achievable. All of the artifacts necessary to perform validation will be located on the machine in well known locations hence that will allow us to separate out "acquisition of artifacts"
- Roslyn will produce versions of dotnet-build-manifest-generator and dotnet-build-validator for Terrapin and they will validate that they can use them on projects they are hosting locally. 
- Rebuild of DLLs on Windows: the target of this milestone is NuPkgs produced on Windows using the .NET Framework compiler. The goal is for us to be able to rebuild the DLLs, whether validation succeeds or fails. This will help us ensure that we have the right process in place between Terrapin and .NET as well that the PDBs do truly contain enough information for us to recreate compilations.
- Validation of DLLs on Windows: the target of this milestone is NuPkgs produced on Windows using the .NET Framework compiler. The goal is for us to validate the provenance of these binaries as well as find common reasons why provenance validation fails. This will let us begin the conversation with .NET customers on how to change their build to make it provenance friendly.
- Validation of DLLs on Windows Part 2: the target of this milestone is NuPkgs produced on Windows using the .NET Core compiler. This will require Terrapin to take the extra environment setup step of installing the correct .NET Core runtime and using that to drive the installation process.
- Validate binaries produced on Linux / Mac: the target of this milestone is NuPkgs produced on Linux / Mac. This will require Terrapin to involve multiple machines in the validation process. It will also help us understand the true impact of OS sub-versions in our ability to do provenance validation.

### FAQ

### What about binaries produced before VS 16.7 was released?
The provenance validation process depends on extra metadata included in the PDB that the compiler uses to generate the artifacts manifest file as well as recreate the compilation. That extra information was first added into PDBs in the Visual Studio 16.7 release. That means any binaries produced before then will fail provenance validation. 

This is a known issue and the understanding is such older NuPkgs will not be supported for provenance validation. 

### How do we handle source generators?
The provenance validation will occur using the output of source generators rather than re-running source generators as part of the validation. This will simplify the process as well as remove the issue of determinism in source generators (customer defined tools that don't have our level of rigor) from the process.

## Open Issues

### Running the validator on .NET Framework
The expectation is that for Windows we should be able to use .NET Core to run the validator for binaries that were produced using the .NET Framework compiler. The runtime differences we are worried about for Unix operating systems don't have the same problems on Windows. Hence any .NET Runtime which can execute the compiler should be able to do provenance validation. 

It's possible we will find differences here though in the real world. In which case we will need to have a mode for `dotnet-build-validator` where by it will shell out to a .NET Framework process to do the validation. 

### Line ending issues
In order for validation to succeed it is important for `dotnet-build-validator` to receive source files that have the same line endings as the one's provided to the original compilation. These factor into items like the checksum of source files stored in PDBs, the content of string literals, etc ... 

It's possible that the information provided by source link is not sufficient to properly recreate files with correct line endings. Consider that a `.gitattributes` file could change how line endings are used on different operating systems. 

This is a problem the debugger team hits today and has solved with a [simple algorithm](https://github.com/dotnet/sourcelink/pull/678/files#diff-3339ce7022ba5de3029d3f15bdf8ca585905bad4f25077ca1b1616c31d841c62R258-R290) we can replicate here. Essentially if the file has consistent LF or CRLF line endings and the SHA don't match, flip the line endings. If that matches then use that version. 

### Future expansions
This document is written to describe how validation will work for the C# and VB compilers but those are not the only tools in the .NET SDK which create or modify DLLs. Other tools include, but is not limited to, the F# compiler and IL trimmer. 

The expectation is these tools can be on boarded with minimal changes to the process outlined here. For example the artifact manifest file can be expanded to include new artifact types, new tools can be consumed by Terrapin to do the validation of various phases of the build.

### What names should we call the tools
As a rule, @jaredpar is terrible at picking names. Should we pick better names for the tools?
