compilation-from-portable-pdb
----------------------

Compilation from portable PDBs today is not completely possible, but is desirable in order to help reconstruct a compilation from source provided via source link or embedded in a pdb. Motivation is derived from [roslyn 41395](https://github.com/dotnet/roslyn/issues/41395). 

This document is restricted to the following assumptions: 

1. Only builds using the `-deterministic` and published to the symbol server.
2. Source generator and analyzer references are not needed for this task. They may be useful, but are out of scope for this document. 
3. Any storage capacity used for PDBs and source should not impact this feature, such as compression algorithm. 
4. Only Portable PDB files will be included for this spec. We can expand this feature past these once it is implemented and proven needed elsewhere. 

This document will provide the expanded specification to the Portable PDB format. Any additions to that format will be ported to expand documentation provided in [dotnet-runtime](https://github.com/jnm2/dotnet-runtime/blob/26efe3467741fe2a85780b2d2cd18875af6ebd98/docs/design/specs/PortablePdb-Metadata.md#source-link-c-and-vb-compilers). 

## List of Expanded Information 

#### Compiler Version

This will be a full compiler version, and if possible the commit SHA for the build of the compiler. This will be used since compilations accros compiler versions are not guaranteed to be the same. 

#### Signing Key

Only public keys will be included. The following command line switches trigger a signing key: 

* `-keyfile`
* `-keycontainer`

Open question: Do we need to know if `-delaysign` or `-publicsign` was specified? 

#### Source Encoding

Encoding for source files that are not declared using BOM. Do we need this?

#### Path Map

Required to replicate any path 

#### Resources

How do we handle `-linkresource`?

`-resource` embeds resources into the output assembly. The order of the resources in the output file is determined from the order specified on the command line. Resource files have a `filename`, `identifier`, and `accessibility-modifier` for each resource. `filename` is the only required property, `identifier` defaults to `filename` if not specified, and `accessibility-modifier` defaults to public. 

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

#### Platform



## PDB Format Additions

#### Metadata References

Metadata references can be easily stored 

#### Key Value Pairs

The remaining values will be stored as key value pairs in the pdb. The storage format will be UTF8 encoded JSON. These are the known keys that will be used: 

1. "compilerversion" : string
2. "signingkey" : string
3. "sourceencoding" : string
4. "pathmap": string
5. "platform": string
6. "resource": array<(string filename, string? identifier, bool? isPrivate)>
