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

These changes to misc files behavior are called "rich miscellaneous files".

The implementation strategy is: the editor creates a "canonical misc files project" under the temp directory, and uses the resulting project info as a "base project" for loose files that are opened in the IDE.

## File-based app detection

A C# file has multiple possible *classifications* in the editor:
- **Project-Based App**. The file is part of an ordinary `.csproj` project.
- **File-Based App**. The file is part of a "file-based app" project, i.e. it is either the entry point of a file-based app or it is `#:include`d by the entry point of the same.
- **Miscellaneous File With Standard References and Semantic Errors**. The file is a valid entry point for either a file-based app, but lacks the `#:`/`#!` directives which give us high certainty that this is the user's intent.
   - Tooling will light up accordingly, showing syntax errors, semantic errors, semantic info for the core library, etc. See *Rich miscellaneous files* section above.
   - These files will not be restored.
- **Miscellaneous File With Standard References**. The file isn't part of any project, and heuristics indicate it's not intended to be a file-based app. The file uses the regular C# language (not the `.csx` scripting dialect).
   - Syntax errors and semantic info for the core library will appear in these files.
   - Semantic errors will not appear in these files.
- **Miscellaneous File With No References**. The file isn't part of any project. It may even be a `.csx`, `.razor`, or other non-.cs type.
   - These files do not have any references to the core library, and do not show semantic errors.
   - Syntax errors, go-to-def on declarations in the same file, etc., may work.
   - When `enableFileBasedPrograms` is disabled, this classification is generally used instead of one of the *miscellaneous file with standard references* or *file-based app* classifications above.

**NOTE:** This is intended to be a living document, and for the set of checks and classifications to possibly change over time depending on our needs.

This is the decision tree for determining how to classify a C# file:

1. **Is the file in a currently loaded project?**
   - **Yes** → Classify as **Project-Based App**
   - **No** → Continue to next check

2. **Is `enableFileBasedPrograms` enabled?** (default: `true` in release)
   - **No** → Classify as **Miscellaneous File With No References**
   - **Yes** → Continue to next check

3. **Is the file a regular C# file? (i.e. not a `.csx` script, and not a file using a language besides C#)**
   - **No** → Classify as **Miscellaneous File With No References**
   - **Yes** → Continue to next check

4. **Does the file have an absolute path and exist on disk?** (i.e. it is not a "virtual document" created for a new, not-yet-saved file, or similar.)
   - **No** → Classify as **Classify as Miscellaneous File With Standard References**
   - **Yes** → Continue to next check

5. **Does the file have `#:` or `#!` directives?**
   - **Yes** → Classify as **File-Based App**. Restore if needed and show semantic errors.
   - **No** → Continue to next check

6. **Is `enableFileBasedProgramsWhenAmbiguous` enabled?** (default: `false` in release, `true` in prerelease)
   - **No** → Classify as **Miscellaneous File With Standard References**
   - **Yes** → Continue to heuristic detection

**Heuristic Detection (when `enableFileBasedProgramsWhenAmbiguous: true`):**

7. **Are top-level statements present?**
   - **No** → Classify as **Miscellaneous File With Standard References**
   - **Yes** → Continue to next check

8. **Is the file included in a `.csproj` cone?**
   - "Cone" means that a containing directory, at some level of nesting, has a `.csproj` file in it.
   - Note that this specific check is only performed at the time the file is opened. We think that the typical case is that the user will load a new project they are creating. Loading the project will cause the file to start being treated as project-based app per (1). If the user does not load the new project, then stale diagnostics may remain present until the file is closed and re-opened.
   - **Yes** → Classify as **Miscellaneous File With Standard References** (wait for project to load)
   - **No** → Classify as **Miscellaneous File With Standard References and Semantic Errors**

### Opt-out

We added an opt-out flag with option name `dotnet.projects.enableFileBasedPrograms`. If issues arise with the file-based program experience, then VS Code users should set the corresponding setting `"dotnet.projects.enableFileBasedPrograms": false` to revert back to the old miscellaneous files experience.

We also have a second, finer-grained opt-out flag `dotnet.projects.enableFileBasedProgramsWhenAmbiguous`. This flag is conditional on the previous flag (i.e. it is ignored when `enableFileBasedPrograms` is `false`). This is used to allow opting out only in cases where it is unclear from the single file itself, whether it should be treated as a file-based program. Presence of `#:` or `#!` directives in a `.cs` file strongly indicates that the file is a file-based program, and editor functionality will continue to light up for such files, even when `enableFileBasedProgramsWhenAmbiguous` is `false`.

> [!NOTE]
> The second flag is being used on a short-term basis while we work out the set of heuristics and cross-component APIs needed to efficiently and satisfactorily resolve whether a file with top-level statements but no directives is a file-based program in the context of a complex workspace.

## LSP handling of file-based apps

When a C# file adds `#:` or `#!` directives, it becomes a file-based app.
Conceptually, what happens is: the file becomes both a C# source file, and a project file, in one.

Conversely, when all `#:`/`#!` directives are removed, it stops being a project file, and goes back to being a C# source file only. In this scheme, we think of a file which contains `#:` as being the "entry point file" of the file-based app.

We are adding support for an `#:include` directive to file-based apps, which lets users point at single files or `*` globs of C# source files, or other additional files (content, resources, etc.), which should be included in the file-based app project. This makes file-based app projects behave much more like ordinary projects in the workspace. In particular, we can have situations like the following:
- `Util.cs` (an ordinary source file)
- `App1.cs`, a file-based app entry point containing `#:include Util.cs`
- `App2.cs`, a file-based app entry point also containing `#:include Util.cs`
- `MyProject.csproj`, also containing `<Compile Include="Util.cs" />`

