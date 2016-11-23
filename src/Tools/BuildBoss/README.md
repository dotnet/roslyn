# BuildBoss

This is a tool to validate the correctness of various build artifacts.  The usage is straight foward:

> BuildBoss.exe <solution / project / targets path / build log paths>

## Solution / Project Content Verified 

### Central properties

There are a number of MSBulid properties which are controlled by our central build files.  Specifying them in an indivdiual project is potentially breaking our build expectations or simply creating redundant data.  Hence these are not allowed.  Some examples:

- FileAlignment
- SolutionDir
- Configuration

### Transitive references

Projects which represent full deployments must have a complete set of project references declared in the project file.  Or in other words the declared set of project references much match the tranistive closure of project references.  Any gap between the two sets won't be deployed on build which in turn will break F5, testing, etc ...

### Unnecessary properties

There are a number of properties which are simple unnecessary for build.  They are instead artifacts of Visual Studio experiences that don't need to be persisted.  This includes:

- SchemaVersion
- OldToolsVersion

### Classifying projects

Our build process depends on being able to correctly classify our projects.  This can typically be inferred from proerties like OutputType.  But in other occasions it requires a more declarative entry via the RoslynProjectType property.  The tool will catch places where projects are incorrectly classified.

This could be done using MSBuild targets but the logic is hard to follow and complicates the build.  It's easier to verify here.

## Target Content Verified

This stage verifies the contents of our central targets files.  Namely the props and targets which contain the central logic for running our build.

## Build Logs

These are log files produced by the MSBuild Structured Logger Tool:

> https://github.com/KirillOsenkov/MSBuildStructuredLog

The log files produced by this tool contain much of the content of a diagnostic log but in well structured XML.  This makes it easy for tools to analyze.

BuildBoss makes use of this log to ensure our build doesn't have any double writes.  That is the build does not write to the same output path twice.  Doing so means our build is incorrect and subject to flaws such as race conditions and incorrect deployments.
