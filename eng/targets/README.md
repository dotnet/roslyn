# Build Targets

## Strategy 

The goal of our targets files is to be the container of all of our build logic.  Our repo has several hundred projects that target various platforms and products.  The requirements of these platforms / products can change frequently during a release.  Reacting to these changes is easier when the build logic is centralized.  

To accomplish this all of the build logic is contained in our central targets files.  This includes logic around packaging, deployment, nuget targeting, signing, etc...  The preference is to contain logic in XML when reasonable but a custom build task is used when appropriate. 

The individual project files contain declaritive information only.  They inherit their build logic by importing [Settings.props](Settings.props) at the start and [Imports.targets](Imports.targets) at the conclusion.  

## General rules

There are a set of general rules to follow for props and targets files:

- props files
    - Do use Import for props files.
    - Do not use Import for targets files.
    - Do use UsingTask elements.
    - Do not use Target elements
- targets files
    - Do use Import for targets files.
    - Do not use Import for props files.
    - Do use Task elements.

## Files

This section describes the purpose and layout of the important files here.

### Settings.props

This file is importanted at the start of projects.  There are two primary purposes of this file:

- Import standard props files. 
- Define the set of properties which ...
    - Projects reasonably need to ready, modify or evaluate.
    - Are necessary for importing standard target files.

Properties like $(Configuration) and $(OutDir) are reasonable to define here as projects can make reasonable use of these values.  For example:

``` xml
<OutDir>$(OutDir)\Shared</OutDir>
```

Properties like $(Deterministic), $(CheckForOverflowUnderflow), etc ... should not be defined here.  No reasonable project will read these values as they are a requirement for how we produce binaries.  These should all instead be defined in Imports.targets.

The general structure of this file is:

- PropertyGroup for setting all of the necessary properties.
- UsingTask for any of our custom tasks.
- Import elements for standard targets.

### Imports.targets

This file is imported at the end of projects.  The primary purposes of this file are:

- Define all properties which are necessary to build the product. 
- Use Import and custom Targets to define the necessary build logic.

Properties which are central to our build should be defined here.  For example $(Deterministic) is unconditionally defined in this file.  No project should be able to override it because it's important to the correctness of our build.  To protect against accidentally setting this property and having it silently ignored, such properties should also be banned in BuildBoss.


The general structure of this file is:

- PropertyGroup for setting properties that are necessary for build or for correctly evaluating the following Imports.
- Import all of the external targets.
- PropertyGroup to adjust properties set by the external projects.
- Custom Targets for our build.

### Version property files

There are a number props files that just control version numbers:

- Packages.props: version numbers for all of the NuGet packages we use. Versions here presumably will change as new packages are available.
- FixedPackages.props: similar to Packages.props but for packages that should never change versions. They are fixed and do not change when new packages are available. Typically used for producing SDKs for older VS / .NET versions.
- Tools.props: version numbers for non-Nuget assets. 

