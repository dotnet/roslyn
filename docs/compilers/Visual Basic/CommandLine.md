# Visual Basic Compiler Options

| FLAG | DESCRIPTION |
| ---- | ---- |
| **OUTPUT FILE**
| `/out:`*file* | Specifies the output file name.
| `/target:exe` | Create a console application (default). (Short form: `/t`)
| `/target:winexe` | Create a Windows application.
| `/target:library` | Create a library assembly.
| `/target:module` | Create a module that can be added to an assembly.
| `/target:appcontainerexe` | Create a Windows application that runs in AppContainer.
| `/target:winmdobj` | Create a Windows Metadata intermediate file
| `/doc`{`+`&#124;`-`} | Generates XML documentation file.
| `/doc:`*file* | Generates XML documentation file to *file*.
| **INPUT FILES**
| `/addmodule:`*file_list* | Reference metadata from the specified modules
| `/link:`*file_list* | Embed metadata from the specified interop assembly. (Short form: `/l`)
| `/recurse:`*wildcard* | Include all files in the current directory and subdirectories according to the wildcard specifications.
| `/reference:`*file_list* | Reference metadata from the specified assembly. (Short form: `/r`)
| `/analyzer:`*file_list* | Run the analyzers from this assembly (Short form: `/a`)
| `/additionalfile:`*file list* | Additional files that don't directly affect code generation but may be used by analyzers for producing errors or warnings.
| **RESOURCES**
| `/linkresource`:*resinfo* | Link the specified resource to this assembly (Short form: `/linkres`) Where the *resinfo* format  is *file*{`,`*string name*{`,``public``|``private`}}
| `/resource`:*resinfo* | Embed the specified resource (Short form: `/res`)
| `/nowin32manifest` | The default manifest should not be embedded in the manifest section of the output PE.
| `/win32icon:`*file* | Specifies a Win32 icon file (.ico) for the default Win32 resources.
| `/win32manifest:`*file* | The provided file is embedded in the manifest section of the output PE.
| `/win32resource:`*file* | Specifies a Win32 resource file (.res).
| **CODE GENERATION**
| `/optimize`{`+`&#124;`-`} | Enable optimizations.
| `/removeintchecks`{`+`&#124;`-`} | Remove integer checks. Default off.
| `/debug`{`+`&#124;`-`} | Emit debugging information.
| `/debug:full` | Emit full debugging information (default).
| `/debug:portable` | Emit debugging information in the portable format.
| `/debug:pdbonly` | Emit PDB file only.
| `/deterministic` | Produce a deterministic assembly (including module version GUID and timestamp)
| **ERRORS AND WARNINGS**
| `/nowarn` | Disable all warnings.
| `/nowarn:`*number_list* | Disable a list of individual warnings.
| `/warnaserror`{`+`&#124;`-`} | Treat all warnings as errors.
| `/warnaserror`{`+`&#124;`-`}:*number_list* | Treat a list of warnings as errors.
| `/ruleset:`*file* | Specify a ruleset file that disables specific diagnostics.
| `/errorlog:`*file* | Specify a file to log all compiler and analyzer diagnostics.
| `/reportanalyzer` | Report additional analyzer information, such as execution time.
| **LANGUAGE**
| `/define:`*symbol_list* | Declare global conditional compilation symbol(s). *symbol_list* is *name*`=`*value*`,`...  (Short form: `/d`)
| `/imports:`*import_list* | Declare global Imports for namespaces in referenced metadata files. *import_list* is *namespace*`,`...
| `/langversion:`*number* | Specify language version: `9`&#124;`9.0`&#124;`10`&#124;`10.0`&#124;`11`&#124;`11.0`&#124;`12`&#124;`12.0`&#124;`13`&#124;`13.0`&#124;`14`&#124;`14.0`. The default is `14`.
| `/optionexplicit`{`+`&#124;`-`} | Require explicit declaration of variables.
| `/optioninfer`{`+`&#124;`-`} | Allow type inference of variables.
| `/rootnamespace`:*string* | Specifies the root Namespace for all top-level type declarations.
| `/optionstrict`{`+`&#124;`-`} | Enforce strict language semantics.
| `/optionstrict:custom` | Warn when strict language semantics are not respected.
| `/optioncompare:binary` | Specifies binary-style string comparisons. This is the default.
| `/optioncompare:text` | Specifies text-style string comparisons.
| **MISCELLANEOUS**
| `/help` | Display a usage message. (Short form: `/?`)
| `/noconfig` | Do not auto-include VBC.RSP file.
| `/nologo` | Do not display compiler copyright banner.
| `/quiet` | Quiet output mode.
| `/verbose` | Display verbose messages.
| `/parallel`{`+`&#124;`-`} | Concurrent build. 
| **ADVANCED**
| `/baseaddress:`*number* | The base address for a library or module (hex).
| `/bugreport:`*file* | Create bug report file.
| `/checksumalgorithm:`*alg* | Specify algorithm for calculating source file checksum stored in PDB. Supported values are: `SHA1` (default) or `SHA256`.
| `/codepage:`*number* | Specifies the codepage to use when opening source files.
| `/delaysign`{`+`&#124;`-`} | Delay-sign the assembly using only the public portion of the strong name key.
| `/errorreport:`*string* | Specifies how to handle internal compiler errors; must be `prompt`, `send`, `none`, or `queue` (default).
| `/filealign:`*number* | Specify the alignment used for output file sections.
| `/highentropyva`{`+`&#124;`-`} | Enable high-entropy ASLR.
| `/keycontainer:`*string* | Specifies a strong name key container.
| `/keyfile:`*file* | Specifies a strong name key file.
| `/libpath:`*path_list* | List of directories to search for metadata references. (Semi-colon delimited.)
| `/main:`*class* | Specifies the Class or Module that contains Sub Main. It can also be a Class that inherits from System.Windows.Forms.Form. (Short form: `/m`)
| `/moduleassemblyname:`*string* | Name of the assembly which this module will be a part of.
| `/netcf` | Target the .NET Compact Framework.
| `/nostdlib` | Do not reference standard libraries (`system.dll` and `VBC.RSP` file).
| `/pathmap:`*k1*=*v1*,*k2*=*v2*,... |  Specify a mapping for source path names output by the compiler.
| `/platform:`*string* | Limit which platforms this code can run on; must be `x86`, `x64`, `Itanium`, `arm`, `AnyCPU32BitPreferred` or `anycpu` (default).
| `/preferreduilang` | Specify the preferred output language name.
| `/sdkpath:`*path* | Location of the .NET Framework SDK directory (`mscorlib.dll`).
| `/subsystemversion:`*version* | Specify subsystem version of the output PE.  *version* is *number*{.*number*}
| `/utf8output`{`+`&#124;`-`} | Emit compiler output in UTF8 character encoding.
| `@`*file* | Insert command-line settings from a text file
| `/vbruntime`{+&#124;-&#124;*} | Compile with/without the default Visual Basic runtime.
| `/vbruntime:`*file* | Compile with the alternate Visual Basic runtime in *file*.
