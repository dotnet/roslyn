#Portable PDB v0.1: Format Specification Draft

## Portable PDB
The Portable PDB (Program Database) format describes an encoding of debugging information produced by compilers of Common Language Infrastructure (CLI) languages and consumed by debuggers and other tools. The format is based on the ECMA-335 Partition II metadata standard. It extends its schema while using the same physical table and stream layouts and encodings. The schema of the debugging metadata is complementary to the ECMA-335 metadata schema, therefore, the debugging metadata can (but doesn’t need to) be stored in the same metadata section of the PE/COFF file as the type system metadata.

## Debugging Metadata Format
### Overview
The format is based on the ECMA-335 Partition II metadata standard. The physical layout of the data is described in the ECMA-335-II Chapter 24 and the Portable PDB debugging metadata format introduces no changes to the file headers, metadata streams, etc.

The ECMA-335-II standard is amended by an addition of the following tables to the “#~” metadata stream:

* [Document](#DocumentTable)
* [MethodBody](#MethodBodyTable)
* [LocalScope](#LocalScopeTable)
* [LocalVariable](#LocalVariableTable)
* [LocalConstant](#LocalConstantTable)
* [ImportScope](#ImportScopeTable)
* [AsyncMethod](#AsyncMethodTable)
* [CustomDebugInformation](#CustomDebugInformationTable)

These tables refer to data in #Guid, #String and #Blob heaps whose structure hasn’t changed.

Debugging metadata may be generated to the same metadata stream that stores type system metadata, or as a standalone stream. In the latter case the standalone stream shall contain a copy of the Module table from the type system metadata. The Module table effectively links the debugging metadata to the corresponding type system metadata.

Debugging metadata generated as a standalone stream refers to the type system metadata entities. All references to tables (tokens, row ids) are references to tables of the metadata stream. References to heaps (strings, blobs, guids) are references to heaps of the debugging metadata stream.

### <a name="DocumentTable"></a>Document Table: 0x30

The Document table has the following columns:
* _Name_ (Blob heap index of [document name blob](#DocumentNameBlob))
* _HashAlgorithm_ (Guid heap index)
* _Hash_ (Blob heap index)
* _Language_ (Guid heap index)

The table is not required to be sorted.

There shall be no duplicate rows in the _Document_ table, based upon _Name_.

The values for which field _Language_ has a defined meaning are listed in the following tables along with the corresponding interpretation:


| _Language_ field value               | language     |
|:-------------------------------------|:-------------|
| 3f5162f8-07c6-11d3-9053-00c04fa302a1 | Visual C#    |
| 3a12d0b8-c26c-11d0-b442-00a0244a1dd2 | Visual Basic |
| ab4f38c9-b6e6-43ba-be3b-58080b2ccce3 | Visual F#    |

The values for which _HashAlgorithm_ has defined meaning are listed in the following table along with the corresponding semantics of the _Hash_ value.

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
* _part_ is a compressed integer into the #Blob heap, where the part is stored in UTF8 encoding (0 represents and empty string).

The document name is a concatenation of the _parts_ separated by the _separator_.
- - -
**Note** Document names are usually normalized full paths, e.g. "C:\Source\file.cs"  "/home/user/source/file.cs".
The representation is optimized for an efficient deserialization of the name into a UTF8 encoded string while minimizing the overall storage space for document names.
- - -

### <a name="MethodBodyTable"></a>MethodBody Table: 0x31

MethodBody table is either empty (missing) or has exactly as many rows as MethodDef table and the following column:

* _SequencePoints_ (Blob heap index, 0 if the method doesn’t have a body, encoding: [sequence points blob](#SequencePointsBlob))

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

    Blob ::= first-point-record SubsequentRecord*
    SubsequentRecord ::= subsequent-point-record |
	                     subsequent-hidden-point-record |
                         subsequent-document-record

#####first-point-record
| component      | value stored                  | integer representation |
|:---------------|:------------------------------|:-----------------------|
| _Document_     | Document table row id         | unsigned compressed    |
| _ILOffset_	 | _ILOffset_                    | unsigned compressed    |
| _ΔLines_       | _EndLine_ - _StartLine_       | unsigned compressed    |
| _ΔColumns_     | _EndColumn_ - _StartColumn_   | _ΔLines_ = 0: unsigned compressed, non-zero |
|                |                               | _ΔLines_ ≠ 0: signed compressed             |
| _StartLine_    | _StartLine_                   | unsigned compressed    |
| _StartColumn_  | _StartColumn_                 | unsigned compressed    |

If _ΔLines_ and _ΔColumns_ are both 0 the record represents a hidden sequence point.

#####subsequent-point-record
| component    | value stored                       | integer representation         |
|:-------------|:-----------------------------------|:-------------------------------|
| _δILOffset_  | _ILOffset_ - _Previous_._ILOffset_	| unsigned compressed, non-zero  |
| _ΔLines_     | _EndLine_ - _StartLine_            | unsigned compressed            |
| _ΔColumns_   | _EndColumn_ - _StartColumn_        | _ΔLines_ = 0: unsigned compressed, non-zero |
|              |                                    | _ΔLines_ ≠ 0: signed compressed             |
| _δStartLine_   | _StartLine_ - _Previous_._StartLine_	    | signed compressed       |
| _δStartColumn_ | _StartColumn_ - _Previous_._StartColumn_	| signed compressed       |

The sequence point represented by this record inherits the value of _Document_ property from the previous record. The values of _IL Offset_, _Start Line_, _Start Column_, _End Line_ and _End Column_ are calculated based on the values of the previous sequence point and the data stored in the record.

#####subsequent-hidden-point-record
| component    | value stored                       | integer representation         |
|:-------------|:-----------------------------------|:-------------------------------|
| _δILOffset_  | _ILOffset_ - _Previous_._ILOffset_ | unsigned compressed, non-zero  |
| _ΔLine_      | 0                                  | unsigned compressed            |
| _ΔColumn_	   | 0                                  | unsigned compressed            |

The hidden sequence point represented by the record inherits the value of _Document_ property from the previous record. The value of _IL Offset_ is calculated based on the value of the previous sequence point and the data stored in the record.

#####subsequent-document-record
| component    | value stored                       | integer representation         |
|:-------------|:-----------------------------------|:-------------------------------|
| _δILOffset_  | 0                                  | unsigned compressed            |
| _Document_   | Document row id                    | unsigned compressed            |

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

* _StartOffset_ (integer [0..231), encoding: uint32)

	Starting IL offset of the scope.

* _Length_ (integer [0..231) , encoding: uint32)

    The scope length in bytes. The scope spans all bytes in range [_StartOffset_, _StartOffset_ + _Length_).

The table is required to be sorted first by _Method_ and then by _StartOffset_.

TODO: Nesting requirements.

### <a name="LocalVariableTable"></a>LocalVariable Table: 0x33

The LocalVariable table has the following columns:

* _Attributes_ ([_LocalVariableAttributes_](#LocalVariableAttributes) value, encoding: uint16)
* _Index_ (integer [0..216), encoding: uint16)

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
* _Value_ (Blob heap index)
* _TypeCode_ (see ECMA-335-II §23.1.16; encoding: uint8)

Conceptually, every row in the LocalConstant table is owned by one, and only one, row in the LocalScope table.

There shall be no duplicate rows in the LocalConstant table, based upon owner and _Name_.

###<a name="ImportScopeTable"></a>ImportScope Table: 0x35
The ImportScope table has the following columns:

* Parent (ImportScope row id or nil)
* Imports (Blob index, encoding: [Imports blob](#ImportsBlob))

ImportScope table is required to be sorted by _Parent_ column.

There shall be no duplicate rows in the ImportScope table, based upon _Parent_.

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
| _target-type_       | Compressed unsigned integer. | TypeDef, TypeRef or TypeSpec encoded as TypeDefOrRefEncoded (see section 23.2.8 of the ECMA-335 Metadata specification). |

| _kind_ | description |
|:-------|:------------|
| 1      | Imports members of target-namespace. |
| 2      | Imports members of target-namespace defined in assembly target-assembly.|
| 3      | Imports members of target-type.|
| 4      | Imports members of XML namespace target-namespace with prefix alias.|
| 5      | Imports assembly reference alias defined in an ancestor scope.|
| 6      | Defines an alias for assembly target-assembly.|
| 7      | Defines an alias for the target-namespace.|
| 8      | Defines an alias for the part of target-namespace defined in assembly target-assembly.|
| 9      | Defines an alias for the target-type.|

The exact import semantics are language specific.

The blob may be empty. An empty import scope may still be target of custom debug information record.

### <a name="AsyncMethodTable"></a>AsyncMethod Table: 0x36

The AsyncMethod table has the following columns:

* _KickoffMethod_ (MethodDef row id)

* _CatchHandlerOffset_ (integer [0..231 + 1), encoding: uint32)

	0 if the handler is not present, otherwise IL offset + 1.

* _Awaits_ (Blob heap index, encoding: [awaits blob](#AwaitsBlob))

The table is required to be sorted by _KickoffMethod_ column.

There shall be no duplicate rows in the AsyncMethod table, based upon _KickoffMethod_.

#### <a name="AwaitsBlob"></a>Awaits Blob
Structure:

	Blob ::= Await+
	Await ::= yield-offset resume-offset resume-method

Each entry corresponds to an await expression in the async method.

| terminal      | encoding                    | description|
|:--------------|:----------------------------|:-----------|
| yield-offset  | compressed unsigned integer | TODO|
| resume-offset	| compressed unsigned integer | TODO|
| resume-method	| compressed unsigned integer | TODO (MethodDef row id)|

### <a name="CustomDebugInformationTable"></a>CustomDebugInformation Table: 0x37
The CustomDebugInformation table has the following columns:

* _Parent_ ([HasCustomDebugInformation](#HasCustomDebugInformation) coded index)
* _Kind_ (Guid heap index)
* _Value_ (Blob heap index)

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

##### State Machine Hoisted Local Scopes (C# & VB compilers)
Parent: MethodDef

Kind: {6DA9A61E-F8C7-4874-BE62-68BC5630DF71}

Scopes of local variables hoisted to state machine fields.

Structure:

    Blob ::= Scope {hoisted-variable-count}
    Scope::= start-offset end-offset

| terminal | encoding | description|
|:---------|:---------|:-----------|
| _start-offset_ | Compressed unsigned integer | Start IL offset of the scope (inclusive)|
| _end-offset_   | Compressed unsigned integer | End IL offset of the scope (exlusive)|

Each scope spans IL instructions in range [_start-offset_, _end-offset_).

_start-offset_ shall point to the starting byte of an instruction of the MoveNext method of the state machine type.

_end-offset_ shall point to the starting byte of an instruction or be equal to the size of the IL block of the MoveNext method of the state machine type.

##### Dynamic Local Variables (C# compiler)
Parent: MethodDef

Kind: {83C563C4-B4F3-47D5-B824-BA5441477EA8}

Structure:

    Blob ::= (slot-index | 0 constant-name) bit-count bit{bit-count} padding

| terminal | encoding | description|
|:---------|:---------|:-----------|
| _slot-index_    | Compressed unsigned integer  | 1-based local signature slot index|
| _constant-name_ | NUL-terminated UTF8 string   | Constant name|
| _bit-count_     | Compressed unsigned integer  | Number of bits|
| _bit_	          | 1 bit                        | 0 or 1|
| _padding_       | n zero bits                  | Padding bits to align to byte boundary.|

TODO: Bit ordering.

##### Edit and Continue Local Slot Map (C# & VB compilers)
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

##### Edit and Continue Lambda and Closure Map (C# & VB compilers)
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



