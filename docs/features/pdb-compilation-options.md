# Embedding Compilation options in Portable PDBs

Prior to this feature the compiler did not emit all information that's necessary to reconstruct the original compilation to the output binaries.
It is desirable to include such information in order to support scenarios such as validation that given binaries can be reproduced from their original sources, [post-build source analysis](https://github.com/dotnet/roslyn/issues/41395), etc.

The goal of this feature is to be able to construct a compilation that is exactly the same as an initial compilation as long as it meets the conditions outlined in the assumptions below.

This document is restricted to the following assumptions:

1. The full benefit is for builds with `-deterministic` and published to the symbol server. That said the compiler embeds compilation options and references to all Portable PDBs.
2. Source generator and analyzer references are not needed for this task. They may be useful, but are out of scope for this feature.
3. Any storage capacity used for PDBs and source should not impact this feature, such as compression algorithm.
4. Only Portable PDB files will be included for this spec. This feature can be expanded past these once it is implemented and proven needed elsewhere.

This document will provide the expanded specification to the Portable PDB format. Any additions to that format will be ported to expand documentation provided in [dotnet-runtime](https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md).

## PDB Format Additions

#### Compilation Metadata References custom debug information

Symbol server uses a [key](https://github.com/dotnet/symstore/blob/main/docs/specs/SSQP_Key_Conventions.md#pe-timestamp-filesize) computed from the COFF header in the PE image:

Timestamp: 4 byte integer
Size of image: 4 byte integer

Example:

    File name: `example.exe` 

    COFF header Timestamp field: `0x542d5742` 

    COFF header SizeOfImage field: `0x32000` 

    Lookup key: `example.exe/542d574232000/example.exe` 

To fully support metadata references, a user will need to be able to find the exact PE image that was used in the compilation. This will be done by storing the parts that make up the symbol server key. The MVID of a reference will be stored since it's a GUID that represents the symbol. This is to future proof the information for reference lookup. 

At this time, only external references for the compilation will be included. Any other references may be added later in a separate blob.

Metadata references are stored as binary. The binary encoding will be as follows (order matters):

Name: A UTF-8 string (null terminated)

Aliases: UTF-8 Comma (, ) separated list of aliases (null terminated). May be empty

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

#### Retrieving Metadata References

Metadata references are stored in the CustomDebugInformation in a PDB using the GUID `7E4D4708-096E-4C5C-AEDA-CB10BA6A740D` . 

Example how to retrieve and read this information using `System.Reflection.Metadata` : 

``` csharp
var metadataReferencesGuid = new Guid("7E4D4708-096E-4C5C-AEDA-CB10BA6A740D");
var path = "path/to/pdb";

using var stream = File.OpenRead(path);
using var metadataReaderProvider = MetadataReaderProvider.FromPortablePdbStream(stream);
var metadataReader = metadataReaderProvider.GetMetadataReader();

foreach (var handle in metadataReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition))
{
    var customDebugInformation = metadataReader.GetCustomDebugInformation(handle);
    if (metadataReader.GetGuid(customDebugInformation.Kind) == metadataReferencesGuid)
    {
        var blobReader = metadataReader.GetBlobReader(customDebugInformation.Value);
        
        // Each loop is one reference
        while (blobReader.RemainingBytes > 0)
        {
            // Order of information
            // File name (null terminated string): A.exe
            // Extern Alias (null terminated string): a1,a2,a3
            // EmbedInteropTypes/MetadataImageKind (byte)
            // COFF header Timestamp field (4 byte int)
            // COFF header SizeOfImage field (4 byte int)
            // MVID (Guid, 24 bytes)

            var terminatorIndex = blobReader.IndexOf(0);
            Assert.NotEqual(-1, terminatorIndex);

            var name = blobReader.ReadUTF8(terminatorIndex);

            // Skip the null terminator
            blobReader.ReadByte();

            terminatorIndex = blobReader.IndexOf(0);
            Assert.NotEqual(-1, terminatorIndex);

            var externAliases = blobReader.ReadUTF8(terminatorIndex);

            // Skip the null terminator
            blobReader.ReadByte();

            var embedInteropTypesAndKind = blobReader.ReadByte();
            var embedInteropTypes = (embedInteropTypesAndKind & 0b10) == 0b10;
            var kind = (embedInteropTypesAndKind & 0b1) == 0b1
                ? MetadataImageKind.Assembly
                : MetadataImageKind.Module;

            var timestamp = blobReader.ReadInt32();
            var imageSize = blobReader.ReadInt32();
            var mvid = blobReader.ReadGuid();

            Console.WriteLine(name);
            Console.WriteLine($"Extern Aliases: \"{externAliases}\"");
            Console.WriteLine($"Embed Interop Types: {embedInteropTypes}");
            Console.WriteLine($"Metadata Image Kind: {kind}");
            Console.WriteLine($"Timestamp: {timestamp}");
            Console.WriteLine($"Image Size: {imageSize}");
            Console.WriteLine($"MVID: {mvid}");
            Console.WriteLine();
        }
    }
}
```

### Compiler Options custom debug information

The remaining values will be stored as key value pairs in the pdb. The storage format will be UTF8 encoded key value pairs that are null terminated. Order is not guaranteed. Any values left out can be assumed to be the default for the type. Keys may be different for Visual Basic and C#. They are serialized to reflect the command line arguments representing the same values

Example: 

`compilerversion\01.2.3-example-sha\0sourceencoding\0utf-8\0checked\01\0unsafe\01\0langversion\0latest\0nullable\0Enable` 

## List of Compiler Flags

#### C# Flags That Can Be Derived From PDB or Assembly

* baseaddress
* checksumalgorithm
* debug
* deterministic
* embed
* filealign
* highentropyva
* link
    - Represented by a metadata reference with `EmbededInteropTypes=true` 
* linkresource
    - Will be represented in metadata reference embedded in pdb
* main
    - Already stored in PDB as the entry point token
* moduleassemblyname
* modulename
* nostdlib
    - Will be represented in metadata reference embedded in pdb
* nowin32manifest
* pdb
* platform
* publicsign
* resource
    - Will be represented in metadata reference embedded in pdb
* subsystemversion
* win32icon
* win32manifest
* win32res

#### C# Flags Not Included

* bugreport
* delaysign
* doc
* errorendlocation
* errorlog
* errorreport
* fullpaths
* incremental
* keycontainer
* keyfile
* noconfig
* nologo
* nowarn
* out
* parallel
* pathmap
* preferreduilang
* recurse
* refout
* refonly
* reportanalyzer
* ruleset
* utf8output
* version
* warn
* warnaserror

#### Visual Basic Flags That Can Be Derived From PDB or Assembly

* baseaddress
* checksumalgorithm
* debug
* filealign
* highentropyva
* linkresource
    - Will be represented in metadata reference embedded in pdb
* main 
* nostdlib
    - Will be represented in metadata reference embedded in pdb
* platform
* resource
    - Will be represented in metadata reference embedded in pdb
* subsystemversion
* win32icon
* win32manifest
* win32resource

#### Visual Basic Flags Not Included

* bugreport
* delaysign
* doc
* errorreport
* help
* keycontainer
* keyfile
* libpath
* moduleassemblyname
* modulename
* netcf
* noconfig
* nologo
* nowarn
* nowin32manifest
* optioncompare
* optionexplicit
* optioninfer
* out
* parallel
* preferreduilang
* quiet
* recurse
* refonly
* refout
* rootnamespace
* ruleset
* sdkpath
* utf8output
* vbruntime
* verbose
* warnaserror

#### Shared Options for C# and Visual Basic

| PDB Key                | Format                                  | Default   | Description  |
| ---------------------- | --------------------------------------- | --------- | ------------ |
| language               | `C#\|Visual Basic`                   | required  | Language name. |
| compiler-version       | [SemVer2](https://semver.org/spec/v2.0.0.html) string | required | Full version with SHA |
| runtime-version        | [SemVer2](https://semver.org/spec/v2.0.0.html) string | required | [runtime version](#runtime-version) |
| source-file-count      | int32                                   | required    | Count of files in the document table that are source files |
| optimization           | `(debug\|debug-plus\|release\|release-debug-plus)` | `'debug'` | [optimization](#optimization) |
| portability-policy     | `(0\|1\|2\|3)`                          | `0`       | [portability policy](#portability-policy) |
| default-encoding       | string                                  | none      | [file encoding](#file-encoding) |
| fallback-encoding      | string                                  | none      | [file encoding](#file-encoding) |
| output-kind            | string                                  | require   | The value passed to `/target` |
| platform               | string                                  | require   | The value passed to `/platform` |

#### Options For C\#

See [compiler options](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/listed-alphabetically) documentation

| PDB Key                | Format                                  | Default   | Description  |
| ---------------------- | --------------------------------------- | --------- | ------------ |
| language-version       | `[0-9]+(\.[0-9]+)?`                     | required  | [langversion](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/langversion-compiler-option) |
| define                 | `,`-separated identifier list           | empty     | [define](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/define-compiler-option) |
| checked                | `(True\|False)`                         | `False`   | [checked](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/checked-compiler-option) |
| nullable               | `(Disable\|Warnings\|Annotations\|Enable)` | `Disable` | [nullable](https://docs.microsoft.com/en-us/dotnet/csharp/nullable-references) |
| unsafe                 | `(True\|False)`                         | `False`   | [unsafe](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/unsafe-compiler-option) |

#### Options For Visual Basic

See [compiler options](https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/command-line-compiler/compiler-options-listed-alphabetically) documentation

| PDB Key                | Format                                     | Default  | Description |
| ---------------------- | ------------------------------------------ | -------- | ----------- |
| language-version       | `[0-9]+(\.[0-9]+)?`                        | required | [langversion](https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/command-line-compiler/langversion) |
| define                 | `,`-separated list of name `=` value pairs | empty    | [define](https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/command-line-compiler/define) |
| checked                | `(True\|False)`                            | `False`  | Opposite of [removeintchecks](https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/command-line-compiler/removeintchecks) |
| option-strict          | `(Off\|Custom\|On)`                        | required | [option strict](https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/command-line-compiler/optionstrict) |
| option-infer           | `(True\|False)`                            | required | [option infer](https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/command-line-compiler/optioninfer) |
| option-compare-text    | `(True\|False)`                            | required | [option compare](https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/command-line-compiler/optioncompare) |
| option-explicit        | `(True\|False)`                            | required | [option explicit](https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/command-line-compiler/optionexplicit) |
| embed-runtime          | `(True\|False)`                            | required | Whether or not the VB runtime was embedded into the PE |
| global-namespaces      | `,` -separated identifier list             | empty    | [imports](https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/command-line-compiler/imports)
| root-namespace         | string                                     | empty    | [root namespace](https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/command-line-compiler/rootnamespace)

#### Portability Policy

Portability policy is derived from the [appconfig command option](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/appconfig-compiler-option). 

Since appconfig is a pointer to a file not embedded in the PDB or PE, information that will directly impact the compilation is extracted. This is stored in a flag called `portability-policy` with a numeric value from \[0-3\]. This value directly correlates to reconstructing the [AssemblyPortabilityPolicy](https://github.com/dotnet/roslyn/blob/bdb3ece74c85892709f5e42ae7d67248999ecc3b/src/Compilers/Core/Portable/Desktop/AssemblyPortabilityPolicy.cs).

* 0 -> SuppressSilverlightPlatformAssembliesPortability = false, SuppressSilverlightLibraryAssembliesPortability = false
* 1 -> SuppressSilverlightPlatformAssembliesPortability = true, SuppressSilverlightLibraryAssembliesPortability = false
* 2 -> SuppressSilverlightPlatformAssembliesPortability = false, SuppressSilverlightLibraryAssembliesPortability = true
* 3 -> SuppressSilverlightPlatformAssembliesPortability = true, SuppressSilverlightLibraryAssembliesPortability = true

#### File Encoding 

Encoding will be stored in two keys: "default-encoding" and "fallback-encoding". 

If `default-encoding` is present, it represents the forced encoding used, such as by passing in "codepage" to the command line. All files without a Byte Order Mark (BOM) should be decoded using this encoding.

If `fallback-encoding` is present, it means that no default encoding was specified but an encoding was detected and used. The compiler [has logic](https://github.com/dotnet/roslyn/blob/462eac607741023e5c2d518ac1045f4c6dabd501/src/Compilers/Core/Portable/EncodedStringText.cs#L32) to determine an encoding is none is specified, so the value that was computed is stored so it can be reused in future compilations.

Both values are written as [WebName](https://docs.microsoft.com/en-us/dotnet/api/system.text.encoding.webname?view=netcore-3.1) values for an encoding.

#### Optimization

Optimization level can be specified from the command line with [optimize](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/optimize-compiler-option). It gets translated to the [OptimizationLevel](https://github.com/dotnet/roslyn/blob/e704ca635bd6de70a0250e34c4567c7a28fa9f6d/src/Compilers/Core/Portable/Compilation/OptimizationLevel.cs) that is emitted to the PDB. 

There are three possible values:

* `debug` -> `OptimizationLevel.Debug` and DebugPlusMode is false
* `debug-plus` -> `OptimizationLevel.Debug` and DebugPlusMode is true
* `release` -> `OptimizationLevel.Release` and DebugPlusMode is false (ignored)

#### Runtime Version

The runtime version used that the compiler was running in when generating the PE. This is stored as [informational version](https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assemblyinformationalversionattribute.informationalversion?view=netcore-3.1#System_Reflection_AssemblyInformationalVersionAttribute_InformationalVersion).

Runtime version is stored since it can impact the unicode character interpretation and decimal arithmetics, which both play a role in how code is compiled from source. There may also be future variations where the different versions of the runtime impact compilation. 

### Retriving Compiler Flags

Compiler flags are stored in the CustomDebugInformation in a PDB using the GUID `B5FEEC05-8CD0-4A83-96DA-466284BB4BD8` . 

Example how to retrieve and read this information using `System.Reflection.Metadata` : 

``` csharp
var compilationOptionsGuid = new Guid("B5FEEC05-8CD0-4A83-96DA-466284BB4BD8");
var path = "path/to/pdb";

using var stream = File.OpenRead(path);
using var metadataReaderProvider = MetadataReaderProvider.FromPortablePdbStream(stream);
var metadataReader = metadataReaderProvider.GetMetadataReader();

foreach (var handle in metadataReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition))
{
    var customDebugInformation = metadataReader.GetCustomDebugInformation(handle);
    if (metadataReader.GetGuid(customDebugInformation.Kind) == compilationOptionsGuid)
    {
        var blobReader = metadataReader.GetBlobReader(customDebugInformation.Value);
        
        // Compiler flag bytes are UTF-8 null-terminated key-value pairs
        var nullIndex = blobReader.IndexOf(0);
        while (nullIndex >= 0)
        {
            var key = blobReader.ReadUTF8(nullIndex);

            // Skip the null terminator
            blobReader.ReadByte();
            
            nullIndex = blobReader.IndexOf(0);
            var value = blobReader.ReadUTF8(nullIndex);

            // Skip the null terminator
            blobReader.ReadByte();

            nullIndex = blobReader.IndexOf(0);

            // key and value now have strings containing serialized compiler flag information
            Console.WriteLine($"{key} = {value}");
        }
    }
}
```
