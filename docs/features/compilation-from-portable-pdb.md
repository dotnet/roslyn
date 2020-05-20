## compilation-from-portable-pdb

Compilation from portable PDBs today is not completely possible, but is desirable in order to help reconstruct a compilation from source provided via source link or embedded in a pdb. Motivation is derived from [roslyn 41395](https://github.com/dotnet/roslyn/issues/41395).

This document is restricted to the following assumptions:

1. The benefit is for builds with `-deterministic` and published to the symbol server.
2. Source generator and analyzer references are not needed for this task. They may be useful, but are out of scope for this document.
3. Any storage capacity used for PDBs and source should not impact this feature, such as compression algorithm.
4. Only Portable PDB files will be included for this spec. We can expand this feature past these once it is implemented and proven needed elsewhere.

This document will provide the expanded specification to the Portable PDB format. Any additions to that format will be ported to expand documentation provided in [dotnet-runtime](https://github.com/jnm2/dotnet-runtime/blob/26efe3467741fe2a85780b2d2cd18875af6ebd98/docs/design/specs/PortablePdb-Metadata.md#source-link-c-and-vb-compilers).

## List of Compiler Flags

#### CSharp Flags Not Included

* appconfig
* baseaddress
* bugreport
* checksumalgorithm 
    - Already stored in PDB for each source file
* debug
* delaysign
* doc
* deterministic
* embed
* errorendlocation
* errorlog
* errorreport
* filealign
* fullpaths
* highentropyva
* incremental
* keycontainer
* keyfile
* link
    - Represented by a metadata reference with `EmbededInteropTypes=true` 
* linkresource
* main
    - Already stored in PDB as the entry point token
* moduleassemblyname
    - Stored in metadata 
* modulename
    - Stored in metadata
* noconfig
* nologo
* nostdlib
* nowarn
* nowin32manifest
* optimize
* out
* parallel
* pathmap
* pdb
* platform
* preferreduilang
* publicsign
* recurse
* refout
* refonly
* reportanalyzer
* resource
* ruleset
* subsystemversion
* target
* utf8output
* version
* warn
* warnaserror
* win32icon
* win32manifest
* win32res

#### Visual Basic Flags Not Included

* baseaddress
* bugreport
* checksumalgorithm
    - already stored in PDB for each source file
* debug
* delaysign
* doc
* errorreport
* filealign
* help
* highentropyva
* keycontainer
* keyfile
* libpath
* linkresource
* main
    - already included in PDB information
* moduleassemblyname
* modulename
* netcf
* noconfig
* nologo
* nostdlib
* nowarn
* nowin32manifest
* optimize
* optioncompare
* optionexplicit
* optioninfer
* out
* parallel
* platform
* preferreduilang
* quiet
* recurse
* refonly
* refout
* resource
* rootnamespace
* ruleset
* sdkpath
* subsystemversion
* target
* utf8output
* vbruntime
* verbose
* warnaserror
* win32icon
* win32manifest
* win32resource

#### Compiler Version

This will be a full compiler version, including commit SHA for the build of the compiler. This will be used since compilations across compiler versions are not guaranteed to be the same.

#### Source Encoding

Encoding for source files that are not declared using BOM. This 

#### Metadata References

Symbol server uses a [key](https://github.com/dotnet/symstore/blob/master/docs/specs/SSQP_Key_Conventions.md#pe-timestamp-filesize) computed from the COFF header in the PE image:

Timestamp: 4 byte integer
Size of image: 4 byte integer

Example:

File name: `Foo.exe` 

COFF header Timestamp field: `0x542d5742` 

COFF header SizeOfImage field: `0x32000` 

Lookup key: `foo.exe/542d574232000/foo.exe` 

To fully support metadata references, we'll need to be able to find the exact PE image that was used in the compilation. We'll do this by storing the parts that make up the symbol server key.

We will also store the MVID of a reference since it's a GUID that represents the symbol. This is to future proof the information we will need to look up references.

At this time, we'll only include external references for the compilation. Any other references may be added later in a separate blob.

## PDB Format Additions

#### Metadata References

Metadata references can be easily stored as binary. The binary encoding will be as follows (order matters):

Name: A UTF-8 string (null terminated)

Aliases: UTF-8 Comma (, ) separate list of aliases (null terminated). May be empty

MetadataImageKind: byte value representing Microsoft. CodeAnalysis. MetadataImageKind

EmbedInteropTypes/MetadataImageKind: byte, reads as binary values starting in order from right to left
    
    MetadataImageKind: 1 if Assembly, 0 if Module
    EmbedInteropTypes: 1 if true
    Examples:
        0b11, MetadataImageKind.Assembly and EmbedInteropTypes = true
        0b01, MetadataImageKind.Module and EmbedInteropTypes = true

Timestamp: 4 byte integer

File Size: 4 byte integer

MVID: 16 byte integer (GUID)

#### Compiler Flag Key Value Pairs

The remaining values will be stored as key value pairs in the pdb. The storage format will be UTF8 encoded key value pairs that are null terminated. Order is not guaranteed. Any values left out can be assumed to be the default for the type. Keys may be different for Visual Basic and CSharp. They are serialized to reflect the command line arguments representing the same values

Example: 

`compilerversion\01.2.3\0sourceencoding\0utf-8\0checked\01\0unsafe\01\0langversion\0latest\0nullable\0Enable` 

##### Options For CSharp

See [compiler options](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/listed-alphabetically) documentation

1. "compilerversion"
2. "[codepage](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/codepage-compiler-option)"
3. "[checked](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/checked-compiler-option)"
4. "[unsafe](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/unsafe-compiler-option)"
5. "[langversion](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/langversion-compiler-option)"
6. "[nullable](https://docs.microsoft.com/en-us/dotnet/csharp/nullable-references)"
7. "[define](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/define-compiler-option)"

##### Options For Visual Basic

See [compiler options](https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/command-line-compiler/compiler-options-listed-alphabetically) documentation

1. "compilerversion"
2. "[removeintchecks](https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/command-line-compiler/removeintchecks)"
3. "[langversion](https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/command-line-compiler/langversion)"
4. "[define](https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/command-line-compiler/define)"
5. "[optionstrict](https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/command-line-compiler/optionstrict)"
