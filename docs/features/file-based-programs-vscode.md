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

## Rich miscellaneous files

There is a long-standing backlog item to enhance the experience of working with miscellaneous files ("loose files" not associated with any project). We think that as part of the "file-based program" work, we can enable the following in such files without substantial issues:
- Syntax diagnostics.
- Intellisense for the "default" set of references. e.g. those references which are included in the project created by `dotnet new console` with the current SDK.
- In certain cases, we can even enable semantic diagnostics, with reasonable confidence that the resulting errors are useful to the user.

These changes to misc files behavior are called "rich misc files".

The implementation strategy is: the editor creates a "canonical misc files project" under the temp directory, and uses the resulting project info as a "base project" for loose files that are opened in the IDE.

### File-based app detection

A C# file has multiple possible *classifications* in the editor:
- **Project-Based App**. The file is part of an ordinary `.csproj` project.
- **File-Based App**. The file is part of a "file-based app" project, i.e. it is either the entry point of a file-based app or it is `#:include`d by the entry point of the same.
- **Misc File w/ Semantic Errors**. The file is a valid entry point for either a file-based app, but lacks the `#:`/`#!` directives which give us high certainty of that.
   - Tooling will light up accordingly, showing syntax errors, semantic errors, semantic info for the core library, etc. See *Rich miscellaneous files* section above.
   - These files will not be restored.
- **Misc File**. The file isn't part of any project, and heuristics indicate it's not intended to be a file-based app.
   - Syntax errors and semantic info for the core library will appear in these files.
   - Semantic errors will not appear in these files.

This is the decision tree for determining how to classify a C# file:

1. **Is the file in a currently loaded project?**
   - **Yes** → Classify as **Project-Based App**
   - **No** → Continue to next check

2. **Is `enableFileBasedPrograms` enabled?** (default: `true` in release)
   - **No** → Classify as **Misc File**
   - **Yes** → Continue to next check

3. **Does the file exist on disk?** (i.e. it is not a "virtual document" created for a new, not-yet-saved file, or similar.)
   - **Yes** → Go to (3)
   - **No** → Go to (4)

3. **Does the file have `#:` or `#!` directives?**
   - **Yes** → Classify as **File-Based App**. Restore if needed and show semantic errors.
   - **No** → Continue to next check

4. **Is `enableFileBasedProgramsWhenAmbiguous` enabled?** (default: `false` in release, `true` in prerelease)
   - **No** → Classify as **Misc File**
   - **Yes** → Continue to heuristic detection

**Heuristic Detection (when `enableFileBasedProgramsWhenAmbiguous: true`):**

5. **Are top-level statements present?**
   - **No** → Classify as **Misc File**
   - **Yes** → Continue to next check

6. **Is the file included in a `.csproj` cone?**
   - "Cone" means that a containing directory, at some level of nesting, has a `.csproj` file in it.
   - **Yes** → Classify as **Misc File** (wait for project to load)
   - **No** → Classify as **Misc File w/ Semantic Errors**

### Opt-out

We added an opt-out flag with option name `dotnet.projects.enableFileBasedPrograms`. If issues arise with the file-based program experience, then VS Code users should set the corresponding setting `"dotnet.projects.enableFileBasedPrograms": false` to revert back to the old miscellaneous files experience.

We also have a second, finer-grained opt-out flag `dotnet.projects.enableFileBasedProgramsWhenAmbiguous`. This flag is conditional on the previous flag (i.e. it is ignored when `enableFileBasedPrograms` is `false`). This is used to allow opting out only in cases where it is unclear from the single file itself, whether it should be treated as a file-based program. Presence of `#:` or `#!` directives in a `.cs` file strongly indicates that the file is a file-based program, and editor functionality will continue to light up for such files, even when `enableFileBasedProgramsWhenAmbiguous` is `false`.

> [!NOTE]
> The second flag is being used on a short-term basis while we work out the set of heuristics and cross-component APIs needed to efficiently and satisfactorily resolve whether a file with top-level statements but no directives is a file-based program in the context of a complex workspace.
