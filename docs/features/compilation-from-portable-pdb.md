## compilation-from-portable-pdb

Compilation from portable PDBs today is not completely possible, but is desirable in order to help reconstruct a compilation from source provided via source link or embedded in a pdb. Motivation is derived from [roslyn 41395](https://github.com/dotnet/roslyn/issues/41395). Once work is finalized on this, a compilation should be able to be made that is exactly the same as an initial compilation as long as it meets the conditions outlined in the assumptions below. [roslyn#44703](https://github.com/dotnet/roslyn/issues/44703) tracks adding an end-to-end validation of the scenario and should also update documentation here to further outline the steps. 

This document is restricted to the following assumptions:

1. The benefit is for builds with `-deterministic` and published to the symbol server.
2. Source generator and analyzer references are not needed for this task. They may be useful, but are out of scope for this document.
3. Any storage capacity used for PDBs and source should not impact this feature, such as compression algorithm.
4. Only Portable PDB files will be included for this spec. We can expand this feature past these once it is implemented and proven needed elsewhere.

This document will provide the expanded specification to the Portable PDB format. Any additions to that format will be ported to expand documentation provided in [dotnet-runtime](https://github.com/jnm2/dotnet-runtime/blob/26efe3467741fe2a85780b2d2cd18875af6ebd98/docs/design/specs/PortablePdb-Metadata.md#source-link-c-and-vb-compilers).

## List of Compiler Flags

#### CSharp Flags That Can Be Derived From PDB or Assembly

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
* target
* win32icon
* win32manifest
* win32res

#### CSharp Flags Not Included

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
* target
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
* optimize
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

#### Compiler Version

This will be a full compiler version, including commit SHA for the build of the compiler. This will be used since compilations across compiler versions are not guaranteed to be the same.

#### Source Encoding

Encoding for source files that are not declared using BOM. This 

#### Metadata References

Symbol server uses a [key](https://github.com/dotnet/symstore/blob/master/docs/specs/SSQP_Key_Conventions.md#pe-timestamp-filesize) computed from the COFF header in the PE image:

Timestamp: 4 byte integer
Size of image: 4 byte integer

Example:

File name: `example.exe` 

COFF header Timestamp field: `0x542d5742` 

COFF header SizeOfImage field: `0x32000` 

Lookup key: `example.exe/542d574232000/example.exe` 

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

#### Retriving Metadata References

Metadata references are stored in the CustomDebugInformation in a PDB using the GUID `7E4D4708-096E-4C5C-AEDA-CB10BA6A740D`. 

Example how to retrieve and read this information using `System.Reflection.Metadata`: 

```csharp
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

#### Compiler Flag Key Value Pairs

The remaining values will be stored as key value pairs in the pdb. The storage format will be UTF8 encoded key value pairs that are null terminated. Order is not guaranteed. Any values left out can be assumed to be the default for the type. Keys may be different for Visual Basic and CSharp. They are serialized to reflect the command line arguments representing the same values

Example: 

`compilerversion\01.2.3-example-sha\0sourceencoding\0utf-8\0checked\01\0unsafe\01\0langversion\0latest\0nullable\0Enable` 

##### Options For CSharp

See [compiler options](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/listed-alphabetically) documentation

* "[appconfig](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/appconfig-compiler-option)"
    - Since appconfig is a pointer to a file not embedded in the PDB or PE, we instead extract the information that will directly impact the compilation. We store this as a flag called `portability-policy` with a numeric value from \[0-3\]. This value directly correlates to reconstructing the [AssemblyPortabilityPolicy](https://github.com/dotnet/roslyn/blob/bdb3ece74c85892709f5e42ae7d67248999ecc3b/src/Compilers/Core/Portable/Desktop/AssemblyPortabilityPolicy.cs).
        - 0 -> SuppressSilverlightPlatformAssembliesPortability = false, SuppressSilverlightLibraryAssembliesPortability = false
        - 1 -> SuppressSilverlightPlatformAssembliesPortability = true, SuppressSilverlightLibraryAssembliesPortability = false
        - 2 -> SuppressSilverlightPlatformAssembliesPortability = false, SuppressSilverlightLibraryAssembliesPortability = true
        - 3 -> SuppressSilverlightPlatformAssembliesPortability = true, SuppressSilverlightLibraryAssembliesPortability = true
* "[checked](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/checked-compiler-option)"
* "[codepage](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/codepage-compiler-option)"
* "compilerversion"
* "[define](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/define-compiler-option)"
* "[langversion](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/langversion-compiler-option)"
* "[nullable](https://docs.microsoft.com/en-us/dotnet/csharp/nullable-references)"
* "[optimize](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/optimize-compiler-option)"
* "[unsafe](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/unsafe-compiler-option)"


##### Options For Visual Basic

See [compiler options](https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/command-line-compiler/compiler-options-listed-alphabetically) documentation

* "compilerversion"
* "[define](https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/command-line-compiler/define)"
* "[langversion](https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/command-line-compiler/langversion)"
* "[optimize](https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/command-line-compiler/optimize)"
* "[optionstrict](https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/command-line-compiler/optionstrict)"
* "[removeintchecks](https://docs.microsoft.com/en-us/dotnet/visual-basic/reference/command-line-compiler/removeintchecks)"


#### Retriving Compiler Flags

Compiler flags are stored in the CustomDebugInformation in a PDB using the GUID `B5FEEC05-8CD0-4A83-96DA-466284BB4BD8`. 

Example how to retrieve and read this information using `System.Reflection.Metadata`: 

```csharp
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

