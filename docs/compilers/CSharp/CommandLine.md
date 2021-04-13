# Visual C# Compiler Options

| FLAG | DESCRIPTION |
| ---- | ---- |
| **OUTPUT FILES** |
| `/out:`*file* | Specify output file name (default: base name of file with main class or first file)
| `/refout:`*file* | Specify the reference assembly's output file name
| `/target:exe` |  Build a console executable (default) (Short form: `/t:exe`)
| `/target:winexe` | Build a Windows executable (Short form: `/t:winexe` )
| `/target:library` | Build a library (Short form: `/t:library`)
| `/target:module` | Build a module that can be added to another assembly (Short form: `/t:module`)
| `/target:appcontainerexe` | Build an Appcontainer executable (Short form: `/t:appcontainerexe`)
| `/target:winmdobj` | Build a Windows Runtime intermediate file that is consumed by WinMDExp (Short form: `/t:winmdobj`)
| `/doc:`*file* | XML Documentation file to generate
| `/platform:`*string* | Limit which platforms this code can run on: `x86`, `Itanium`, `x64`, `arm`, `anycpu32bitpreferred`, or `anycpu`. The default is `anycpu`.
| **INPUT FILES**
| `/recurse:`*wildcard* | Include all files in the current directory and subdirectories according to the wildcard  specifications
| `/reference:`*alias*=*file* | Reference metadata from the specified assembly file using the given alias (Short form: `/r`)
| `/reference:`*file list* | Reference metadata from the specified assembly files (Short form: `/r`)
| `/addmodule:`*file list* | Link the specified modules into this assembly
| `/link:`*file list* | Embed metadata from the specified interop assembly files (Short form: `/l`)
| `/analyzer:`*file list* | Run the analyzers from this assembly (Short form: `/a`)
| `/additionalfile:`*file list* | Additional files that don't directly affect code generation but may be used by analyzers for producing errors or warnings.
| **RESOURCES**
| `/win32res:`*file* | Specify a Win32 resource file (.res)
| `/win32icon:`*file* | Use this icon for the output
| `/win32manifest`:*file* | Specify a Win32 manifest file (.xml)
| `/nowin32manifest` | Do not include the default Win32 manifest
| `/resource`:*resinfo* | Embed the specified resource (Short form: `/res`)
| `/linkresource`:*resinfo* | Link the specified resource to this assembly (Short form: `/linkres`) Where the *resinfo* format  is *file*{`,`*string name*{`,``public``|``private`}}
| **CODE GENERATION**
| `/debug`{`+`&#124;`-`} | Emit (or do not Emit) debugging information
| `/debug`:`full` | Emit debugging information to .pdb file using default format for the current platform: _Windows PDB_ on Windows, _Portable PDB_ on other systems
| `/debug`:`pdbonly` | Same as `/debug:full`. For backward compatibility. 
| `/debug`:`portable` | Emit debugging information to to .pdb file using cross-platform [Portable PDB format](https://github.com/dotnet/core/blob/main/Documentation/diagnostics/portable_pdb.md)
| `/debug`:`embedded` | Emit debugging information into the .dll/.exe itself (.pdb file is not produced) using [Portable PDB format](https://github.com/dotnet/core/blob/main/Documentation/diagnostics/portable_pdb.md).
| `/sourcelink`:*file* | [Source link](https://github.com/dotnet/core/blob/main/Documentation/diagnostics/source_link.md) info to embed into PDB.
| `/optimize`{`+`&#124;`-`} | Enable optimizations (Short form: `/o`)
| `/deterministic` | Produce a deterministic assembly (including module version GUID and timestamp)
| `/refonly` | Produce a reference assembly, instead of a full assembly, as the primary output 
| **ERRORS AND WARNINGS**
| `/warnaserror`{`+`&#124;`-`} | Report all warnings as errors
| `/warnaserror`{`+`&#124;`-`}`:`*warn list* | Report specific warnings as errors
| `/warn`:*n* | Set warning level (non-negative integer) (Short form: `/w`)
| `/nowarn`:*warn list* | Disable specific warning messages
| `/ruleset`:*file* | Specify a ruleset file that disables specific diagnostics.
| `/errorlog`:*file* | Specify a file to log all compiler and analyzer diagnostics.
| `/reportanalyzer` | Report additional analyzer information, such as execution time.
| **LANGUAGE**
| `/checked`{`+`&#124;`-`} | Generate overflow checks
| `/unsafe`{`+`&#124;`-`} | Allow 'unsafe' code
| `/define:`*symbol list* | Define conditional compilation symbol(s) (Short form: `/d`)
| `/langversion:?` | Display the allowed values for language version
| `/langversion`:*string* | Specify language version such as `default` (latest major version), or `latest` (latest version, including minor versions)
| **SECURITY**
| `/delaysign`{`+`&#124;`-`} | Delay-sign the assembly using only the public portion of the strong name key
| `/keyfile:`*file* | Specify a strong name key file
| `/keycontainer`:*string* | Specify a strong name key container
| `/highentropyva`{`+`&#124;`-`} | Enable high-entropy ASLR
| **MISCELLANEOUS**
| `@`*file* | Read response file for more options
| `/help` | Display a usage message (Short form: `/?`)
| `/nologo` | Suppress compiler copyright message
| `/noconfig` | Do not auto include `CSC.RSP` file
| `/parallel`{`+`&#124;`-`} | Concurrent build.
| **ADVANCED**
| `/baseaddress:`*address* | Base address for the library to be built
| `/bugreport:`*file* | Create a 'Bug Report' file
| `/checksumalgorithm:`*alg* | Specify algorithm for calculating source file checksum stored in PDB. Supported values are: `SHA1` (default) or `SHA256`.
| `/codepage:`*n* | Specify the codepage to use when opening source files
| `/utf8output` | Output compiler messages in UTF-8 encoding
| `/main`:*type* | Specify the type that contains the entry point (ignore all other possible entry points) (Short form: `/m`)
| `/fullpaths` | Compiler generates fully qualified paths
| `/filealign`:*n* | Specify the alignment used for output file sections
| `/pathmap:`*k1*=*v1*,*k2*=*v2*,... | Specify a mapping for source path names output by the compiler. Two consecutive separator characters are treated as a single character that is part of the key or value (i.e. `==` stands for `=` and `,,` for `,`).
| `/pdb:`*file* | Specify debug information file name (default: output file name with `.pdb` extension)
| `/errorendlocation` | Output line and column of the end location of each error
| `/preferreduilang` | Specify the preferred output language name.
| `/nostdlib`{`+`&#124;`-`} | Do not reference standard library `mscorlib.dll`
| `/subsystemversion:`*string* | Specify subsystem version of this assembly
| `/lib:`*file list* | Specify additional directories to search in for references
| `/errorreport:`*string* | Specify how to handle internal compiler errors: `prompt`, `send`, `queue`, or `none`. The default is `queue`.
| `/appconfig:`*file* | Specify an application configuration file containing assembly binding settings
| `/moduleassemblyname:`*string* | Name of the assembly which this module will be a part of
| `/modulename:`*string* | Specify the name of the source module