Because all these projects are simply added as projects to the host workspace, it's expected that features like "active project context" and multi-targeting-aware Quick Info "just work" with all of them.

One key assumption we are making is: it is not valid for a file-based app *entry point* to be a member of an ordinary project. e.g. you cannot have the following:
- `Util.cs` (an ordinary source file)
- `App1.cs`, a file-based app entry point containing `#:include Util.cs`
- `MyProject.csproj`, containing `<Compile Include="App1.cs" />` **<-- This part is considered malformed**

An error is reported generally for presence of `#:`/`#!` directives in ordinary projects. Depending on the order that things load, such files may or may not also be detected as file-based app entry points.

In this case we want the user to do one of 2 things to resolve the issue:
1. Delete the `#:`/`#!` directives. We will unload the file as file-based app in this case.
2. Remove the file-based app entry point as a member of any ordinary project(s).

We expect the appropriate project system(s) to be able to observe either of the above changes and move the workspace into a "healthy" state once the user has corrected the error.

### `FileBasedProgramsProjectSystem`

Manages projects for file-based programs and miscellaneous files.

This project system effectively performs the classification process described in [File-based app detection](#file-based-app-detection) when a design-time build is performed for the project, and transitions the state of the project to match the latest classification.

This uses the file-based program entry point file, translates it to a virtual msbuild project, then runs a design-time build on that project. If it detects missing assets, it may also restore the virtual project.

It uses file watchers to watch the project globs and redo the design time build on relevant changes, such as changes to `#:` directives.

## Automatic discovery

The Roslyn LSP will automatically discover and load file-based apps in the opened workspace folders. The user can opt out of this discovery process by setting `"dotnet.fileBasedApps.enableAutomaticDiscovery": false`.
For the first release of the feature in the VS Code C# extension, the setting will be disabled-by-default in the stable release channel and enabled-by-default in the prerelease channel.

Certain subfolders in a workspace are excluded from this discovery process:
- Any folders which contain a `.csproj` file.
- Any folders with names conventionally reserved for build artifacts, such as `artifacts`, `bin`, and `obj`.
- Any folders marked "hidden" in the file system. `.git` and `.vs` typically fall into this.

The first time discovery is performed in a workspace, the LSP will read all `.cs` files in the opened workspace folders which are not excluded by the above conditions. If the file content starts with `#!`, it is marked as a file-based app and loaded.

A cache file is created after each discovery pass and stored in the user temp directory. This file holds:
- The time that the previous discovery pass started.
- Paths of file-based apps found during the last discovery pass.
- Paths of folders that were found to contain `.csproj` files during the last discovery pass.

The cache data allows the following optimizations in subsequent discovery passes:
- Allows not reading any C# files whose last write time is older than the cached time.
- Allows reducing the number of times we list files in directories whose last write time is older than the cached time.

### `#!` requirement

This design requires files to start with `#!` in order to participate in discovery.
Specifically, a discoverable file must start with either the byte sequence `0x23, 0x21` (ASCII/UTF-8 `#!`), or the byte sequence `0xEF, 0xBB, 0xBF, 0x23, 0x21` (UTF-8 BOM followed by `#!`).

The reason for this is: we anticipate adding support for `#:` to non-entry-point files. This means that having `#:` is not going to be enough to identify a file as definitely the entry point.

Instead, it will be necessary to search for both `#:` and top-level statements at a minimum. This cost is acceptable for files that were explicitly opened in the editor, but is a bit steep for a broad discovery pass.

For this reason, we intend to put `#!`-at-start as a standard for entry points of file-based apps. We plan on shipping an analyzer which reports a warning in files which contain both `#:include` and top-level statements, but do not have `#!` at the top.

## Future considerations

This section is not intended to serve as permanent documentation but as more of a roadmap for a series of changes we may make in this area in the near future. It should not be necessary to read/understand this in order to evaluate a PR currently under review. i.e. anything that the current PR is actually implementing is covered in previous sections.

### Treating files with no directives as file-based app entry points

**Miscellaneous File With Standard References and Semantic Errors**, is a designation we essentially have in order to avoid restoring things we aren't 100% sure are file-based apps. This particularly includes files which have top-level statements, but no `#:`/`#!` directives.

We may want to make a change in the future, to stop using this designation for files which exist on disk, and instead classify files not part of an ordinary project, containing top-level statements, and with no csproj-in-cone, as being file-based apps. This would improve accuracy in the editor in certain cases, and make it easier to do things like avoid showing the *This is a miscellanous file, things may be broken* popup.

### Allowing non-entry-point files to contain `#:` directives

We are considering adding support for non-entry-point files to contain `#:` in the future. In this case, we would need an additional bit of information to distinguish entry points from non-entry-points.
For *multi-file file-based apps*, users should use a `#!` at the top of the entry point file to make it easy to identify.
For *single-file file-based apps*, we think that just using `#:` and top-level statements together should be enough, to identify a file that was explicitly opened in the editor as an entry point.

### Checking top-level statements presence without doing a full parse

Currently there are cases where we may end up needing to do an additional parse of a file just to check if it contains top-level statements. This is generally a situation we'd like to avoid, and, would prefer to either use a pattern where the file already exists in some project and has a syntax tree we can check incrementally, or, that we devise some other solution for performing our heuristics which doesn't require a full parse.
