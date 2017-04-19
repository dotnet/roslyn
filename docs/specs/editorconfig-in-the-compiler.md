All-capital words are interpreted per [RFC 2119](https://www.ietf.org/rfc/rfc2119.txt).

# Compiler Command Arguments

The compilers *MUST* receive a new switch, `/editorconfig`, which accepts a single
file which is an .editorconfig file that may apply to one or more files in the
compilation. Multiple files are passed with multiple `/editorconfig` switches,
the same way you can pass multiple references with /reference or multiple analyzers
with /analyzer. There is no short form of the argument. Files passed with relative
file names are resolved relative to the current working directory the same
as source files.

Examples:

```
    csc.exe /editorconfig:.editorconfig
    vbc.exe /editorconfig:.editorconfig /editorconfig:..\.editorconfig
    csc.exe /editorconfig:.editorconfig /editorconfig:Subdirectory\.editorconfig
```

It is expected that the compiler is given the .editorconfig files with casing that
matches the underlying casing of the file system. The compiler *MUST NOT* do any case correction. So

```
    csc.exe /editorconfig:Subdirectory\.editorconfig /editorconfig:SubDirectory\.editorconfig
```

is a valid input to the compiler. The compiler *MAY* choose to issue a warning if two paths given
are exactly equal by ordinal in a case-sensitive way.

If multiple .editorconfigs apply to a given file (the precise definition of
"apply" is defined later), and more than one file is defining the same value,
.editorconfig file listed first in the command arguments takes precedence over
later files.

# Discovery of .editorconfig files in MSBuild

Discovery of .editorconfig files is not done by csc.exe or vbc.exe, but rather
the host.  For managed code projects, a new build task,
DiscoverEditorConfigFiles, is introduced which runs immediately before the
CoreCompile target so all inputs are found.  The inputs to the task are:

- InputFiles: a list of ITaskItems which are the input files from which we must
  locate .editorconfig files in parent directories. This *MUST* include Compile
  and AdditionalFile items, and potentially others.
- RootDirectories: an optional list of root directories which the build task
  *MUST NOT* ascend higher during any walking. This is a list because a build
  environment that segregates intermediate directories or build directories
  outside the root of the source tree might wish to ensure that no walking is
  ever done outside the source path, or intermediate/output directories, none
  of which may be a parent of any other root (or indeed on Windows might not
  even live on the same drive!)

The outputs of this task are:

- EditorConfigFiles: a list of ITaskItems which are paths to discovered
  .editorconfig files. These *MAY* be relative paths to the current working
  directory, in the same way that input files *MAY* be relative paths.

The default target maps these items to an EditorConfig item group, that is
consumed by the CoreCompile target.

A boolean property DiscoverEditorConfigFiles *MUST* be provided which
conditions the running of the DiscoverEditorConfigFiles target in the first
place. If unset, the target behaves as if this is set to true.

The output of this task *MUST* be deterministic relative to the inputs.
Ordering changes (even if there is no semantic difference) is explicitly
disallowed.

# Interpretation of the contents of an .editorconfig file

.editorconfig files are parsed per the [.editorconfig spec](http://editorconfig.org/#file-format-details).

The compilers as of this writing *MUST NOT* assign any meaning to any
.editorconfig key or value and apply it to core compiler behavior, to mitigate
backwards compatibility concerns about existing .editorconfig files. The
previous statement is scoped to allow for the possibility that in future
versions the compiler *MAY* consume .editorconfig options to control other
settings (such as defaults for warnings/errors) but the spec will be clarified
at such a time. The expectation is such a change happens either at a major
version where breaking changes are acceptable, or controlled under a /langver
or equivalent switch.

# PathMap

*This section is informational only and does not propose any new behavior.*

As a reminder, the `/pathmap` flag to the compiler allows the compiler to "rewrite" paths prior to them being
seen in the following cases today:

- The embedding of the path into a PDB file.
- The embedding of the path into a `[CallerFilePath]` argument.

The syntax for this flag is of the form:

```
    /pathmap:from1=to1,from2=to2,...
```

# Computation of resultant .editorconfig properties for a given file

The computation of the resultant .editorconfig values for a given file *MUST* give the same output as the algorithm
below. Actual implementations may break these into different stages and may have some caching/optimizations as appropriate,
but such caching *MUST NOT* be observable.

1. The compiler goes through and combines any non-absolute-pathed /editorconfig: switches, and combines them the current
   working directory of the compiler. Parent folder references are eliminated; for example `C:\Dog\Cat\..\Dog\.editorconfig` is converted to `C:\Dog\Dog\.editorconfig`.
   All casing is preserved during this transformation. The order of .editorconfig files as given is preserved to the compiler.
   Call this resulting list `editor_config_files`
2. For each `source_file_path` given to the compiler (applying to source files and additional files), we:
   1. If `source_file_path` is a relative path, convert it to an absolute path relative to the current working directory of the compiler.
      Parent folder references are eliminated the same as we did in step #1 for .editorconfig files. Call this `source_full_file_path`.
      If the path is already absolute, we still do parent folder reference elimination and call this `source_full_file_path`. Casing is preserved.
   2. Take `source_full_file_path` and map it through any pathmaps, producing `source_full_mapped_path`.
   3. Define a dictionary `editorconfig_properties`, a map of string to string, as empty for this file.
   4. Loop through each `editor_config_file` in `editor_config_files`:
      1. Strip the filename portion of `editor_config_file` and determine if the resulting directory is a container of
         `source_full_mapped_path`. This determination is simply if each path component of `editor_config_file`'s directory
         name is present, in order and case sensitively, at the start of `source_full_mapped_path`. If this check fails,
         continue to the next `editor_config_file`.
      2. Compute the relative path `relative_path` of `source_full_mapped_path` to `editor_config_file`. This is in effect the path components
         that were not looked at in the previous step.
      3. Determine all sections of the .editorconfig that apply to `relative_path` per the globbing algorithm as defined in the .editorconfig spec.
      4. For each section (last to first), take the defined properties in include them into `editorconfig_properties`, preserving any key/value pairs already in 
         `editorconfig_properties`.
      5. If the `editor_config_file` says it is a root file, terminate the enumeration of `editor_config_files`. (NB: this check is done last, and after
         we have validated the path containment.)
