dotnet-format
=============
`dotnet-format` is a code formatter for `dotnet` that applies style preferences to a project or solution. Preferences will be read from an `.editorconfig` file, if present, otherwise a default set of preferences will be used.

### How To Use

```
Usage:
  dotnet-format [options]

Options:
  -w, --workspace    The solution or project file to operate on. If a file is not specified, the command will search
                     the current directory for one.
  -v, --verbosity    Set the verbosity level. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and
                     diag[nostic]
  --version          Display version information
```

Add `format` after `dotnet` and before the command arguments that you want to run:

| Examples                                                 |
| -------------------------------------------------------- |
| dotnet **format**                                        |
| dotnet **format** &lt;workspace&gt;                      |
| dotnet **format** -w &lt;workspace&gt;                   |
| dotnet **format** -v diag                                |

## Build the Tool from source

You can build and package the tool using the following commands. The instructions assume that you are in the root of the repository.

```console
cd src
cd Tools
cd dotnet-format
dotnet pack -c release -o nupkg /p:SemanticVersioningV1=false
# The final line from the build will read something like
# Successfully created package '..\roslyn\src\Tools\dotnet-code-format\nupkg\dotnet-format.2.11.0-dev.nupkg'.
# Use the value that is in the form `2.11.0-dev` as the version in the next command.
dotnet tool install --add-source .\nupkg -g dotnet-format --version <version>
dotnet format
```

> Note: On macOS and Linux, `.\nupkg` will need be switched to `./nupkg` to accomodate for the different slash directions.

You can uninstall the tool using the following command.

```console
dotnet tool uninstall -g dotnet-format
```
