# File-based programs VS Code support

See also [dotnet-run-file.md](https://github.com/dotnet/sdk/blob/main/documentation/general/dotnet-run-file.md).

## Feature overview

A file-based program embeds a subset of MSBuild project capabilities into C# code, allowing single files to stand alone as ordinary projects.

The following is a file-based program:

```cs
Console.WriteLine("Hello World!");
```

So is the following:

```cs
#!/usr/bin/env dotnet run
#:sdk Microsoft.Net.Sdk
#:package Newtonsoft.Json@13.0.3
#:property LangVersion=preview

using Newtonsoft.Json;

Main();

void Main()
{
    if (args is not [_, var jsonPath, ..])
    {
        Console.Error.WriteLine("Usage: app <json-file>");
        return;
    }

    var json = File.ReadAllText(jsonPath);
    var data = JsonConvert.DeserializeObject<Data>(json);
    // ...
}

record Data(string field1, int field2);
```

This basically works by having the `dotnet` command line interpret the `#:` directives in source files, produce a C# project XML document in memory, and pass it off to MSBuild. The in-memory project is sometimes called a "virtual project".

## Miscellaneous files changes

There is a long-standing backlog item to enhance the experience of working with miscellaneous files ("loose files" not associated with any project). We think that as part of the "file-based program" work, we can enable the following in such files without substantial issues:
- Syntax diagnostics.
- Intellisense for the "default" set of references. e.g. those references which are included in the project created by `dotnet new console` with the current SDK.

### Heuristic
The IDE considers a file to be a file-based program, if:
- It has any `#:` directives which configure the file-based program project, or,
- It has any top-level statements.
Any of the above is met, and, the file is not included in an ordinary `.csproj` project (i.e. it is not part of any ordinary project's list of `Compile` items).

### Opt-out

We added an opt-out flag with option name `dotnet.projects.enableFileBasedPrograms`. If issues arise with the file-based program experience, then VS Code users should set the corresponding setting `"dotnet.projects.enableFileBasedPrograms": false` to revert back to the old miscellaneous files experience.
