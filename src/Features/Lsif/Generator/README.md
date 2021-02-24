This tool consumes a project or solution and generates a Language Server Index Format file per the
[LSIF specification](https://github.com/Microsoft/language-server-protocol/blob/master/indexFormat/specification.md).

# Command Line Switches
## Specifying the Project to Process
### `--solution`
The path to the Visual Studio Solution file to process. The LSIF graph for all projects will be outputted.

### `--compiler-invocation`
Specifies a path to a JSON file that contains the information of a compiler invocation. This JSON file includes the compiler that was ran, the command string passed, and path information. This allows you to generate an LSIF file that
matches the same setup as an actual build without the tool having to run MSBuild itself to regreate the compiler command line string.

## Output Files

### `--output`
Accepts a file path, and writes the results to that file instead of the console output.

## `--output-format`
Either "Line" (the default) to write out the LSIF where each line in the text is a separate JSON object, or
"JSON" where the LSIF is written out as a single large JSON array.

### `--log`
Accepts a file path where a log file is written to. The log file contains information regarding how long various
parts of the LSIF generation took.