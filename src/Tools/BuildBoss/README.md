# BuildBoss

This is a tool to validate the content of our solutions and project files.  The usage is very straight forward:

> BuildBoss.exe <solution path>

## Content verified 

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


