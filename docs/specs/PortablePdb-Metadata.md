#Portable PDB v1.0: Format Specification

## Portable PDB
The Portable PDB (Program Database) format describes an encoding of debugging information produced by compilers of Common Language Infrastructure (CLI) languages and consumed by debuggers and other tools. The format is based on the ECMA-335 Partition II metadata standard. It extends its schema while using the same physical table and stream layouts and encodings. The schema of the debugging metadata is complementary to the ECMA-335 metadata schema, therefore, the debugging metadata can (but doesn’t need to) be stored in the same metadata section of the PE/COFF file as the type system metadata.

## Debugging Metadata Format
### Overview
The format is based on the ECMA-335 Partition II metadata standard. The physical layout of the data is described in the ECMA-335-II Chapter 24 and the Portable PDB debugging metadata format introduces no changes to the fundamental structure.

The ECMA-335-II standard is amended by an addition of the following tables to the “#~” metadata stream:

* [Document](#DocumentTable)
* [MethodDebugInformation](#MethodDebugInformationTable)
* [LocalScope](#LocalScopeTable)
* [LocalVariable](#LocalVariableTable)
* [LocalConstant](#LocalConstantTable)
* [ImportScope](#ImportScopeTable)
* [StateMachineMethod](#StateMachineMethodTable)
* [CustomDebugInformation](#CustomDebugInformationTable)
    * [StateMachineHoistedLocalScopes](#StateMachineHoistedLocalScopes)
    * [DynamicLocalVariables](#DynamicLocalVariables)
    * [DefaultNamespace](#DefaultNamespace)
    * [EditAndContinueLocalSlotMap](#EditAndContinueLocalSlotMap)
    * [EditAndContinueLambdaAndClosureMap](#EditAndContinueLambdaAndClosureMap)

Debugging metadata tables may be embedded into type system metadata (and part of a PE file), or they may be stored separately in a metadata blob contained in a .pdb file. In the latter case additional information is included that connects the debugging metadata to the type system metadata.

### Standalone debugging metadata

When debugging metadata is generated to a separate data blob "#Pdb" and "#~" streams shall be present. The standalone debugging metadata may also include #Guid, #String and #Blob heaps, which have the same physical layout but are distict from the corresponding streams of the type system metadata.

#### <a name="PdbStream"></a>#Pdb stream

The #Pdb stream has the following structure:
 
| Offset | Size | Field          | Description                                                    |
|:-------|:-----|:---------------|----------------------------------------------------------------|
| 0      | 20   | PDB id         | A byte sequence uniquely representing the debugging metadata blob content. |
| 20     | 4    | EntryPoint     | Entry point MethodDef token, or 0 if not applicable. The same value as stored in CLI header of the PE file. See ECMA-335-II 15.4.1.2. |
| 24     | 8    | ReferencedTypeSystemTables | Bit vector of referenced type system metadata tables, let n be the number of bits that are 1. |
| 32     | 4*n  | TypeSystemTableRows     | Array of n 4-byte unsigned integers indicating the number of rows for each referenced type system metadata table. |

#### #~ stream 

"#~" stream shall only contain debugging information tables defined above and a copy of the Module table from the type system metadata but no other type system metadata table. The Module table effectively links the debugging metadata to the corresponding type system metadata.
 
References to heaps (strings, blobs, guids) are references to heaps of the debugging metadata. The sizes of references to type system tables are determined using the algorithm described in ECMA-335-II Chapter 24.2.6, except their respective row counts are found in _TypeSystemTableRows_ field of the #Pdb stream.

### <a name="DocumentTable"></a>Document Table: 0x30

The Document table has the following columns:
* _Name_ (Blob heap index of [document name blob](#DocumentNameBlob))
* _HashAlgorithm_ (Guid heap index)
* _Hash_ (Blob heap index)
* _Language_ (Guid heap index)

The table is not required to be sorted.

There shall be no duplicate rows in the _Document_ table, based upon document name. 

_Name_ shall not be nil. It can however encode an empty name string.

The values for which field _Language_ has a defined meaning are listed in the following tables along with the corresponding interpretation:

| _Language_ field value               | language     |
|:-------------------------------------|:-------------|
| 3f5162f8-07c6-11d3-9053-00c04fa302a1 | Visual C#    |
| 3a12d0b8-c26c-11d0-b442-00a0244a1dd2 | Visual Basic |
| ab4f38c9-b6e6-43ba-be3b-58080b2ccce3 | Visual F#    |

The values for which _HashAlgorithm_ has defined meaning are listed in the following table along with the corresponding semantics of the _Hash_ value.

| _HashAlgorithm_ field value          | hash field semantics |
|:-------------------------------------|:---------------------|
| ff1816ec-aa5e-4d10-87f7-6f4963833460 | SHA-1 hash           |
| 8829d00f-11b8-4213-878b-770e8597ac16 | SHA-256 hash         |

Otherwise, the meaning of _Language_, _HashAlgorithm_ and _Hash_ values is undefined and the reader can interpret them arbitrarily.

#### <a name="DocumentNameBlob"></a>Document Name Blob

Document name blob is a sequence:

    Blob ::= separator part+

where

* _separator_ is a UTF8 encoded character, or byte 0 to represent an empty separator.
* _part_ is a compressed integer into the #Blob heap, where the part is stored in UTF8 encoding (0 represents an empty string).

The document name is a concatenation of the _parts_ separated by the _separator_.
- - -
**Note** Document names are usually normalized full paths, e.g. "C:\Source\file.cs"  "/home/user/source/file.cs".
The representation is optimized for an efficient deserialization of the name into a UTF8 encoded string while minimizing the overall storage space for document names.
- - -

### <a name="MethodDebugInformationTable"></a>MethodDebugInformation Table: 0x31

MethodDebugInformation table is either empty (missing) or has exactly as many rows as MethodDef table and the following column:

* _Document_       (The row id of the single document containing all sequence points of the method, or 0 if the method doesn't have sequence points or spans multiple documents)
* _SequencePoints_ (Blob heap index, 0 if the method doesn’t have sequence points, encoding: [sequence points blob](#SequencePointsBlob))

The table is a logical extension of MethodDef table (adding a column to the table) and as such can be indexed by MethodDef row id.

#### <a name="SequencePointsBlob"></a>Sequence Points Blob
Sequence point is a quintuple of integers and a document reference:

* IL Offset
* Start Line
* Start Column
* End Line
* End Column
* Document

_Hidden sequence point_ is a sequence point whose Start Line = End Line = 0xfeefee and Start Column = End Column = 0.

The values of non-hidden sequence point must satisfy the following constraints

* IL Offset is within range [0, 0x20000000)
* IL Offset of a sequence point is lesser than IL Offset of the subsequent sequence point.
* Start Line is within range [0, 0x20000000) and not equal to 0xfeefee.
* End Line is within range [0, 0x20000000) and not equal to 0xfeefee.
* Start Column is within range [0, 0x10000)
* End Column is within range [0, 0x10000)
* End Line is greater or equal to Start Line.
* If Start Line is equal to End Line then End Column is greater than Start Column.

_Sequence points blob_ has the following structure:

    Blob ::= header SequencePointRecord (SequencePointRecord | document-record)*
    SequencePointRecord ::= sequence-point-record | hidden-sequence-point-record

#####header
| component        | value stored                  | integer representation |
|:-----------------|:------------------------------|:-----------------------|
| _LocalSignature_ | StandAloneSig table row id    | unsigned compressed    |
| _InitialDocument_ (opt)| Document row id         | unsigned compressed            |

_LocalSignature_ stores the row id of the local signature of the method. This information is somewhat redundant since it can be retrieved from the IL stream. However in some scenarios the IL stream is not available or loading it would unnecessary page in memory that might not otherwise be needed.

_InitialDocument_ is only present if the _Document_ field of the _MethodDebugInformation_ table is nil (i.e. the method body spans multiple documents).

#####sequence-point-record
| component      | value stored                                         | integer representation                      |
|:---------------|:-----------------------------------------------------|:--------------------------------------------|
| _δILOffset_    | _ILOffset_ if this is the first sequence point       | unsigned compressed                         |
|                | _ILOffset_ - _Previous_._ILOffset_ otherwise         | unsigned compressed, non-zero               |
| _ΔLines_       | _EndLine_ - _StartLine_                              | unsigned compressed                         |
| _ΔColumns_     | _EndColumn_ - _StartColumn_                          | _ΔLines_ = 0: unsigned compressed, non-zero |
|                |                                                      | _ΔLines_ > 0: signed compressed             |
| _δStartLine_   | _StartLine_ if this is the first non-hidden sequence point   | unsigned compressed |
|                | _StartLine_ - _PreviousNonHidden_._StartLine_ otherwise      | signed compressed |
| _δStartColumn_ | _StartColumn_ if this is the first non-hidden sequence point | unsigned compressed |
|                | _StartColumn_ - _PreviousNonHidden_._StartColumn_ otherwise  | signed compressed   |

#####hidden-sequence-point-record
| component    | value stored                                           | integer representation          |
|:-------------|:-------------------------------------------------------|:--------------------------------|
| _δILOffset_  | _ILOffset_ if this is the first sequence point         | unsigned compressed             |
|              | _ILOffset_ - _Previous_._ILOffset_ otherwise           | unsigned compressed, non-zero   |
| _ΔLine_      | 0                                                      | unsigned compressed             |
| _ΔColumn_	   | 0                                                      | unsigned compressed             |

#####document-record
| component    | value stored                       | integer representation         |
|:-------------|:-----------------------------------|:-------------------------------|
| _δILOffset_  | 0                                  | unsigned compressed            |
| _Document_   | Document row id                    | unsigned compressed            |

Each _SequencePointRecord_ represents a single sequence point. The sequence point inherits the value of _Document_ property from the previous record (_SequencePointRecord_ or _document-record_), from the _Document_ field of the _MethodDebugInformation_ table if it's the first sequence point of a method body that spans a single document, or from _InitialDocument_ if it's the first sequence point of a method body that spans multiple documents. The value of _IL Offset_ is calculated using the value of the previous sequence point (if any) and the value stored in the record. 

The values of _Start Line_, _Start Column_, _End Line_ and _End Column_ of a non-hidden sequence point are calculated based upon the values of the previous non-hidden sequence point (if any) and the data stored in the record.

### <a name="LocalScopeTable"></a>LocalScope Table: 0x32

The LocalScope table has the following columns:

* _Method_ (MethodDef row id)

* _ImportScope_ (ImportScope row id)

* _VariableList_ (LocalVariable row id)

	An index into the LocalVariable table; it marks the first of a contiguous run of _LocalVariables_ owned by this LocalScope. The run continues to the smaller of:
	* the last row of the _LocalVariable_ table
	* the next run of _LocalVariables_, found by inspecting the _VariableList_ of the next row in this LocalScope table.

* _ConstantList_ (LocalConstant row id)

	An index into the LocalConstant table; it marks the first of a contiguous run of _LocalConstants_ owned by this LocalScope. The run continues to the smaller of:
    * the last row of the _LocalConstant_ table
	* the next run of _LocalConstants_, found by inspecting the _ConstantList_ of the next row in this LocalScope table.

* _StartOffset_ (integer [0..0x80000000), encoding: uint32)

	Starting IL offset of the scope.

* _Length_ (integer (0..0x80000000), encoding: uint32)

    The scope length in bytes.

The table is required to be sorted first by _Method_ in ascending order, then by _StartOffset_ in ascending order, then by _Length_ in descending order.

_StartOffset_ + _Length_ shall be in range (0..0x80000000).

Each scope spans IL instructions in range [_StartOffset_, _StartOffset_ + _Length_).

_StartOffset_ shall point to the starting byte of an instruction of the _Method_.

_StartOffset_ + _Length_ shall point to the starting byte of an instruction of the _Method_ or be equal to the size of the IL stream of the _Method_.

For each pair of scopes belonging to the same _Method_ the intersection of their respective ranges _R1_ and _R2_ shall be either _R1_ or _R2_ or empty.

### <a name="LocalVariableTable"></a>LocalVariable Table: 0x33

The LocalVariable table has the following columns:

* _Attributes_ ([_LocalVariableAttributes_](#LocalVariableAttributes) value, encoding: uint16)
* _Index_ (integer [0..0x10000), encoding: uint16)

	Slot index in the local signature of the containing MethodDef.
* _Name_ (String heap index)

Conceptually, every row in the LocalVariable table is owned by one, and only one, row in the LocalScope table.

There shall be no duplicate rows in the LocalVariable table, based upon owner and _Index_.

There shall be no duplicate rows in the LocalVariable table, based upon owner and _Name_.

#####<a name="LocalVariableAttributes"></a>LocalVariableAttributes
| flag  | value | description |
|:------|:------|:------------|
| DebuggerHidden | 0x0001 | Variable shouldn’t appear in the list of variables displayed by the debugger |

###<a name="LocalConstantTable"></a>LocalConstant Table: 0x34

The LocalConstant table has the following columns:

* _Name_ (String heap index)
* _Signature_ (Blob heap index, [LocalConstantSig blob](#LocalConstantSig))

Conceptually, every row in the LocalConstant table is owned by one, and only one, row in the LocalScope table.

There shall be no duplicate rows in the LocalConstant table, based upon owner and _Name_.

####<a name="LocalConstantSig"></a>LocalConstantSig Blob

The structure of the blob is

    Blob ::= CustomMod* (PrimitiveConstant | EnumConstant | GeneralConstant)
             
    PrimitiveConstant ::= PrimitiveTypeCode PrimitiveValue 
    PrimitiveTypeCode ::= BOOLEAN | CHAR | I1 | U1 | I2 | U2 | I4 | U4 | I8 | U8 | R4 | R8 | STRING
    
    EnumConstant ::= EnumTypeCode EnumValue EnumType 
    EnumTypeCode ::= BOOLEAN | CHAR | I1 | U1 | I2 | U2 | I4 | U4 | I8 | U8
    EnumType ::= TypeDefOrRefOrSpecEncoded
    
    GeneralConstant ::= (CLASS | VALUETYPE) TypeDefOrRefOrSpecEncoded GeneralValue? |
                        OBJECT

| component                   | description                                                |
|:----------------------------|:-----------------------------------------------------------|
| _PrimitiveTypeCode_         | A 1-byte constant describing the structure of the _PrimitiveValue_. |
| _PrimitiveValue_            | The value of the constant.                                 |
| _EnumTypeCode_              | A 1-byte constant describing the structure of the _EnumValue_. |
| _EnumValue_                 | The underlying value of the enum.                          |
| _CustomMod_                 | Custom modifier as specified in ECMA-335 §II.23.2.7        |
| _TypeDefOrRefOrSpecEncoded_ | TypeDef, TypeRef or TypeSpec encoded as specified in ECMA-335 §II.23.2.8 |

The encoding of the _PrimitiveValue_ and _EnumValue_ is determined based upon the value of _PrimitiveTypeCode_ and _EnumTypeCode_, respectively.

| Type code     | Value                      |
|:--------------|:---------------------------|
| ```BOOLEAN``` | uint8: 0 represents false, 1 represents true |
| ```CHAR```    | uint16                     |
| ```I1```      | int8                       |
| ```U1```      | uint8                      |
| ```I2```      | int16                      |
| ```U2```      | uint16                     |
| ```I4```      | int32                      |
| ```U4```      | uint32                     |
| ```I8```      | int64                      |
| ```U8```      | uint64                     |
| ```R4```      | float32                    |
| ```R8```      | float64                    |
| ```STRING```  | A single byte 0xff (represents a null string reference), or a UTF-16 little-endian encoded string (possibly empty). | 

The numeric values of the type codes are defined by ECMA-335 §II.23.1.16.

_EnumType_ must be an enum type as defined in ECMA-335 §II.14.3. The value of _EnumTypeCode_ must match the underlying type of the _EnumType_.

The encoding of the _GeneralValue_ is determined based upon the type expressed by _TypeDefOrRefOrSpecEncoded_ specified in _GeneralConstant_. If the _GeneralValue_ is not present the value of the constant is the default value of the type. If the type is a reference type the value is a null reference, if the type is a pointer type the value is a null pointer, etc. 

| Namespace     | Name     | _GeneralValue_ encoding  |
|:--------------|:---------|:-------------------------|
| System        | Decimal  | sign (highest bit), scale (bits 0..7), low (uint32), mid (uint32), high (uint32) |
| System        | DateTime | int64: ticks             | 

###<a name="ImportScopeTable"></a>ImportScope Table: 0x35
The ImportScope table has the following columns:

* Parent (ImportScope row id or nil)
* Imports (Blob index, encoding: [Imports blob](#ImportsBlob))

####<a name="ImportsBlob"></a>Imports Blob
Imports blob represents all imports declared by an import scope.

Imports blob has the following structure:

	Blob ::= Import*
	Import ::= kind alias? target-assembly? target-namespace? target-type?

| terminal            | value                        | description                          |
|:--------------------|:-----------------------------|:-------------------------------------|
| _kind_              | Compressed unsigned integer  | Import kind.                         |
| _alias_             | Compressed unsigned Blob heap index of a UTF8 string. | A name that can be used to refer to the target within the import scope. |
| _target-assembly_   | Compressed unsigned integer. | Row id of the AssemblyRef table. |
| _target-namespace_  | Compressed unsigned Blob heap index of a UTF8 string. | Fully qualified namespace name or XML namespace name. |
| _target-type_       | Compressed unsigned integer. | TypeDef, TypeRef or TypeSpec encoded as TypeDefOrRefOrSpecEncoded (see section II.23.2.8 of the ECMA-335 Metadata specification). |

| _kind_ | description |
|:-------|:------------|
| 1      | Imports members of _target-namespace_. |
| 2      | Imports members of _target-namespace_ defined in assembly _target-assembly_.|
| 3      | Imports members of _target-type_.|
| 4      | Imports members of XML namespace _target-namespace_ with prefix _alias_.|
| 5      | Imports assembly reference _alias_ defined in an ancestor scope.|
| 6      | Defines an alias for assembly _target-assembly_.|
| 7      | Defines an alias for the _target-namespace_.|
| 8      | Defines an alias for the part of _target-namespace_ defined in assembly _target-assembly_.|
| 9      | Defines an alias for the _target-type_.|

The exact import semantics are language specific.

The blob may be empty. An empty import scope may still be target of custom debug information record.

### <a name="StateMachineMethodTable"></a>StateMachineMethod Table: 0x36

The StateMachineMethod table has the following columns:

* _MoveNextMethod_ (MethodDef row id)
* _KickoffMethod_ (MethodDef row id)

The table associates the kickoff implementation method of an async or an iterator method (the method that initializes and starts the state machine) with the MoveNext method that implements the state transition.

The table is required to be sorted by _MoveNextMethod_ column.

There shall be no duplicate rows in the StateMachineMethod table, based upon _MoveNextMethod_.

There shall be no duplicate rows in the StateMachineMethod table, based upon _KickoffMethod_.

### <a name="CustomDebugInformationTable"></a>CustomDebugInformation Table: 0x37
The CustomDebugInformation table has the following columns:

* _Parent_ ([HasCustomDebugInformation](#HasCustomDebugInformation) coded index)
* _Kind_ (Guid heap index)
* _Value_ (Blob heap index)

The table is required to be sorted by _Parent_.

Kind is an id defined by the tool producing the information.

| HasCustomDebugInformation | tag (5 bits)|
|:--------------------------|:------------|
| MethodDef|0|
| Field|1|
| TypeRef|2|
| TypeDef|3|
| Param|4|
| InterfaceImpl|5|
| MemberRef|6|
| Module|7|
| DeclSecurity|8|
| Property|9|
| Event|10|
| StandAloneSig|11|
| ModuleRef|12|
| TypeSpec|13|
| Assembly|14|
| AssemblyRef|15|
| File|16|
| ExportedType|17|
| ManifestResource|18|
| GenericParam|19|
| GenericParamConstraint|20||
| MethodSpec|21|
| Document|22|
| LocalScope|23|
| LocalVariable|24|
| LocalConstant|25|
| ImportScope|26|

#### Language Specific Custom Debug Information Records

The following _Custom Debug Information_ records are currently produced by C#, VB and F# compilers. In future the compilers and other tools may define new records. Once specified they may not change. If a change is needed the owner has to define a new record with a new kind (GUID).

##### <a name="StateMachineHoistedLocalScopes"></a>State Machine Hoisted Local Scopes (C# & VB compilers)
Parent: MethodDef

Kind: {6DA9A61E-F8C7-4874-BE62-68BC5630DF71}

Scopes of local variables hoisted to state machine fields.

Structure:

    Blob ::= Scope{hoisted-variable-count}
    Scope::= start-offset length

| terminal       | encoding | description|
|:---------------|:---------|:-----------|
| _start-offset_ | uint32   | Start IL offset of the scope, a value in range [0..0x80000000).|
| _length_       | uint32   | Length of the scope span, a value in range (0..0x80000000).    |

Each scope spans IL instructions in range [_start-offset_, _start-offset_ + _length_).

_start-offset_ shall point to the starting byte of an instruction of the MoveNext method of the state machine type.

_start-offset_ + _length_ shall point to the starting byte of an instruction or be equal to the size of the IL stream of the MoveNext method of the state machine type.

##### <a name="DynamicLocalVariables"></a>Dynamic Local Variables (C# compiler)
Parent: LocalVariables or LocalConstant

Kind: {83C563C4-B4F3-47D5-B824-BA5441477EA8}

Structure:

    Blob ::= bit-sequence

A sequence of bits for a local variable or constant whose type contains _dynamic_ type (e.g. dynamic, dynamic[], List<dynamic> etc.) that describes which System.Object types encoded in the metadata signature of the local type were specified as _dynamic_ in source code.

Bits of the sequence are grouped by 8. If the sequence length is not a multiple of 8 it is padded by 0 bit to the closest multiple of 8. Each group of 8 bits is encoded as a byte whose least significant bit is the first bit of the group and the highest significant bit is the 8th bit of the group. The sequence is encoded as a sequence of bytes representing these groups. Trailing zero bytes may be omitted.

TODO: Specify the meaning of the bits in the sequence.

##### <a name="DefaultNamespace"></a>Default Namespace (VB compiler)
Parent: Module

Kind: {58b2eab6-209f-4e4e-a22c-b2d0f910c782}

Structure:

    Blob ::= namespace

| terminal | encoding | description|
|:---------|:---------|:-----------|
| _namespace_ | UTF8 string | The default namespace for the module/project. |

##### <a name="EditAndContinueLocalSlotMap"></a>Edit and Continue Local Slot Map (C# and VB compilers)
Parent: MethodDef

Kind: {755F52A8-91C5-45BE-B4B8-209571E552BD}

If _Parent_ is a kickoff method of a state machine (marked in metadata by a custom attribute derived from System.Runtime.CompilerServices.StateMachineAttribute) associates variables hoisted to fields of the state machine type with their syntax offsets. Otherwise, associates slots of the Parent method local signature with their syntax offsets.

Syntax offset is an integer distance from the start of the method body (it may be negative). It is used by the compiler to map the slot to the syntax node that declares the corresponding variable.

The blob has the following structure:

    Blob ::= (has-syntax-offset-baseline syntax-offset-baseline)? SlotId{slot count}
    SlotId ::= has-ordinal kind syntax-offset ordinal?

| terminal | encoding | description |
|:---------|:---------|:------------|
| _has-syntax-offset-baseline_ | 8 bits or none              | 0xff or not present.|
| _syntax-offset-baseline_     | compressed unsigned integer | Negated syntax offset baseline. Only present if the minimal syntax offset stored in the slot map is less than -1. Defaults to -1 if not present.|
| _has-ordinal_                | 1 bit (highest)             | Set iff ordinal is present.|
| _kind_                       | 7 bits (lowest)             | Implementation specific slot kind in range [0, 0x7f).|
| _syntax-offset_              | compressed unsigned integer | The value of syntax-offset + syntax-offset-baseline is the distance of the syntax node that declares the corresponding variable from the start of the method body.|
| _ordinal_                    | compressed unsigned integer | Defines ordering of slots with the same syntax offset.|

The exact algorithm used to calculate syntax offsets and the algorithm that maps slots to syntax nodes is language and implementation specific and may change in future versions of the compiler.

##### <a name="EditAndContinueLambdaAndClosureMap"></a>Edit and Continue Lambda and Closure Map (C# and VB compilers)
Parent: MethodDef

Kind: {A643004C-0240-496F-A783-30D64F4979DE}

Encodes information used by the compiler when mapping lambdas and closures declared in the Parent method to their implementing methods and types and to the syntax nodes that declare them.

The blob has the following structure:

    Blob ::= method-ordinal syntax-offset-baseline closure-count Closure{closure-count} Lambda*
    Closure ::= syntax-offset
    Lambda ::= syntax-offset closure-ordinal

The number of lambda entries is determined by the size of the blob (the reader shall read lambda records until the end of the blob is reached).

| terminal | encoding | description|
|:---------|:---------|:-----------|
| _method-ordinal_ | compressed unsigned integer | Implementation specific number derived from the source location of Parent method.|
| _syntax-offset-baseline_ | compressed unsigned integer | Negated minimum of syntax offsets stored in the map and -1.|
| _closure-count_ | compressed unsigned integer | The number of closure entries.|
| _syntax-offset_ | compressed unsigned integer | The value of _syntax-offset_ + _syntax-offset-baseline_ is the distance of the syntax node that represents the lambda/closure in the source from the start of the method body.|
| _closure-ordinal_ | compressed unsigned integer | 0 if the lambda doesn’t have a closure. Otherwise, 1-based index into the closure list.|

The exact algorithm used to calculate syntax offsets and the algorithm that maps lambdas/closures to their implementing methods, types and syntax nodes is language and implementation specific and may change in future versions of the compiler.



