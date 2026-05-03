# BuildBoss

This is a tool to validate the state of our solutions, project files and other build artifacts.  This helps ensure our build is correct and predictable across the variety of frameworks we build and ship against.  The usage of the tool is straight forward:

``` cmd
> BuildBoss.exe <solution / project / targets path / build log paths>
```

This tool is run on every CI job against our important solution files: [Roslyn.slnx](https://github.com/dotnet/roslyn/blob/main/Roslyn.slnx).

Violations reported are important to fix as they represent correctness issues in our build.  Many of the properties verified represent problems that otherwise won't be verified at check in time.

## Project Content Verified 

### Central properties

These are the collection of properties which are controlled in our central targets file.  Often these properties are unconditionally set in the targets file because there is only one valid setting for the entire repo.  

The tool will verify these are not specified in individual project files.  Allowing these properties causes confusion for developers because the value would be ignored by the build if different from the central targets file.  Hence these are not allowed.  

This list includes:

- FileAlignment
- SolutionDir
- Configuration
- Deterministic
- CheckForOverflowUnderflow
- RemoveIntegerChecks

### Unnecessary properties

There are a number of properties which are simply unnecessary for build.  They are instead artifacts of Visual Studio experiences that don't need to be persisted.  These are banned to keep our project files concise and containing only the necessary elements for building the project.  This list includes:

- SchemaVersion
- OldToolsVersion
- RestorePackages
- FileUpgradeFlags
- UpgradeBackupLocation

### Transitive references

Projects which represent full deployments must have a complete set of project references declared in the project file. Or in other words the declared set of project references much match the transitive closure of project references. Any gap between the two sets won't be deployed on build which in turn will break F5, testing, etc ...

### Classifying projects

Our build process depends on being able to correctly classify our projects: exe, VSIX, dll, etc ...  This can typically be inferred from properties like OutputType.  But in other occasions it requires a more declarative entry via the `<RoslynProjectType>` property.  The tool will catch places where projects are incorrectly classified.

This could be done using MSBuild targets but the logic is hard to follow and complicates the build.  It's easier and more readable to have a declarative entry in the file.

## Solution Content Verified

The solution file will be checked to ensure it includes all of the necessary project files.  When project files are missing but included as project references it can produce unexpected build outputs.  

This is best demonstrated by example.  Consider the following setup:

- Project Util.csproj produces Util.exe
- Project App.csproj produces App.exe and references Util.csproj
- Solution App.sln includes App.csproj only

Now consider when App.sln is built with the following command line:

``` cmd
msbuild /p:Configuration=Release App.sln
```

This will cause both App.csproj and Util.csproj to be built.  However only App.csproj will see the specified configuration value, Util.csproj will get the default value as calculated by its targets.  To ensure a consistent build all the projects must be included in the solution.

## Target Content Verified

This stage verifies the contents of our central targets files.  Namely the props and targets which contain the central logic for running our build.

## Build Logs

These are log files produced by the MSBuild Structured Logger Tool:

> https://github.com/KirillOsenkov/MSBuildStructuredLog

The log files produced by this tool contain much of the content of a diagnostic log but in well structured XML.  This makes it easy for tools to analyze.

BuildBoss makes use of this log to ensure our build doesn't have any double writes.  That is the build does not write to the same output path twice.  Doing so means our build is incorrect and subject to flaws such as race conditions and incorrect deployments.
